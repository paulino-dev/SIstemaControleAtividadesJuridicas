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
    private HttpClient _cliente = null!;

    public Task InitializeAsync()
    {
        // Acrescentada por último, esta fonte vence o appsettings.Development.json.
        var configuracao = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Padrao"] = banco.NovaConexao(),
            ["AdminInicial:Nome"] = "Diana Diretora",
            ["AdminInicial:Email"] = "diana@x.com",
            ["AdminInicial:Senha"] = "senha-segura",
        };
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(configuracao)));
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _fabrica.DisposeAsync();

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
    [InlineData("/minha-senha")]
    [InlineData("/rota-que-nao-existe")]
    public async Task Anonimo_e_redirecionado_ao_login_em_qualquer_rota(string rota)
    {
        var resposta = await _cliente.GetAsync(rota);

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }

    [Fact]
    public async Task Pagina_de_login_e_seu_css_sao_publicos()
    {
        (await _cliente.GetAsync("/conta/entrar")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _cliente.GetAsync("/app.css")).StatusCode.Should().Be(HttpStatusCode.OK);
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
