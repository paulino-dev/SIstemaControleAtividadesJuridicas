using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>A aplicação Web inteira contra um banco novo, com atalhos para os testes de página.</summary>
public sealed partial class AplicacaoDeTeste(BancoFixture banco) : WebApplicationFactory<Program>
{
    private readonly string conexao = banco.NovaConexao();

    public const string EmailDoAdmin = "diana@x.com";
    public const string SenhaDoAdmin = "senha-segura";
    public const string EmailDoPedro = "pedro@x.com";
    public const string SenhaDoPedro = "senha-do-pedro";
    public const string IpDeTeste = "203.0.113.7";

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await banco.RemoverAsync(conexao);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Acrescentada por último, esta fonte vence o appsettings.Development.json.
        builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Padrao"] = conexao,
            ["AdminInicial:Nome"] = "Diana Diretora",
            ["AdminInicial:Email"] = EmailDoAdmin,
            ["AdminInicial:Senha"] = SenhaDoAdmin,
        }));

        // O TestServer não define o endereço remoto; toda requisição passa a vir de um IP conhecido.
        builder.ConfigureServices(s => s.AddTransient<IStartupFilter, IpFixoStartupFilter>());
    }

    private sealed class IpFixoStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> proximo) => app =>
        {
            app.Use((contexto, seguir) =>
            {
                contexto.Connection.RemoteIpAddress = IPAddress.Parse(IpDeTeste);
                return seguir();
            });
            proximo(app);
        };
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    public static string Token(string html) => TokenRegex().Match(html).Groups[1].Value;

    public HttpClient NovoCliente() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public static async Task<HttpResponseMessage> EntrarAsync(HttpClient cliente, string email, string senha)
    {
        var pagina = await cliente.GetStringAsync("/conta/entrar");
        return await cliente.PostAsync("/conta/entrar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "entrar",
            ["__RequestVerificationToken"] = Token(pagina),
            ["Entrada.Email"] = email,
            ["Entrada.Senha"] = senha,
        }));
    }

    public async Task<HttpClient> ClienteAutenticadoAsync(string email = EmailDoAdmin, string senha = SenhaDoAdmin)
    {
        var cliente = NovoCliente();
        (await EntrarAsync(cliente, email, senha)).StatusCode.Should().Be(HttpStatusCode.Redirect);
        return cliente;
    }

    public async Task<T> EmEscopoAsync<T>(Guid? usuarioId, Func<IServiceProvider, Task<T>> acao)
    {
        await using var escopo = Services.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<IdentidadeAtual>().DefinirInterno(usuarioId);
        return await acao(escopo.ServiceProvider);
    }

    public Task<TResposta> ExecutarAsync<TResposta>(Guid? usuarioId, IRequisicao<TResposta> requisicao) =>
        EmEscopoAsync(usuarioId, sp => sp.GetRequiredService<ExecutorDeRequisicoes>().ExecutarAsync(requisicao, default));

    public Task<Guid> IdDoUsuarioAsync(string email) => EmEscopoAsync(null, async sp =>
        (await sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking()
            .SingleAsync(u => u.Email == Email.Criar(email))).Id);

    public async Task<Guid> CriarPedroAsync()
    {
        var adminId = await IdDoUsuarioAsync(EmailDoAdmin);
        var producao = await EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
                .SingleAsync(p => p.Tipo == TipoPerfil.Producao)).Id);

        var criado = await ExecutarAsync(adminId,
            new CriarUsuario("Pedro Produção", EmailDoPedro, SenhaDoPedro, producao, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    public Task<List<RegistroAuditoria>> EventosAsync(string evento) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAuditoria.AsNoTracking()
            .Where(r => r.Evento == evento).OrderBy(r => r.Id).ToListAsync());
}
