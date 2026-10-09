using AtividadesJuridicas.Application;
using AtividadesJuridicas.Infrastructure;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>A migration da complexidade aplicada a um banco que já tinha atividades.</summary>
[Collection(ColecaoBanco.Nome)]
public class MigracaoDeComplexidadeTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly string _conexao = banco.NovaConexao();
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "atividades-testes", Guid.NewGuid().ToString("N"));
    private ServiceProvider _raiz = null!;

    public Task InitializeAsync()
    {
        _raiz = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(_ => _conexao, _ => _pasta, criarPastaDeArquivos: true)
            .BuildServiceProvider();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _raiz.DisposeAsync();
            if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
        }
        finally
        {
            await banco.RemoverAsync(_conexao);
        }
    }

    [Fact]
    public async Task Atividade_que_ja_existia_recebe_complexidade_1_e_a_coluna_nao_guarda_valor_padrao()
    {
        var id = Guid.CreateVersion7();

        await using (var escopo = _raiz.CreateAsyncScope())
        {
            var migrador = escopo.ServiceProvider.GetRequiredService<AppDbContext>().GetService<IMigrator>();

            // O banco como estava antes desta funcionalidade, já com uma atividade gravada.
            await migrador.MigrateAsync("IndicesDeAuditoria");
            await ComandoAsync(
                "INSERT INTO atividades (id, titulo, descricao, status, tipo_autor, criado_em) " +
                "VALUES (@id, 'Antiga', 'Criada antes da complexidade.', 1, 2, now())", id);

            await migrador.MigrateAsync();
        }

        (await ComandoAsync("SELECT complexidade FROM atividades WHERE id = @id", id)).Should().Be(1);
        (await ComandoAsync(
            "SELECT column_default FROM information_schema.columns " +
            "WHERE table_name = 'atividades' AND column_name = 'complexidade'")).Should().Be(DBNull.Value);
    }

    private async Task<object?> ComandoAsync(string sql, Guid? id = null)
    {
        await using var conexao = new NpgsqlConnection(_conexao);
        await conexao.OpenAsync();
        await using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        if (id is { } valor) comando.Parameters.AddWithValue("id", valor);
        return await comando.ExecuteScalarAsync();
    }
}
