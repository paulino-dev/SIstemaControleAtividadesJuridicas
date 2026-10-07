using Npgsql;
using Testcontainers.PostgreSql;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Um contêiner PostgreSQL para toda a execução; cada teste pede um banco novo.</summary>
public sealed class BancoFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _conteiner = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public Task InitializeAsync() => _conteiner.StartAsync();

    public Task DisposeAsync() => _conteiner.DisposeAsync().AsTask();

    public string NovaConexao() =>
        new NpgsqlConnectionStringBuilder(_conteiner.GetConnectionString())
        {
            Database = "t_" + Guid.NewGuid().ToString("N"),
        }.ConnectionString;
}

[CollectionDefinition(Nome)]
public sealed class ColecaoBanco : ICollectionFixture<BancoFixture>
{
    public const string Nome = "banco";
}
