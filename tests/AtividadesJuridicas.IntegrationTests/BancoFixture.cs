using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Um contêiner PostgreSQL para toda a execução; cada teste pede um banco novo.</summary>
public sealed class BancoFixture : IAsyncLifetime
{
    private const string PortaDoBanco = "5432/tcp";

    // No Windows com WSL, uma porta publicada só em 0.0.0.0 também é assumida em 127.0.0.1 pelo
    // encaminhador do WSL (wslrelay), e nesta máquina de desenvolvimento esse caminho derruba as
    // conexões. Publicando explicitamente em 127.0.0.1, quem escuta é o próprio Docker.
    //
    // O Ryuk (limpeza automática do Testcontainers) publica a sua porta do jeito padrão e por isso
    // não inicializa aqui; fica desligado. Sem ele, um processo de teste morto pode deixar o
    // contêiner para trás: use `docker ps` para encontrá-lo.
    static BancoFixture() => TestcontainersSettings.ResourceReaperEnabled = false;

    private readonly PostgreSqlContainer _conteiner = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithCreateParameterModifier(parametros => parametros.HostConfig.PortBindings =
            new Dictionary<string, IList<PortBinding>>
            {
                [PortaDoBanco] = [new PortBinding { HostIP = "127.0.0.1", HostPort = string.Empty }],
            })
        .Build();

    public async Task InitializeAsync()
    {
        await _conteiner.StartAsync();

        // Só segue quando uma conexão real abre pelo host, que é o caminho que os testes usam.
        var limite = DateTime.UtcNow.AddSeconds(60);
        while (true)
        {
            try
            {
                await using var conexao = new NpgsqlConnection(_conteiner.GetConnectionString());
                await conexao.OpenAsync();
                return;
            }
            catch (NpgsqlException) when (DateTime.UtcNow < limite)
            {
                await Task.Delay(500);
            }
        }
    }

    public Task DisposeAsync() => _conteiner.DisposeAsync().AsTask();

    // Cada classe de teste usa um banco próprio e, com ele, um pool de conexões próprio. Sem estes
    // limites as conexões ociosas das classes já encerradas se acumulam até estourar o máximo do
    // servidor (erro 53300, "too many clients").
    public string NovaConexao() =>
        new NpgsqlConnectionStringBuilder(_conteiner.GetConnectionString())
        {
            Database = "t_" + Guid.NewGuid().ToString("N"),
            MaxPoolSize = 25,
            ConnectionIdleLifetime = 5,
            ConnectionPruningInterval = 2,
            // Folga para máquina compartilhada: criar o banco de cada teste pode demorar sob carga.
            Timeout = 60,
            CommandTimeout = 180,
        }.ConnectionString;
}

[CollectionDefinition(Nome)]
public sealed class ColecaoBanco : ICollectionFixture<BancoFixture>
{
    public const string Nome = "banco";
}
