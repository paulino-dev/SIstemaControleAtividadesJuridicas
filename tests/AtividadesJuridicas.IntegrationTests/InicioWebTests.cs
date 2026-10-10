using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using static AtividadesJuridicas.IntegrationTests.LayoutWebTests;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>O início de cada perfil: o cabeçalho, os atalhos e a barra superior da tela no mobile.</summary>
[Collection(ColecaoBanco.Nome)]
public class InicioWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(HttpClient cliente, string rota) =>
        WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));

    private static string BarraDaTela(string html) =>
        Regex.Match(html, "<header class=\"aj-barra-superior\">.*?</header>", RegexOptions.Singleline).Value;

    [Fact]
    public async Task Inicio_da_equipe_tem_o_cabecalho_os_atalhos_e_a_barra_superior_da_tela()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, "/");

        var corpo = Regex.Match(html, "<main[^>]*>.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().Contain("<h1 class=\"aj-titulo\">Início</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Olá, Diana Diretora. Seu perfil é de Diretoria.</p>")
            .And.Contain("Minhas atividades").And.Contain("Prazos, situações e o que espera por você.")
            .And.Contain("Agenda da semana").And.Contain("Audiências, reuniões e prazos dos próximos dias.");
        Regex.Matches(corpo, "href=\"([^\"]*)\"").Select(m => m.Groups[1].Value)
            .Should().Equal("/atividades", "/agenda");

        // No mobile o título vai para a barra superior da tela, que vem antes da padrão.
        var barra = BarraDaTela(html);
        barra.Should().Contain("<h1 class=\"aj-titulo\">Início</h1>")
            .And.Contain("<span class=\"aj-subtitulo\">Olá, Diana Diretora</span>")
            .And.MatchRegex("<a href=\"/minha-senha\" class=\"aj-avatar\"[^>]*>DD</a>");
        html.IndexOf("<header class=\"aj-barra-superior\">", StringComparison.Ordinal)
            .Should().BeLessThan(html.IndexOf("aj-barra-superior--padrao", StringComparison.Ordinal));
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().Equal("/");
        // A regra que esconde a barra de logo exige as duas barras lado a lado: nada entre elas.
        html.Should().MatchRegex("</header>(?:\\s|<!--.*?-->)*<header class=\"aj-barra-superior aj-barra-superior--padrao\">");
    }

    [Fact]
    public async Task Inicio_do_cliente_nao_fala_do_perfil()
    {
        await _app.CriarCarlaAsync();
        var carla = await _app.ClienteAutenticadoAsync(
            UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);

        var html = await PaginaAsync(carla, "/");

        html.Should().Contain("<p class=\"aj-subtitulo\">Olá, Carla Cliente.</p>")
            .And.Contain("<span class=\"aj-subtitulo\">Olá, Carla Cliente</span>")
            .And.Contain("O que o escritório está fazendo por você.")
            .And.Contain("Arquivos que o escritório compartilhou com você.")
            .And.Contain("Modelos e orientações para todos os clientes.")
            .And.NotContain("Seu perfil é de");
    }

    private static readonly UsuarioAutenticado Cliente =
        new(Guid.NewGuid(), "Carla Cliente", TipoPerfil.Cliente, Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task Inicio_do_cliente_com_falha_nos_totais_mostra_o_erro_e_mantem_os_atalhos()
    {
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(Cliente)
            .Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(
                Resultado<AndamentoDoCliente>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Home>(despachante, new()));

        html.Should().MatchRegex("<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">[\\s\\S]*?<span>Falha de teste\\.</span></p>")
            .And.Contain("href=\"/andamento\"").And.Contain("href=\"/meus-documentos\"").And.Contain("href=\"/documentos-comuns\"")
            .And.NotContain("aj-totais").And.NotContain("Carregando");
    }

    [Fact]
    public async Task Inicio_do_cliente_com_acesso_encerrado_nao_mostra_atalho()
    {
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(Cliente)
            .Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(
                Resultado<AndamentoDoCliente>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Home>(despachante, new()));

        html.Should().Contain("Seu acesso foi encerrado. Entre novamente.")
            .And.NotContain("aj-cartao--atalho").And.NotContain("aj-totais").And.NotContain("Acesso negado.");
    }
}
