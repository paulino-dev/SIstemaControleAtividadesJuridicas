using System.Diagnostics;
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
        // O diretório de dados em memória evita que o CREATE DATABASE de cada teste espere o disco,
        // que nesta máquina compartilhada chega a travar sob carga.
        .WithTmpfsMount("/var/lib/postgresql/data")
        // Permite achar e remover contêineres deixados por execuções interrompidas.
        .WithLabel(RotuloDoConteiner, "banco")
        .Build();

    private const string RotuloDoConteiner = "atividades-juridicas.testes";

    public async Task InitializeAsync()
    {
        await RemoverConteineresOrfaosAsync();
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

    // Sem o Ryuk, uma execução morta deixa o contêiner rodando; a próxima remove os que tiverem o rótulo.
    private static async Task RemoverConteineresOrfaosAsync()
    {
        try
        {
            var listar = await ExecutarDockerAsync("ps -aq --filter label=" + RotuloDoConteiner);
            var ids = listar.Split([(char)13, (char)10], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (ids.Length > 0) await ExecutarDockerAsync("rm -f " + string.Join(' ', ids));
        }
        catch (Exception)
        {
            // A limpeza é só um cuidado: não pode derrubar os testes.
        }
    }

    private static async Task<string> ExecutarDockerAsync(string argumentos)
    {
        using var processo = Process.Start(new ProcessStartInfo("docker", argumentos)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;
        var saida = await processo.StandardOutput.ReadToEndAsync();
        await processo.WaitForExitAsync();
        return saida;
    }

    /// <summary>Solta o pool de conexões e apaga o banco de um teste; falhas são ignoradas.</summary>
    public async Task RemoverAsync(string conexao)
    {
        try
        {
            var nome = new NpgsqlConnectionStringBuilder(conexao).Database!;
            using (var alvo = new NpgsqlConnection(conexao)) NpgsqlConnection.ClearPool(alvo);

            await using var admin = new NpgsqlConnection(_conteiner.GetConnectionString());
            await admin.OpenAsync();
            await using var comando = admin.CreateCommand();
            comando.CommandText = $"DROP DATABASE IF EXISTS \"{nome}\" WITH (FORCE)";
            await comando.ExecuteNonQueryAsync();
        }
        catch (Exception)
        {
            // Um banco que sobra não invalida o teste; some com o contêiner.
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
