using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public partial class AutenticacaoWebTests(BancoFixture banco) : IAsyncLifetime
{
    private WebApplicationFactory<Program> _fabrica = null!;
    private string _conexao = null!;
    private HttpClient _cliente = null!;

    public Task InitializeAsync()
    {
        _conexao = banco.NovaConexao();
        // Acrescentada por último, esta fonte vence o appsettings.Development.json.
        var configuracao = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Padrao"] = _conexao,
            ["AdminInicial:Nome"] = "Diana Diretora",
            ["AdminInicial:Email"] = "diana@x.com",
            ["AdminInicial:Senha"] = "senha-segura",
        };
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(configuracao)));
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fabrica.DisposeAsync();
        await banco.RemoverAsync(_conexao);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    private async Task<HttpResponseMessage> EntrarAsync(string email, string senha, string consulta = "")
    {
        var pagina = await _cliente.GetStringAsync("/conta/entrar" + consulta);
        var token = TokenRegex().Match(pagina).Groups[1].Value;
        token.Should().NotBeEmpty("o formulário de login deve trazer o token antifalsificação");

        return await _cliente.PostAsync("/conta/entrar" + consulta, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "entrar",
            ["__RequestVerificationToken"] = token,
            ["Entrada.Email"] = email,
            ["Entrada.Senha"] = senha,
        }));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/usuarios")]
    [InlineData("/clientes/0199a3f2-7c1e-7000-8000-000000000001")]
    [InlineData("/clientes/novo")]
    [InlineData("/clientes/0199a3f2-7c1e-7000-8000-000000000001/editar")]
    [InlineData("/perfis")]
    [InlineData("/minha-senha")]
    [InlineData("/rota-que-nao-existe")]
    public async Task Anonimo_e_redirecionado_ao_login_em_qualquer_rota(string rota)
    {
        var resposta = await _cliente.GetAsync(rota);

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }

    [Fact]
    public async Task Pagina_de_erro_e_anonima_generica_e_sem_detalhes_tecnicos()
    {
        var resposta = await _cliente.GetAsync("/erro");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
        html.Should().Contain("Ocorreu um erro inesperado.");
        html.Should().NotContain("Exception").And.NotContain("StackTrace").And.NotContain("   at ");
        html.Should().MatchRegex("<main class=\"aj-estado aj-estado--pagina\">\\s*<img[^>]*>\\s*<h1 class=\"aj-titulo\">Ocorreu um erro inesperado\\.</h1>")
            .And.Contain("<a href=\"/\" class=\"aj-botao aj-botao--secundario\">Voltar ao início</a>");
    }

    [Fact]
    public async Task Rota_inexistente_para_usuario_autenticado_mostra_pagina_nao_encontrada()
    {
        await EntrarAsync("diana@x.com", "senha-segura");

        var resposta = await _cliente.GetAsync("/rota-que-nao-existe");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var html = WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
        html.Should().Contain("Página não encontrada").And.Contain("href=\"/\"");
        var corpo = Regex.Match(html, "<main[^>]*>.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().MatchRegex("<div class=\"aj-estado\">\\s*<img[^>]*>\\s*<h1 class=\"aj-titulo\">Página não encontrada</h1>\\s*<p class=\"aj-subtitulo\">O endereço acessado não existe\\.</p>\\s*<a href=\"/\" class=\"aj-botao aj-botao--secundario\">Voltar ao início</a>");
        // No mobile vale a barra de logo: a tela não entrega barra própria.
        Regex.Matches(html, "<header class=\"aj-barra-superior").Should().ContainSingle();
        Regex.Matches(html, "<h1").Should().ContainSingle();
    }

    [Fact]
    public async Task Faixa_de_erro_e_aviso_de_reconexao_estao_em_portugues()
    {
        await EntrarAsync("diana@x.com", "senha-segura");

        var html = WebUtility.HtmlDecode(await _cliente.GetStringAsync("/"));

        html.Should().MatchRegex("<div id=\"blazor-error-ui\" class=\"aj-faixa-erro\"")
            .And.Contain("aria-label=\"Fechar aviso\"")
            .And.Contain("Reconectando ao servidor…").And.Contain("Tentar de novo").And.Contain("Retomar")
            .And.NotContain("Rejoin").And.NotContain("Retry").And.NotContain("Resume");
    }

    [Fact]
    public async Task Pagina_de_login_e_seu_css_sao_publicos()
    {
        (await _cliente.GetAsync("/conta/entrar")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _cliente.GetAsync("/app.css")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _cliente.GetAsync("/css/tokens.css")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _cliente.GetAsync("/css/componentes.css")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _cliente.GetAsync("/img/logo-vertical-ampla-escuro.svg")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Tela_de_login_segue_o_quadro()
    {
        var html = WebUtility.HtmlDecode(await _cliente.GetStringAsync("/conta/entrar"));

        html.Should().Contain("<span class=\"aj-entrar__sobretitulo\">Sistema interno</span>")
            .And.Contain("<p class=\"aj-entrar__nome\">Atividades Jurídicas</p>")
            .And.Contain("Agenda, prazos e atividades do escritório em um só lugar.")
            .And.Contain("<span class=\"aj-desktop-tablet\">Acesse com o e-mail e a senha do escritório.</span>")
            .And.Contain("<span class=\"aj-so-mobile\">Use o e-mail e a senha do escritório.</span>")
            .And.Contain("placeholder=\"nome@araujoadvocacia.com.br\"")
            .And.MatchRegex("<button type=\"submit\" class=\"aj-botao aj-botao--primario\"><span>Entrar</span>\\s*<svg");
        Regex.Matches(html, "Araújo Advocacia · uso interno").Should().HaveCount(2);
        // Um rodapé por visão: o de dentro do cartão (desktop) e o da página (tablet e mobile).
        html.Should().Contain("<p class=\"aj-entrar__rodape aj-entrar__rodape--cartao\">Araújo Advocacia · uso interno</p>")
            .And.Contain("<p class=\"aj-entrar__rodape aj-entrar__rodape--pagina\">Araújo Advocacia · uso interno</p>");
        Regex.Matches(html, "<main").Should().ContainSingle();
        Regex.Matches(html, "<h1").Should().ContainSingle();
        html.Should().NotContain("aj-aviso");
    }

    [Fact]
    public async Task Login_com_campos_vazios_marca_os_campos_e_mantem_o_formulario()
    {
        var resposta = await EntrarAsync("", "");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
        Regex.Matches(html, "<label class=\"aj-rotulo aj-rotulo--erro\">").Should().HaveCount(2);
        html.Should().Contain("<span class=\"aj-campo-erro\">Informe o e-mail.</span>")
            .And.Contain("<span class=\"aj-campo-erro\">Informe a senha.</span>");
        Regex.Matches(html, "aria-invalid=\"true\"").Should().HaveCount(2);
        // O formulário continua pronto para a próxima tentativa.
        TokenRegex().IsMatch(html).Should().BeTrue();
        html.Should().Contain("name=\"Entrada.Email\"").And.Contain("name=\"Entrada.Senha\"");
    }

    [Fact]
    public async Task Login_errado_mostra_o_aviso_de_erro_dentro_do_cartao()
    {
        var resposta = await EntrarAsync("diana@x.com", "senha-errada");

        var html = WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
        var cartao = Regex.Match(html, "<form[^>]*class=\"aj-entrar__cartao\".*?</form>", RegexOptions.Singleline).Value;
        cartao.Should().MatchRegex("<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">\\s*<svg.*?</svg>\\s*<span>E-mail ou senha inválidos\\.</span></p>");
        cartao.Should().NotContain("aj-rotulo--erro");
    }

    [Fact]
    public async Task Login_depois_da_troca_de_senha_mostra_o_aviso_de_sucesso()
    {
        var html = WebUtility.HtmlDecode(await _cliente.GetStringAsync("/conta/entrar?aviso=senha-alterada"));

        html.Should().MatchRegex("<p class=\"aj-aviso aj-aviso--sucesso\" role=\"status\">\\s*<svg.*?</svg>\\s*<span>Senha alterada\\. Entre novamente com a nova senha\\.</span></p>");
    }

    [Fact]
    public async Task Login_correto_redireciona_e_libera_a_pagina_inicial()
    {
        var resposta = await EntrarAsync("Diana@X.com", "senha-segura");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.OriginalString.Should().EndWith("/");

        var inicio = await _cliente.GetAsync("/");
        inicio.StatusCode.Should().Be(HttpStatusCode.OK);
        (await inicio.Content.ReadAsStringAsync()).Should().Contain("Diana Diretora");
    }

    [Fact]
    public async Task Login_errado_fica_na_pagina_com_mensagem_generica()
    {
        var resposta = await EntrarAsync("diana@x.com", "senha-errada");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync()).Should().Contain("E-mail ou senha inválidos.");
        (await _cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task ReturnUrl_externo_e_ignorado()
    {
        var resposta = await EntrarAsync("diana@x.com", "senha-segura", "?ReturnUrl=https%3A%2F%2Fmal.example");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.OriginalString.Should().NotContain("mal.example");
    }

    [Fact]
    public async Task ReturnUrl_local_e_respeitado()
    {
        var resposta = await EntrarAsync("diana@x.com", "senha-segura", "?ReturnUrl=%2Fminha-senha");

        resposta.Headers.Location!.OriginalString.Should().EndWith("/minha-senha");
    }

    [Fact]
    public async Task Sair_encerra_a_sessao()
    {
        await EntrarAsync("diana@x.com", "senha-segura");
        var inicio = await _cliente.GetStringAsync("/");
        var token = TokenRegex().Match(inicio).Groups[1].Value;

        var saida = await _cliente.PostAsync("/conta/sair", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        saida.StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await _cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Sair_sem_token_antifalsificacao_e_recusado()
    {
        await EntrarAsync("diana@x.com", "senha-segura");

        var saida = await _cliente.PostAsync("/conta/sair", new FormUrlEncodedContent(new Dictionary<string, string>()));

        saida.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Usuario_inativado_perde_o_acesso_na_requisicao_seguinte()
    {
        await EntrarAsync("diana@x.com", "senha-segura");
        (await _cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Inativa direto no banco: o caso de uso recusaria inativar o último diretor.
        await using (var escopo = _fabrica.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<Infrastructure.Persistencia.AppDbContext>();
            (await db.Usuarios.SingleAsync()).Inativar();
            await db.SaveChangesAsync();
        }

        (await _cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }
}
