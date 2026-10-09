using AtividadesJuridicas.Application;
using AtividadesJuridicas.Infrastructure;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>A migration dos documentos do portal aplicada a um banco que já tinha documentos.</summary>
[Collection(ColecaoBanco.Nome)]
public class MigracaoDosDocumentosDoPortalTests(BancoFixture banco) : IAsyncLifetime
{
    private const string Restricao = "ck_documentos_cliente_conforme_a_categoria";

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
    public async Task Migration_troca_a_restricao_do_cliente_num_banco_que_ja_tem_documentos()
    {
        var cliente = Guid.CreateVersion7();
        var atividade = Guid.CreateVersion7();
        var geral = Guid.CreateVersion7();
        var contrato = Guid.CreateVersion7();
        var daAtividade = Guid.CreateVersion7();

        await using (var escopo = _raiz.CreateAsyncScope())
        {
            var migrador = escopo.ServiceProvider.GetRequiredService<AppDbContext>().GetService<IMigrator>();

            // O banco como estava antes desta fase, já com um cliente, uma atividade avulsa e um documento de cada
            // categoria antiga.
            await migrador.MigrateAsync("ComplexidadeDaAtividade");
            await ComandoAsync(
                "INSERT INTO clientes (id, nome, tipo, cpf, ativo, criado_em) " +
                $"VALUES ('{cliente}', 'Maria Silva', 1, '52998224725', true, now())");
            await ComandoAsync(
                "INSERT INTO atividades (id, titulo, descricao, status, tipo_autor, complexidade, criado_em) " +
                $"VALUES ('{atividade}', 'Avulsa', 'Sem cliente.', 1, 2, 1, now())");
            await InserirDocumentoAsync(geral, 1, cliente, null);
            await InserirDocumentoAsync(contrato, 2, cliente, null);
            await InserirDocumentoAsync(daAtividade, 3, null, atividade);

            await migrador.MigrateAsync();
        }

        (await ComandoAsync("SELECT count(*) FROM documentos")).Should().Be(3L);
        (await ComandoAsync($"SELECT categoria FROM documentos WHERE id = '{geral}'")).Should().Be(1);
        (await ComandoAsync($"SELECT cliente_id FROM documentos WHERE id = '{geral}'")).Should().Be(cliente);
        (await ComandoAsync($"SELECT categoria FROM documentos WHERE id = '{contrato}'")).Should().Be(2);
        (await ComandoAsync($"SELECT cliente_id FROM documentos WHERE id = '{contrato}'")).Should().Be(cliente);
        (await ComandoAsync($"SELECT categoria FROM documentos WHERE id = '{daAtividade}'")).Should().Be(3);
        (await ComandoAsync($"SELECT cliente_id FROM documentos WHERE id = '{daAtividade}'")).Should().Be(DBNull.Value);
        (await ComandoAsync($"SELECT atividade_id FROM documentos WHERE id = '{daAtividade}'")).Should().Be(atividade);

        (await ComandoAsync(
            "SELECT count(*) FROM pg_constraint WHERE conname = 'ck_documentos_cliente_obrigatorio'")).Should().Be(0L);
        (await ComandoAsync(
            $"SELECT count(*) FROM pg_constraint WHERE conname = '{Restricao}'")).Should().Be(1L);

        var comumComCliente = () => InserirDocumentoAsync(Guid.CreateVersion7(), 5, cliente, null);
        var doClienteSemCliente = () => InserirDocumentoAsync(Guid.CreateVersion7(), 4, null, null);
        (await comumComCliente.Should().ThrowAsync<PostgresException>()).Which
            .Should().Match<PostgresException>(e => e.SqlState == PostgresErrorCodes.CheckViolation && e.ConstraintName == Restricao);
        (await doClienteSemCliente.Should().ThrowAsync<PostgresException>()).Which
            .Should().Match<PostgresException>(e => e.SqlState == PostgresErrorCodes.CheckViolation && e.ConstraintName == Restricao);

        await InserirDocumentoAsync(Guid.CreateVersion7(), 5, null, null);
        await InserirDocumentoAsync(Guid.CreateVersion7(), 4, cliente, null);
        (await ComandoAsync("SELECT count(*) FROM documentos")).Should().Be(5L);
    }

    private Task<object?> InserirDocumentoAsync(Guid id, int categoria, Guid? cliente, Guid? atividade) =>
        ComandoAsync(
            "INSERT INTO documentos (id, nome, nome_arquivo_original, tipo_conteudo, tamanho_bytes, hash_sha256, " +
            "chave_armazenamento, categoria, cliente_id, atividade_id, criado_em) " +
            $"VALUES ('{id}', 'x', 'x.pdf', 'application/pdf', 1, '{new string('a', 64)}', '2026/10/{id:N}', {categoria}, " +
            $"{(cliente is { } c ? $"'{c}'" : "NULL")}, {(atividade is { } a ? $"'{a}'" : "NULL")}, now())");

    private async Task<object?> ComandoAsync(string sql)
    {
        await using var conexao = new NpgsqlConnection(_conexao);
        await conexao.OpenAsync();
        await using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        return await comando.ExecuteScalarAsync();
    }
}
