using System.Text;
using System.Text.Json.Nodes;
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

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class DocumentosDoPortalTests(BancoFixture banco) : TesteComBanco(banco)
{
    private Guid _admin, _pedro, _maria, _carla, _jose, _joao;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _pedro = (await ExecutarAsync(_admin, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null))).Valor;
        _maria = await ClienteAsync("Maria Silva", "529.982.247-25");
        _jose = await ClienteAsync("José Souza", "111.444.777-35");
        _carla = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _maria));
        _joao = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _jose, "João Cliente", "joao@x.com"));
    }

    private async Task<Guid> ClienteAsync(string nome, string cpf) =>
        (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada(nome, null, null, new EnderecoEntrada(), null), cpf, null, null))).Valor;

    private static MemoryStream Conteudo(string texto = "conteúdo") => new(Encoding.UTF8.GetBytes(texto));

    private async Task<Guid> EnviarAsync(CategoriaDocumento categoria, Guid cliente, string arquivo)
    {
        var resultado = await ExecutarAsync(_admin, new EnviarDocumento(
            categoria, cliente, null, arquivo, "application/pdf", Conteudo()));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private async Task<Guid> EnviarComumAsync(string arquivo)
    {
        var resultado = await ExecutarAsync(_admin, new EnviarDocumentoComum(null, arquivo, "application/pdf", Conteudo(), true));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private async Task<IReadOnlyList<DocumentoDoPortal>> MeusItensAsync(Guid usuario) =>
        (await ExecutarAsync(usuario, new ListarMeusDocumentos())).Valor!.Itens;

    private async Task<IReadOnlyList<DocumentoDoPortal>> ComunsItensAsync(Guid usuario) =>
        (await ExecutarAsync(usuario, new ListarDocumentosComuns())).Valor!.Itens;

    private async Task<IReadOnlyList<Guid>> MeusAsync(Guid usuario) => [.. (await MeusItensAsync(usuario)).Select(d => d.Id)];

    private async Task<IReadOnlyList<Guid>> ComunsAsync(Guid usuario) => [.. (await ComunsItensAsync(usuario)).Select(d => d.Id)];

    private async Task<Resultado<ArquivoParaBaixar>> BaixarAsync(Guid usuario, Guid documento)
    {
        var resultado = await ExecutarAsync(usuario, new BaixarDocumento(documento));
        if (resultado.Valor is { } arquivo) await arquivo.DisposeAsync();
        return resultado;
    }

    private async Task<List<RegistroAuditoria>> NegadosDeDownloadAsync(Guid usuario) =>
        [.. (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == usuario)];

    private Task<int> DocumentosNoBancoAsync() => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Documentos.IgnoreQueryFilters().CountAsync());

    private int ArquivosNoDisco() =>
        Directory.Exists(PastaDeArquivos) ? Directory.GetFiles(PastaDeArquivos, "*", SearchOption.AllDirectories).Length : 0;

    [Fact]
    public async Task Cliente_A_nao_lista_nem_baixa_documento_do_cliente_B()
    {
        var daMaria = await EnviarAsync(CategoriaDocumento.DocumentoDoCliente, _maria, "da-maria.pdf");
        var doJose = await EnviarAsync(CategoriaDocumento.DocumentoDoCliente, _jose, "do-jose.pdf");
        // Outras categorias da própria Maria, e um comum: nenhuma delas entra em "meus documentos".
        await EnviarAsync(CategoriaDocumento.DocumentoGeral, _maria, "geral.pdf");
        await EnviarAsync(CategoriaDocumento.ContratoHonorarios, _maria, "contrato.pdf");
        await EnviarComumAsync("comum.pdf");

        (await MeusAsync(_carla)).Should().Equal(daMaria);
        (await MeusAsync(_joao)).Should().Equal(doJose);
        (await MeusItensAsync(_carla)).Should().OnlyContain(d => d.EnviadoPor == null);
        (await MeusItensAsync(_joao)).Should().OnlyContain(d => d.EnviadoPor == null);
        (await BaixarAsync(_carla, daMaria)).Sucesso.Should().BeTrue();
        (await BaixarAsync(_joao, doJose)).Sucesso.Should().BeTrue();
        (await NegadosDeDownloadAsync(_carla)).Should().BeEmpty();

        var carlaNoDoJose = await BaixarAsync(_carla, doJose);
        var joaoNoDaMaria = await BaixarAsync(_joao, daMaria);

        carlaNoDoJose.Erro.Should().Be(TipoErro.NaoEncontrado);
        joaoNoDaMaria.Erro.Should().Be(TipoErro.NaoEncontrado);
        var daCarla = (await NegadosDeDownloadAsync(_carla)).Should().ContainSingle().Subject;
        daCarla.Entidade.Should().Be("Documento");
        daCarla.EntidadeId.Should().Be(doJose);
        var doJoao = (await NegadosDeDownloadAsync(_joao)).Should().ContainSingle().Subject;
        doJoao.EntidadeId.Should().Be(daMaria);
    }

    [Fact]
    public async Task Producao_nao_lista_nem_baixa_as_duas_categorias()
    {
        var doCliente = await EnviarAsync(CategoriaDocumento.DocumentoDoCliente, _maria, "do-cliente.pdf");
        var comum = await EnviarComumAsync("comum.pdf");
        var geral = await EnviarAsync(CategoriaDocumento.DocumentoGeral, _maria, "geral.pdf");

        (await ExecutarAsync(_pedro, new ListarMeusDocumentos())).Erro.Should().Be(TipoErro.AcessoNegado);
        (await ExecutarAsync(_pedro, new ListarDocumentosComuns())).Erro.Should().Be(TipoErro.AcessoNegado);
        (await BaixarAsync(_pedro, doCliente)).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await BaixarAsync(_pedro, comum)).Erro.Should().Be(TipoErro.NaoEncontrado);

        var daFicha = (await ExecutarAsync(_pedro, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.DocumentoGeral))).Valor!;
        daFicha.Itens.Select(d => d.Id).Should().Equal(geral);
        (await ExecutarAsync(_pedro, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.DocumentoDoCliente)))
            .Erro.Should().Be(TipoErro.AcessoNegado);
        (await NegadosDeDownloadAsync(_pedro)).Select(r => r.EntidadeId).Should().Contain([doCliente, comum]);
    }

    [Fact]
    public async Task Documentos_comuns_chegam_a_todo_cliente_e_o_autor_so_a_diretoria()
    {
        var comum = await EnviarComumAsync("comum.pdf");
        // Documentos de cliente presentes no banco não entram nos comuns.
        await EnviarAsync(CategoriaDocumento.DocumentoDoCliente, _maria, "da-maria.pdf");
        await EnviarAsync(CategoriaDocumento.DocumentoGeral, _maria, "geral.pdf");

        (await ComunsAsync(_carla)).Should().Equal(comum);
        (await ComunsAsync(_joao)).Should().Equal(comum);
        (await ComunsAsync(_admin)).Should().Equal(comum);
        (await BaixarAsync(_carla, comum)).Sucesso.Should().BeTrue();
        (await BaixarAsync(_joao, comum)).Sucesso.Should().BeTrue();

        var doCliente = (await ExecutarAsync(_carla, new ListarDocumentosComuns())).Valor!.Itens.Single();
        doCliente.EnviadoPor.Should().BeNull();
        var daDiretoria = (await ExecutarAsync(_admin, new ListarDocumentosComuns())).Valor!.Itens.Single();
        daDiretoria.EnviadoPor.Should().Be(Admin.Nome);
        daDiretoria.Nome.Should().Be("comum.pdf");
    }

    [Fact]
    public async Task Documento_comum_sem_confirmacao_nao_grava_arquivo_nem_registro()
    {
        var arquivosAntes = ArquivosNoDisco();
        var documentosAntes = await DocumentosNoBancoAsync();

        var resultado = await ExecutarAsync(_admin, new EnviarDocumentoComum(null, "comum.pdf", "application/pdf", Conteudo(), false));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Confirme que o documento ficará visível para todos os clientes.");
        ArquivosNoDisco().Should().Be(arquivosAntes);
        (await DocumentosNoBancoAsync()).Should().Be(documentosAntes);
    }

    [Fact]
    public async Task Trocar_o_cliente_do_usuario_troca_os_documentos_que_ele_ve()
    {
        var daMaria = await EnviarAsync(CategoriaDocumento.DocumentoDoCliente, _maria, "da-maria.pdf");
        var doJose = await EnviarAsync(CategoriaDocumento.DocumentoDoCliente, _jose, "do-jose.pdf");
        (await MeusAsync(_carla)).Should().Equal(daMaria);
        var perfilCliente = await IdDoPerfilAsync(TipoPerfil.Cliente);

        var troca = await ExecutarAsync(_admin, new EditarUsuario(
            _carla, "Carla Cliente", UsuariosDeClienteDeTeste.EmailDaCarla, perfilCliente, null, _jose));

        troca.Sucesso.Should().BeTrue(troca.Mensagem);
        (await MeusAsync(_carla)).Should().Equal(doJose);
        (await BaixarAsync(_carla, doJose)).Sucesso.Should().BeTrue();
        (await BaixarAsync(_carla, daMaria)).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    [Fact]
    public async Task Cliente_inativo_continua_recebendo_documentos_da_diretoria()
    {
        (await ExecutarAsync(_admin, new DefinirAtivoCliente(_maria, false))).Sucesso.Should().BeTrue();

        var id = await EnviarAsync(CategoriaDocumento.DocumentoDoCliente, _maria, "novo.pdf");

        var daFicha = (await ExecutarAsync(_admin, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.DocumentoDoCliente))).Valor!;
        daFicha.Itens.Select(d => d.Id).Should().Equal(id);
        (await ExecutarAsync(_carla, new ListarMeusDocumentos())).Erro.Should().Be(TipoErro.AcessoNegado);
        (await ExecutarAsync(_carla, new BaixarDocumento(id))).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Listas_do_portal_sao_paginadas_da_mais_recente_para_a_mais_antiga()
    {
        var enviados = new List<Guid>();
        for (var i = 1; i <= 21; i++) enviados.Add(await EnviarComumAsync($"comum-{i:00}.pdf"));

        var primeira = (await ExecutarAsync(_carla, new ListarDocumentosComuns())).Valor!;
        var segunda = (await ExecutarAsync(_carla, new ListarDocumentosComuns(2))).Valor!;

        primeira.Total.Should().Be(21);
        primeira.Itens.Select(d => d.Id).Should().Equal(Enumerable.Reverse(enviados).Take(20));
        segunda.Itens.Select(d => d.Id).Should().Equal(enviados[0]);
    }

    [Fact]
    public async Task Excluir_documento_comum_o_tira_de_todos_os_clientes()
    {
        var comum = await EnviarComumAsync("comum.pdf");
        var outro = await EnviarComumAsync("outro.pdf");
        (await ComunsAsync(_carla)).Should().Contain(comum);
        (await ComunsAsync(_joao)).Should().Contain(comum);

        (await ExecutarAsync(_admin, new ExcluirDocumento(comum))).Sucesso.Should().BeTrue();

        (await ComunsAsync(_carla)).Should().Equal(outro);
        (await ComunsAsync(_joao)).Should().Equal(outro);
        (await BaixarAsync(_carla, comum)).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await BaixarAsync(_joao, comum)).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    [Fact]
    public async Task Listagens_e_downloads_do_cliente_ficam_no_registro_de_consultas()
    {
        var doCliente = await EnviarAsync(CategoriaDocumento.DocumentoDoCliente, _maria, "do-cliente.pdf");
        await EnviarComumAsync("comum.pdf");

        await MeusAsync(_carla);
        await ComunsAsync(_carla);
        (await BaixarAsync(_carla, doCliente)).Sucesso.Should().BeTrue();

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(r => r.UsuarioId == _carla).ToListAsync());
        acessos.Should().OnlyContain(r => r.TipoRecurso == "Documento");
        acessos.Count(r => r.Acao == AcaoDeAcesso.Listou).Should().Be(2);
        acessos.Should().ContainSingle(r => r.Acao == AcaoDeAcesso.Baixou).Which.RecursoId.Should().Be(doCliente);
        acessos.Should().HaveCount(3);
    }

    [Fact]
    public async Task Envio_renomeacao_e_exclusao_ficam_na_auditoria_do_documento()
    {
        var id = await EnviarAsync(CategoriaDocumento.DocumentoDoCliente, _maria, "do-cliente.pdf");
        (await ExecutarAsync(_admin, new RenomearDocumento(id, "Procuração assinada"))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_admin, new ExcluirDocumento(id))).Sucesso.Should().BeTrue();

        var registros = await AuditoriaAsync();

        var doDocumento = registros.Where(r => r.Entidade == "Documento" && r.EntidadeId == id).ToList();
        doDocumento.Select(r => r.Operacao).Should().BeEquivalentTo(
            [OperacaoAuditada.Criacao, OperacaoAuditada.Alteracao, OperacaoAuditada.Exclusao]);
        var criacao = doDocumento.Single(r => r.Operacao == OperacaoAuditada.Criacao);
        ((string?)JsonNode.Parse(criacao.Dados!)!["Categoria"]).Should().Be("DocumentoDoCliente");
        registros.Should().NotContain(r => r.Entidade == "DocumentoPerfilAcesso" || r.Entidade == "DocumentoUsuarioAcesso",
            "o documento visível ao cliente não tem lista de acesso");
    }
}
