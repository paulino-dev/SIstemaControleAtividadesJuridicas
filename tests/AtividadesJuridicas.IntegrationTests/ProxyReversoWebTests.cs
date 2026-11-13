using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Atrás de um proxy reverso (túnel do VS Code, IIS, nginx), o endereço que o navegador usa chega nos cabeçalhos
/// X-Forwarded-*; a aplicação só confia neles quando a configuração manda.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public class ProxyReversoWebTests(BancoFixture banco) : IAsyncLifetime
{
    private const string EnderecoDoTunel = "exemplo-5133.brs.devtunnels.ms";

    private readonly List<WebApplicationFactory<Program>> _fabricas = [];
    private readonly List<string> _conexoes = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var fabrica in _fabricas) await fabrica.DisposeAsync();
        foreach (var conexao in _conexoes) await banco.RemoverAsync(conexao);
    }

    private HttpClient Cliente(bool confiarNosCabecalhos)
    {
        var conexao = banco.NovaConexao();
        _conexoes.Add(conexao);
        // Acrescentada por último, esta fonte vence o appsettings.Development.json.
        var configuracao = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Padrao"] = conexao,
            ["ProxyReverso:ConfiarNosCabecalhos"] = confiarNosCabecalhos ? "true" : null,
        };
        var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(configuracao)));
        _fabricas.Add(fabrica);
        return fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static Task<HttpResponseMessage> PedirPeloTunelAsync(HttpClient cliente)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Get, "/");
        pedido.Headers.Add("X-Forwarded-Host", EnderecoDoTunel);
        pedido.Headers.Add("X-Forwarded-Proto", "https");
        return cliente.SendAsync(pedido);
    }

    [Fact]
    public async Task Com_a_configuracao_o_login_volta_para_o_endereco_do_proxy()
    {
        var resposta = await PedirPeloTunelAsync(Cliente(confiarNosCabecalhos: true));

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.GetLeftPart(UriPartial.Path).Should().Be($"https://{EnderecoDoTunel}/conta/entrar");
    }

    [Fact]
    public async Task Sem_a_configuracao_os_cabecalhos_do_proxy_sao_ignorados()
    {
        var resposta = await PedirPeloTunelAsync(Cliente(confiarNosCabecalhos: false));

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.GetLeftPart(UriPartial.Path).Should().Be("http://localhost/conta/entrar");
    }
}
