using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>O que o layout decide sozinho: o destino aceso em cada barra, o avatar, o foco no título e a tela ainda não migrada.</summary>
[Collection(ColecaoBanco.Nome)]
public class LayoutWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    // Sem a barra na página, as asserções de "nada aceso" passariam sem provar nada.
    internal static string Navegacao(string html, string classe)
    {
        var barra = Regex.Match(html, $"<nav class=\"{classe}\"[^>]*>.*?</nav>", RegexOptions.Singleline);
        barra.Success.Should().BeTrue($"a página deve trazer a navegação {classe}");
        return barra.Value;
    }

    // O destino aceso de uma barra: o href do link, ou "mais" quando é o botão.
    internal static string[] Acesos(string barra) =>
        Regex.Matches(barra, "<(a|button)\\b[^>]*aria-current=\"page\"[^>]*>")
            .Select(m => m.Groups[1].Value == "button"
                ? "mais"
                : Regex.Match(m.Value, "href=\"([^\"]*)\"").Groups[1].Value)
            .ToArray();

    private async Task<string> PaginaAsync(HttpClient cliente, string rota) =>
        WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));

    [Theory]
    [InlineData("/atividades", "/atividades", "/atividades")]
    [InlineData("/clientes", "/clientes", "mais")]
    [InlineData("/clientes?busca=maria", "/clientes", "mais")]
    [InlineData("/areas", "mais", "mais")]
    [InlineData("/documentos-comuns", "mais", "mais")]
    public async Task Diretoria_ve_o_destino_ou_mais_aceso_em_cada_barra(string rota, string noTrilho, string naBarra)
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, rota);

        Acesos(Navegacao(html, "aj-trilho")).Should().Equal(noTrilho);
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().Equal(naBarra);
    }

    [Theory]
    [InlineData("/minha-senha")]
    [InlineData("/rota-que-nao-existe")]
    public async Task Minha_senha_e_pagina_nao_encontrada_nao_acendem_nada(string rota)
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await (await admin.GetAsync(rota)).Content.ReadAsStringAsync());

        Acesos(Navegacao(html, "aj-trilho")).Should().BeEmpty();
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().BeEmpty();
    }

    [Fact]
    public async Task Producao_em_tela_que_o_menu_dela_nao_tem_nao_acende_mais()
    {
        await _app.CriarPedroAsync();
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = await PaginaAsync(pedro, "/usuarios");

        Acesos(Navegacao(html, "aj-trilho")).Should().BeEmpty();
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().BeEmpty();
    }

    [Fact]
    public async Task Producao_em_clientes_tem_o_destino_no_trilho_e_mais_na_barra()
    {
        await _app.CriarPedroAsync();
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = await PaginaAsync(pedro, "/clientes");

        Acesos(Navegacao(html, "aj-trilho")).Should().Equal("/clientes");
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().Equal("mais");
    }

    [Fact]
    public async Task Cliente_em_pagina_que_o_layout_nao_monta_nao_acende_nada()
    {
        await _app.CriarCarlaAsync();
        var carla = await _app.ClienteAutenticadoAsync(
            UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);

        var html = await PaginaAsync(carla, "/clientes");

        html.Should().Contain("Página não encontrada");
        Acesos(Navegacao(html, "aj-trilho")).Should().BeEmpty();
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().BeEmpty();
    }

    [Fact]
    public async Task Modulo_do_foco_no_titulo_e_servido_e_pagina_estatica_usa_o_foco_do_blazor()
    {
        var anonimo = _app.NovoCliente();

        var modulo = await anonimo.GetAsync("/Components/Layout/FocoNoTitulo.razor.js");
        modulo.StatusCode.Should().Be(HttpStatusCode.OK);
        (await modulo.Content.ReadAsStringAsync()).Should().Contain("export function focarTituloVisivel");

        // Sem circuito (entrar, erro) quem foca o título é o FocusOnNavigate; com circuito, o módulo.
        (await anonimo.GetStringAsync("/conta/entrar")).Should().Contain("<blazor-focus-on-navigate");
        var admin = await _app.ClienteAutenticadoAsync();
        (await admin.GetStringAsync("/")).Should().NotContain("<blazor-focus-on-navigate");
    }

    [Fact]
    public async Task Cliente_em_documentos_comuns_tem_o_destino_no_trilho_e_mais_na_barra()
    {
        await _app.CriarCarlaAsync();
        var carla = await _app.ClienteAutenticadoAsync(
            UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);

        var html = await PaginaAsync(carla, "/documentos-comuns");

        Acesos(Navegacao(html, "aj-trilho")).Should().Equal("/documentos-comuns");
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().Equal("mais");
    }

    [Fact]
    public async Task Menu_lateral_marca_o_destino_atual()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, "/clientes");

        var menu = Regex.Match(html, "<aside class=\"aj-menu\".*?</aside>", RegexOptions.Singleline).Value;
        Regex.Matches(menu, "<a\\b[^>]*aria-current=\"page\"[^>]*>")
            .Select(m => Regex.Match(m.Value, "href=\"([^\"]*)\"").Groups[1].Value)
            .Should().Equal("/clientes");
    }

    [Fact]
    public async Task Tela_ainda_nao_migrada_fica_com_a_barra_de_logo_e_sem_involucro()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, "/atividades");

        Regex.Matches(html, "<header class=\"aj-barra-superior").Should().ContainSingle();
        html.Should().Contain("<header class=\"aj-barra-superior aj-barra-superior--padrao\">")
            .And.Contain("class=\"aj-barra-superior__logo\"")
            .And.NotContain("class=\"pagina\"");
        // O conteúdo antigo continua lá, direto no main; a faixa de exibir o menu fica fora dele.
        var corpo = Regex.Match(html, "<main class=\"aj-app__conteudo\">.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().Contain("<h1").And.NotContain("Exibir menu");
        html.Should().MatchRegex("<div class=\"aj-exibir-menu\">\\s*<button[^>]*aria-label=\"Exibir menu\"");
    }

    // A regra interina do app.css trata como migrada a tela cujo conteúdo começa por um bloco do design system.
    // Enquanto a tela não for migrada, nenhum componente aj- pode abrir o conteúdo dela.
    [Theory]
    [InlineData("/atividades")]
    [InlineData("/agenda")]
    [InlineData("/clientes")]
    [InlineData("/processos")]
    public async Task Tela_ainda_nao_migrada_nao_abre_o_conteudo_com_bloco_do_design_system(string rota)
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, rota);

        var primeiro = Regex.Match(html, "<main class=\"aj-app__conteudo\">(?:\\s|<!--.*?-->)*(<[a-z][^>]*>)",
            RegexOptions.Singleline).Groups[1].Value;
        primeiro.Should().NotBeEmpty().And.NotContain("class=\"aj-");
    }

    [Theory]
    [InlineData("Madonna", "M")]
    [InlineData("Maria Aparecida dos Santos Albuquerque Cavalcanti", "MC")]
    public async Task Avatar_da_barra_superior_traz_as_iniciais(string nome, string iniciais)
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var producao = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
                .SingleAsync(p => p.Tipo == TipoPerfil.Producao && p.Ativo)).Id);
        var criado = await _app.ExecutarAsync(adminId, new CriarUsuario(nome, "nome@x.com", "senha-do-nome", producao, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        var cliente = await _app.ClienteAutenticadoAsync("nome@x.com", "senha-do-nome");

        var html = await PaginaAsync(cliente, "/atividades");

        var barra = Regex.Match(html, "<header class=\"aj-barra-superior.*?</header>", RegexOptions.Singleline).Value;
        barra.Should().MatchRegex($"<a href=\"/minha-senha\" class=\"aj-avatar\"[^>]*>{iniciais}</a>");
    }
}
