using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ConsultaDeAuditoriaTests(BancoFixture banco) : TesteComBanco(banco)
{
    private async Task<(Guid AdminId, Guid PedroId)> PrepararAsync()
    {
        var adminId = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        var pedro = await ExecutarAsync(adminId, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null));
        await ExecutarAsync(adminId, new EditarUsuario(pedro.Valor, "Pedro Silva", "pedro@x.com", producao, null));
        return (adminId, pedro.Valor);
    }

    private async Task<Pagina<RegistroAuditoriaResumo>> ConsultarAsync(Guid ator, FiltroDeAuditoria filtro, int pagina = 1)
    {
        var resultado = await ExecutarAsync(ator, new ConsultarAuditoria(filtro, pagina));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor!;
    }

    [Fact]
    public async Task Sem_filtro_traz_tudo_do_mais_recente_para_o_mais_antigo_com_o_nome_de_quem_fez()
    {
        var (adminId, pedroId) = await PrepararAsync();

        var pagina = await ConsultarAsync(adminId, new FiltroDeAuditoria());

        pagina.Itens.Should().BeInDescendingOrder(r => r.DataHora);
        pagina.Total.Should().Be(pagina.Itens.Count);
        var maisRecente = pagina.Itens[0];
        maisRecente.Should().BeEquivalentTo(new
        {
            Operacao = OperacaoAuditada.Alteracao, Entidade = "Usuario", EntidadeId = pedroId,
            UsuarioId = adminId, UsuarioNome = "Diana Diretora",
        });
        pagina.Itens.Should().Contain(r => r.UsuarioId == null && r.UsuarioNome == null, "a carga inicial é do sistema");
    }

    [Fact]
    public async Task Filtra_por_entidade_operacao_e_usuario()
    {
        var (adminId, pedroId) = await PrepararAsync();

        var criacoesDeUsuario = await ConsultarAsync(adminId,
            new FiltroDeAuditoria(Entidade: "Usuario", Operacao: OperacaoAuditada.Criacao, UsuarioId: adminId));

        criacoesDeUsuario.Itens.Should().ContainSingle().Which.EntidadeId.Should().Be(pedroId);
    }

    [Fact]
    public async Task Filtra_por_periodo_com_inicio_inclusivo_e_fim_exclusivo()
    {
        var (adminId, _) = await PrepararAsync();
        var todos = (await ConsultarAsync(adminId, new FiltroDeAuditoria())).Itens;
        var corte = todos[0].DataHora;

        (await ConsultarAsync(adminId, new FiltroDeAuditoria(DeUtc: corte))).Itens
            .Should().Contain(r => r.Id == todos[0].Id);
        (await ConsultarAsync(adminId, new FiltroDeAuditoria(AteUtc: corte))).Itens
            .Should().NotContain(r => r.Id == todos[0].Id);
        (await ConsultarAsync(adminId, new FiltroDeAuditoria(DeUtc: corte.AddDays(1)))).Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Pagina_de_cinquenta_em_cinquenta()
    {
        var adminId = await IdDoAdminAsync();
        await EmEscopoAsync(adminId, async sp =>
        {
            var auditoria = sp.GetRequiredService<IAuditoria>();
            for (var i = 0; i < 120; i++) await auditoria.RegistrarAsync("evento_de_teste", dados: new { i });
            return 0;
        });

        var filtro = new FiltroDeAuditoria(Operacao: OperacaoAuditada.Evento);
        var primeira = await ConsultarAsync(adminId, filtro);
        var terceira = await ConsultarAsync(adminId, filtro, 3);

        primeira.Itens.Should().HaveCount(50);
        primeira.Total.Should().Be(120);
        primeira.TotalDePaginas.Should().Be(3);
        terceira.Itens.Should().HaveCount(20);
        primeira.Itens.Select(r => r.Id).Should().NotIntersectWith(terceira.Itens.Select(r => r.Id));
    }

    [Fact]
    public async Task Usuario_excluido_ainda_aparece_pelo_nome()
    {
        var (adminId, pedroId) = await PrepararAsync();
        await ExecutarAsync(pedroId, new AlterarMinhaSenha("senha-do-pedro", "senha-nova-do-pedro"));
        await EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Usuarios.Remove(await db.Usuarios.SingleAsync(u => u.Id == pedroId));
            await db.SaveChangesAsync();
            return 0;
        });

        var doPedro = await ConsultarAsync(adminId, new FiltroDeAuditoria(UsuarioId: pedroId));

        doPedro.Itens.Should().NotBeEmpty().And.OnlyContain(r => r.UsuarioNome == "Pedro Silva");
    }

    [Fact]
    public async Task Producao_nao_consulta_e_a_tentativa_fica_registrada()
    {
        var (_, pedroId) = await PrepararAsync();

        var auditoria = await ExecutarAsync(pedroId, new ConsultarAuditoria(new FiltroDeAuditoria()));
        var acessos = await ExecutarAsync(pedroId, new ConsultarAcessos(new FiltroDeAcessos()));

        auditoria.Erro.Should().Be(TipoErro.AcessoNegado);
        auditoria.Valor.Should().BeNull();
        acessos.Erro.Should().Be(TipoErro.AcessoNegado);
        (await AuditoriaAsync()).Count(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == pedroId)
            .Should().Be(2);
    }

    [Fact]
    public async Task Consultar_a_auditoria_gera_registro_de_acesso_com_os_filtros()
    {
        var (adminId, _) = await PrepararAsync();

        await ConsultarAsync(adminId, new FiltroDeAuditoria(Entidade: "Usuario"));

        var acessos = await ExecutarAsync(adminId, new ConsultarAcessos(new FiltroDeAcessos(TipoRecurso: "RegistroAuditoria")));
        var acesso = acessos.Valor!.Itens.Should().ContainSingle().Subject;
        acesso.Acao.Should().Be(AcaoDeAcesso.Listou);
        acesso.UsuarioId.Should().Be(adminId);
        acesso.UsuarioNome.Should().Be("Diana Diretora");
        acesso.Detalhe.Should().Contain("Usuario");
    }

    [Fact]
    public async Task Acessos_filtram_por_usuario_e_acao()
    {
        var (adminId, _) = await PrepararAsync();
        await ExecutarAsync(adminId, new ListarUsuarios());

        var acessos = await ExecutarAsync(adminId,
            new ConsultarAcessos(new FiltroDeAcessos(UsuarioId: adminId, TipoRecurso: "Usuario", Acao: AcaoDeAcesso.Listou)));

        acessos.Valor!.Itens.Should().ContainSingle().Which.TipoRecurso.Should().Be("Usuario");
    }

    [Fact]
    public async Task Login_bem_sucedido_aparece_filtrado_pelo_usuario_com_o_nome_de_quem_entrou()
    {
        var adminId = await IdDoAdminAsync();
        (await ExecutarAsync(null, new AutenticarUsuario(Admin.Email, Admin.Senha))).Sucesso.Should().BeTrue();

        var pagina = await ConsultarAsync(adminId,
            new FiltroDeAuditoria(UsuarioId: adminId));

        pagina.Itens.Should().ContainSingle(r => r.Evento == EventosDeSeguranca.LoginSucesso)
            .Which.UsuarioNome.Should().Be("Diana Diretora");
    }
}
