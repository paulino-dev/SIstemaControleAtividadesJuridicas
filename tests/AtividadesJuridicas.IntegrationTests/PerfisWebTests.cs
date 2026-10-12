using System.Net;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using TelaPerfis = AtividadesJuridicas.Web.Components.Pages.Perfis;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class PerfisWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public async Task InitializeAsync()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var estagio = await _app.ExecutarAsync(admin, new SalvarPerfil(null, "Estagiário", TipoPerfil.Producao));
        estagio.Sucesso.Should().BeTrue(estagio.Mensagem);
        (await _app.ExecutarAsync(admin, new DefinirAtivoPerfil(estagio.Valor, false))).Sucesso.Should().BeTrue();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_os_perfis_ativos_e_inativos_com_as_acoes()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/perfis"));

        html.Should().Contain("<p class=\"aj-subtitulo\">4 perfis · 1 inativo</p>").And.Contain("<h2>Novo perfil</h2>")
            .And.MatchRegex("<form[^>]*class=\"aj-painel aj-lado-a-lado__painel\"")
            .And.Contain("O tipo não pode ser mudado depois.")
            .And.MatchRegex("<div class=\"aj-painel__acoes\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--fantasma\"[^>]*>Cancelar</button>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario\"[^>]*>Salvar</button>");
        html.Should().NotContain("Acesso negado");
        // A lista do tablet e do mobile vem antes do painel.
        html.IndexOf("<h2>Perfis</h2>", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("<h2>Novo perfil</h2>", StringComparison.Ordinal));

        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Usuários ativos</th>")
            .And.MatchRegex("<tr>\\s*<td class=\"aj-tabela__principal\">Diretor</td>\\s*<td>Diretoria</td>\\s*<td>1</td>\\s*<td>Ativo</td>")
            .And.MatchRegex("<tr class=\"aj-tabela__linha--inativa\">\\s*<td class=\"aj-tabela__principal\">Estagiário</td>\\s*<td>Produção</td>\\s*<td>0</td>\\s*<td>Inativo</td>")
            .And.MatchRegex("<td class=\"aj-tabela__acao aj-tabela__acao--varias\"><button[^>]*>Renomear</button><button[^>]*>Ativar</button></td>");

        var linhas = BlocoDeVisao.De(html, "aj-tablet-mobile");
        linhas.Should().MatchRegex("<section class=\"aj-painel aj-painel--principal aj-painel--lista\">\\s*<h2>Perfis</h2>")
            .And.Contain("<span class=\"aj-ajuda\">Diretoria · 1 usuário ativo · Ativo</span>")
            .And.MatchRegex("<div class=\"aj-linha aj-linha--inativa\">\\s*<div class=\"aj-linha__texto\"><span class=\"aj-linha__titulo\">Estagiário</span><span class=\"aj-ajuda\">Produção · 0 usuários ativos · Inativo</span></div>")
            .And.Contain("aria-label=\"Ações de Estagiário\"");
        linhas.Should().NotContain("<table");
    }

    [Fact]
    public async Task Falha_na_consulta_dos_perfis_mostra_o_erro_no_lugar_da_lista_e_mantem_o_formulario()
    {
        var diretora = new UsuarioAutenticado(Guid.NewGuid(), "Diana Diretora", TipoPerfil.Diretoria, Guid.NewGuid(), null);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretora)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(
                Resultado<IReadOnlyList<PerfilDetalhe>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<TelaPerfis>(despachante, new()));

        html.Should().MatchRegex("<div class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\"")
            .And.Contain("Falha de teste.").And.Contain(">Tentar de novo</button>").And.Contain("<h2>Novo perfil</h2>");
        html.Should().NotContain("Nenhum perfil.").And.NotContain("<table").And.NotContain("aj-subtitulo\">0");
    }
    [Fact]
    public async Task Producao_nao_ve_a_gestao_de_perfis()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/perfis"));

        html.Should().Contain("<h1 class=\"aj-titulo\">Perfis</h1>").And.Contain("Acesso negado.");
        html.Should().NotContain("Estagiário").And.NotContain("Novo perfil").And.NotContain("Renomear");
    }

    [Fact]
    public async Task Menu_mostra_perfis_so_para_a_diretoria()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/perfis\"");
        (await doPedro.GetStringAsync("/")).Should().NotContain("href=\"/perfis\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/perfis");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
