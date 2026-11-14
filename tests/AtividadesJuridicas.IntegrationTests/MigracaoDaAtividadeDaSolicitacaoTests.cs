using AtividadesJuridicas.Application;
using AtividadesJuridicas.Infrastructure;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A migration que liga a atividade à solicitação do cliente e deixa de exigir chave de armazenamento única,
/// aplicada a um banco que já tinha atividades e documentos.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public class MigracaoDaAtividadeDaSolicitacaoTests(BancoFixture banco) : IAsyncLifetime
{
    private const string Anterior = "SolicitacoesDoCliente";
    private const string Migration = "AtividadeDaSolicitacao";
    private const string IndiceDaChave = "ix_documentos_chave_armazenamento";
    private const string IndiceDaSolicitacao = "ix_atividades_solicitacao_id";

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
    public async Task Migration_deixa_solicitacao_id_nulo_na_atividade_que_ja_existia()
    {
        var cliente = Guid.CreateVersion7();
        var atividade = Guid.CreateVersion7();

        // O banco como estava antes desta migration, com um cliente e uma atividade dele.
        await MigrarAsync(Anterior);
        await ComandoAsync(
            "INSERT INTO clientes (id, nome, tipo, cpf, ativo, criado_em) " +
            $"VALUES ('{cliente}', 'Maria Silva', 1, '52998224725', true, now())");
        await ComandoAsync(
            "INSERT INTO atividades (id, titulo, descricao, status, tipo_autor, complexidade, cliente_id, criado_em) " +
            $"VALUES ('{atividade}', 'Certidão', 'Texto.', 1, 2, 1, '{cliente}', now())");
        (await ComandoAsync("SELECT count(*) FROM information_schema.columns " +
            "WHERE table_name = 'atividades' AND column_name = 'solicitacao_id'")).Should().Be(0L);
        (await ComandoAsync($"SELECT count(*) FROM pg_indexes WHERE indexname = '{IndiceDaSolicitacao}'")).Should().Be(0L);

        await MigrarAsync(Migration);

        // A atividade continua como estava, sem solicitação.
        (await ComandoAsync("SELECT count(*) FROM atividades")).Should().Be(1L);
        (await ComandoAsync($"SELECT titulo FROM atividades WHERE id = '{atividade}'")).Should().Be("Certidão");
        (await ComandoAsync($"SELECT cliente_id FROM atividades WHERE id = '{atividade}'")).Should().Be(cliente);
        (await ComandoAsync($"SELECT solicitacao_id FROM atividades WHERE id = '{atividade}'")).Should().Be(DBNull.Value);

        // A coluna é opcional, a chave estrangeira não tem cascata e o índice é único só entre as atividades vivas.
        (await ComandoAsync("SELECT is_nullable FROM information_schema.columns " +
            "WHERE table_name = 'atividades' AND column_name = 'solicitacao_id'")).Should().Be("YES");
        (await ComandoAsync(
            "SELECT confdeltype FROM pg_constraint WHERE contype = 'f' AND conrelid = 'atividades'::regclass " +
            "AND confrelid = 'solicitacoes_do_cliente'::regclass")).Should().Be('r');
        var indice = (string)(await ComandoAsync($"SELECT indexdef FROM pg_indexes WHERE indexname = '{IndiceDaSolicitacao}'"))!;
        indice.Should().Contain("UNIQUE").And.Contain("(solicitacao_id)").And.Contain("excluido_em IS NULL");
    }

    [Fact]
    public async Task Indice_da_chave_de_armazenamento_deixa_de_ser_unico()
    {
        await MigrarAsync(Anterior);
        var antes = (string)(await ComandoAsync($"SELECT indexdef FROM pg_indexes WHERE indexname = '{IndiceDaChave}'"))!;
        antes.Should().Contain("UNIQUE");
        const string chave = "2026/11/arquivo-compartilhado";
        await InserirDocumentoComumAsync(chave);
        var antesDaMigration = () => InserirDocumentoComumAsync(chave);
        (await antesDaMigration.Should().ThrowAsync<PostgresException>()).Which.Should().Match<PostgresException>(
            e => e.SqlState == PostgresErrorCodes.UniqueViolation && e.ConstraintName == IndiceDaChave);

        await MigrarAsync(Migration);

        // Continua havendo um índice na chave (a busca pelos documentos de um arquivo precisa dele), mas comum.
        var depois = (string)(await ComandoAsync($"SELECT indexdef FROM pg_indexes WHERE indexname = '{IndiceDaChave}'"))!;
        depois.Should().NotContain("UNIQUE").And.Contain("(chave_armazenamento)");
        await InserirDocumentoComumAsync(chave);
        (await ComandoAsync($"SELECT count(*) FROM documentos WHERE chave_armazenamento = '{chave}'")).Should().Be(2L);
    }

    [Fact]
    public async Task Down_num_banco_sem_chave_compartilhada_volta_o_indice_unico()
    {
        await MigrarAsync(Migration);
        await InserirDocumentoComumAsync("2026/11/um");
        await InserirDocumentoComumAsync("2026/11/dois");

        await MigrarAsync(Anterior);

        var indice = (string)(await ComandoAsync($"SELECT indexdef FROM pg_indexes WHERE indexname = '{IndiceDaChave}'"))!;
        indice.Should().Contain("UNIQUE").And.Contain("(chave_armazenamento)");
        var repetida = () => InserirDocumentoComumAsync("2026/11/um");
        (await repetida.Should().ThrowAsync<PostgresException>()).Which.Should().Match<PostgresException>(
            e => e.SqlState == PostgresErrorCodes.UniqueViolation && e.ConstraintName == IndiceDaChave);

        // O que a migration criou saiu, e os documentos ficaram.
        (await ComandoAsync("SELECT count(*) FROM information_schema.columns " +
            "WHERE table_name = 'atividades' AND column_name = 'solicitacao_id'")).Should().Be(0L);
        (await ComandoAsync($"SELECT count(*) FROM pg_indexes WHERE indexname = '{IndiceDaSolicitacao}'")).Should().Be(0L);
        (await ComandoAsync("SELECT count(*) FROM documentos")).Should().Be(2L);
    }

    private async Task MigrarAsync(string alvo)
    {
        await using var escopo = _raiz.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<AppDbContext>().GetService<IMigrator>().MigrateAsync(alvo);
    }

    /// <summary>Um documento comum (categoria 5, sem cliente nem atividade): serve para o teste do índice da chave.</summary>
    private Task<object?> InserirDocumentoComumAsync(string chave) =>
        ComandoAsync(
            "INSERT INTO documentos (id, nome, nome_arquivo_original, tipo_conteudo, tamanho_bytes, hash_sha256, " +
            "chave_armazenamento, categoria, criado_em) " +
            $"VALUES ('{Guid.CreateVersion7()}', 'x', 'x.pdf', 'application/pdf', 1, '{new string('a', 64)}', '{chave}', 5, now())");

    private async Task<object?> ComandoAsync(string sql)
    {
        await using var conexao = new NpgsqlConnection(_conexao);
        await conexao.OpenAsync();
        await using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        return await comando.ExecuteScalarAsync();
    }
}
