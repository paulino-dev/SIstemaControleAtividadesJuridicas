using System.Text;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Solicitacoes;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.UnitTests.Application;

public class SolicitacoesDoPortalTests
{
    private const string Descricao = "Preciso de uma certidão atualizada.";

    private readonly Cenario _c = new();
    private readonly Cliente _maria;
    private readonly Cliente _jose;
    private readonly Usuario _pedro;
    private readonly Usuario _carla;
    private readonly Usuario _ana;
    private readonly Usuario _joao;

    public SolicitacoesDoPortalTests()
    {
        _maria = _c.NovoCliente("Maria Silva", "529.982.247-25");
        _jose = _c.NovoCliente("José Souza", "111.444.777-35");
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _carla = _c.NovoUsuarioDeCliente("Carla Cliente", "carla@x.com", _maria);
        _ana = _c.NovoUsuarioDeCliente("Ana Cliente", "ana@x.com", _maria);
        _joao = _c.NovoUsuarioDeCliente("João Cliente", "joao@x.com", _jose);
    }

    private static MemoryStream Conteudo(string texto = "conteúdo") => new(Encoding.UTF8.GetBytes(texto));

    private static ArquivoParaEnviar Arquivo(string nome = "certidao.pdf", string texto = "conteúdo") =>
        new(nome, "application/pdf", Conteudo(texto));

    private static List<ArquivoParaEnviar> Arquivos(int quantidade) =>
        [.. Enumerable.Range(1, quantidade).Select(i => Arquivo($"documento-{i:00}.pdf", $"conteúdo {i}"))];

    private static ArquivoDoDocumento ArquivoGravado(string chave = "2026/10/chave") =>
        new("laudo.pdf", "application/pdf", 10, new string('a', 64), chave);

    private Task<Resultado<Guid>> Abrir(
        Usuario? ator, string descricao = Descricao, UrgenciaDaSolicitacao urgencia = UrgenciaDaSolicitacao.Media,
        IReadOnlyList<ArquivoParaEnviar>? arquivos = null, IArmazenamentoDeArquivos? armazenamento = null) =>
        new AbrirSolicitacaoManipulador(
                _c.Como(ator), _c.Solicitacoes, _c.Documentos, armazenamento ?? _c.Armazenamento, _c.Unidade)
            .ManipularAsync(new AbrirSolicitacao(descricao, urgencia, arquivos ?? []), default);

    private Task<Resultado<Pagina<SolicitacaoDoPortal>>> Listar(Usuario? ator, ListarMinhasSolicitacoes? consulta = null) =>
        new ListarMinhasSolicitacoesManipulador(_c.Como(ator), _c.Solicitacoes)
            .ManipularAsync(consulta ?? new ListarMinhasSolicitacoes(), default);

    private Task<Resultado<MinhaSolicitacaoDetalhe>> Obter(Usuario? ator, Guid id) =>
        new ObterMinhaSolicitacaoManipulador(_c.Como(ator), _c.Solicitacoes, _c.Documentos)
            .ManipularAsync(new ObterMinhaSolicitacao(id), default);

    private Task<Resultado> Alterar(
        Usuario? ator, Guid id, string descricao = "Nova descrição.", UrgenciaDaSolicitacao urgencia = UrgenciaDaSolicitacao.Alta,
        uint versao = 0) =>
        new AlterarMinhaSolicitacaoManipulador(_c.Como(ator), _c.Solicitacoes, _c.Unidade)
            .ManipularAsync(new AlterarMinhaSolicitacao(id, descricao, urgencia, versao), default);

    private Task<Resultado<Guid>> Anexar(Usuario? ator, Guid id, string arquivo = "extra.pdf", uint versao = 0) =>
        new AnexarDocumentoASolicitacaoManipulador(
                _c.Como(ator), _c.Solicitacoes, _c.Documentos, _c.Armazenamento, _c.Unidade)
            .ManipularAsync(new AnexarDocumentoASolicitacao(id, arquivo, "application/pdf", Conteudo(), versao), default);

    private Task<Resultado> Remover(Usuario? ator, Guid documento, uint versao = 0) =>
        new RemoverDocumentoDaSolicitacaoManipulador(_c.Como(ator), _c.Solicitacoes, _c.Documentos, _c.Unidade)
            .ManipularAsync(new RemoverDocumentoDaSolicitacao(documento, versao), default);

    private Task<Resultado> Cancelar(Usuario? ator, Guid id, uint versao = 0) =>
        new CancelarMinhaSolicitacaoManipulador(_c.Como(ator), _c.Solicitacoes, _c.Relogio, _c.Unidade)
            .ManipularAsync(new CancelarMinhaSolicitacao(id, versao), default);

    private async Task<Guid> AbertaAsync(
        Usuario autor, string descricao = Descricao, int arquivos = 0,
        UrgenciaDaSolicitacao urgencia = UrgenciaDaSolicitacao.Media)
    {
        var resultado = await Abrir(autor, descricao, urgencia, Arquivos(arquivos));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private SolicitacaoDoCliente Solicitacao(Guid id) => _c.Solicitacoes.Itens.Single(s => s.Id == id);

    private List<Documento> DocumentosDa(Guid solicitacao) =>
        [.. _c.Documentos.Itens.Where(d => d.SolicitacaoId == solicitacao)];

    /// <summary>Os quatro comandos que agem sobre uma solicitação que já existe, na mesma forma.</summary>
    private (string Nome, Func<Usuario?, uint, Task<Resultado>> Executar)[] Comandos(Guid solicitacao, Guid documento) =>
    [
        ("Alterar", (ator, versao) => Alterar(ator, solicitacao, versao: versao)),
        ("Anexar", async (ator, versao) => await Anexar(ator, solicitacao, versao: versao)),
        ("Remover", (ator, versao) => Remover(ator, documento, versao)),
        ("Cancelar", (ator, versao) => Cancelar(ator, solicitacao, versao)),
    ];

    private async Task<Guid> AtividadeAsync(string titulo)
    {
        var resultado = await new SalvarAtividadeManipulador(
                _c.Como(_c.Diretor), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(null, titulo, "Texto.", new DateOnly(2026, 10, 20), _maria.Id, null, null, 1), default);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private async Task ConcluirAsync(Guid atividade)
    {
        (await new DefinirResponsaveisDaAtividadeManipulador(
                _c.Como(_c.Diretor), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirResponsaveisDaAtividade(
                atividade, [new ResponsavelEntrada(_pedro.Id, "Redigir")], _c.Diretor.Id), default)).Sucesso.Should().BeTrue();
        (await new ConcluirMinhaParteManipulador(_c.Como(_pedro), _c.Atividades, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new ConcluirMinhaParte(atividade), default)).Sucesso.Should().BeTrue();
        (await new AprovarAtividadeManipulador(_c.Como(_c.Diretor), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new AprovarAtividade(atividade), default)).Sucesso.Should().BeTrue();
    }

    // Regra 1: o ator.

    [Fact]
    public async Task Equipe_recebe_acesso_negado_nas_requisicoes_do_portal()
    {
        var daCarla = await AbertaAsync(_carla, arquivos: 1);
        var documento = DocumentosDa(daCarla).Single().Id;
        var salvamentos = _c.Unidade.Salvamentos;
        var arquivosGravados = _c.Armazenamento.Arquivos.Count;

        foreach (var ator in new Usuario?[] { _c.Diretor, _pedro, null })
        {
            (await Abrir(ator, arquivos: [Arquivo()])).Erro.Should().Be(TipoErro.AcessoNegado);
            (await Listar(ator)).Erro.Should().Be(TipoErro.AcessoNegado);
            (await Obter(ator, daCarla)).Erro.Should().Be(TipoErro.AcessoNegado);
            (await Alterar(ator, daCarla)).Erro.Should().Be(TipoErro.AcessoNegado);
            (await Anexar(ator, daCarla)).Erro.Should().Be(TipoErro.AcessoNegado);
            (await Remover(ator, documento)).Erro.Should().Be(TipoErro.AcessoNegado);
            (await Cancelar(ator, daCarla)).Erro.Should().Be(TipoErro.AcessoNegado);
        }

        _c.Solicitacoes.Itens.Should().ContainSingle();
        _c.Armazenamento.Arquivos.Should().HaveCount(arquivosGravados);
        _c.Unidade.Salvamentos.Should().Be(salvamentos);
        Solicitacao(daCarla).Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
    }

    [Fact]
    public async Task Acesso_negado_diz_acesso_negado()
    {
        var resultado = await Listar(_c.Diretor);

        resultado.Mensagem.Should().Be("Acesso negado.");
        resultado.Valor.Should().BeNull();
    }

    [Fact]
    public void Requisicoes_do_portal_estao_abertas_ao_cliente_e_nao_tem_campo_de_cliente()
    {
        Type[] tipos =
        [
            typeof(AbrirSolicitacao), typeof(ListarMinhasSolicitacoes), typeof(ObterMinhaSolicitacao),
            typeof(AlterarMinhaSolicitacao), typeof(AnexarDocumentoASolicitacao), typeof(RemoverDocumentoDaSolicitacao),
            typeof(CancelarMinhaSolicitacao),
        ];

        foreach (var tipo in tipos)
        {
            typeof(IRequisicaoAbertaAoCliente).IsAssignableFrom(tipo).Should().BeTrue(tipo.Name);
            tipo.GetProperties().Select(p => p.Name).Should().NotContain(n => n.Contains("Cliente"), tipo.Name);
        }
    }

    [Fact]
    public void Consultas_do_portal_sao_auditaveis_no_recurso_Solicitacao()
    {
        var id = Guid.CreateVersion7();

        IConsultaAuditavel lista = new ListarMinhasSolicitacoes(SituacaoParaOCliente.Cancelada, 2);
        IConsultaAuditavel ficha = new ObterMinhaSolicitacao(id);

        lista.TipoRecurso.Should().Be("Solicitacao");
        lista.Acao.Should().Be(AcaoDeAcesso.Listou);
        lista.RecursoId.Should().BeNull();
        lista.Detalhe.Should().Be("""{"Situacao":"Cancelada","Pagina":2}""");
        ficha.TipoRecurso.Should().Be("Solicitacao");
        ficha.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        ficha.RecursoId.Should().Be(id);
    }

    // Abrir.

    [Fact]
    public async Task Cliente_abre_a_solicitacao_com_arquivos_numa_unica_gravacao()
    {
        var resultado = await Abrir(
            _carla, "  Preciso de uma certidão atualizada.  ", UrgenciaDaSolicitacao.Alta,
            [Arquivo("um.pdf", "primeiro"), Arquivo("dois.pdf", "segundo")]);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var solicitacao = _c.Solicitacoes.Itens.Should().ContainSingle().Subject;
        solicitacao.Id.Should().Be(resultado.Valor);
        solicitacao.ClienteId.Should().Be(_maria.Id, "o cliente é o do usuário que pede, nunca o informado");
        solicitacao.SolicitanteId.Should().Be(_carla.Id);
        solicitacao.Descricao.Should().Be(Descricao);
        solicitacao.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        var documentos = DocumentosDa(solicitacao.Id);
        documentos.Select(d => d.NomeArquivoOriginal).Should().Equal("um.pdf", "dois.pdf");
        documentos.Should().OnlyContain(d => d.Categoria == CategoriaDocumento.DocumentoDaSolicitacao && d.ClienteId == _maria.Id);
        documentos.Select(d => d.ChaveArmazenamento).Should().BeEquivalentTo(_c.Armazenamento.Arquivos.Keys);
        _c.Unidade.Salvamentos.Should().Be(1, "a solicitação e os documentos entram na mesma gravação");
    }

    [Fact]
    public async Task Abrir_sem_arquivos_e_valido()
    {
        var resultado = await Abrir(_carla);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _c.Solicitacoes.Itens.Should().ContainSingle();
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Abrir_com_dez_arquivos_e_aceito()
    {
        var resultado = await Abrir(_carla, arquivos: Arquivos(SolicitacoesOpcoes.MaximoDeDocumentos));

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        DocumentosDa(resultado.Valor).Should().HaveCount(10);
    }

    [Fact]
    public async Task Abrir_com_onze_arquivos_e_recusado_sem_gravar_arquivo()
    {
        var resultado = await Abrir(_carla, arquivos: Arquivos(SolicitacoesOpcoes.MaximoDeDocumentos + 1));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(SolicitacoesOpcoes.LimiteDeDocumentos);
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Solicitacoes.Itens.Should().BeEmpty();
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Theory]
    [InlineData("", "A descrição da solicitação é obrigatória.")]
    [InlineData("   ", "A descrição da solicitação é obrigatória.")]
    public async Task Abrir_com_descricao_invalida_nao_grava_arquivo(string descricao, string mensagem)
    {
        var resultado = await Abrir(_carla, descricao, arquivos: [Arquivo(), Arquivo("outro.pdf")]);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(mensagem);
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Solicitacoes.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Abrir_com_descricao_longa_ou_urgencia_invalida_nao_grava_arquivo()
    {
        var longa = await Abrir(_carla, new string('d', SolicitacaoDoCliente.TamanhoMaximoDescricao + 1), arquivos: [Arquivo()]);
        var urgencia = await Abrir(_carla, urgencia: (UrgenciaDaSolicitacao)99, arquivos: [Arquivo()]);

        longa.Erro.Should().Be(TipoErro.Validacao);
        longa.Mensagem.Should().Be($"A descrição da solicitação deve ter até {SolicitacaoDoCliente.TamanhoMaximoDescricao} caracteres.");
        urgencia.Erro.Should().Be(TipoErro.Validacao);
        urgencia.Mensagem.Should().Be("Urgência inválida.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Solicitacoes.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Abrir_com_um_arquivo_invalido_nao_grava_nenhum()
    {
        var resultado = await Abrir(_carla, arquivos: [Arquivo("bom.pdf"), Arquivo("")]);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Informe o arquivo.");
        _c.Armazenamento.Arquivos.Should().BeEmpty("todos são validados antes de o primeiro ser gravado");
        _c.Solicitacoes.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Abrir_com_arquivo_vazio_remove_os_ja_gravados_e_nao_cria_a_solicitacao()
    {
        var resultado = await Abrir(_carla, arquivos: [Arquivo("bom.pdf"), Arquivo("vazio.pdf", "")]);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O arquivo está vazio.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Solicitacoes.Itens.Should().BeEmpty();
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Abrir_com_o_segundo_arquivo_acima_do_limite_remove_o_primeiro()
    {
        var armazenamento = new ArmazenamentoQueFalhaNaGravacao(
            _c.Armazenamento, 2, new ExcecaoDeArquivoGrandeDemais(DocumentosOpcoes.TamanhoMaximoBytes));

        var resultado = await Abrir(_carla, arquivos: Arquivos(3), armazenamento: armazenamento);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O arquivo passa do limite de 50 MB.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Solicitacoes.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Abrir_com_falha_do_armazenamento_remove_os_ja_gravados_e_deixa_a_falha_subir()
    {
        var armazenamento = new ArmazenamentoQueFalhaNaGravacao(_c.Armazenamento, 2, new IOException("disco cheio"));

        var abrir = () => Abrir(_carla, arquivos: Arquivos(3), armazenamento: armazenamento);

        await abrir.Should().ThrowAsync<IOException>();
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Solicitacoes.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Abrir_com_falha_ao_gravar_no_banco_remove_todos_os_arquivos()
    {
        _c.Unidade.Falha = new InvalidOperationException("banco indisponível");

        var abrir = () => Abrir(_carla, arquivos: Arquivos(2));

        await abrir.Should().ThrowAsync<InvalidOperationException>();
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Fact]
    public async Task Falha_ao_remover_o_arquivo_nao_troca_o_desfecho_original()
    {
        _c.Armazenamento.FalharAoRemover = true;

        var resultado = await Abrir(_carla, arquivos: [Arquivo("bom.pdf"), Arquivo("vazio.pdf", "")]);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O arquivo está vazio.");
    }

    // Listar.

    [Fact]
    public async Task Cliente_lista_so_as_solicitacoes_do_proprio_cliente_da_mais_recente_para_a_mais_antiga()
    {
        var primeira = await AbertaAsync(_carla, "Primeira da Maria.", arquivos: 2, urgencia: UrgenciaDaSolicitacao.Baixa);
        await AbertaAsync(_joao, "Do José.");
        var segunda = await AbertaAsync(_ana, "Segunda da Maria.", urgencia: UrgenciaDaSolicitacao.Alta);

        var daCarla = (await Listar(_carla)).Valor!;
        var daAna = (await Listar(_ana)).Valor!;

        daCarla.Itens.Select(s => s.Id).Should().Equal(segunda, primeira);
        daCarla.Total.Should().Be(2);
        daAna.Itens.Select(s => s.Id).Should().Equal(
            new[] { segunda, primeira }, "quem é do mesmo cliente vê as solicitações dos colegas");
        var linha = daCarla.Itens[1];
        linha.Trecho.Should().Be("Primeira da Maria.");
        linha.Urgencia.Should().Be(UrgenciaDaSolicitacao.Baixa);
        linha.SolicitanteNome.Should().Be("Carla Cliente");
        linha.Situacao.Should().Be(SituacaoParaOCliente.AguardandoAnalise);
        linha.Documentos.Should().Be(2);
        daCarla.Itens[0].SolicitanteNome.Should().Be("Ana Cliente");
        (await Listar(_joao)).Valor!.Itens.Should().ContainSingle().Which.Trecho.Should().Be("Do José.");
    }

    [Fact]
    public async Task Listar_filtra_pela_situacao_e_pagina()
    {
        var cancelada = await AbertaAsync(_carla, "Cancelada.");
        (await Cancelar(_carla, cancelada)).Sucesso.Should().BeTrue();
        for (var i = 1; i <= SolicitacoesOpcoes.TamanhoDaPagina; i++) await AbertaAsync(_carla, $"Aguardando {i}.");

        var aguardando = (await Listar(_carla, new ListarMinhasSolicitacoes(SituacaoParaOCliente.AguardandoAnalise))).Valor!;
        var canceladas = (await Listar(_carla, new ListarMinhasSolicitacoes(SituacaoParaOCliente.Cancelada))).Valor!;
        var todasNaSegunda = (await Listar(_carla, new ListarMinhasSolicitacoes(Pagina: 2))).Valor!;

        aguardando.Total.Should().Be(SolicitacoesOpcoes.TamanhoDaPagina);
        canceladas.Itens.Select(s => s.Id).Should().Equal(cancelada);
        canceladas.Itens.Single().Situacao.Should().Be(SituacaoParaOCliente.Cancelada);
        todasNaSegunda.Total.Should().Be(SolicitacoesOpcoes.TamanhoDaPagina + 1);
        todasNaSegunda.Numero.Should().Be(2);
        todasNaSegunda.Itens.Select(s => s.Id).Should().Equal(cancelada);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task Listar_com_situacao_fora_do_enum_responde_validacao(int situacao)
    {
        var resultado = await Listar(_carla, new ListarMinhasSolicitacoes((SituacaoParaOCliente)situacao));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Situação inválida.");
    }

    // Obter.

    [Fact]
    public async Task Obter_traz_a_ficha_com_os_documentos_e_quem_pode_alterar()
    {
        var id = await AbertaAsync(_carla, "Preciso de uma certidão.", arquivos: 2, urgencia: UrgenciaDaSolicitacao.Alta);

        var daCarla = (await Obter(_carla, id)).Valor!;
        var daAna = (await Obter(_ana, id)).Valor!;

        daCarla.Id.Should().Be(id);
        daCarla.Descricao.Should().Be("Preciso de uma certidão.");
        daCarla.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        daCarla.Situacao.Should().Be(SituacaoParaOCliente.AguardandoAnalise);
        daCarla.SolicitanteNome.Should().Be("Carla Cliente");
        daCarla.PodeAlterar.Should().BeTrue();
        daCarla.Documentos.Select(d => d.Nome).Should().Equal("documento-01.pdf", "documento-02.pdf");
        daCarla.AtividadeTitulo.Should().BeNull();
        daCarla.AtividadeConcluidaEm.Should().BeNull();
        daCarla.Versao.Should().Be(Solicitacao(id).Versao);
        daAna.PodeAlterar.Should().BeFalse("quem não abriu a solicitação só a vê");
        daAna.SolicitanteNome.Should().Be("Carla Cliente");
        daAna.Documentos.Should().HaveCount(2);
    }

    [Fact]
    public async Task Obter_cancelada_nao_pode_mais_ser_alterada()
    {
        var id = await AbertaAsync(_carla);
        (await Cancelar(_carla, id)).Sucesso.Should().BeTrue();

        var ficha = (await Obter(_carla, id)).Valor!;

        ficha.Situacao.Should().Be(SituacaoParaOCliente.Cancelada);
        ficha.PodeAlterar.Should().BeFalse();
    }

    [Fact]
    public async Task Obter_segue_a_atividade_viva_vinculada()
    {
        var id = await AbertaAsync(_carla);
        var atividade = await AtividadeAsync("Certidão da Maria");
        Solicitacao(id).VincularAtividade(atividade, confirmouCancelada: false);

        var emAndamento = (await Obter(_carla, id)).Valor!;

        emAndamento.Situacao.Should().Be(SituacaoParaOCliente.EmAndamento);
        emAndamento.AtividadeTitulo.Should().Be("Certidão da Maria");
        emAndamento.AtividadeConcluidaEm.Should().BeNull();
        emAndamento.PodeAlterar.Should().BeFalse("a solicitação já virou atividade");

        await ConcluirAsync(atividade);
        var concluida = (await Obter(_carla, id)).Valor!;

        concluida.Situacao.Should().Be(SituacaoParaOCliente.Concluida);
        concluida.AtividadeTitulo.Should().Be("Certidão da Maria");
        concluida.AtividadeConcluidaEm.Should().NotBeNull()
            .And.Be(_c.Atividades.Itens.Single(a => a.Id == atividade).ConcluidaEm);

        // A atividade excluída deixa de valer: volta a situação da própria solicitação.
        _c.Atividades.Remover(_c.Atividades.Itens.Single(a => a.Id == atividade));
        var semAtividade = (await Obter(_carla, id)).Valor!;

        semAtividade.Situacao.Should().Be(SituacaoParaOCliente.AguardandoAnalise);
        semAtividade.AtividadeTitulo.Should().BeNull();
        semAtividade.AtividadeConcluidaEm.Should().BeNull();
    }

    [Fact]
    public async Task Obter_cancelada_com_atividade_viva_segue_a_atividade_e_sem_ela_volta_a_cancelada()
    {
        var id = await AbertaAsync(_carla);
        (await Cancelar(_carla, id)).Sucesso.Should().BeTrue();
        var atividade = await AtividadeAsync("Certidão da Maria");
        Solicitacao(id).VincularAtividade(atividade, confirmouCancelada: true);

        (await Obter(_carla, id)).Valor!.Situacao.Should().Be(SituacaoParaOCliente.EmAndamento);

        _c.Atividades.Remover(_c.Atividades.Itens.Single(a => a.Id == atividade));

        (await Obter(_carla, id)).Valor!.Situacao.Should().Be(SituacaoParaOCliente.Cancelada);
    }

    // Regra 2: só o que é do cliente do ator.

    [Fact]
    public async Task Solicitacao_inexistente_ou_de_outro_cliente_responde_nao_encontrado()
    {
        var daMaria = await AbertaAsync(_carla, arquivos: 1);
        var documento = DocumentosDa(daMaria).Single().Id;
        var salvamentos = _c.Unidade.Salvamentos;
        var arquivosGravados = _c.Armazenamento.Arquivos.Count;

        foreach (var id in new[] { daMaria, Guid.CreateVersion7() })
        {
            // O João é de outro cliente; para o id que não existe, a resposta é a mesma da Carla.
            var ator = id == daMaria ? _joao : _carla;

            var ficha = await Obter(ator, id);
            ficha.Erro.Should().Be(TipoErro.NaoEncontrado);
            ficha.Mensagem.Should().Be(SolicitacoesOpcoes.NaoEncontrada);

            foreach (var (nome, executar) in Comandos(id, documento).Where(c => c.Nome != "Remover"))
            {
                var resultado = await executar(ator, 0);

                resultado.Erro.Should().Be(TipoErro.NaoEncontrado, nome);
                resultado.Mensagem.Should().Be(SolicitacoesOpcoes.NaoEncontrada, nome);
            }
        }

        var remocao = await Remover(_joao, documento);
        remocao.Erro.Should().Be(TipoErro.NaoEncontrado);
        remocao.Mensagem.Should().Be(SolicitacoesOpcoes.NaoEncontrada);

        _c.Unidade.Salvamentos.Should().Be(salvamentos);
        _c.Armazenamento.Arquivos.Should().HaveCount(arquivosGravados);
        _c.Documentos.Itens.Should().ContainSingle();
        var intacta = Solicitacao(daMaria);
        intacta.Descricao.Should().Be(Descricao);
        intacta.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        intacta.VersaoDosDocumentos.Should().Be(0);
    }

    [Fact]
    public async Task Cliente_nao_lista_solicitacao_de_outro_cliente()
    {
        await AbertaAsync(_carla);

        (await Listar(_joao)).Valor!.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Remover_documento_de_outra_categoria_responde_nao_encontrado()
    {
        var id = await AbertaAsync(_carla);
        var doCliente = Documento.CriarDoCliente(_maria.Id, null, ArquivoGravado("2026/10/do-cliente"));
        var comum = Documento.CriarComum(null, ArquivoGravado("2026/10/comum"));
        var geral = Documento.CriarGeral(_maria.Id, null, ArquivoGravado("2026/10/geral"), []);
        foreach (var documento in new[] { doCliente, comum, geral }) _c.Documentos.Adicionar(documento);

        foreach (var documento in new[] { doCliente, comum, geral })
        {
            var resultado = await Remover(_carla, documento.Id);

            resultado.Erro.Should().Be(TipoErro.NaoEncontrado, documento.Categoria.ToString());
            resultado.Mensagem.Should().Be(SolicitacoesOpcoes.NaoEncontrada);
        }

        _c.Documentos.Itens.Should().HaveCount(3);
        Solicitacao(id).VersaoDosDocumentos.Should().Be(0);
    }

    [Fact]
    public async Task Remover_documento_inexistente_ou_de_solicitacao_que_nao_existe_responde_nao_encontrado()
    {
        var orfao = Documento.CriarDaSolicitacao(Guid.CreateVersion7(), _maria.Id, null, ArquivoGravado());
        _c.Documentos.Adicionar(orfao);

        (await Remover(_carla, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await Remover(_carla, orfao.Id)).Erro.Should().Be(TipoErro.NaoEncontrado);

        _c.Documentos.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Remover_documento_de_solicitacao_de_outro_cliente_responde_nao_encontrado()
    {
        var doJose = await AbertaAsync(_joao, arquivos: 1);
        var documento = DocumentosDa(doJose).Single().Id;

        var resultado = await Remover(_carla, documento);

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be(SolicitacoesOpcoes.NaoEncontrada);
        DocumentosDa(doJose).Should().ContainSingle();
        Solicitacao(doJose).VersaoDosDocumentos.Should().Be(0);
    }

    // Regra 3: solicitante, versão, domínio, limite, gravação.

    [Fact]
    public async Task Comandos_conferem_solicitante_antes_da_versao()
    {
        var id = await AbertaAsync(_carla, arquivos: 1);
        var documento = DocumentosDa(id).Single().Id;
        var salvamentos = _c.Unidade.Salvamentos;
        var arquivosGravados = _c.Armazenamento.Arquivos.Count;

        foreach (var (nome, executar) in Comandos(id, documento))
        {
            // A Ana é do mesmo cliente, mas não abriu; a versão também está errada.
            var resultado = await executar(_ana, 99);

            resultado.Erro.Should().Be(TipoErro.Validacao, nome);
            resultado.Mensagem.Should().Be(SolicitacoesOpcoes.SoQuemAbriu, nome);
        }

        _c.Unidade.Salvamentos.Should().Be(salvamentos);
        _c.Armazenamento.Arquivos.Should().HaveCount(arquivosGravados);
        _c.Documentos.Itens.Should().ContainSingle();
        Solicitacao(id).Descricao.Should().Be(Descricao);
        Solicitacao(id).Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
    }

    [Fact]
    public async Task Comandos_com_versao_antiga_respondem_conflito_e_nada_muda()
    {
        var id = await AbertaAsync(_carla, arquivos: 1);
        var documento = DocumentosDa(id).Single().Id;
        var salvamentos = _c.Unidade.Salvamentos;
        var arquivosGravados = _c.Armazenamento.Arquivos.Count;
        var versaoAtual = Solicitacao(id).Versao;

        foreach (var (nome, executar) in Comandos(id, documento))
        {
            var resultado = await executar(_carla, versaoAtual + 1);

            resultado.Erro.Should().Be(TipoErro.Conflito, nome);
            resultado.Mensagem.Should().Be(SolicitacoesOpcoes.Alterada, nome);
        }

        _c.Unidade.Salvamentos.Should().Be(salvamentos);
        _c.Armazenamento.Arquivos.Should().HaveCount(arquivosGravados);
        _c.Documentos.Itens.Should().ContainSingle();
        Solicitacao(id).Descricao.Should().Be(Descricao);
        Solicitacao(id).Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
    }

    [Fact]
    public async Task Depois_de_cancelada_nada_mais_muda()
    {
        var id = await AbertaAsync(_carla, arquivos: 1);
        var documento = DocumentosDa(id).Single().Id;
        (await Cancelar(_carla, id)).Sucesso.Should().BeTrue();
        var salvamentos = _c.Unidade.Salvamentos;
        var arquivosGravados = _c.Armazenamento.Arquivos.Count;

        foreach (var (nome, executar) in Comandos(id, documento))
        {
            var resultado = await executar(_carla, 0);

            resultado.Erro.Should().Be(TipoErro.Validacao, nome);
            resultado.Mensagem.Should().Be(SolicitacaoDoCliente.NaoPodeMaisSerAlterada, nome);
        }

        _c.Unidade.Salvamentos.Should().Be(salvamentos);
        _c.Armazenamento.Arquivos.Should().HaveCount(arquivosGravados, "o anexo recusado não deixa arquivo");
        _c.Documentos.Itens.Should().ContainSingle();
        Solicitacao(id).Descricao.Should().Be(Descricao);
    }

    [Fact]
    public async Task Cancelada_com_o_limite_cheio_recebe_a_recusa_do_dominio_e_nao_a_do_limite()
    {
        var id = await AbertaAsync(_carla, arquivos: SolicitacoesOpcoes.MaximoDeDocumentos);
        (await Cancelar(_carla, id)).Sucesso.Should().BeTrue();

        var resultado = await Anexar(_carla, id);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(SolicitacaoDoCliente.NaoPodeMaisSerAlterada);
    }

    [Fact]
    public async Task Alterar_muda_a_descricao_e_a_urgencia_numa_gravacao()
    {
        var id = await AbertaAsync(_carla, urgencia: UrgenciaDaSolicitacao.Baixa);
        var salvamentos = _c.Unidade.Salvamentos;

        var resultado = await Alterar(_carla, id, "  Descrição melhor.  ", UrgenciaDaSolicitacao.Alta);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        Solicitacao(id).Descricao.Should().Be("Descrição melhor.");
        Solicitacao(id).Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        _c.Unidade.Salvamentos.Should().Be(salvamentos + 1);
    }

    [Fact]
    public async Task Alterar_com_dados_invalidos_responde_validacao_com_a_mensagem_do_dominio()
    {
        var id = await AbertaAsync(_carla);
        var salvamentos = _c.Unidade.Salvamentos;

        var vazia = await Alterar(_carla, id, " ");
        var longa = await Alterar(_carla, id, new string('d', SolicitacaoDoCliente.TamanhoMaximoDescricao + 1));
        var urgencia = await Alterar(_carla, id, urgencia: (UrgenciaDaSolicitacao)99);

        vazia.Erro.Should().Be(TipoErro.Validacao);
        vazia.Mensagem.Should().Be("A descrição da solicitação é obrigatória.");
        longa.Mensagem.Should().Be($"A descrição da solicitação deve ter até {SolicitacaoDoCliente.TamanhoMaximoDescricao} caracteres.");
        urgencia.Mensagem.Should().Be("Urgência inválida.");
        _c.Unidade.Salvamentos.Should().Be(salvamentos);
        Solicitacao(id).Descricao.Should().Be(Descricao);
    }

    [Fact]
    public async Task Cancelar_marca_a_data_do_relogio_e_so_vale_uma_vez()
    {
        var id = await AbertaAsync(_carla);

        var primeira = await Cancelar(_carla, id);
        var segunda = await Cancelar(_carla, id);

        primeira.Sucesso.Should().BeTrue(primeira.Mensagem);
        Solicitacao(id).CanceladaEm.Should().Be(_c.Relogio.AgoraUtc);
        Solicitacao(id).Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);
        segunda.Erro.Should().Be(TipoErro.Validacao);
        segunda.Mensagem.Should().Be(SolicitacaoDoCliente.NaoPodeMaisSerAlterada);
    }

    [Fact]
    public async Task Anexar_alem_do_limite_e_recusado()
    {
        var id = await AbertaAsync(_carla, arquivos: SolicitacoesOpcoes.MaximoDeDocumentos);
        var salvamentos = _c.Unidade.Salvamentos;

        var resultado = await Anexar(_carla, id);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(SolicitacoesOpcoes.LimiteDeDocumentos);
        DocumentosDa(id).Should().HaveCount(10);
        _c.Armazenamento.Arquivos.Should().HaveCount(10);
        _c.Unidade.Salvamentos.Should().Be(salvamentos);
    }

    [Fact]
    public async Task Anexar_o_decimo_documento_e_aceito_e_o_removido_libera_a_vaga()
    {
        var id = await AbertaAsync(_carla, arquivos: SolicitacoesOpcoes.MaximoDeDocumentos - 1);

        var decimo = await Anexar(_carla, id);

        decimo.Sucesso.Should().BeTrue(decimo.Mensagem);
        (await Anexar(_carla, id)).Mensagem.Should().Be(SolicitacoesOpcoes.LimiteDeDocumentos);

        (await Remover(_carla, decimo.Valor)).Sucesso.Should().BeTrue();

        (await Anexar(_carla, id)).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Concorrencia_na_gravacao_vira_conflito_e_o_anexo_nao_deixa_arquivo()
    {
        foreach (Exception falha in new Exception[] { new ExcecaoDeConcorrencia("alguém gravou antes"), new ExcecaoDeConflito("unicidade") })
        {
            foreach (var nome in new[] { "Alterar", "Anexar", "Remover", "Cancelar" })
            {
                // Uma solicitação para cada comando: no fake, o que um comando muda em memória não se desfaz.
                _c.Unidade.Falha = null;
                var id = await AbertaAsync(_carla, arquivos: 1);
                var documento = DocumentosDa(id).Single().Id;
                var arquivosGravados = _c.Armazenamento.Arquivos.Count;
                _c.Unidade.Falha = falha;

                var resultado = await Comandos(id, documento).Single(c => c.Nome == nome).Executar(_carla, 0);

                resultado.Erro.Should().Be(TipoErro.Conflito, nome);
                resultado.Mensagem.Should().Be(SolicitacoesOpcoes.Alterada, nome);
                _c.Armazenamento.Arquivos.Should().HaveCount(arquivosGravados, $"{nome} não deixa arquivo órfão");
            }
        }
    }

    // Regra 5: anexar e remover são mudança nos documentos, na mesma gravação.

    [Fact]
    public async Task Anexar_cria_o_documento_da_solicitacao_e_registra_a_mudanca()
    {
        var id = await AbertaAsync(_carla);
        var salvamentos = _c.Unidade.Salvamentos;

        var resultado = await Anexar(_carla, id, @"C:\fakepath\procuração.pdf");

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var documento = DocumentosDa(id).Should().ContainSingle().Subject;
        documento.Id.Should().Be(resultado.Valor);
        documento.Categoria.Should().Be(CategoriaDocumento.DocumentoDaSolicitacao);
        documento.ClienteId.Should().Be(_maria.Id);
        documento.NomeArquivoOriginal.Should().Be("procuração.pdf", "só o último segmento do nome é guardado");
        _c.Armazenamento.Arquivos.Should().ContainKey(documento.ChaveArmazenamento);
        Solicitacao(id).VersaoDosDocumentos.Should().Be(1);
        _c.Unidade.Salvamentos.Should().Be(salvamentos + 1, "o documento e a mudança na solicitação entram na mesma gravação");
    }

    [Fact]
    public async Task Remover_tira_o_documento_registra_a_mudanca_e_o_arquivo_permanece()
    {
        var id = await AbertaAsync(_carla, arquivos: 2);
        var removido = DocumentosDa(id)[0];
        var salvamentos = _c.Unidade.Salvamentos;

        var resultado = await Remover(_carla, removido.Id);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        DocumentosDa(id).Should().ContainSingle().Which.Id.Should().NotBe(removido.Id);
        Solicitacao(id).VersaoDosDocumentos.Should().Be(1);
        _c.Armazenamento.Arquivos.Should().ContainKey(removido.ChaveArmazenamento, "a exclusão é lógica e o arquivo permanece");
        _c.Unidade.Salvamentos.Should().Be(salvamentos + 1);
    }

    [Fact]
    public async Task Anexar_arquivo_invalido_nao_grava_nada()
    {
        var id = await AbertaAsync(_carla);
        var salvamentos = _c.Unidade.Salvamentos;

        var semNome = await Anexar(_carla, id, "");
        _c.Armazenamento.RecusarPorTamanho = true;
        var grande = await Anexar(_carla, id);

        semNome.Erro.Should().Be(TipoErro.Validacao);
        semNome.Mensagem.Should().Be("Informe o arquivo.");
        grande.Erro.Should().Be(TipoErro.Validacao);
        grande.Mensagem.Should().Be("O arquivo passa do limite de 50 MB.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(salvamentos);
    }

    // O solicitante que abriu segue podendo tudo enquanto a solicitação aguarda.

    [Fact]
    public async Task Quem_abriu_altera_anexa_remove_e_cancela_com_a_versao_lida()
    {
        var id = await AbertaAsync(_carla, arquivos: 1);
        var documento = DocumentosDa(id).Single().Id;
        var versao = (await Obter(_carla, id)).Valor!.Versao;

        (await Alterar(_carla, id, versao: versao)).Sucesso.Should().BeTrue();
        (await Anexar(_carla, id, versao: versao)).Sucesso.Should().BeTrue();
        (await Remover(_carla, documento, versao)).Sucesso.Should().BeTrue();
        (await Cancelar(_carla, id, versao)).Sucesso.Should().BeTrue();

        Solicitacao(id).Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);
    }

    /// <summary>Um armazenamento que falha na N-ésima gravação e, nas outras, grava no armazenamento de verdade do cenário.</summary>
    private sealed class ArmazenamentoQueFalhaNaGravacao(ArmazenamentoFalso interno, int numero, Exception falha)
        : IArmazenamentoDeArquivos
    {
        private int _gravacoes;

        public Task<ArquivoArmazenado> GravarAsync(Stream conteudo, long limiteBytes, CancellationToken ct) =>
            ++_gravacoes == numero
                ? Task.FromException<ArquivoArmazenado>(falha)
                : interno.GravarAsync(conteudo, limiteBytes, ct);

        public Task<Stream?> AbrirAsync(string chave, CancellationToken ct) => interno.AbrirAsync(chave, ct);

        public Task RemoverAsync(string chave, CancellationToken ct) => interno.RemoverAsync(chave, ct);
    }
}
