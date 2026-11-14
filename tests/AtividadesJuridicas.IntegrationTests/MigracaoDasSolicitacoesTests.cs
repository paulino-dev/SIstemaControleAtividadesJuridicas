using AtividadesJuridicas.Application;
using AtividadesJuridicas.Infrastructure;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>A migration das solicitações do cliente aplicada a um banco que já tinha documentos.</summary>
[Collection(ColecaoBanco.Nome)]
public class MigracaoDasSolicitacoesTests(BancoFixture banco) : IAsyncLifetime
{
    private const string RestricaoDoCliente = "ck_documentos_cliente_conforme_a_categoria";
    private const string RestricaoDaSolicitacao = "ck_documentos_solicitacao_so_na_categoria_6";
    private const string IndiceDaChave = "ix_documentos_chave_armazenamento";

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
    public async Task Migration_troca_a_restricao_num_banco_que_ja_tem_documentos()
    {
        var cliente = Guid.CreateVersion7();
        var atividade = Guid.CreateVersion7();
        var geral = Guid.CreateVersion7();
        var contrato = Guid.CreateVersion7();
        var daAtividade = Guid.CreateVersion7();
        var doCliente = Guid.CreateVersion7();
        var comum = Guid.CreateVersion7();
        string indiceAntes;

        await using (var escopo = _raiz.CreateAsyncScope())
        {
            var migrador = escopo.ServiceProvider.GetRequiredService<AppDbContext>().GetService<IMigrator>();

            // O banco como estava antes desta fase, já com um cliente, uma atividade avulsa e um documento de cada
            // categoria antiga.
            await migrador.MigrateAsync("DocumentosDoPortal");
            await ComandoAsync(
                "INSERT INTO clientes (id, nome, tipo, cpf, ativo, criado_em) " +
                $"VALUES ('{cliente}', 'Maria Silva', 1, '52998224725', true, now())");
            await ComandoAsync(
                "INSERT INTO atividades (id, titulo, descricao, status, tipo_autor, complexidade, criado_em) " +
                $"VALUES ('{atividade}', 'Avulsa', 'Sem cliente.', 1, 2, 1, now())");
            await InserirDocumentoAsync(geral, 1, cliente, null);
            await InserirDocumentoAsync(contrato, 2, cliente, null);
            await InserirDocumentoAsync(daAtividade, 3, null, atividade);
            await InserirDocumentoAsync(doCliente, 4, cliente, null);
            await InserirDocumentoAsync(comum, 5, null, null);

            // Antes, a categoria 6 não existia para o banco, e a restrição velha a recusa.
            var antes = () => InserirDocumentoAsync(Guid.CreateVersion7(), 6, cliente, null);
            (await antes.Should().ThrowAsync<PostgresException>()).Which
                .Should().Match<PostgresException>(e => e.SqlState == PostgresErrorCodes.CheckViolation && e.ConstraintName == RestricaoDoCliente);
            (await ComandoAsync($"SELECT count(*) FROM pg_constraint WHERE conname = '{RestricaoDaSolicitacao}'")).Should().Be(0L);
            indiceAntes = (string)(await ComandoAsync($"SELECT indexdef FROM pg_indexes WHERE indexname = '{IndiceDaChave}'"))!;

            // Só até a migration das solicitações: a seguinte (AtividadeDaSolicitacao) troca o índice da chave.
            await migrador.MigrateAsync("SolicitacoesDoCliente");
        }

        // Nenhum dado foi tocado: os cinco documentos continuam como estavam, sem solicitação.
        (await ComandoAsync("SELECT count(*) FROM documentos")).Should().Be(5L);
        (await ComandoAsync("SELECT count(*) FROM documentos WHERE solicitacao_id IS NOT NULL")).Should().Be(0L);
        foreach (var (id, categoria) in new[] { (geral, 1), (contrato, 2), (daAtividade, 3), (doCliente, 4), (comum, 5) })
            (await ComandoAsync($"SELECT categoria FROM documentos WHERE id = '{id}'")).Should().Be(categoria);
        (await ComandoAsync($"SELECT cliente_id FROM documentos WHERE id = '{geral}'")).Should().Be(cliente);
        (await ComandoAsync($"SELECT cliente_id FROM documentos WHERE id = '{daAtividade}'")).Should().Be(DBNull.Value);
        (await ComandoAsync($"SELECT atividade_id FROM documentos WHERE id = '{daAtividade}'")).Should().Be(atividade);
        (await ComandoAsync($"SELECT cliente_id FROM documentos WHERE id = '{comum}'")).Should().Be(DBNull.Value);

        // A restrição do cliente foi trocada (continua uma só, e agora cita a categoria 6) e a nova existe.
        (await ComandoAsync($"SELECT count(*) FROM pg_constraint WHERE conname = '{RestricaoDoCliente}'")).Should().Be(1L);
        ((string)(await ComandoAsync($"SELECT pg_get_constraintdef(oid) FROM pg_constraint WHERE conname = '{RestricaoDoCliente}'"))!)
            .Should().Contain("6");
        (await ComandoAsync($"SELECT count(*) FROM pg_constraint WHERE conname = '{RestricaoDaSolicitacao}'")).Should().Be(1L);

        // O índice único da chave de armazenamento não mudou.
        (await ComandoAsync($"SELECT indexdef FROM pg_indexes WHERE indexname = '{IndiceDaChave}'")).Should().Be(indiceAntes);
        indiceAntes.Should().Contain("UNIQUE");

        // A tabela nova existe, e a chave estrangeira do documento não tem cascata.
        (await ComandoAsync("SELECT count(*) FROM information_schema.tables WHERE table_name = 'solicitacoes_do_cliente'"))
            .Should().Be(1L);
        (await ComandoAsync(
            "SELECT confdeltype FROM pg_constraint WHERE contype = 'f' AND conrelid = 'documentos'::regclass " +
            "AND confrelid = 'solicitacoes_do_cliente'::regclass")).Should().Be('r');

        var solicitacao = await InserirSolicitacaoAsync(cliente);

        var semSolicitacao = () => InserirDocumentoAsync(Guid.CreateVersion7(), 6, cliente, null);
        var geralComSolicitacao = () => InserirDocumentoAsync(Guid.CreateVersion7(), 1, cliente, null, solicitacao);
        var semCliente = () => InserirDocumentoAsync(Guid.CreateVersion7(), 6, null, null, solicitacao);
        (await semSolicitacao.Should().ThrowAsync<PostgresException>()).Which.Should().Match<PostgresException>(
            e => e.SqlState == PostgresErrorCodes.CheckViolation && e.ConstraintName == RestricaoDaSolicitacao);
        (await geralComSolicitacao.Should().ThrowAsync<PostgresException>()).Which.Should().Match<PostgresException>(
            e => e.SqlState == PostgresErrorCodes.CheckViolation && e.ConstraintName == RestricaoDaSolicitacao);
        (await semCliente.Should().ThrowAsync<PostgresException>()).Which.Should().Match<PostgresException>(
            e => e.SqlState == PostgresErrorCodes.CheckViolation && e.ConstraintName == RestricaoDoCliente);

        // O que já valia continua valendo.
        var comumComCliente = () => InserirDocumentoAsync(Guid.CreateVersion7(), 5, cliente, null);
        (await comumComCliente.Should().ThrowAsync<PostgresException>()).Which.Should().Match<PostgresException>(
            e => e.SqlState == PostgresErrorCodes.CheckViolation && e.ConstraintName == RestricaoDoCliente);

        await InserirDocumentoAsync(Guid.CreateVersion7(), 6, cliente, null, solicitacao);
        await InserirDocumentoAsync(Guid.CreateVersion7(), 4, cliente, null);
        (await ComandoAsync("SELECT count(*) FROM documentos")).Should().Be(7L);
        (await ComandoAsync($"SELECT count(*) FROM documentos WHERE solicitacao_id = '{solicitacao}'")).Should().Be(1L);
    }

    /// <summary>Uma solicitação do cliente, com um usuário qualquer como solicitante (o teste é do esquema).</summary>
    private async Task<Guid> InserirSolicitacaoAsync(Guid cliente)
    {
        var perfil = Guid.CreateVersion7();
        var usuario = Guid.CreateVersion7();
        var solicitacao = Guid.CreateVersion7();
        await ComandoAsync(
            "INSERT INTO perfis (id, ativo, criado_em, nome, tipo) " +
            $"VALUES ('{perfil}', true, now(), 'Cliente', 3)");
        await ComandoAsync(
            "INSERT INTO usuarios (id, ativo, criado_em, email, falhas_de_login, nome, perfil_id, senha_hash, versao_de_sessao, cliente_id) " +
            $"VALUES ('{usuario}', true, now(), 'carla@x.com', 0, 'Carla Cliente', '{perfil}', 'hash', '{Guid.NewGuid()}', '{cliente}')");
        await ComandoAsync(
            "INSERT INTO solicitacoes_do_cliente (id, cliente_id, solicitante_id, descricao, urgencia, versao_dos_documentos, criado_em) " +
            $"VALUES ('{solicitacao}', '{cliente}', '{usuario}', 'Preciso de uma certidão.', 2, 0, now())");
        return solicitacao;
    }

    private Task<object?> InserirDocumentoAsync(Guid id, int categoria, Guid? cliente, Guid? atividade, Guid? solicitacao = null)
    {
        // Antes da migration a coluna da solicitação não existe: só entra no comando quando há o que gravar nela.
        var colunaDaSolicitacao = solicitacao is null ? string.Empty : ", solicitacao_id";
        var valorDaSolicitacao = solicitacao is { } s ? $", '{s}'" : string.Empty;
        return ComandoAsync(
            "INSERT INTO documentos (id, nome, nome_arquivo_original, tipo_conteudo, tamanho_bytes, hash_sha256, " +
            $"chave_armazenamento, categoria, cliente_id, atividade_id, criado_em{colunaDaSolicitacao}) " +
            $"VALUES ('{id}', 'x', 'x.pdf', 'application/pdf', 1, '{new string('a', 64)}', '2026/11/{id:N}', {categoria}, " +
            $"{(cliente is { } c ? $"'{c}'" : "NULL")}, {(atividade is { } a ? $"'{a}'" : "NULL")}, now(){valorDaSolicitacao})");
    }

    private async Task<object?> ComandoAsync(string sql)
    {
        await using var conexao = new NpgsqlConnection(_conexao);
        await conexao.OpenAsync();
        await using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        return await comando.ExecuteScalarAsync();
    }
}
