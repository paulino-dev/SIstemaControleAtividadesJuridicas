using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ProdutividadeTests(BancoFixture banco) : TesteComBanco(banco)
{
    // Meses de 2026 no fuso do escritório (UTC-3): cada um começa às 03:00 UTC do dia 1.
    private static readonly DateTime Setembro = new(2026, 9, 1, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Outubro = new(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Novembro = new(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Entrega = new(2090, 6, 15);

    private Guid _diana, _davi, _pedro, _paula, _maria;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _diana = await IdDoAdminAsync();
        _davi = await NovoUsuarioAsync("Davi Diretor", "davi@x.com", TipoPerfil.Diretoria);
        _pedro = await NovoUsuarioAsync("Pedro", "pedro@x.com", TipoPerfil.Producao);
        _paula = await NovoUsuarioAsync("Paula", "paula@x.com", TipoPerfil.Producao);
        _maria = (await ExecutarAsync(_diana, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null),
            "529.982.247-25", null, null))).Valor;
    }

    private static DateTime Em(int mes, int dia) => new(2026, mes, dia, 15, 0, 0, DateTimeKind.Utc);

    private async Task<Guid> NovoUsuarioAsync(string nome, string email, TipoPerfil tipo)
    {
        var perfil = await IdDoPerfilAsync(tipo);
        var criado = await ExecutarAsync(_diana, new CriarUsuario(nome, email, "senha-do-" + nome[..3].ToLowerInvariant(), perfil, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    /// <summary>Direcionada aos responsáveis informados, com Diana de revisora.</summary>
    private async Task<Guid> DirecionadaAsync(string titulo, int complexidade, Guid? cliente, params Guid[] responsaveis)
    {
        var criada = await ExecutarAsync(_diana, new SalvarAtividade(
            null, titulo, "Descrição.", Entrega, cliente, null, null, complexidade));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        var definida = await ExecutarAsync(_diana, new DefinirResponsaveisDaAtividade(
            criada.Valor, [.. responsaveis.Select(r => new ResponsavelEntrada(r, null))], _diana));
        definida.Sucesso.Should().BeTrue(definida.Mensagem);
        return criada.Valor;
    }

    /// <summary>
    /// Conclui pelos casos de uso (o relógio da infraestrutura é o real) e depois acerta <c>ConcluidaEm</c> para o
    /// instante do cenário, pelo contexto e com os interceptores.
    /// </summary>
    private async Task<Guid> ConcluidaAsync(string titulo, int complexidade, DateTime concluidaEm, Guid? cliente, params Guid[] responsaveis)
    {
        var id = await DirecionadaAsync(titulo, complexidade, cliente, responsaveis);
        foreach (var responsavel in responsaveis)
            (await ExecutarAsync(responsavel, new ConcluirMinhaParte(id))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_diana, new AprovarAtividade(id))).Sucesso.Should().BeTrue();

        await EmEscopoAsync(null, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var atividade = await db.Atividades.SingleAsync(a => a.Id == id);
            db.Entry(atividade).Property(a => a.ConcluidaEm).CurrentValue = concluidaEm;
            return await db.SaveChangesAsync();
        });
        return id;
    }

    private async Task<ProdutividadeDoProdutor> ProdutividadeAsync(Guid produtor, DateTime? de = null, DateTime? ate = null)
    {
        var resultado = await ExecutarAsync(_diana, new ObterProdutividadeDoProdutor(produtor, de ?? Outubro, ate ?? Novembro));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor!;
    }

    private static (int Complexidade, int Total)[] Totais(ProdutividadeDoProdutor produtividade) =>
        [.. produtividade.Grupos.Select(g => (g.Complexidade, g.Total))];

    [Fact]
    public async Task Traz_so_as_concluidas_do_periodo_em_que_o_produtor_e_responsavel()
    {
        var simples = await ConcluidaAsync("Petição simples", 1, Em(10, 5), null, _pedro);
        var dosDois = await ConcluidaAsync("Recurso dos dois", 3, Em(10, 10), _maria, _pedro, _paula);
        var daPaula = await ConcluidaAsync("Só da Paula", 2, Em(10, 7), null, _paula);
        await ConcluidaAsync("De setembro", 2, Em(9, 15), null, _pedro);
        await DirecionadaAsync("Em andamento", 1, null, _pedro);

        var dePedro = await ProdutividadeAsync(_pedro);
        var dePaula = await ProdutividadeAsync(_paula);

        dePedro.ProdutorNome.Should().Be("Pedro");
        Totais(dePedro).Should().Equal((1, 1), (2, 0), (3, 1));
        dePedro.Total.Should().Be(2);
        dePedro.Grupos[0].Atividades.Should().Equal(new AtividadeRealizada(simples, "Petição simples", 1, Em(10, 5), null));
        dePedro.Grupos[2].Atividades.Should().Equal(new AtividadeRealizada(dosDois, "Recurso dos dois", 3, Em(10, 10), "Maria Silva"));
        dePedro.Grupos[2].Atividades.Single().ConcluidaEm.Kind.Should().Be(DateTimeKind.Utc);
        dePedro.PrimeiraConclusaoEm.Should().Be(Em(9, 15));

        // A atividade dos dois aparece inteira para cada um, uma vez só.
        Totais(dePaula).Should().Equal((1, 0), (2, 1), (3, 1));
        dePaula.Grupos[1].Atividades.Select(a => a.Id).Should().Equal(daPaula);
        dePaula.Grupos[2].Atividades.Select(a => a.Id).Should().Equal(dosDois);
    }

    [Fact]
    public async Task Periodo_e_semiaberto_e_cada_grupo_vem_em_ordem_de_conclusao()
    {
        // 30/09 23:59:59 no escritório é 01/10 02:59:59 em UTC: ainda setembro.
        var fimDeSetembro = await ConcluidaAsync("Último segundo de setembro", 1, Outubro.AddSeconds(-1), null, _pedro);
        var fimDeOutubro = await ConcluidaAsync("Último segundo de outubro", 1, Novembro.AddSeconds(-1), null, _pedro);
        var meioDeOutubro = await ConcluidaAsync("Meio de outubro", 1, Em(10, 15), null, _pedro);
        var inicioDeOutubro = await ConcluidaAsync("Primeiro instante de outubro", 1, Outubro, null, _pedro);
        await ConcluidaAsync("Primeiro instante de novembro", 1, Novembro, null, _pedro);

        (await ProdutividadeAsync(_pedro)).Grupos[0].Atividades.Select(a => a.Id)
            .Should().Equal(inicioDeOutubro, meioDeOutubro, fimDeOutubro);
        (await ProdutividadeAsync(_pedro, Setembro, Outubro)).Grupos[0].Atividades.Select(a => a.Id)
            .Should().Equal(fimDeSetembro);
    }

    [Fact]
    public async Task Reaberta_e_excluida_deixam_de_contar()
    {
        var reaberta = await ConcluidaAsync("Será reaberta", 1, Em(10, 5), null, _pedro);
        var excluida = await ConcluidaAsync("Será excluída", 2, Em(10, 6), null, _pedro);
        var fica = await ConcluidaAsync("Fica", 3, Em(10, 7), null, _pedro);

        (await ExecutarAsync(_diana, new ReabrirAtividade(reaberta, "Cliente pediu ajuste", [_pedro]))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_diana, new ExcluirAtividade(excluida))).Sucesso.Should().BeTrue();

        var produtividade = await ProdutividadeAsync(_pedro);
        Totais(produtividade).Should().Equal((1, 0), (2, 0), (3, 1));
        produtividade.Grupos[2].Atividades.Select(a => a.Id).Should().Equal(fica);
        produtividade.PrimeiraConclusaoEm.Should().Be(Em(10, 7));
    }

    [Fact]
    public async Task Sem_nada_concluido_vem_os_tres_grupos_vazios_e_nenhuma_primeira_conclusao()
    {
        await DirecionadaAsync("Em andamento", 1, null, _pedro);

        var produtividade = await ProdutividadeAsync(_pedro);

        Totais(produtividade).Should().Equal((1, 0), (2, 0), (3, 0));
        produtividade.PrimeiraConclusaoEm.Should().BeNull();
    }

    [Fact]
    public async Task So_a_diretoria_consulta_e_so_quem_e_de_producao_e_produtor()
    {
        await ConcluidaAsync("Parecer", 1, Em(10, 5), null, _pedro);

        var dePedro = await ExecutarAsync(_pedro, new ObterProdutividadeDoProdutor(_pedro, Outubro, Novembro));
        var doDiretor = await ExecutarAsync(_diana, new ObterProdutividadeDoProdutor(_davi, Outubro, Novembro));
        var deNinguem = await ExecutarAsync(_diana, new ObterProdutividadeDoProdutor(Guid.CreateVersion7(), Outubro, Novembro));

        dePedro.Erro.Should().Be(TipoErro.AcessoNegado);
        doDiretor.Erro.Should().Be(TipoErro.NaoEncontrado);
        doDiretor.Mensagem.Should().Be("Produtor não encontrado.");
        deNinguem.Erro.Should().Be(TipoErro.NaoEncontrado);

        // Só a recusa por perfil é tentativa de acesso negada, e fica registrada com o produtor pedido.
        var negado = (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado)
            .Should().ContainSingle().Subject;
        negado.UsuarioId.Should().Be(_pedro);
        negado.Dados.Should().Contain(_pedro.ToString());
    }

    [Fact]
    public async Task Consulta_fica_no_registro_de_acessos_com_o_produtor_e_o_periodo()
    {
        await ProdutividadeAsync(_pedro);
        await ExecutarAsync(_diana, new ObterProdutividadeDoProdutor(_davi, Outubro, Novembro));   // recusada: não registra

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso
            .AsNoTracking().Where(a => a.TipoRecurso == "Produtividade").ToListAsync());

        var acesso = acessos.Should().ContainSingle().Subject;
        acesso.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        acesso.RecursoId.Should().Be(_pedro);
        acesso.UsuarioId.Should().Be(_diana);
        acesso.Detalhe.Should().Contain("2026-10-01T03:00:00Z").And.Contain("2026-11-01T03:00:00Z");
    }

    [Fact]
    public async Task Inativo_continua_consultavel_e_quem_vira_diretoria_deixa_de_ser_produtor()
    {
        await ConcluidaAsync("Parecer", 1, Em(10, 5), null, _pedro, _paula);
        (await ExecutarAsync(_diana, new DefinirAtivoUsuario(_pedro, false))).Sucesso.Should().BeTrue();
        var diretoria = await IdDoPerfilAsync(TipoPerfil.Diretoria);
        (await ExecutarAsync(_diana, new EditarUsuario(_paula, "Paula", "paula@x.com", diretoria, null))).Sucesso.Should().BeTrue();

        (await ProdutividadeAsync(_pedro)).Total.Should().Be(1);
        (await ExecutarAsync(_diana, new ObterProdutividadeDoProdutor(_paula, Outubro, Novembro)))
            .Erro.Should().Be(TipoErro.NaoEncontrado);
    }
}
