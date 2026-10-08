using System.Text;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class DocumentosTests
{
    private readonly Cenario _c = new();
    private readonly ClientePessoaFisica _maria;
    private readonly Perfil _estagio = Perfil.Criar("Estagiário", TipoPerfil.Producao);
    private readonly Usuario _pedro;
    private readonly Usuario _ester;

    public DocumentosTests()
    {
        _maria = ClientePessoaFisica.Criar(
            new DadosDoCliente("Maria Silva", null, null, Endereco.Vazio, null), Cpf.Criar("529.982.247-25"), null, null);
        _c.Clientes.Itens.Add(_maria);
        _c.Perfis.Itens.Add(_estagio);
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _ester = _c.NovoUsuario("Ester", "ester@x.com", _estagio);
    }

    private static MemoryStream Conteudo(string texto = "conteúdo") => new(Encoding.UTF8.GetBytes(texto));

    private Task<Resultado<Guid>> Enviar(Usuario? ator, CategoriaDocumento categoria = CategoriaDocumento.DocumentoGeral,
        string arquivo = "procuracao.pdf", string? nome = null, Guid? cliente = null, Stream? conteudo = null,
        string? tipo = "application/pdf") =>
        new EnviarDocumentoManipulador(_c.Como(ator), _c.Documentos, _c.Clientes, _c.Perfis, _c.Armazenamento, _c.Unidade)
            .ManipularAsync(new EnviarDocumento(categoria, cliente ?? _maria.Id, nome, arquivo, tipo, conteudo ?? Conteudo()), default);

    private async Task<Documento> Enviado(CategoriaDocumento categoria = CategoriaDocumento.DocumentoGeral, string arquivo = "procuracao.pdf")
    {
        var resultado = await Enviar(_c.Diretor, categoria, arquivo);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return _c.Documentos.Itens.Single(d => d.Id == resultado.Valor);
    }

    private Task<Resultado<Pagina<DocumentoResumo>>> Listar(Usuario? ator,
        CategoriaDocumento categoria = CategoriaDocumento.DocumentoGeral, int pagina = 1) =>
        new ListarDocumentosDoClienteManipulador(_c.Como(ator), _c.Documentos)
            .ManipularAsync(new ListarDocumentosDoCliente(_maria.Id, categoria, pagina), default);

    private Task<Resultado<ArquivoParaBaixar>> Baixar(Usuario? ator, Guid id) =>
        new BaixarDocumentoManipulador(_c.Como(ator), _c.Documentos, _c.Armazenamento, _c.Auditoria)
            .ManipularAsync(new BaixarDocumento(id), default);

    private Task<Resultado> DefinirAcesso(Usuario? ator, Guid id, Guid[] perfis, Guid[] usuarios) =>
        new DefinirAcessoDoDocumentoManipulador(_c.Como(ator), _c.Documentos, _c.Perfis, _c.Usuarios, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirAcessoDoDocumento(id, perfis, usuarios), default);

    // ---- Enviar ----

    [Fact]
    public async Task Diretor_envia_documento_geral_liberado_para_os_perfis_ativos()
    {
        _estagio.Inativar();

        var resultado = await Enviar(_c.Diretor, conteudo: Conteudo("abc"));

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var documento = _c.Documentos.Itens.Should().ContainSingle().Subject;
        documento.Id.Should().Be(resultado.Valor);
        documento.Categoria.Should().Be(CategoriaDocumento.DocumentoGeral);
        documento.ClienteId.Should().Be(_maria.Id);
        documento.Nome.Should().Be("procuracao.pdf");
        documento.TipoConteudo.Should().Be("application/pdf");
        documento.TamanhoBytes.Should().Be(3);
        documento.HashSha256.Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        documento.PerfisComAcesso.Select(a => a.PerfilId)
            .Should().BeEquivalentTo([_c.PerfilDiretor.Id, _c.PerfilProducao.Id], "o perfil inativo fica de fora");
        _c.Armazenamento.Arquivos.Should().ContainKey(documento.ChaveArmazenamento);
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Contrato_de_honorarios_nasce_sem_ninguem_na_lista()
    {
        var contrato = await Enviado(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");

        contrato.Categoria.Should().Be(CategoriaDocumento.ContratoHonorarios);
        contrato.PerfisComAcesso.Should().BeEmpty();
    }

    [Theory]
    [InlineData(@"C:\Users\diana\Documents\contrato.pdf", "contrato.pdf")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("/tmp/laudo.pdf", "laudo.pdf")]
    [InlineData("  laudo final.pdf  ", "laudo final.pdf")]
    public async Task So_o_ultimo_segmento_do_nome_enviado_e_guardado(string enviado, string esperado)
    {
        var documento = await Enviado(arquivo: enviado);

        documento.NomeArquivoOriginal.Should().Be(esperado);
        documento.ChaveArmazenamento.Should().NotContain("passwd").And.NotContain("contrato").And.NotContain("laudo");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("pasta/")]
    public async Task Envio_sem_nome_de_arquivo_e_recusado_antes_de_gravar(string arquivo)
    {
        var resultado = await Enviar(_c.Diretor, arquivo: arquivo);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Informe o arquivo.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Tipo_de_conteudo_ausente_vira_o_padrao(string? tipo)
    {
        var resultado = await Enviar(_c.Diretor, tipo: tipo);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _c.Documentos.Itens.Single().TipoConteudo.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task Tipo_de_conteudo_longo_demais_vira_o_padrao()
    {
        (await Enviar(_c.Diretor, tipo: new string('x', 300))).Sucesso.Should().BeTrue();

        _c.Documentos.Itens.Single().TipoConteudo.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task Producao_nao_envia_e_nada_e_gravado()
    {
        var resultado = await Enviar(_pedro);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Documentos.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Documento_de_atividade_nao_e_enviado_por_aqui()
    {
        var resultado = await Enviar(_c.Diretor, CategoriaDocumento.DocumentoAtividade);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Categoria inválida para envio.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Fact]
    public async Task Cliente_inexistente_e_recusado_antes_de_gravar()
    {
        var resultado = await Enviar(_c.Diretor, cliente: Guid.CreateVersion7());

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Cliente não encontrado.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Fact]
    public async Task Arquivo_vazio_e_recusado_e_nao_fica_no_armazenamento()
    {
        var resultado = await Enviar(_c.Diretor, conteudo: Conteudo(""));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O arquivo está vazio.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Arquivo_acima_do_limite_e_recusado_com_o_limite_na_mensagem()
    {
        _c.Armazenamento.RecusarPorTamanho = true;

        var resultado = await Enviar(_c.Diretor);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O arquivo passa do limite de 50 MB.");
        _c.Documentos.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Falha_ao_gravar_no_banco_remove_o_arquivo_e_sobe()
    {
        _c.Unidade.Falha = new InvalidOperationException("banco fora do ar");

        var acao = () => Enviar(_c.Diretor);

        await acao.Should().ThrowAsync<InvalidOperationException>().WithMessage("banco fora do ar");
        _c.Armazenamento.Arquivos.Should().BeEmpty("o arquivo não pode ficar sem registro");
    }

    [Fact]
    public async Task Falha_na_limpeza_nao_troca_a_recusa_do_dominio()
    {
        _c.Armazenamento.FalharAoRemover = true;

        var resultado = await Enviar(_c.Diretor, conteudo: Conteudo(""));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O arquivo está vazio.");
    }

    [Fact]
    public async Task Falha_na_limpeza_nao_troca_a_excecao_original()
    {
        _c.Armazenamento.FalharAoRemover = true;
        _c.Unidade.Falha = new InvalidOperationException("banco fora do ar");

        var acao = () => Enviar(_c.Diretor);

        await acao.Should().ThrowAsync<InvalidOperationException>().WithMessage("banco fora do ar");
    }

    [Fact]
    public async Task Nome_de_arquivo_longo_demais_e_recusado_antes_de_gravar()
    {
        var resultado = await Enviar(_c.Diretor, arquivo: new string('a', Documento.TamanhoMaximoNome + 1));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be($"O nome do arquivo deve ter até {Documento.TamanhoMaximoNome} caracteres.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Fact]
    public async Task Nome_de_documento_longo_demais_e_recusado_antes_de_gravar()
    {
        var resultado = await Enviar(_c.Diretor, nome: new string('a', Documento.TamanhoMaximoNome + 1));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be($"O nome do documento deve ter até {Documento.TamanhoMaximoNome} caracteres.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Theory]
    [InlineData("laudo\u0000.pdf")]
    [InlineData("la\tudo.pdf")]
    [InlineData("laudo\u007f.pdf")]
    public async Task Caractere_de_controle_no_nome_do_arquivo_e_recusado_antes_de_gravar(string arquivo)
    {
        var resultado = await Enviar(_c.Diretor, arquivo: arquivo);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O nome do arquivo é inválido.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Procuração\r\nX-Injetado: 1")]
    [InlineData("Procu\u0000ração")]
    public async Task Caractere_de_controle_no_nome_do_documento_e_recusado_antes_de_gravar(string nome)
    {
        var resultado = await Enviar(_c.Diretor, nome: nome);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O nome do documento é inválido.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Fact]
    public async Task Tipo_de_conteudo_com_caractere_de_controle_vira_o_padrao()
    {
        (await Enviar(_c.Diretor, tipo: "text/plain\r\nX-Injetado: 1")).Sucesso.Should().BeTrue();

        _c.Documentos.Itens.Single().TipoConteudo.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task Sem_usuario_nenhuma_operacao_de_documento_e_permitida()
    {
        var documento = await Enviado();
        var id = documento.Id;

        (await Enviar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await new RenomearDocumentoManipulador(_c.Como(null), _c.Documentos, _c.Unidade)
            .ManipularAsync(new RenomearDocumento(id, "x"), default)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await new ExcluirDocumentoManipulador(_c.Como(null), _c.Documentos, _c.Unidade)
            .ManipularAsync(new ExcluirDocumento(id), default)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await new ObterAcessoDoDocumentoManipulador(_c.Como(null), _c.Documentos)
            .ManipularAsync(new ObterAcessoDoDocumento(id), default)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await DefinirAcesso(null, id, [], [])).Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Documentos.Itens.Should().ContainSingle();
    }

    // ---- Listar ----

    [Fact]
    public async Task Diretoria_lista_todos_e_producao_so_os_que_pode_acessar()
    {
        var liberado = await Enviado(arquivo: "a-liberado.pdf");
        var restrito = await Enviado(arquivo: "b-restrito.pdf");
        var soDaEster = await Enviado(arquivo: "c-da-ester.pdf");
        (await DefinirAcesso(_c.Diretor, restrito.Id, [_c.PerfilDiretor.Id], [])).Sucesso.Should().BeTrue();
        (await DefinirAcesso(_c.Diretor, soDaEster.Id, [], [_ester.Id])).Sucesso.Should().BeTrue();

        (await Listar(_c.Diretor)).Valor!.Itens.Select(d => d.Id)
            .Should().Equal(soDaEster.Id, restrito.Id, liberado.Id);
        (await Listar(_pedro)).Valor!.Itens.Select(d => d.Id).Should().Equal(liberado.Id);
        // O perfil da Ester estava ativo quando "liberado" foi enviado: ela o alcança pelo perfil.
        (await Listar(_ester)).Valor!.Itens.Select(d => d.Id).Should().Equal(soDaEster.Id, liberado.Id);
    }

    [Fact]
    public async Task Producao_nao_lista_contratos_de_honorarios()
    {
        await Enviado(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");

        (await Listar(_pedro, CategoriaDocumento.ContratoHonorarios)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Listar(_c.Diretor, CategoriaDocumento.ContratoHonorarios)).Valor!.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Sem_usuario_nao_lista()
    {
        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Pagina_absurda_devolve_lista_vazia_com_o_total()
    {
        await Enviado();

        var pagina = (await Listar(_c.Diretor, pagina: int.MaxValue)).Valor!;

        pagina.Itens.Should().BeEmpty();
        pagina.Total.Should().Be(1);
    }

    [Fact]
    public void Listagem_e_consulta_auditavel_com_os_filtros_no_detalhe()
    {
        IConsultaAuditavel consulta = new ListarDocumentosDoCliente(_maria.Id, CategoriaDocumento.ContratoHonorarios, 2);

        consulta.TipoRecurso.Should().Be("Documento");
        consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
        consulta.RecursoId.Should().BeNull();
        consulta.Detalhe.Should().Contain(_maria.Id.ToString()).And.Contain("\"Pagina\":2");
    }

    // ---- Baixar ----

    [Fact]
    public async Task Diretor_baixa_o_conteudo_com_o_nome_do_documento()
    {
        var documento = await Enviado();
        documento.Renomear("Procuração assinada");

        var resultado = await Baixar(_c.Diretor, documento.Id);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        resultado.Valor!.NomeArquivo.Should().Be("Procuração assinada.pdf");
        resultado.Valor.TamanhoBytes.Should().Be(documento.TamanhoBytes);
        using var leitor = new StreamReader(resultado.Valor.Conteudo, Encoding.UTF8);
        (await leitor.ReadToEndAsync()).Should().Be("conteúdo");
    }

    [Fact]
    public async Task Producao_com_acesso_baixa()
    {
        var documento = await Enviado();

        (await Baixar(_pedro, documento.Id)).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Producao_sem_acesso_recebe_nao_encontrado_e_a_tentativa_fica_registrada()
    {
        var documento = await Enviado();
        (await DefinirAcesso(_c.Diretor, documento.Id, [_c.PerfilDiretor.Id], [])).Sucesso.Should().BeTrue();
        _c.Auditoria.Eventos.Clear();

        var resultado = await Baixar(_pedro, documento.Id);

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be("Documento não encontrado.");
        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
        evento.Entidade.Should().Be("Documento");
        evento.EntidadeId.Should().Be(documento.Id);
        evento.Imediato.Should().BeTrue();
    }

    [Fact]
    public async Task Contrato_responde_nao_encontrado_para_producao()
    {
        var contrato = await Enviado(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");

        var resultado = await Baixar(_pedro, contrato.Id);

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        _c.Auditoria.Eventos.Should().ContainSingle(e => e.Evento == EventosDeSeguranca.AcessoNegado);
    }

    [Fact]
    public async Task Documento_inexistente_e_nao_encontrado_sem_evento()
    {
        var resultado = await Baixar(_pedro, Guid.CreateVersion7());

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be("Documento não encontrado.");
        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Sem_usuario_nao_baixa()
    {
        var documento = await Enviado();

        (await Baixar(null, documento.Id)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Arquivo_sumido_do_armazenamento_e_falha_de_servidor_e_fica_registrado()
    {
        var documento = await Enviado();
        var chave = documento.ChaveArmazenamento;
        _c.Armazenamento.Arquivos.Clear();
        _c.Auditoria.Eventos.Clear();

        var acao = () => Baixar(_c.Diretor, documento.Id);

        var excecao = (await acao.Should().ThrowAsync<ExcecaoDeArquivoAusente>()).Which;
        excecao.Message.Should().Contain(documento.Id.ToString()).And.Contain(chave);
        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeDocumento.ArquivoAusente);
        evento.Entidade.Should().Be("Documento");
        evento.EntidadeId.Should().Be(documento.Id);
        evento.Imediato.Should().BeTrue();
        evento.Dados.Should().BeEquivalentTo(new { chave });
    }

    [Fact]
    public async Task Arquivo_sumido_para_quem_nao_tem_acesso_continua_nao_encontrado()
    {
        var documento = await Enviado();
        (await DefinirAcesso(_c.Diretor, documento.Id, [_c.PerfilDiretor.Id], [])).Sucesso.Should().BeTrue();
        _c.Armazenamento.Arquivos.Clear();
        _c.Auditoria.Eventos.Clear();

        var resultado = await Baixar(_pedro, documento.Id);

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        _c.Auditoria.Eventos.Should().ContainSingle().Which.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
        _c.Auditoria.Eventos.Should().NotContain(e => e.Evento == EventosDeDocumento.ArquivoAusente);
    }

    [Fact]
    public void Baixar_e_consulta_auditavel_do_documento()
    {
        var id = Guid.CreateVersion7();
        IConsultaAuditavel consulta = new BaixarDocumento(id);

        consulta.TipoRecurso.Should().Be("Documento");
        consulta.Acao.Should().Be(AcaoDeAcesso.Baixou);
        consulta.RecursoId.Should().Be(id);
    }

    // ---- Renomear e excluir ----

    [Fact]
    public async Task Diretor_renomeia()
    {
        var documento = await Enviado();

        var resultado = await new RenomearDocumentoManipulador(_c.Como(_c.Diretor), _c.Documentos, _c.Unidade)
            .ManipularAsync(new RenomearDocumento(documento.Id, " Procuração 2026 "), default);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        documento.Nome.Should().Be("Procuração 2026");
    }

    [Fact]
    public async Task Renomear_valida_e_respeita_o_acesso()
    {
        var documento = await Enviado();
        Task<Resultado> Renomear(Usuario? ator, Guid id, string nome) =>
            new RenomearDocumentoManipulador(_c.Como(ator), _c.Documentos, _c.Unidade)
                .ManipularAsync(new RenomearDocumento(id, nome), default);

        (await Renomear(_pedro, documento.Id, "x")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Renomear(_c.Diretor, documento.Id, " ")).Erro.Should().Be(TipoErro.Validacao);
        (await Renomear(_c.Diretor, Guid.CreateVersion7(), "x")).Erro.Should().Be(TipoErro.NaoEncontrado);
        documento.Nome.Should().Be("procuracao.pdf");
    }

    [Fact]
    public async Task Diretor_exclui_o_documento_e_o_arquivo_permanece_no_armazenamento()
    {
        var documento = await Enviado();

        var resultado = await new ExcluirDocumentoManipulador(_c.Como(_c.Diretor), _c.Documentos, _c.Unidade)
            .ManipularAsync(new ExcluirDocumento(documento.Id), default);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Armazenamento.Arquivos.Should().ContainKey(documento.ChaveArmazenamento);
    }

    [Fact]
    public async Task Producao_nao_exclui()
    {
        var documento = await Enviado();

        var resultado = await new ExcluirDocumentoManipulador(_c.Como(_pedro), _c.Documentos, _c.Unidade)
            .ManipularAsync(new ExcluirDocumento(documento.Id), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Documentos.Itens.Should().ContainSingle();
    }

    // ---- Lista de acesso ----

    [Fact]
    public async Task Diretor_ve_a_lista_de_acesso()
    {
        var documento = await Enviado();
        (await DefinirAcesso(_c.Diretor, documento.Id, [_c.PerfilDiretor.Id], [_ester.Id])).Sucesso.Should().BeTrue();

        var resultado = await new ObterAcessoDoDocumentoManipulador(_c.Como(_c.Diretor), _c.Documentos)
            .ManipularAsync(new ObterAcessoDoDocumento(documento.Id), default);

        resultado.Valor.Should().BeEquivalentTo(new AcessoDoDocumento(
            documento.Id, "procuracao.pdf", CategoriaDocumento.DocumentoGeral, [_c.PerfilDiretor.Id], [_ester.Id]));
    }

    [Fact]
    public async Task Producao_nao_ve_a_lista_de_acesso()
    {
        var documento = await Enviado();

        var resultado = await new ObterAcessoDoDocumentoManipulador(_c.Como(_pedro), _c.Documentos)
            .ManipularAsync(new ObterAcessoDoDocumento(documento.Id), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Definir_acesso_anota_o_evento_com_o_que_mudou()
    {
        // O perfil Estagiário estava inativo no envio: o documento nasce só com Diretor e Produção.
        _estagio.Inativar();
        var documento = await Enviado();
        _estagio.Ativar();

        var resultado = await DefinirAcesso(_c.Diretor, documento.Id, [_c.PerfilDiretor.Id, _estagio.Id], [_pedro.Id]);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.DocumentoAcessoAlterado);
        evento.Entidade.Should().Be("Documento");
        evento.EntidadeId.Should().Be(documento.Id);
        evento.Imediato.Should().BeFalse("o evento é gravado na mesma transação da mudança");
        evento.Dados.Should().BeEquivalentTo(new
        {
            perfisAdicionados = new[] { _estagio.Id },
            perfisRemovidos = new[] { _c.PerfilProducao.Id },
            usuariosAdicionados = new[] { _pedro.Id },
            usuariosRemovidos = Array.Empty<Guid>(),
        });
    }

    [Fact]
    public async Task Definir_o_mesmo_acesso_nao_anota_evento()
    {
        var documento = await Enviado();

        var salvamentos = _c.Unidade.Salvamentos;

        (await DefinirAcesso(_c.Diretor, documento.Id, [_c.PerfilDiretor.Id, _c.PerfilProducao.Id, _estagio.Id], []))
            .Sucesso.Should().BeTrue();

        _c.Auditoria.Eventos.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(salvamentos, "sem mudança nada é salvo");
    }

    [Fact]
    public async Task Contrato_nao_e_liberado_para_producao_nem_por_perfil_nem_por_usuario()
    {
        var contrato = await Enviado(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");

        var porPerfil = await DefinirAcesso(_c.Diretor, contrato.Id, [_c.PerfilProducao.Id], []);
        var porUsuario = await DefinirAcesso(_c.Diretor, contrato.Id, [], [_pedro.Id]);

        porPerfil.Erro.Should().Be(TipoErro.Validacao);
        porPerfil.Mensagem.Should().Be(Documento.ContratoSoParaDiretoria);
        porUsuario.Erro.Should().Be(TipoErro.Validacao);
        contrato.PerfisComAcesso.Should().BeEmpty();
        contrato.UsuariosComAcesso.Should().BeEmpty();
        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Perfil_ou_usuario_inexistente_e_recusado()
    {
        var documento = await Enviado();

        (await DefinirAcesso(_c.Diretor, documento.Id, [Guid.CreateVersion7()], [])).Mensagem.Should().Be("Perfil não encontrado.");
        (await DefinirAcesso(_c.Diretor, documento.Id, [], [Guid.CreateVersion7()])).Mensagem.Should().Be("Usuário não encontrado.");
        documento.PerfisComAcesso.Should().HaveCount(3, "Diretor, Produção e Estagiário, como no envio");
    }

    [Fact]
    public async Task Producao_nao_define_acesso()
    {
        var documento = await Enviado();

        (await DefinirAcesso(_pedro, documento.Id, [], [_pedro.Id])).Erro.Should().Be(TipoErro.AcessoNegado);
        documento.UsuariosComAcesso.Should().BeEmpty();
    }

    [Fact]
    public async Task Conflito_ao_gravar_a_lista_vira_mensagem_de_recarregar()
    {
        var documento = await Enviado();
        _c.Unidade.Falha = new ExcecaoDeConflito("duplicado");

        var resultado = await DefinirAcesso(_c.Diretor, documento.Id, [_c.PerfilDiretor.Id], []);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("A lista de acesso foi alterada por outra pessoa. Recarregue e tente novamente.");
    }
}
