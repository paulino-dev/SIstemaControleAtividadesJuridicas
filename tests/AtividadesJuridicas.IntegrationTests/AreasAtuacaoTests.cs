using System.Net;
using System.Text.Json.Nodes;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using TelaAreas = AtividadesJuridicas.Web.Components.Pages.Areas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AreasAtuacaoTests(BancoFixture banco) : TesteComBanco(banco)
{
    [Fact]
    public async Task Criar_listar_e_auditar()
    {
        var adminId = await IdDoAdminAsync();

        var criada = await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Trabalhista"));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);

        var lista = await ExecutarAsync(adminId, new ListarAreasAtuacao());
        lista.Valor.Should().ContainSingle().Which.Should().Be(new AreaAtuacaoResumo(criada.Valor, "Trabalhista", true));

        var registro = (await AuditoriaAsync()).Single(r => r.EntidadeId == criada.Valor);
        registro.Entidade.Should().Be("AreaAtuacao");
        registro.Operacao.Should().Be(OperacaoAuditada.Criacao);
        registro.UsuarioId.Should().Be(adminId);
        registro.Dados.Should().Contain("Trabalhista");
    }

    [Fact]
    public async Task Nome_duplicado_e_barrado_tambem_pelo_banco()
    {
        var adminId = await IdDoAdminAsync();
        await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Cível"));

        (await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "cível"))).Erro.Should().Be(TipoErro.Conflito);

        var direto = () => EmEscopoAsync(adminId, async sp =>
        {
            sp.GetRequiredService<AppDbContext>().Add(AreaAtuacao.Criar("Cível"));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });
        await direto.Should().ThrowAsync<ExcecaoDeConflito>();
    }

    [Theory]
    [InlineData("CÍVEL")]
    [InlineData("  cível ")]
    public async Task Nome_repetido_com_acento_em_maiuscula_e_conflito_no_banco_real(string nome)
    {
        var adminId = await IdDoAdminAsync();
        await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Cível"));

        (await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, nome))).Erro.Should().Be(TipoErro.Conflito);
    }

    [Fact]
    public async Task Renomear_registra_de_e_para()
    {
        var adminId = await IdDoAdminAsync();
        var criada = await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Civel"));

        (await ExecutarAsync(adminId, new SalvarAreaAtuacao(criada.Valor, "Cível"))).Sucesso.Should().BeTrue();

        var alteracao = (await AuditoriaAsync())
            .Single(r => r.EntidadeId == criada.Valor && r.Operacao == OperacaoAuditada.Alteracao);
        // A coluna é jsonb: o PostgreSQL normaliza os espaços, então compara-se o JSON, não o texto.
        JsonNode.DeepEquals(JsonNode.Parse(alteracao.Dados!), JsonNode.Parse("""{"Nome":{"de":"Civel","para":"Cível"}}"""))
            .Should().BeTrue($"os dados registrados foram {alteracao.Dados}");
    }
}

[Collection(ColecaoBanco.Nome)]
public class AreasWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_as_areas_e_o_formulario()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        await _app.ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Trabalhista"));
        var previdenciario = await _app.ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Previdenciário"));
        (await _app.ExecutarAsync(adminId, new DefinirAtivoAreaAtuacao(previdenciario.Valor, false))).Sucesso.Should().BeTrue();
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/areas"));

        html.Should().Contain("<p class=\"aj-subtitulo\">2 áreas · 1 inativa</p>")
            .And.Contain("<span class=\"aj-subtitulo\">2 áreas · 1 inativa</span>")
            .And.MatchRegex("<form[^>]*class=\"aj-painel aj-lado-a-lado__painel\"")
            .And.Contain("<h2>Nova área</h2>")
            .And.MatchRegex("(?s)<div class=\"aj-campo-com-botao\">.*?<input[^>]*class=\"aj-campo[^\"]*\".*?<button type=\"submit\" class=\"aj-botao aj-botao--primario\"[^>]*>Salvar</button>");
        // Fora da renomeação, o painel não tem "Cancelar" (o "Salvar" do mesmo painel está acima).
        html.Should().NotContain(">Cancelar</button>");
        // A tabela vem antes do painel e o painel antes da lista do tablet e do mobile.
        html.IndexOf("<th scope=\"col\">Nome</th>", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("<h2>Nova área</h2>", StringComparison.Ordinal));
        html.IndexOf("<h2>Nova área</h2>", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("<h2>Áreas</h2>", StringComparison.Ordinal));

        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Situação</th>").And.Contain("<span class=\"aj-so-leitor\">Ações</span>")
            .And.MatchRegex("<tr>\\s*<td class=\"aj-tabela__principal\">Trabalhista</td>\\s*<td>Ativa</td>")
            .And.MatchRegex("<tr class=\"aj-tabela__linha--inativa\">\\s*<td class=\"aj-tabela__principal\">Previdenciário</td>\\s*<td>Inativa</td>")
            .And.MatchRegex("<td class=\"aj-tabela__acao aj-tabela__acao--varias\"><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Renomear</button><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Ativar</button></td>");

        var linhas = BlocoDeVisao.De(html, "aj-tablet-mobile");
        linhas.Should().MatchRegex("<section class=\"aj-painel aj-painel--principal aj-painel--lista\">\\s*<h2>Áreas</h2>")
            .And.MatchRegex("<div class=\"aj-linha aj-linha--inativa\">\\s*<div class=\"aj-linha__texto\"><span class=\"aj-linha__titulo\">Previdenciário</span><span class=\"aj-ajuda\">Inativa</span></div>")
            .And.MatchRegex("(?s)aria-label=\"Ações de Trabalhista\".*?<button type=\"button\" class=\"aj-menu-acoes__item\"[^>]*>Renomear</button>\\s*<button type=\"button\" class=\"aj-menu-acoes__item\"[^>]*>Inativar</button>");
        linhas.Should().NotContain("<table");
    }

    [Fact]
    public async Task Falha_na_consulta_das_areas_mostra_o_erro_no_lugar_da_lista_e_mantem_o_formulario()
    {
        var diretora = new UsuarioAutenticado(Guid.NewGuid(), "Diana Diretora", TipoPerfil.Diretoria, Guid.NewGuid(), null);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretora)
            .Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(
                Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<TelaAreas>(despachante, new()));

        html.Should().MatchRegex("<div class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\"")
            .And.Contain("Falha de teste.").And.Contain(">Tentar de novo</button>").And.Contain("<h2>Nova área</h2>");
        html.Should().NotContain("Nenhuma área.").And.NotContain("<table").And.NotContain("aj-subtitulo\">0");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_o_link_no_menu()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        WebUtility.HtmlDecode(await cliente.GetStringAsync("/areas"))
            .Should().Contain("<h1 class=\"aj-titulo\">Áreas de atuação</h1>").And.Contain("Acesso negado.")
            .And.NotContain("Nova área").And.NotContain("<table");
        (await cliente.GetStringAsync("/")).Should().NotContain("href=\"/areas\"");
    }

    [Fact]
    public async Task Recarga_que_falha_depois_de_ativar_mantem_a_lista_e_avisa()
    {
        var diretora = new UsuarioAutenticado(Guid.NewGuid(), "Diana Diretora", TipoPerfil.Diretoria, Guid.NewGuid(), null);
        var inativa = new AreaAtuacaoResumo(Guid.NewGuid(), "Previdenciário", false);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretora)
            .Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Ok([inativa]))
            .Responde<DefinirAtivoAreaAtuacao, Resultado>(Resultado.Ok());

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarEAgirAsync<TelaAreas>(despachante, new(), async tela =>
        {
            // A ativação dá certo, mas a consulta que a segue falha.
            despachante.Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(
                Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Falha(TipoErro.Validacao, "Falha de teste."));
            await TelaDeTeste.ChamarAsync(tela, "AlternarAtivoAsync", inativa);
        }));

        html.Should().MatchRegex("(?s)<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">.*?<span>Área ativada, mas não foi possível atualizar a lista.</span>")
            .And.Contain("<p class=\"aj-subtitulo\">1 área · 1 inativa</p>");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().Contain("Previdenciário");
        html.Should().NotContain("Falha de teste.").And.NotContain(">Tentar de novo</button>");
    }
}
