using System.Text.Json;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ComentariosTests(BancoFixture banco) : TesteComBanco(banco)
{
    private static readonly DateOnly Entrega = new(2090, 6, 15);

    private Guid _diana, _pedro, _paula;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _diana = await IdDoAdminAsync();
        _pedro = await NovoUsuarioAsync("Pedro", "pedro@x.com");
        _paula = await NovoUsuarioAsync("Paula", "paula@x.com");
    }

    private async Task<Guid> NovoUsuarioAsync(string nome, string email)
    {
        var perfil = await IdDoPerfilAsync(TipoPerfil.Producao);
        var criado = await ExecutarAsync(_diana, new CriarUsuario(nome, email, "senha-do-" + nome[..3].ToLowerInvariant(), perfil, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    private async Task<Guid> DirecionadaAsync()
    {
        var criada = await ExecutarAsync(_diana, new SalvarAtividade(
            null, "Elaborar contestação", "Prazo de 15 dias.", Entrega, null, null, null, 1));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        await DirecionarAsync(criada.Valor, (_pedro, "Redigir"), (_paula, "Juntar documentos"));
        return criada.Valor;
    }

    private async Task DirecionarAsync(Guid id, params (Guid Usuario, string? Direcionamento)[] responsaveis)
    {
        var definido = await ExecutarAsync(_diana, new DefinirResponsaveisDaAtividade(
            id, [.. responsaveis.Select(r => new ResponsavelEntrada(r.Usuario, r.Direcionamento))], _diana));
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
    }

    private async Task<Guid> ComentarAsync(Guid quem, Guid atividade, string texto)
    {
        var resultado = await ExecutarAsync(quem, new ComentarAtividade(atividade, texto));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private Task<List<AtividadeComentario>> ComentariosNoBancoAsync(Guid atividade) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Set<AtividadeComentario>().IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.AtividadeId == atividade).OrderBy(c => c.DataHora).ThenBy(c => c.Id).ToListAsync());

    private async Task<uint> VersaoAsync(Guid id) => (await ExecutarAsync(_diana, new ObterAtividade(id))).Valor!.Versao;

    private static PostgresException PostgresDe(Exception erro)
    {
        for (var e = erro; e is not null; e = e.InnerException)
            if (e is PostgresException pg) return pg;
        throw new InvalidOperationException("Nenhuma PostgresException na cadeia de exceções: " + erro);
    }

    [Fact]
    public async Task Comentario_vai_e_volta_do_banco_com_autor_e_ordem()
    {
        var id = await DirecionadaAsync();
        var primeiro = await ComentarAsync(_diana, id, "Revisei o prazo.");
        var segundo = await ComentarAsync(_pedro, id, "  Começando agora.\nVolto à tarde.  ");

        foreach (var leitor in new[] { _diana, _pedro })
        {
            var lista = await ExecutarAsync(leitor, new ListarComentariosDaAtividade(id));
            lista.Sucesso.Should().BeTrue(lista.Mensagem);
            lista.Valor!.Select(c => (c.Id, c.UsuarioId, c.UsuarioNome, c.Texto)).Should().Equal(
                (primeiro, _diana, "Diana Diretora", "Revisei o prazo."),
                (segundo, _pedro, "Pedro", "Começando agora.\nVolto à tarde."));
            lista.Valor![0].DataHora.Should().BeOnOrBefore(lista.Valor[1].DataHora);
            lista.Valor[0].DataHora.Kind.Should().Be(DateTimeKind.Utc);
        }

        var linhas = await ComentariosNoBancoAsync(id);
        linhas.Should().HaveCount(2);
        linhas.Should().OnlyContain(c => c.ExcluidoEm == null && c.CriadoEm != default);
        linhas.Single(c => c.Id == segundo).CriadoPor.Should().Be(_pedro);
    }

    [Fact]
    public async Task Comentar_nao_muda_a_versao_da_atividade()
    {
        var id = await DirecionadaAsync();
        var antes = await VersaoAsync(id);

        await ComentarAsync(_diana, id, "Um comentário.");
        await ComentarAsync(_pedro, id, "Outro comentário.");

        (await VersaoAsync(id)).Should().Be(antes);
        var editada = await ExecutarAsync(_diana, new SalvarAtividade(
            id, "Contestação revisada", "Prazo de 15 dias.", Entrega, null, null, null, 1, antes));
        editada.Sucesso.Should().BeTrue(editada.Mensagem);
    }

    [Fact]
    public async Task Comentario_gera_registro_de_criacao_e_a_listagem_fica_no_registro_de_consultas()
    {
        var id = await DirecionadaAsync();
        var comentario = await ComentarAsync(_pedro, id, "Começando agora.");
        (await ExecutarAsync(_diana, new ListarComentariosDaAtividade(id))).Sucesso.Should().BeTrue();

        var criacao = (await AuditoriaAsync())
            .Where(r => r.Entidade == "AtividadeComentario" && r.Operacao == OperacaoAuditada.Criacao)
            .Should().ContainSingle().Subject;
        criacao.EntidadeId.Should().Be(comentario);
        criacao.UsuarioId.Should().Be(_pedro);
        var dados = JsonDocument.Parse(criacao.Dados!).RootElement;
        dados.TryGetProperty("Texto", out _).Should().BeTrue();
        dados.TryGetProperty("AtividadeId", out _).Should().BeTrue();

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso
            .AsNoTracking().Where(a => a.TipoRecurso == "AtividadeComentario").ToListAsync());
        var listou = acessos.Should().ContainSingle().Subject;
        listou.Acao.Should().Be(AcaoDeAcesso.Listou);
        listou.UsuarioId.Should().Be(_diana);
        listou.Detalhe.Should().Contain(id.ToString());
    }

    [Fact]
    public async Task Responsavel_removido_deixa_de_ler_e_de_comentar()
    {
        var id = await DirecionadaAsync();
        await ComentarAsync(_paula, id, "Antes de sair.");
        (await ExecutarAsync(_paula, new ListarComentariosDaAtividade(id))).Sucesso.Should().BeTrue();

        await DirecionarAsync(id, (_pedro, "Redigir"));

        var leu = await ExecutarAsync(_paula, new ListarComentariosDaAtividade(id));
        var comentou = await ExecutarAsync(_paula, new ComentarAtividade(id, "Ainda posso?"));
        leu.Erro.Should().Be(TipoErro.NaoEncontrado);
        leu.Mensagem.Should().Be("Atividade não encontrada.");
        comentou.Erro.Should().Be(TipoErro.NaoEncontrado);
        (await ComentariosNoBancoAsync(id)).Should().ContainSingle();
        (await ExecutarAsync(_pedro, new ListarComentariosDaAtividade(id))).Valor!.Should().ContainSingle();

        var negados = (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == _paula).ToList();
        negados.Should().HaveCount(2).And.OnlyContain(r => r.Entidade == "Atividade" && r.EntidadeId == id);
    }

    [Fact]
    public async Task Atividade_excluida_esconde_os_comentarios()
    {
        var id = await DirecionadaAsync();
        await ComentarAsync(_diana, id, "Antes de excluir.");

        (await ExecutarAsync(_diana, new ExcluirAtividade(id))).Sucesso.Should().BeTrue();

        (await ExecutarAsync(_diana, new ListarComentariosDaAtividade(id))).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await ExecutarAsync(_diana, new ComentarAtividade(id, "Depois."))).Erro.Should().Be(TipoErro.NaoEncontrado);
        var linhas = await ComentariosNoBancoAsync(id);
        linhas.Should().ContainSingle().Which.ExcluidoEm.Should().BeNull();
    }

    [Fact]
    public async Task Banco_recusa_apagar_fisicamente_atividade_com_comentario()
    {
        var id = await DirecionadaAsync();
        await ComentarAsync(_diana, id, "Fica registrado.");

        // Tira as outras filhas do caminho: só o comentário pode impedir a exclusão.
        var apagar = () => EmEscopoAsync(null, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM atividades_responsaveis WHERE atividade_id = {id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM atividades_historico_status WHERE atividade_id = {id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM atividades WHERE id = {id}");
            return 0;
        });

        var erro = await apagar.Should().ThrowAsync<Exception>();
        var pg = PostgresDe(erro.Which);
        pg.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        pg.ConstraintName.Should().Be("fk_atividades_comentarios_atividades_atividade_id");
        (await ExecutarAsync(_diana, new ListarComentariosDaAtividade(id))).Valor!.Should().ContainSingle();
    }
}
