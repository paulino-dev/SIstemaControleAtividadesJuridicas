using System.Text;
using System.Text.Json;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class DocumentosTests(BancoFixture banco) : TesteComBanco(banco)
{
    private Guid _admin, _maria, _jose, _perfilDiretor, _perfilProducao, _pedro;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        _perfilDiretor = await IdDoPerfilAsync(TipoPerfil.Diretoria);
        _perfilProducao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _maria = await PfAsync("Maria Silva", "529.982.247-25");
        _jose = await PfAsync("José Souza", "111.444.777-35");
        _pedro = (await ExecutarAsync(_admin,
            new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", _perfilProducao, null))).Valor;
    }

    private async Task<Guid> PfAsync(string nome, string cpf) =>
        (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada(nome, null, null, new EnderecoEntrada(), null), cpf, null, null))).Valor;

    private async Task<Guid> EnviarAsync(CategoriaDocumento categoria = CategoriaDocumento.DocumentoGeral,
        string arquivo = "procuracao.pdf", string conteudo = "conteúdo do arquivo", Guid? cliente = null, string? nome = null)
    {
        var resultado = await ExecutarAsync(_admin, new EnviarDocumento(
            categoria, cliente ?? _maria, nome, arquivo, "application/pdf", new MemoryStream(Encoding.UTF8.GetBytes(conteudo))));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private async Task<string?> BaixarTextoAsync(Guid usuario, Guid documento)
    {
        var resultado = await ExecutarAsync(usuario, new BaixarDocumento(documento));
        if (resultado.Valor is not { } arquivo) return null;

        await using var conteudo = arquivo.Conteudo;
        using var leitor = new StreamReader(conteudo, Encoding.UTF8);
        return await leitor.ReadToEndAsync();
    }

    private Task<Documento> NoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Documentos.IgnoreQueryFilters().AsNoTracking()
            .Include(d => d.PerfisComAcesso).Include(d => d.UsuariosComAcesso).SingleAsync(d => d.Id == id));

    private Task<List<DocumentoPerfilAcesso>> EntradasDePerfilAsync(Guid documento) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Set<DocumentoPerfilAcesso>().IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.DocumentoId == documento).OrderBy(a => a.CriadoEm).ThenBy(a => a.Id).ToListAsync());

    private Task<List<RegistroAcesso>> AcessosAsync() => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().OrderBy(r => r.Id).ToListAsync());

    private string[] ArquivosNoDisco() =>
        Directory.Exists(PastaDeArquivos) ? Directory.GetFiles(PastaDeArquivos, "*", SearchOption.AllDirectories) : [];

    private async Task<IReadOnlyList<Guid>> IdsListadosAsync(Guid usuario, CategoriaDocumento categoria = CategoriaDocumento.DocumentoGeral) =>
        [.. (await ExecutarAsync(usuario, new ListarDocumentosDoCliente(_maria, categoria))).Valor!.Itens.Select(d => d.Id)];

    private static PostgresException PostgresDe(Exception erro)
    {
        for (var e = erro; e is not null; e = e.InnerException)
            if (e is PostgresException pg) return pg;
        throw new InvalidOperationException("Nenhuma PostgresException na cadeia de exceções: " + erro);
    }

    [Fact]
    public async Task Documento_enviado_vai_para_o_disco_e_os_metadados_para_o_banco()
    {
        var id = await EnviarAsync(conteudo: "abc", nome: "Procuração assinada");

        var documento = await NoBancoAsync(id);
        documento.Should().BeEquivalentTo(new
        {
            Nome = "Procuração assinada", NomeArquivoOriginal = "procuracao.pdf", TipoConteudo = "application/pdf",
            TamanhoBytes = 3L, HashSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            Categoria = CategoriaDocumento.DocumentoGeral, ClienteId = (Guid?)_maria, CriadoPor = (Guid?)_admin,
        });
        documento.PerfisComAcesso.Select(a => a.PerfilId).Should().BeEquivalentTo([_perfilDiretor, _perfilProducao]);
        ArquivosNoDisco().Should().ContainSingle();
        (await BaixarTextoAsync(_admin, id)).Should().Be("abc");
    }

    [Fact]
    public async Task Envio_gera_registros_de_criacao_do_documento_e_de_cada_entrada_da_lista()
    {
        var id = await EnviarAsync();

        var registros = (await AuditoriaAsync()).Where(r => r.Entidade is "Documento" or "DocumentoPerfilAcesso").ToList();

        var doDocumento = registros.Should().ContainSingle(r => r.Entidade == "Documento").Subject;
        doDocumento.Operacao.Should().Be(OperacaoAuditada.Criacao);
        doDocumento.EntidadeId.Should().Be(id);
        doDocumento.UsuarioId.Should().Be(_admin);
        using var dados = JsonDocument.Parse(doDocumento.Dados!);
        dados.RootElement.GetProperty("Nome").GetString().Should().Be("procuracao.pdf");
        dados.RootElement.GetProperty("Categoria").GetString().Should().Be("DocumentoGeral");
        dados.RootElement.GetProperty("ClienteId").GetGuid().Should().Be(_maria);
        registros.Where(r => r.Entidade == "DocumentoPerfilAcesso").Should().HaveCount(2)
            .And.OnlyContain(r => r.Operacao == OperacaoAuditada.Criacao && r.IdCorrelacao == doDocumento.IdCorrelacao);
    }

    [Fact]
    public async Task Listagem_respeita_a_lista_de_acesso_no_banco()
    {
        var liberado = await EnviarAsync(arquivo: "liberado.pdf");
        var restrito = await EnviarAsync(arquivo: "restrito.pdf");
        var doPedro = await EnviarAsync(arquivo: "do-pedro.pdf");
        var deOutroCliente = await EnviarAsync(arquivo: "outro.pdf", cliente: _jose);
        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(restrito, [_perfilDiretor], []))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(doPedro, [], [_pedro]))).Sucesso.Should().BeTrue();

        (await IdsListadosAsync(_admin)).Should().BeEquivalentTo([liberado, restrito, doPedro]);
        (await IdsListadosAsync(_pedro)).Should().BeEquivalentTo([liberado, doPedro]);
        (await IdsListadosAsync(_pedro)).Should().NotContain(deOutroCliente);
    }

    [Fact]
    public async Task Listagem_vem_do_mais_recente_para_o_mais_antigo_com_quem_enviou()
    {
        var primeiro = await EnviarAsync(arquivo: "primeiro.pdf");
        var segundo = await EnviarAsync(arquivo: "segundo.pdf");

        var pagina = (await ExecutarAsync(_admin, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.DocumentoGeral))).Valor!;

        pagina.Total.Should().Be(2);
        pagina.Itens.Select(d => d.Id).Should().Equal(segundo, primeiro);
        pagina.Itens.Should().OnlyContain(d => d.EnviadoPor == "Diana Diretora" && d.EnviadoEm.Kind == DateTimeKind.Utc);
    }

    [Fact]
    public async Task Retirar_o_perfil_tira_o_documento_da_producao_e_devolver_o_perfil_o_traz_de_volta()
    {
        var id = await EnviarAsync();

        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(id, [_perfilDiretor], []))).Sucesso.Should().BeTrue();
        (await IdsListadosAsync(_pedro)).Should().BeEmpty();
        (await BaixarTextoAsync(_pedro, id)).Should().BeNull();

        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(id, [_perfilDiretor, _perfilProducao], []))).Sucesso.Should().BeTrue();
        (await IdsListadosAsync(_pedro)).Should().Equal(id);
        (await BaixarTextoAsync(_pedro, id)).Should().Be("conteúdo do arquivo");

        // A entrada antiga do perfil continua no banco, excluída logicamente; a nova é outra linha.
        var entradas = (await EntradasDePerfilAsync(id)).Where(a => a.PerfilId == _perfilProducao).ToList();
        entradas.Should().HaveCount(2);
        entradas.Count(a => a.ExcluidoEm is not null).Should().Be(1);
    }

    [Fact]
    public async Task Mudanca_na_lista_de_acesso_grava_o_evento_e_os_registros_na_mesma_transacao()
    {
        var id = await EnviarAsync();

        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(id, [_perfilDiretor], [_pedro]))).Sucesso.Should().BeTrue();

        var registros = await AuditoriaAsync();
        var evento = registros.Should().ContainSingle(r => r.Evento == EventosDeSeguranca.DocumentoAcessoAlterado).Subject;
        evento.Entidade.Should().Be("Documento");
        evento.EntidadeId.Should().Be(id);
        evento.UsuarioId.Should().Be(_admin);
        using var dados = JsonDocument.Parse(evento.Dados!);
        dados.RootElement.GetProperty("perfisRemovidos").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_perfilProducao);
        dados.RootElement.GetProperty("usuariosAdicionados").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_pedro);
        registros.Should().ContainSingle(r =>
            r.Entidade == "DocumentoPerfilAcesso" && r.Operacao == OperacaoAuditada.Exclusao && r.IdCorrelacao == evento.IdCorrelacao);
        registros.Should().ContainSingle(r =>
            r.Entidade == "DocumentoUsuarioAcesso" && r.Operacao == OperacaoAuditada.Criacao && r.IdCorrelacao == evento.IdCorrelacao);
    }

    [Fact]
    public async Task Contrato_de_honorarios_e_invisivel_para_a_producao()
    {
        var contrato = await EnviarAsync(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");

        var listagem = await ExecutarAsync(_pedro, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.ContratoHonorarios));
        var download = await ExecutarAsync(_pedro, new BaixarDocumento(contrato));
        var liberar = await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(contrato, [_perfilProducao], [_pedro]));

        listagem.Erro.Should().Be(TipoErro.AcessoNegado);
        download.Erro.Should().Be(TipoErro.NaoEncontrado);
        liberar.Erro.Should().Be(TipoErro.Validacao);
        (await NoBancoAsync(contrato)).PerfisComAcesso.Should().BeEmpty();
        (await IdsListadosAsync(_admin, CategoriaDocumento.ContratoHonorarios)).Should().Equal(contrato);
        (await BaixarTextoAsync(_admin, contrato)).Should().Be("conteúdo do arquivo");

        // As duas tentativas do Pedro ficam registradas: a listagem (negada) e o download (respondido como não encontrado).
        var negados = (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado).ToList();
        negados.Should().HaveCount(2).And.OnlyContain(r => r.UsuarioId == _pedro);
        negados.Should().ContainSingle(r => r.Entidade == "Documento" && r.EntidadeId == contrato);
    }

    [Fact]
    public async Task Listar_baixar_e_ver_a_lista_de_acesso_ficam_no_registro_de_consultas()
    {
        var id = await EnviarAsync();

        await ExecutarAsync(_pedro, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.DocumentoGeral));
        (await BaixarTextoAsync(_pedro, id)).Should().NotBeNull();
        await ExecutarAsync(_admin, new ObterAcessoDoDocumento(id));

        var acessos = (await AcessosAsync()).Where(a => a.TipoRecurso == "Documento").ToList();
        acessos.Select(a => (a.UsuarioId, a.Acao, a.RecursoId)).Should().Equal(
            (_pedro, AcaoDeAcesso.Listou, (Guid?)null),
            (_pedro, AcaoDeAcesso.Baixou, (Guid?)id),
            (_admin, AcaoDeAcesso.Visualizou, (Guid?)id));
        acessos[0].Detalhe.Should().Contain(_maria.ToString());
    }

    [Fact]
    public async Task Download_negado_nao_entra_no_registro_de_consultas()
    {
        var id = await EnviarAsync();
        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(id, [_perfilDiretor], []))).Sucesso.Should().BeTrue();

        (await BaixarTextoAsync(_pedro, id)).Should().BeNull();

        (await AcessosAsync()).Should().NotContain(a => a.Acao == AcaoDeAcesso.Baixou);
    }

    [Fact]
    public async Task Excluir_e_logico_para_o_documento_e_para_a_lista_e_o_arquivo_fica_no_disco()
    {
        var id = await EnviarAsync();

        (await ExecutarAsync(_admin, new ExcluirDocumento(id))).Sucesso.Should().BeTrue();

        var documento = await NoBancoAsync(id);
        documento.ExcluidoEm.Should().NotBeNull();
        documento.ExcluidoPor.Should().Be(_admin);
        (await EntradasDePerfilAsync(id)).Should().HaveCount(2).And.OnlyContain(a => a.ExcluidoEm != null);
        (await IdsListadosAsync(_admin)).Should().BeEmpty();
        (await ExecutarAsync(_admin, new BaixarDocumento(id))).Erro.Should().Be(TipoErro.NaoEncontrado);
        ArquivosNoDisco().Should().ContainSingle("o arquivo de um documento excluído permanece no armazenamento");
        (await AuditoriaAsync()).Where(r => r.Operacao == OperacaoAuditada.Exclusao)
            .Select(r => r.Entidade).Should().BeEquivalentTo(["Documento", "DocumentoPerfilAcesso", "DocumentoPerfilAcesso"]);
    }

    [Fact]
    public async Task Renomear_gera_registro_de_alteracao_so_do_nome()
    {
        var id = await EnviarAsync();

        (await ExecutarAsync(_admin, new RenomearDocumento(id, "Procuração 2026"))).Sucesso.Should().BeTrue();

        var alteracao = (await AuditoriaAsync()).Should()
            .ContainSingle(r => r.Entidade == "Documento" && r.Operacao == OperacaoAuditada.Alteracao).Subject;
        using var dados = JsonDocument.Parse(alteracao.Dados!);
        dados.RootElement.EnumerateObject().Select(p => p.Name).Should().Equal("Nome");
        dados.RootElement.GetProperty("Nome").GetProperty("para").GetString().Should().Be("Procuração 2026");
        (await NoBancoAsync(id)).NomeArquivoOriginal.Should().Be("procuracao.pdf");
    }

    [Fact]
    public async Task Envio_recusado_nao_deixa_arquivo_no_disco_nem_registro_no_banco()
    {
        var vazio = await ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, _maria, null, "vazio.pdf", "application/pdf", new MemoryStream()));
        var semCliente = await ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, Guid.CreateVersion7(), null, "x.pdf", "application/pdf", new MemoryStream([1, 2, 3])));
        var doPedro = await ExecutarAsync(_pedro, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, _maria, null, "x.pdf", "application/pdf", new MemoryStream([1, 2, 3])));

        vazio.Erro.Should().Be(TipoErro.Validacao);
        semCliente.Erro.Should().Be(TipoErro.Validacao);
        doPedro.Erro.Should().Be(TipoErro.AcessoNegado);
        ArquivosNoDisco().Should().BeEmpty();
        (await IdsListadosAsync(_admin)).Should().BeEmpty();
    }

    [Fact]
    public async Task Banco_recusa_apagar_fisicamente_um_documento_com_lista_de_acesso()
    {
        var id = await EnviarAsync();

        var acao = () => EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Database.ExecuteSqlAsync($"DELETE FROM documentos WHERE id = {id}"));

        var erro = (await acao.Should().ThrowAsync<Exception>()).Which;
        PostgresDe(erro).SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        (await EntradasDePerfilAsync(id)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Banco_recusa_duas_entradas_vivas_do_mesmo_perfil_no_mesmo_documento()
    {
        var id = await EnviarAsync();
        var novoId = Guid.CreateVersion7();
        var agora = DateTime.UtcNow;

        var acao = () => EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Database.ExecuteSqlAsync(
            $"INSERT INTO documentos_perfis_acesso (id, documento_id, perfil_id, criado_em) VALUES ({novoId}, {id}, {_perfilProducao}, {agora})"));

        var erro = (await acao.Should().ThrowAsync<Exception>()).Which;
        PostgresDe(erro).SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
}
