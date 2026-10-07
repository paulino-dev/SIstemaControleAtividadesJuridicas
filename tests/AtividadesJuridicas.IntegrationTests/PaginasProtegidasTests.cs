using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public partial class PaginasProtegidasTests(BancoFixture banco) : IAsyncLifetime
{
    private WebApplicationFactory<Program> _fabrica = null!;

    public async Task InitializeAsync()
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

        // Cria um usuário de Produção agindo como a diretora semeada.
        await using var escopo = _fabrica.Services.CreateAsyncScope();
        var sp = escopo.ServiceProvider;
        var diana = await sp.GetRequiredService<IUsuarioRepositorio>().ObterPorEmailAsync(Email.Criar("diana@x.com"), default);
        sp.GetRequiredService<IdentidadeAtual>().Definir(diana!.Id);
        var producao = (await sp.GetRequiredService<IPerfilRepositorio>().ListarAtivosAsync(default))
            .Single(p => p.Tipo == TipoPerfil.Producao);
        var criado = await sp.GetRequiredService<IManipulador<CriarUsuario, Resultado<Guid>>>().ManipularAsync(
            new CriarUsuario("Pedro Produção", "pedro@x.com", "senha-do-pedro", producao.Id, null), default);
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
    }

    public async Task DisposeAsync() => await _fabrica.DisposeAsync();

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    private async Task<HttpClient> ClienteAutenticadoAsync(string email, string senha)
    {
        var cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var pagina = await cliente.GetStringAsync("/conta/entrar");
        var resposta = await cliente.PostAsync("/conta/entrar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "entrar",
            ["__RequestVerificationToken"] = TokenRegex().Match(pagina).Groups[1].Value,
            ["Entrada.Email"] = email,
            ["Entrada.Senha"] = senha,
        }));
        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return cliente;
    }

    [Fact]
    public async Task Diretor_ve_a_lista_de_usuarios_sem_nenhum_hash_de_senha()
    {
        var cliente = await ClienteAutenticadoAsync("diana@x.com", "senha-segura");

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/usuarios"));

        html.Should().Contain("Diana Diretora").And.Contain("pedro@x.com").And.Contain("Novo usuário");
        html.Should().NotContain("pbkdf2-sha256");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_dados_de_usuarios()
    {
        var cliente = await ClienteAutenticadoAsync("pedro@x.com", "senha-do-pedro");

        var html = await cliente.GetStringAsync("/usuarios");

        html.Should().Contain("Acesso negado");
        html.Should().NotContain("diana@x.com").And.NotContain("Novo usuário");
    }

    [Fact]
    public async Task Menu_de_producao_nao_mostra_o_link_de_usuarios()
    {
        var cliente = await ClienteAutenticadoAsync("pedro@x.com", "senha-do-pedro");

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/"));

        html.Should().Contain("Pedro Produção").And.NotContain("href=\"/usuarios\"");
    }

    [Fact]
    public async Task Qualquer_usuario_autenticado_abre_a_troca_de_senha()
    {
        var cliente = await ClienteAutenticadoAsync("pedro@x.com", "senha-do-pedro");

        var html = await cliente.GetStringAsync("/minha-senha");

        html.Should().Contain("Senha atual");
    }
}
