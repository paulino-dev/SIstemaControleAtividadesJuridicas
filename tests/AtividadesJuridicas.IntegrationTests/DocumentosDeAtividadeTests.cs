using System.Text;
using System.Text.Json;
using AtividadesJuridicas.Application.Atividades;
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
public class DocumentosDeAtividadeTests(BancoFixture banco) : TesteComBanco(banco)
{
    private Guid _diana, _pedro, _ester, _maria, _jose, _perfilDiretor, _perfilProducao;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _diana = await IdDoAdminAsync();
        _perfilDiretor = await IdDoPerfilAsync(TipoPerfil.Diretoria);
        _perfilProducao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _pedro = (await ExecutarAsync(_diana, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", _perfilProducao, null))).Valor;
        _ester = (await ExecutarAsync(_diana, new CriarUsuario("Ester", "ester@x.com", "senha-da-ester", _perfilProducao, null))).Valor;
        _maria = await PfAsync("Maria Silva", "529.982.247-25");
        _jose = await PfAsync("José Souza", "111.444.777-35");
    }

    private async Task<Guid> PfAsync(string nome, string cpf) =>
        (await ExecutarAsync(_diana, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada(nome, null, null, new EnderecoEntrada(), null), cpf, null, null))).Valor;

    private async Task<Guid> AtividadeAsync(string titulo, Guid? cliente, params Guid[] responsaveis)
    {
        var criada = await ExecutarAsync(_diana, new SalvarAtividade(
            null, titulo, "Prazo de 15 dias.", new DateOnly(2090, 6, 15), cliente, null, null));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        if (responsaveis.Length > 0) await DirecionarAsync(criada.Valor, responsaveis);
        return criada.Valor;
    }

    private async Task DirecionarAsync(Guid atividade, params Guid[] responsaveis)
    {
        var definido = await ExecutarAsync(_diana, new DefinirResponsaveisDaAtividade(
            atividade, [.. responsaveis.Select(r => new ResponsavelEntrada(r, "Redigir"))], _diana));
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
    }

    private Task<Resultado<Guid>> AnexarAsync(Guid quem, Guid atividade, string arquivo = "peticao.pdf", string conteudo = "conteúdo") =>
        ExecutarAsync(quem, new EnviarDocumentoDaAtividade(
            atividade, null, arquivo, "application/pdf", new MemoryStream(Encoding.UTF8.GetBytes(conteudo))));

    private async Task<Guid> AnexadoAsync(Guid atividade, string arquivo = "peticao.pdf", string conteudo = "conteúdo")
    {
        var anexado = await AnexarAsync(_diana, atividade, arquivo, conteudo);
        anexado.Sucesso.Should().BeTrue(anexado.Mensagem);
        return anexado.Valor;
    }

    private async Task<string?> BaixarTextoAsync(Guid usuario, Guid documento)
    {
        var resultado = await ExecutarAsync(usuario, new BaixarDocumento(documento));
        if (resultado.Valor is not { } arquivo) return null;

        await using var conteudo = arquivo.Conteudo;
        using var leitor = new StreamReader(conteudo, Encoding.UTF8);
        return await leitor.ReadToEndAsync();
    }

    private async Task<IReadOnlyList<Guid>> DaAtividadeAsync(Guid usuario, Guid atividade) =>
        [.. (await ExecutarAsync(usuario, new ListarDocumentosDaAtividade(atividade))).Valor?.Itens.Select(d => d.Id) ?? []];

    private Task<Resultado<Pagina<DocumentoResumo>>> DoClienteAsync(Guid usuario, Guid? cliente = null,
        CategoriaDocumento categoria = CategoriaDocumento.DocumentoAtividade) =>
        ExecutarAsync(usuario, new ListarDocumentosDoCliente(cliente ?? _maria, categoria));

    private Task<Documento> NoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Documentos.IgnoreQueryFilters().AsNoTracking()
            .Include(d => d.PerfisComAcesso).Include(d => d.UsuariosComAcesso).SingleAsync(d => d.Id == id));

    private string[] ArquivosNoDisco() =>
        Directory.Exists(PastaDeArquivos) ? Directory.GetFiles(PastaDeArquivos, "*", SearchOption.AllDirectories) : [];

    private static PostgresException PostgresDe(Exception erro)
    {
        for (var e = erro; e is not null; e = e.InnerException)
            if (e is PostgresException pg) return pg;
        throw new InvalidOperationException("Nenhuma PostgresException na cadeia de exceções: " + erro);
    }

    private static JsonElement DadosDe(RegistroAuditoria registro) => JsonDocument.Parse(registro.Dados!).RootElement;

    [Fact]
    public async Task Documento_de_atividade_vai_para_o_disco_e_para_o_banco_com_atividade_e_cliente()
    {
        var atividade = await AtividadeAsync("Contestação", _maria, _pedro);

        var anexado = await AnexarAsync(_pedro, atividade, conteudo: "abc");

        anexado.Sucesso.Should().BeTrue(anexado.Mensagem);
        var documento = await NoBancoAsync(anexado.Valor);
        documento.Should().BeEquivalentTo(new
        {
            Nome = "peticao.pdf", NomeArquivoOriginal = "peticao.pdf", TipoConteudo = "application/pdf", TamanhoBytes = 3L,
            HashSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            Categoria = CategoriaDocumento.DocumentoAtividade, ClienteId = (Guid?)_maria, AtividadeId = (Guid?)atividade,
            CriadoPor = (Guid?)_pedro,
        });
        documento.PerfisComAcesso.Should().BeEmpty();
        documento.UsuariosComAcesso.Should().BeEmpty();
        ArquivosNoDisco().Should().ContainSingle();
        (await BaixarTextoAsync(_pedro, anexado.Valor)).Should().Be("abc");

        var criacao = (await AuditoriaAsync()).Where(r => r.Entidade == "Documento").Should().ContainSingle().Subject;
        criacao.Operacao.Should().Be(OperacaoAuditada.Criacao);
        DadosDe(criacao).GetProperty("AtividadeId").GetGuid().Should().Be(atividade);
    }

    [Fact]
    public async Task Responsavel_lista_e_baixa_e_quem_nao_e_responsavel_nao()
    {
        var dele = await AtividadeAsync("Contestação", _maria, _pedro);
        var dela = await AtividadeAsync("Recurso", _maria, _ester);
        var documentoDele = await AnexadoAsync(dele, "dele.pdf", "do pedro");
        var documentoDela = await AnexadoAsync(dela, "dela.pdf", "da ester");

        (await DaAtividadeAsync(_pedro, dele)).Should().Equal(documentoDele);
        (await BaixarTextoAsync(_pedro, documentoDele)).Should().Be("do pedro");
        (await BaixarTextoAsync(_pedro, documentoDela)).Should().BeNull("não é responsável da atividade dela");
        (await ExecutarAsync(_pedro, new ListarDocumentosDaAtividade(dela))).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await AnexarAsync(_pedro, dela)).Erro.Should().Be(TipoErro.NaoEncontrado);

        var doCliente = (await DoClienteAsync(_pedro)).Valor!;
        doCliente.Total.Should().Be(1, "existem 2 documentos de atividade do cliente, mas o Pedro só alcança 1");
        var item = doCliente.Itens.Should().ContainSingle().Subject;
        item.Id.Should().Be(documentoDele);
        item.AtividadeId.Should().Be(dele);
        item.AtividadeTitulo.Should().Be("Contestação");

        var daDiretoria = (await DoClienteAsync(_diana)).Valor!;
        daDiretoria.Total.Should().Be(2);
        daDiretoria.Itens.Select(d => (d.Id, d.AtividadeTitulo)).Should().Equal((documentoDela, "Recurso"), (documentoDele, "Contestação"));
        (await BaixarTextoAsync(_diana, documentoDela)).Should().Be("da ester");

        // Os acessos negados ficam registrados com a atividade ou o documento.
        var negados = (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado).ToList();
        negados.Should().Contain(r => r.Entidade == "Documento" && r.EntidadeId == documentoDela && r.UsuarioId == _pedro);
        negados.Where(r => r.Entidade == "Atividade" && r.EntidadeId == dela && r.UsuarioId == _pedro).Should().HaveCount(2);
    }

    [Fact]
    public async Task Responsavel_removido_perde_os_documentos_na_hora_e_o_incluido_ganha()
    {
        var atividade = await AtividadeAsync("Contestação", _maria, _pedro);
        var documento = await AnexadoAsync(atividade, conteudo: "texto");
        (await DaAtividadeAsync(_pedro, atividade)).Should().Equal(documento);
        (await DoClienteAsync(_pedro)).Valor!.Total.Should().Be(1);
        (await BaixarTextoAsync(_pedro, documento)).Should().Be("texto");
        (await DaAtividadeAsync(_ester, atividade)).Should().BeEmpty();
        (await BaixarTextoAsync(_ester, documento)).Should().BeNull();

        await DirecionarAsync(atividade, _ester);

        (await DaAtividadeAsync(_pedro, atividade)).Should().BeEmpty();
        (await ExecutarAsync(_pedro, new ListarDocumentosDaAtividade(atividade))).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await DoClienteAsync(_pedro)).Valor!.Total.Should().Be(0);
        (await BaixarTextoAsync(_pedro, documento)).Should().BeNull();
        (await DaAtividadeAsync(_ester, atividade)).Should().Equal(documento);
        (await DoClienteAsync(_ester)).Valor!.Itens.Select(d => d.Id).Should().Equal(documento);
        (await BaixarTextoAsync(_ester, documento)).Should().Be("texto");
        (await BaixarTextoAsync(_diana, documento)).Should().Be("texto");
        (await NoBancoAsync(documento)).UsuariosComAcesso.Should().BeEmpty("o acesso é derivado, não gravado");
    }

    [Fact]
    public async Task Trocar_o_cliente_da_atividade_move_os_documentos_de_ficha_e_audita()
    {
        var atividade = await AtividadeAsync("Contestação", _maria, _pedro);
        var documento = await AnexadoAsync(atividade);
        var outraAtividade = await AtividadeAsync("Recurso", _maria, _pedro);
        var deOutra = await AnexadoAsync(outraAtividade, "outra.pdf");

        var salvo = await ExecutarAsync(_diana, new SalvarAtividade(
            atividade, "Contestação", "Prazo de 15 dias.", new DateOnly(2090, 6, 15), _jose, null, null));

        salvo.Sucesso.Should().BeTrue(salvo.Mensagem);
        (await NoBancoAsync(documento)).ClienteId.Should().Be(_jose);
        (await NoBancoAsync(deOutra)).ClienteId.Should().Be(_maria, "outra atividade não é tocada");
        (await DoClienteAsync(_diana, _jose)).Valor!.Itens.Select(d => d.Id).Should().Equal(documento);
        (await DoClienteAsync(_diana, _maria)).Valor!.Itens.Select(d => d.Id).Should().Equal(deOutra);
        (await DoClienteAsync(_pedro, _jose)).Valor!.Itens.Select(d => d.Id).Should().Equal(documento);

        var auditoria = await AuditoriaAsync();
        var doDocumento = auditoria.Where(r => r.Entidade == "Documento" && r.EntidadeId == documento
            && r.Operacao == OperacaoAuditada.Alteracao).Should().ContainSingle().Subject;
        var dados = DadosDe(doDocumento);
        dados.EnumerateObject().Select(p => p.Name).Should().Equal("ClienteId");
        dados.GetProperty("ClienteId").GetProperty("de").GetGuid().Should().Be(_maria);
        dados.GetProperty("ClienteId").GetProperty("para").GetGuid().Should().Be(_jose);
        var daAtividade = auditoria.Where(r => r.Entidade == "Atividade" && r.EntidadeId == atividade
            && r.Operacao == OperacaoAuditada.Alteracao && DadosDe(r).TryGetProperty("ClienteId", out _))
            .Should().ContainSingle().Subject;
        doDocumento.IdCorrelacao.Should().NotBeNull().And.Be(daAtividade.IdCorrelacao);
        auditoria.Should().NotContain(r => r.Entidade == "Documento" && r.EntidadeId == deOutra && r.Operacao == OperacaoAuditada.Alteracao);
    }

    [Fact]
    public async Task Documento_de_atividade_avulsa_nao_aparece_em_ficha_de_cliente_mas_e_listado_e_baixado_pela_atividade()
    {
        var avulsa = await AtividadeAsync("Avulsa", null, _pedro);
        var anexado = await AnexarAsync(_pedro, avulsa, conteudo: "sem cliente");
        anexado.Sucesso.Should().BeTrue(anexado.Mensagem);

        (await NoBancoAsync(anexado.Valor)).ClienteId.Should().BeNull();
        (await DaAtividadeAsync(_pedro, avulsa)).Should().Equal(anexado.Valor);
        (await BaixarTextoAsync(_pedro, anexado.Valor)).Should().Be("sem cliente");
        (await DoClienteAsync(_diana, _maria)).Valor!.Itens.Should().BeEmpty();
        (await DoClienteAsync(_diana, _jose)).Valor!.Itens.Should().BeEmpty();

        // Ganhar um cliente leva o documento para a ficha dele; perdê-lo, de volta.
        (await ExecutarAsync(_diana, new SalvarAtividade(
            avulsa, "Avulsa", "Prazo de 15 dias.", new DateOnly(2090, 6, 15), _maria, null, null))).Sucesso.Should().BeTrue();
        (await DoClienteAsync(_diana, _maria)).Valor!.Itens.Select(d => d.Id).Should().Equal(anexado.Valor);
        (await ExecutarAsync(_diana, new SalvarAtividade(
            avulsa, "Avulsa", "Prazo de 15 dias.", new DateOnly(2090, 6, 15), null, null, null))).Sucesso.Should().BeTrue();
        (await NoBancoAsync(anexado.Valor)).ClienteId.Should().BeNull();
    }

    [Fact]
    public async Task Excluir_a_atividade_mantem_os_documentos_para_a_diretoria_e_os_esconde_da_producao()
    {
        var atividade = await AtividadeAsync("Contestação", _maria, _pedro);
        var documento = await AnexadoAsync(atividade, conteudo: "guardado");

        (await ExecutarAsync(_diana, new ExcluirAtividade(atividade))).Sucesso.Should().BeTrue();

        var documentoNoBanco = await NoBancoAsync(documento);
        documentoNoBanco.ExcluidoEm.Should().BeNull();
        documentoNoBanco.AtividadeId.Should().Be(atividade);
        var daDiretoria = (await DoClienteAsync(_diana)).Valor!;
        daDiretoria.Total.Should().Be(1);
        var item = daDiretoria.Itens.Should().ContainSingle().Subject;
        item.Id.Should().Be(documento);
        item.AtividadeTitulo.Should().Be("Contestação", "o título aparece mesmo com a atividade excluída");
        (await BaixarTextoAsync(_diana, documento)).Should().Be("guardado");
        (await DoClienteAsync(_pedro)).Valor!.Total.Should().Be(0);
        (await DoClienteAsync(_pedro)).Valor!.Itens.Should().BeEmpty();
        (await BaixarTextoAsync(_pedro, documento)).Should().BeNull("a atividade não está mais viva");
        ArquivosNoDisco().Should().ContainSingle();
    }

    [Fact]
    public async Task Banco_exige_atividade_em_documento_de_atividade_e_so_nele()
    {
        var atividade = await AtividadeAsync("Contestação", _maria);

        var semAtividade = () => InserirDocumentoCruAsync(CategoriaDocumento.DocumentoAtividade, cliente: null, atividade: null);
        var comAtividade = () => InserirDocumentoCruAsync(CategoriaDocumento.DocumentoGeral, cliente: _maria, atividade: atividade);

        foreach (var acao in new[] { semAtividade, comAtividade })
        {
            var erro = (await acao.Should().ThrowAsync<Exception>()).Which;
            var pg = PostgresDe(erro);
            pg.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
            pg.ConstraintName.Should().Be("ck_documentos_atividade_so_na_categoria_3");
        }

        await InserirDocumentoCruAsync(CategoriaDocumento.DocumentoAtividade, cliente: null, atividade: atividade);
    }

    [Fact]
    public async Task Documentos_de_cliente_e_contratos_nao_mudaram()
    {
        var geral = (await ExecutarAsync(_diana, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, _maria, null, "geral.pdf", "application/pdf", new MemoryStream("g"u8.ToArray())))).Valor;
        var contrato = (await ExecutarAsync(_diana, new EnviarDocumento(
            CategoriaDocumento.ContratoHonorarios, _maria, null, "contrato.pdf", "application/pdf", new MemoryStream("c"u8.ToArray())))).Valor;

        var noBancoGeral = await NoBancoAsync(geral);
        var noBancoContrato = await NoBancoAsync(contrato);
        noBancoGeral.AtividadeId.Should().BeNull();
        noBancoGeral.PerfisComAcesso.Select(a => a.PerfilId).Should().BeEquivalentTo([_perfilDiretor, _perfilProducao]);
        noBancoContrato.AtividadeId.Should().BeNull();
        noBancoContrato.PerfisComAcesso.Should().BeEmpty();

        (await BaixarTextoAsync(_pedro, geral)).Should().Be("g");
        (await BaixarTextoAsync(_pedro, contrato)).Should().BeNull();
        (await ExecutarAsync(_pedro, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.ContratoHonorarios)))
            .Erro.Should().Be(TipoErro.AcessoNegado);
        var geralListado = (await DoClienteAsync(_pedro, _maria, CategoriaDocumento.DocumentoGeral)).Valor!;
        geralListado.Itens.Should().ContainSingle().Which.AtividadeId.Should().BeNull();
        geralListado.Itens[0].AtividadeTitulo.Should().BeNull();

        // Um contrato nunca é alcançado pelo caminho de documento de atividade, nem pelo repositório.
        var doContrato = await EmEscopoAsync(null, sp => sp.GetRequiredService<IDocumentoRepositorio>().ListarDoClienteAsync(
            _maria, CategoriaDocumento.ContratoHonorarios, new FiltroDeAcesso(_pedro, _perfilProducao), 1, 20, default));
        doContrato.Total.Should().Be(0);
    }

    private Task<int> InserirDocumentoCruAsync(CategoriaDocumento categoria, Guid? cliente, Guid? atividade)
    {
        var id = Guid.CreateVersion7();
        var chave = $"2026/10/{Guid.NewGuid():N}";
        var hash = new string('a', 64);
        var agora = DateTime.UtcNow;
        var codigo = (int)categoria;
        return EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Database.ExecuteSqlAsync(
            $"""
            INSERT INTO documentos (id, nome, nome_arquivo_original, tipo_conteudo, tamanho_bytes, hash_sha256,
                                    chave_armazenamento, categoria, cliente_id, atividade_id, criado_em)
            VALUES ({id}, 'x', 'x.pdf', 'application/pdf', 1, {hash}, {chave}, {codigo}, {cliente}, {atividade}, {agora})
            """));
    }
}
