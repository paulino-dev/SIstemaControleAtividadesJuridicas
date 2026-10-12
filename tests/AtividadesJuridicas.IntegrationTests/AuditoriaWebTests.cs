using System.Net;
using System.Text.RegularExpressions;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AuditoriaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_os_registros_sem_nenhum_hash_de_senha()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/auditoria"));

        html.Should().MatchRegex("<p class=\"aj-subtitulo\">\\d+ registros? no período</p>")
            .And.Contain("login_sucesso").And.Contain("pedro@x.com").And.Contain("Diana Diretora")
            .And.MatchRegex("<span class=\"aj-subtitulo\">Página 1 de \\d+ · \\d+ registros?</span>");
        html.Should().NotContain("pbkdf2-sha256");

        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Correlação</th>")
            .And.MatchRegex("<code class=\"aj-dados\">[^<]*: pedro@x.com[^<]*</code>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-identificador\" title=\"Ver tudo deste registro\"[^>]*>[0-9a-f]{8}…</button>");
        // Dados resumidos: nada de JSON cru na tela.
        tabela.Should().NotContain("{\"");

        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().MatchRegex("<div class=\"aj-painel aj-painel--registro\">\\s*<span class=\"aj-registro__titulo\">Criação · Usuario</span>")
            .And.MatchRegex(">Gravação [0-9a-f]{8}…</button>");
    }

    [Fact]
    public async Task Diretor_ve_o_controle_segmentado_e_os_filtros_de_cada_visao()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/auditoria"));

        // O mesmo controle no cabeçalho (desktop e tablet) e na barra do mobile.
        Regex.Matches(html, "<div class=\"aj-segmentado aj-segmentado--recuado\" role=\"group\" aria-label=\"O que consultar\">").Count.Should().Be(2);
        html.Should().MatchRegex("<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"true\"[^>]*>Alterações e eventos</button>");

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("<form[^>]*class=\"aj-barra-filtros\"[^>]*role=\"search\"")
            .And.Contain("placeholder=\"Identificador do registro\"").And.Contain("placeholder=\"Identificador de correlação\"")
            .And.Contain("placeholder=\"Usuario, Perfil…\"")
            .And.MatchRegex("<button type=\"submit\" class=\"aj-botao aj-botao--secundario\"[^>]*>Filtrar</button>");
        // A ordem do quadro: Operação vem antes de Entidade.
        desktop.IndexOf("<span>Operação</span>", StringComparison.Ordinal).Should().BeLessThan(desktop.IndexOf("<span>Entidade</span>", StringComparison.Ordinal));

        var linha = BlocoDeVisao.De(html, "aj-tablet-mobile");
        linha.Should().MatchRegex("<form[^>]*class=\"aj-linha-filtros\"[^>]*role=\"search\"")
            .And.Contain("aria-controls=\"filtros-da-auditoria\"").And.Contain("aria-expanded=\"false\"")
            // O botão de envio escondido: sem ele, o Enter nos campos de data não envia o formulário da linha.
            .And.MatchRegex("(?s)<form[^>]*class=\"aj-linha-filtros\".*?<button type=\"submit\" class=\"aj-botao\" hidden[^>]*>Filtrar</button>\\s*</form>");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex("(?s)<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--com-icone\"[^>]*>.*?Usuário, operação</button>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().Contain("aria-label=\"Mais filtros\"");
        // O painel de filtros só aparece aberto.
        html.Should().NotContain("aj-painel--filtros").And.NotContain("Identificador inválido.");
    }

    [Fact]
    public async Task Diretor_ve_os_campos_de_filtro_por_registro_e_por_correlacao_e_os_botoes_das_linhas()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/auditoria"));

        html.Should().Contain("Registro").And.Contain("Gravação");
        html.Should().Contain("Identificador do registro").And.Contain("Identificador de correlação");
        html.Should().Contain("Ver tudo deste registro").And.Contain("Ver o que foi gravado junto");
        html.Should().NotContain("Identificador inválido.");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_registros()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/auditoria"));

        html.Should().Contain("Acesso negado").And.Contain("<h1 class=\"aj-titulo\">Auditoria</h1>");
        html.Should().NotContain("login_sucesso").And.NotContain("diana@x.com").And.NotContain("aj-segmentado");
    }

    [Fact]
    public async Task Menu_mostra_auditoria_so_para_a_diretoria()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/auditoria\"");
        (await doPedro.GetStringAsync("/")).Should().NotContain("href=\"/auditoria\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/auditoria");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
