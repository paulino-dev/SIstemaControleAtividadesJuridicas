using System.Text;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class DocumentosDeAtividadeTests
{
    private readonly Cenario _c = new();
    private readonly ClientePessoaFisica _maria;
    private readonly ClientePessoaFisica _jose;
    private readonly Usuario _pedro;
    private readonly Usuario _ester;

    public DocumentosDeAtividadeTests()
    {
        _maria = Cliente("Maria Silva", "529.982.247-25");
        _jose = Cliente("José Souza", "111.444.777-35");
        _c.Clientes.Itens.AddRange([_maria, _jose]);
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _ester = _c.NovoUsuario("Ester", "ester@x.com", _c.PerfilProducao);
    }

    private static ClientePessoaFisica Cliente(string nome, string cpf) =>
        ClientePessoaFisica.Criar(new DadosDoCliente(nome, null, null, Endereco.Vazio, null), Cpf.Criar(cpf), null, null);

    private Usuario Diana => _c.Diretor;

    private static MemoryStream Conteudo(string texto = "conteúdo") => new(Encoding.UTF8.GetBytes(texto));

    private async Task<Guid> AtividadeAsync(string titulo = "Elaborar contestação", Guid? cliente = null, params Usuario[] responsaveis)
    {
        var criada = await new SalvarAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(null, titulo, "Prazo de 15 dias.", new DateOnly(2026, 10, 20), cliente, null, null), default);
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        if (responsaveis.Length > 0) await DirecionarAsync(criada.Valor, responsaveis);
        return criada.Valor;
    }

    private async Task DirecionarAsync(Guid id, params Usuario[] responsaveis)
    {
        var definido = await new DefinirResponsaveisDaAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirResponsaveisDaAtividade(
                id, [.. responsaveis.Select(r => new ResponsavelEntrada(r.Id, "Redigir"))], Diana.Id), default);
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
    }

    private Task<Resultado<Guid>> Enviar(Usuario? ator, Guid atividade, string arquivo = "peticao.pdf",
        string? nome = null, Stream? conteudo = null, string? tipo = "application/pdf") =>
        new EnviarDocumentoDaAtividadeManipulador(
                _c.Como(ator), _c.Atividades, _c.Documentos, _c.Armazenamento, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new EnviarDocumentoDaAtividade(atividade, nome, arquivo, tipo, conteudo ?? Conteudo()), default);

    private Task<Resultado<Pagina<DocumentoResumo>>> ListarDaAtividade(Usuario? ator, Guid atividade, int pagina = 1) =>
        new ListarDocumentosDaAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Documentos, _c.Auditoria)
            .ManipularAsync(new ListarDocumentosDaAtividade(atividade, pagina), default);

    private Task<Resultado<Pagina<DocumentoResumo>>> ListarDoCliente(Usuario? ator, Guid cliente,
        CategoriaDocumento categoria = CategoriaDocumento.DocumentoAtividade) =>
        new ListarDocumentosDoClienteManipulador(_c.Como(ator), _c.Documentos)
            .ManipularAsync(new ListarDocumentosDoCliente(cliente, categoria), default);

    private Task<Resultado<ArquivoParaBaixar>> Baixar(Usuario? ator, Guid id) =>
        new BaixarDocumentoManipulador(_c.Como(ator), _c.Documentos, _c.Armazenamento, _c.Auditoria, _c.ResponsaveisDeAtividade)
            .ManipularAsync(new BaixarDocumento(id), default);

    private Task<Resultado<Guid>> Salvar(Guid atividade, Guid? cliente) =>
        new SalvarAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(atividade, "Elaborar contestação", "Prazo de 15 dias.", new DateOnly(2026, 10, 20), cliente, null, null), default);

    private async Task<Guid> EnviadoAsync(Guid atividade, string arquivo = "peticao.pdf")
    {
        var enviado = await Enviar(Diana, atividade, arquivo);
        enviado.Sucesso.Should().BeTrue(enviado.Mensagem);
        return enviado.Valor;
    }

    [Fact]
    public async Task Responsavel_e_diretor_anexam_e_o_documento_leva_a_atividade_e_o_cliente_dela()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);

        var dele = await Enviar(_pedro, id, "dele.pdf", conteudo: Conteudo("abc"));
        var da_diretora = await Enviar(Diana, id, "dela.pdf");

        dele.Sucesso.Should().BeTrue(dele.Mensagem);
        da_diretora.Sucesso.Should().BeTrue(da_diretora.Mensagem);
        _c.Documentos.Itens.Should().HaveCount(2);
        var documento = _c.Documentos.Itens.Single(d => d.Id == dele.Valor);
        documento.Categoria.Should().Be(CategoriaDocumento.DocumentoAtividade);
        documento.AtividadeId.Should().Be(id);
        documento.ClienteId.Should().Be(_maria.Id);
        documento.NomeArquivoOriginal.Should().Be("dele.pdf");
        documento.TamanhoBytes.Should().Be(3);
        documento.PerfisComAcesso.Should().BeEmpty();
        documento.UsuariosComAcesso.Should().BeEmpty();
        _c.Armazenamento.Arquivos.Should().ContainKey(documento.ChaveArmazenamento);
        _c.Armazenamento.Arquivos.Should().HaveCount(2);
    }

    [Fact]
    public async Task Atividade_avulsa_aceita_documento_sem_cliente()
    {
        var id = await AtividadeAsync(cliente: null, responsaveis: _pedro);

        var enviado = await Enviar(_pedro, id);

        enviado.Sucesso.Should().BeTrue(enviado.Mensagem);
        var documento = _c.Documentos.Itens.Should().ContainSingle().Subject;
        documento.ClienteId.Should().BeNull();
        documento.AtividadeId.Should().Be(id);
    }

    [Fact]
    public async Task Quem_nao_ve_a_atividade_nao_anexa_nem_lista_e_a_tentativa_fica_registrada()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);
        var existente = await EnviadoAsync(id);
        _c.Auditoria.Eventos.Clear();
        var arquivosAntes = _c.Armazenamento.Arquivos.Count;

        var anexou = await Enviar(_ester, id);
        var listou = await ListarDaAtividade(_ester, id);
        var semUsuario = await Enviar(null, id);
        var inexistente = await Enviar(Diana, Guid.CreateVersion7());

        anexou.Erro.Should().Be(TipoErro.NaoEncontrado);
        anexou.Mensagem.Should().Be("Atividade não encontrada.");
        listou.Erro.Should().Be(TipoErro.NaoEncontrado);
        listou.Mensagem.Should().Be("Atividade não encontrada.");
        semUsuario.Erro.Should().Be(TipoErro.AcessoNegado);
        inexistente.Erro.Should().Be(TipoErro.NaoEncontrado);
        inexistente.Mensagem.Should().Be("Atividade não encontrada.");
        _c.Documentos.Itens.Should().ContainSingle().Which.Id.Should().Be(existente);
        _c.Armazenamento.Arquivos.Should().HaveCount(arquivosAntes, "nada foi gravado para quem não vê a atividade");
        _c.Auditoria.Eventos.Should().HaveCount(2).And.OnlyContain(e =>
            e.Evento == EventosDeSeguranca.AcessoNegado && e.Entidade == "Atividade" && e.EntidadeId == id && e.Imediato);
    }

    [Fact]
    public async Task Validacoes_do_arquivo_sao_as_mesmas_do_documento_de_cliente()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);

        var vazio = await Enviar(_pedro, id, conteudo: new MemoryStream());
        _c.Armazenamento.Arquivos.Should().BeEmpty("o arquivo vazio gravado é removido");
        var semNome = await Enviar(_pedro, id, arquivo: "  ");
        var nomeLongo = await Enviar(_pedro, id, nome: new string('x', Documento.TamanhoMaximoNome + 1));
        var nomeComControle = await Enviar(_pedro, id, nome: "a\u0001b");
        var arquivoComControle = await Enviar(_pedro, id, arquivo: "a\u0001b.pdf");
        _c.Armazenamento.RecusarPorTamanho = true;
        var grande = await Enviar(_pedro, id);
        _c.Armazenamento.RecusarPorTamanho = false;

        vazio.Erro.Should().Be(TipoErro.Validacao);
        vazio.Mensagem.Should().Be("O arquivo está vazio.");
        semNome.Mensagem.Should().Be("Informe o arquivo.");
        nomeLongo.Mensagem.Should().Be($"O nome do documento deve ter até {Documento.TamanhoMaximoNome} caracteres.");
        nomeComControle.Mensagem.Should().Be("O nome do documento é inválido.");
        arquivoComControle.Mensagem.Should().Be("O nome do arquivo é inválido.");
        grande.Erro.Should().Be(TipoErro.Validacao);
        grande.Mensagem.Should().Be("O arquivo passa do limite de 50 MB.");
        new[] { semNome, nomeLongo, nomeComControle, arquivoComControle, grande }
            .Should().OnlyContain(r => r.Erro == TipoErro.Validacao);
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Armazenamento.Arquivos.Should().BeEmpty();

        var comCaminho = await Enviar(_pedro, id, arquivo: @"C:\Users\pedro\Documentos/contrato final.pdf");
        comCaminho.Sucesso.Should().BeTrue(comCaminho.Mensagem);
        _c.Documentos.Itens.Single().NomeArquivoOriginal.Should().Be("contrato final.pdf");
    }

    [Fact]
    public async Task Falha_ao_gravar_no_banco_remove_o_arquivo()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);
        _c.Unidade.Falha = new InvalidOperationException("banco fora do ar");

        var acao = () => Enviar(_pedro, id);

        await acao.Should().ThrowAsync<InvalidOperationException>().WithMessage("banco fora do ar");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Fact]
    public async Task Baixar_documento_de_atividade_depende_de_ser_responsavel_hoje()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);
        var documento = await EnviadoAsync(id);

        var antes = await Baixar(_pedro, documento);
        antes.Sucesso.Should().BeTrue(antes.Mensagem);
        antes.Valor!.Dispose();

        await DirecionarAsync(id, _ester);
        _c.Auditoria.Eventos.Clear();
        var depois = await Baixar(_pedro, documento);
        var ester = await Baixar(_ester, documento);
        var diretor = await Baixar(Diana, documento);

        depois.Erro.Should().Be(TipoErro.NaoEncontrado);
        depois.Mensagem.Should().Be("Documento não encontrado.");
        ester.Sucesso.Should().BeTrue("quem entrou na atividade ganha o acesso na hora");
        ester.Valor!.Dispose();
        diretor.Sucesso.Should().BeTrue();
        diretor.Valor!.Dispose();
        var negado = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        negado.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
        negado.Entidade.Should().Be("Documento");
        negado.EntidadeId.Should().Be(documento);
    }

    [Fact]
    public async Task Lista_do_cliente_traz_os_documentos_de_atividade_com_o_titulo_e_respeita_os_responsaveis()
    {
        var daqueles = await AtividadeAsync("Contestação", _maria.Id, _pedro);
        var dosOutros = await AtividadeAsync("Recurso", _maria.Id, _ester);
        var avulsa = await AtividadeAsync("Avulsa", null, _pedro);
        var primeiro = await EnviadoAsync(daqueles, "a.pdf");
        var segundo = await EnviadoAsync(dosOutros, "b.pdf");
        await EnviadoAsync(avulsa, "c.pdf");

        var diretoria = await ListarDoCliente(Diana, _maria.Id);
        var pedro = await ListarDoCliente(_pedro, _maria.Id);
        var geral = await ListarDoCliente(Diana, _maria.Id, CategoriaDocumento.DocumentoGeral);

        diretoria.Valor!.Total.Should().Be(2);
        diretoria.Valor.Itens.Select(d => (d.Id, d.AtividadeId, d.AtividadeTitulo)).Should().Equal(
            (segundo, dosOutros, "Recurso"), (primeiro, daqueles, "Contestação"));
        pedro.Valor!.Total.Should().Be(1, "o total conta só o que ele alcança");
        pedro.Valor.Itens.Select(d => (d.Id, d.AtividadeId, d.AtividadeTitulo)).Should().Equal((primeiro, daqueles, "Contestação"));
        geral.Valor!.Itens.Should().BeEmpty();

        await DirecionarAsync(daqueles, _ester);
        (await ListarDoCliente(_pedro, _maria.Id)).Valor!.Itens.Should().BeEmpty("saiu da atividade");
    }

    [Fact]
    public async Task Listagem_de_documentos_de_cliente_continua_sem_atividade()
    {
        var envio = await new EnviarDocumentoManipulador(
                _c.Como(Diana), _c.Documentos, _c.Clientes, _c.Perfis, _c.Armazenamento, _c.Unidade)
            .ManipularAsync(new EnviarDocumento(CategoriaDocumento.DocumentoGeral, _maria.Id, null, "geral.pdf", "application/pdf", Conteudo()), default);
        envio.Sucesso.Should().BeTrue(envio.Mensagem);

        var lista = await ListarDoCliente(Diana, _maria.Id, CategoriaDocumento.DocumentoGeral);

        var item = lista.Valor!.Itens.Should().ContainSingle().Subject;
        item.AtividadeId.Should().BeNull();
        item.AtividadeTitulo.Should().BeNull();
    }

    [Fact]
    public async Task Lista_da_atividade_vem_do_mais_recente_para_o_mais_antigo()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);
        var primeiro = await EnviadoAsync(id, "primeiro.pdf");
        var segundo = await EnviadoAsync(id, "segundo.pdf");
        var deOutra = await EnviadoAsync(await AtividadeAsync("Outra", _maria.Id), "outra.pdf");

        var lista = await ListarDaAtividade(_pedro, id);

        lista.Sucesso.Should().BeTrue(lista.Mensagem);
        lista.Valor!.Itens.Select(d => d.Id).Should().Equal(segundo, primeiro).And.NotContain(deOutra);
        lista.Valor.Total.Should().Be(2);
    }

    [Fact]
    public void Listar_a_atividade_e_uma_consulta_auditavel_de_documento()
    {
        var id = Guid.CreateVersion7();
        var consulta = new ListarDocumentosDaAtividade(id, 3);

        consulta.TipoRecurso.Should().Be("Documento");
        consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
        consulta.Detalhe.Should().Contain(id.ToString()).And.Contain("3");
    }

    [Fact]
    public async Task Trocar_o_cliente_da_atividade_leva_os_documentos()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);
        var documento = await EnviadoAsync(id);
        var salvamentos = _c.Unidade.Salvamentos;

        var salvo = await Salvar(id, _jose.Id);

        salvo.Sucesso.Should().BeTrue(salvo.Mensagem);
        _c.Documentos.Itens.Single(d => d.Id == documento).ClienteId.Should().Be(_jose.Id);
        _c.Unidade.Salvamentos.Should().Be(salvamentos + 1, "atividade e documentos são gravados juntos");
    }

    [Fact]
    public async Task Tirar_o_cliente_da_atividade_deixa_os_documentos_sem_cliente()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);
        var documento = await EnviadoAsync(id);

        var salvo = await Salvar(id, null);

        salvo.Sucesso.Should().BeTrue(salvo.Mensagem);
        _c.Documentos.Itens.Single(d => d.Id == documento).ClienteId.Should().BeNull();

        // E o caminho de volta: atividade avulsa que ganha cliente.
        (await Salvar(id, _maria.Id)).Sucesso.Should().BeTrue();
        _c.Documentos.Itens.Single(d => d.Id == documento).ClienteId.Should().Be(_maria.Id);
    }

    [Fact]
    public async Task Editar_sem_trocar_o_cliente_alinha_so_os_documentos_que_estavam_fora()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);
        var fora = await EnviadoAsync(id, "fora.pdf");
        var alinhado = await EnviadoAsync(id, "alinhado.pdf");
        _c.Documentos.Itens.Single(d => d.Id == fora).AcompanharClienteDaAtividade(_jose.Id);

        (await Salvar(id, _maria.Id)).Sucesso.Should().BeTrue();

        _c.Documentos.Itens.Single(d => d.Id == fora).ClienteId.Should().Be(_maria.Id, "o documento fora do cliente é realinhado");
        _c.Documentos.Itens.Single(d => d.Id == alinhado).ClienteId.Should().Be(_maria.Id);
    }

    [Fact]
    public async Task Cliente_trocado_entre_a_leitura_e_a_gravacao_leva_o_documento_ao_cliente_novo()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);
        _c.Atividades.ClienteAtualForcado[id] = _jose.Id;

        var enviado = await Enviar(_pedro, id);

        enviado.Sucesso.Should().BeTrue(enviado.Mensagem);
        _c.Documentos.Itens.Single().ClienteId.Should().Be(_jose.Id);
    }

    [Fact]
    public async Task Atividade_excluida_durante_o_envio_recusa_remove_o_arquivo_e_nao_grava()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);
        var salvamentos = _c.Unidade.Salvamentos;
        _c.Atividades.AntesDeLerOClienteAtual = alvo => _c.Atividades.Itens.RemoveAll(a => a.Id == alvo);

        var enviado = await Enviar(_pedro, id);

        enviado.Erro.Should().Be(TipoErro.NaoEncontrado);
        enviado.Mensagem.Should().Be("Atividade não encontrada.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(salvamentos);
    }

    [Fact]
    public async Task Definir_acesso_de_documento_de_atividade_e_recusado()
    {
        var id = await AtividadeAsync(cliente: _maria.Id, responsaveis: _pedro);
        var documento = await EnviadoAsync(id);

        var resultado = await new DefinirAcessoDoDocumentoManipulador(
                _c.Como(Diana), _c.Documentos, _c.Perfis, _c.Usuarios, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirAcessoDoDocumento(documento, [_c.PerfilProducao.Id], [_pedro.Id]), default);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O acesso a um documento de atividade é o dos responsáveis da atividade.");
        var acesso = await new ObterAcessoDoDocumentoManipulador(_c.Como(Diana), _c.Documentos)
            .ManipularAsync(new ObterAcessoDoDocumento(documento), default);
        acesso.Valor!.Perfis.Should().BeEmpty();
        acesso.Valor.Usuarios.Should().BeEmpty();
    }

    [Fact]
    public async Task Enviar_documento_de_cliente_continua_recusando_a_categoria_de_atividade()
    {
        var resultado = await new EnviarDocumentoManipulador(
                _c.Como(Diana), _c.Documentos, _c.Clientes, _c.Perfis, _c.Armazenamento, _c.Unidade)
            .ManipularAsync(new EnviarDocumento(
                CategoriaDocumento.DocumentoAtividade, _maria.Id, null, "x.pdf", "application/pdf", Conteudo()), default);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Categoria inválida para envio.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }
}
