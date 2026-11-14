using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Solicitacoes;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.UnitTests.Application;

public class AtividadeDaSolicitacaoTests
{
    private const string Descricao = "Preciso de uma certidão atualizada.";
    private const string JaVirouAtividade = "Esta solicitação já virou atividade.";
    private const string Cancelada = "Cliente cancelou a solicitação, mesmo assim deseja criar a atividade?";
    private const string Alterada = "O cliente alterou a solicitação enquanto você criava a atividade. Confira os dados atualizados.";
    private const string DocumentoFora = "Um documento marcado não está mais nesta solicitação.";
    private static readonly DateOnly Entrega = new(2026, 10, 20);

    private readonly Cenario _c = new();
    private readonly Cliente _maria;
    private readonly Cliente _jose;
    private readonly Usuario _pedro;
    private readonly Usuario _carla;
    private readonly SolicitacaoDoCliente _solicitacao;
    private readonly Documento _certidao;
    private readonly Documento _procuracao;

    public AtividadeDaSolicitacaoTests()
    {
        _maria = _c.NovoCliente("Maria Silva", "529.982.247-25");
        _jose = _c.NovoCliente("José Souza", "111.444.777-35");
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _carla = _c.NovoUsuarioDeCliente("Carla Cliente", "carla@x.com", _maria);
        _solicitacao = NovaSolicitacao(_maria, _carla, Descricao, out var documentos, "certidao.pdf", "procuracao.pdf");
        (_certidao, _procuracao) = (documentos[0], documentos[1]);
    }

    /// <summary>Uma solicitação aguardando, com um documento por nome, direto nos repositórios (sem gravar).</summary>
    private SolicitacaoDoCliente NovaSolicitacao(
        Cliente cliente, Usuario solicitante, string descricao, out List<Documento> documentos, params string[] nomes)
    {
        var solicitacao = SolicitacaoDoCliente.Abrir(cliente.Id, solicitante.Id, descricao, UrgenciaDaSolicitacao.Media);
        _c.Solicitacoes.Adicionar(solicitacao);
        documentos = [.. nomes.Select(nome => Documento.CriarDaSolicitacao(
            solicitacao.Id, cliente.Id, null,
            new ArquivoDoDocumento(nome, "application/pdf", 10, new string('a', 64), $"2026/10/{Guid.NewGuid():N}")))];
        foreach (var documento in documentos) _c.Documentos.Adicionar(documento);
        return solicitacao;
    }

    private Task<Resultado<Guid>> Criar(
        Usuario? ator, Guid? solicitacao = null, uint? versao = null, string titulo = "Preparar a certidão",
        Guid? processo = null, Guid? area = null, bool comentario = false, IReadOnlyList<Guid>? documentos = null,
        bool confirmou = false) =>
        new CriarAtividadeDaSolicitacaoManipulador(
                _c.Como(ator), _c.Solicitacoes, _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Documentos,
                _c.Comentarios, _c.Relogio, _c.Unidade)
            .ManipularAsync(new CriarAtividadeDaSolicitacao(
                solicitacao ?? _solicitacao.Id, versao ?? _solicitacao.Versao, titulo, "Prazo de 15 dias.", Entrega,
                processo, area, 1, comentario, documentos ?? [], confirmou), default);

    private Task<Resultado<Guid>> SalvarEdicao(Guid atividade, Guid? cliente) =>
        new SalvarAtividadeManipulador(
                _c.Como(_c.Diretor), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(
                atividade, "Preparar a certidão", "Prazo de 15 dias.", Entrega, cliente, null, null, 1), default);

    private List<Documento> DocumentosDaAtividade() =>
        [.. _c.Documentos.Itens.Where(d => d.Categoria == CategoriaDocumento.DocumentoAtividade)];

    private void NadaFoiGravado()
    {
        _c.Unidade.Salvamentos.Should().Be(0);
        _c.Atividades.Itens.Should().BeEmpty();
        _c.Comentarios.Itens.Should().BeEmpty();
        DocumentosDaAtividade().Should().BeEmpty();
        _solicitacao.AtividadeId.Should().BeNull();
    }

    [Fact]
    public async Task Diretoria_cria_a_atividade_do_cliente_da_solicitacao_e_a_vincula()
    {
        var resultado = await Criar(_c.Diretor);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var atividade = _c.Atividades.Itens.Should().ContainSingle().Subject;
        resultado.Valor.Should().Be(atividade.Id);
        atividade.ClienteId.Should().Be(_maria.Id);
        atividade.SolicitacaoId.Should().Be(_solicitacao.Id);
        atividade.Titulo.Should().Be("Preparar a certidão");
        atividade.TipoAutor.Should().Be(TipoAutor.Usuario);
        atividade.AutorId.Should().Be(_c.Diretor.Id);
        _solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);
        _solicitacao.AtividadeId.Should().Be(atividade.Id);
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Producao_e_Cliente_recebem_AcessoNegado()
    {
        foreach (var ator in new Usuario?[] { _pedro, _carla, null })
        {
            var resultado = await Criar(ator);

            resultado.Erro.Should().Be(TipoErro.AcessoNegado);
            resultado.Valor.Should().Be(Guid.Empty);
        }

        NadaFoiGravado();
    }

    [Fact]
    public async Task Solicitacao_inexistente_e_NaoEncontrada()
    {
        var resultado = await Criar(_c.Diretor, solicitacao: Guid.CreateVersion7());

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be("Solicitação não encontrada.");
        NadaFoiGravado();
    }

    [Fact]
    public async Task Solicitacao_que_ja_virou_atividade_e_Conflito_mesmo_com_versao_e_confirmacao_erradas()
    {
        (await Criar(_c.Diretor)).Sucesso.Should().BeTrue();

        var outra = await Criar(_c.Diretor, versao: _solicitacao.Versao + 1, confirmou: true);

        outra.Erro.Should().Be(TipoErro.Conflito);
        outra.Mensagem.Should().Be(JaVirouAtividade);
        _c.Atividades.Itens.Should().ContainSingle();
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Solicitacao_cancelada_sem_confirmacao_pede_confirmacao_antes_de_conferir_a_versao()
    {
        _solicitacao.Cancelar(_c.Relogio.AgoraUtc);

        var resultado = await Criar(_c.Diretor, versao: _solicitacao.Versao + 1);

        resultado.Erro.Should().Be(TipoErro.ConfirmacaoNecessaria);
        resultado.Mensagem.Should().Be(Cancelada);
        _c.Unidade.Salvamentos.Should().Be(0);
        _c.Atividades.Itens.Should().BeEmpty();
        _solicitacao.AtividadeId.Should().BeNull();
    }

    [Fact]
    public async Task Versao_diferente_da_atual_e_Conflito()
    {
        var resultado = await Criar(_c.Diretor, versao: _solicitacao.Versao + 1);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be(Alterada);
        NadaFoiGravado();
    }

    [Fact]
    public async Task Versao_diferente_com_confirmacao_continua_Conflito()
    {
        _solicitacao.Cancelar(_c.Relogio.AgoraUtc);

        var resultado = await Criar(_c.Diretor, versao: _solicitacao.Versao + 1, confirmou: true);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be(Alterada);
        _c.Unidade.Salvamentos.Should().Be(0);
        _c.Atividades.Itens.Should().BeEmpty();
        _solicitacao.AtividadeId.Should().BeNull();
    }

    [Fact]
    public async Task Cancelada_com_confirmacao_e_versao_certa_cria_e_mantem_o_cancelamento()
    {
        _solicitacao.Cancelar(_c.Relogio.AgoraUtc);

        var resultado = await Criar(_c.Diretor, confirmou: true);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _solicitacao.AtividadeId.Should().Be(resultado.Valor);
        _solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);
        _solicitacao.CanceladaEm.Should().Be(_c.Relogio.AgoraUtc);
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Com_a_caixa_marcada_cria_um_comentario_com_a_descricao_da_solicitacao_e_o_diretor_como_autor()
    {
        var resultado = await Criar(_c.Diretor, comentario: true);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var comentario = _c.Comentarios.Itens.Should().ContainSingle().Subject;
        comentario.Texto.Should().Be(Descricao);
        comentario.UsuarioId.Should().Be(_c.Diretor.Id);
        comentario.AtividadeId.Should().Be(resultado.Valor);
        comentario.DataHora.Should().Be(_c.Relogio.AgoraUtc);
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Sem_a_caixa_marcada_nao_cria_comentario()
    {
        (await Criar(_c.Diretor, comentario: false)).Sucesso.Should().BeTrue();

        _c.Comentarios.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task So_os_documentos_marcados_viram_documentos_da_atividade_com_o_mesmo_arquivo()
    {
        var resultado = await Criar(_c.Diretor, documentos: [_procuracao.Id]);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var levado = DocumentosDaAtividade().Should().ContainSingle().Subject;
        levado.ChaveArmazenamento.Should().Be(_procuracao.ChaveArmazenamento);
        levado.HashSha256.Should().Be(_procuracao.HashSha256);
        levado.Categoria.Should().Be(CategoriaDocumento.DocumentoAtividade);
        levado.ClienteId.Should().Be(_maria.Id);
        levado.AtividadeId.Should().Be(resultado.Valor);
        levado.SolicitacaoId.Should().BeNull();
        levado.Nome.Should().Be(_procuracao.Nome);
        _c.Documentos.Itens.Where(d => d.Categoria == CategoriaDocumento.DocumentoDaSolicitacao)
            .Should().BeEquivalentTo([_certidao, _procuracao], "os documentos da solicitação ficam como estão");
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Lista_vazia_nao_leva_nenhum_documento()
    {
        (await Criar(_c.Diretor, documentos: [])).Sucesso.Should().BeTrue();

        DocumentosDaAtividade().Should().BeEmpty();
    }

    [Fact]
    public async Task Id_repetido_leva_um_so_documento()
    {
        (await Criar(_c.Diretor, documentos: [_certidao.Id, _certidao.Id])).Sucesso.Should().BeTrue();

        DocumentosDaAtividade().Should().ContainSingle().Which.ChaveArmazenamento.Should().Be(_certidao.ChaveArmazenamento);
    }

    [Fact]
    public async Task Documento_de_outra_solicitacao_marcado_e_Validacao_e_nada_e_gravado()
    {
        var outra = NovaSolicitacao(_maria, _carla, "Outro pedido da mesma cliente.", out var deOutra, "alheio.pdf");

        var resultado = await Criar(_c.Diretor, documentos: [_certidao.Id, deOutra[0].Id]);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(DocumentoFora);
        outra.AtividadeId.Should().BeNull();
        NadaFoiGravado();
    }

    [Fact]
    public async Task Documento_de_categoria_comum_marcado_e_Validacao_e_nada_e_gravado()
    {
        var comum = Documento.CriarComum(
            "Modelo", new ArquivoDoDocumento("modelo.pdf", "application/pdf", 10, new string('b', 64), "2026/10/comum"));
        _c.Documentos.Adicionar(comum);

        var resultado = await Criar(_c.Diretor, documentos: [comum.Id]);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(DocumentoFora);
        NadaFoiGravado();
    }

    [Fact]
    public async Task Documento_removido_marcado_e_Validacao_e_nada_e_gravado()
    {
        _c.Documentos.Remover(_procuracao);

        var resultado = await Criar(_c.Diretor, documentos: [_certidao.Id, _procuracao.Id]);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(DocumentoFora);
        NadaFoiGravado();
    }

    [Fact]
    public async Task Documento_inexistente_marcado_e_Validacao_e_nada_e_gravado()
    {
        var resultado = await Criar(_c.Diretor, documentos: [Guid.CreateVersion7()]);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(DocumentoFora);
        NadaFoiGravado();
    }

    [Fact]
    public async Task Processo_com_o_cliente_da_solicitacao_entra_no_vinculo_com_a_area_do_processo()
    {
        var area = AreaAtuacao.Criar("Trabalhista");
        _c.Areas.Itens.Add(area);
        var processo = Processo.Criar(
            new DadosDoProcesso(null, "Reclamação trabalhista", area.Id, null, null, null, null, null, null),
            [new ClienteDoProcesso(_maria.Id, PoloProcessual.Ativo)]);
        _c.Processos.Itens.Add(processo);

        var resultado = await Criar(_c.Diretor, processo: processo.Id);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var atividade = _c.Atividades.Itens.Single();
        atividade.ClienteId.Should().Be(_maria.Id);
        atividade.ProcessoId.Should().Be(processo.Id);
        atividade.AreaAtuacaoId.Should().Be(area.Id);
    }

    [Fact]
    public async Task Processo_sem_o_cliente_da_solicitacao_e_Validacao_e_nada_e_gravado()
    {
        var area = AreaAtuacao.Criar("Trabalhista");
        _c.Areas.Itens.Add(area);
        var doJose = Processo.Criar(
            new DadosDoProcesso(null, "Ação do José", area.Id, null, null, null, null, null, null),
            [new ClienteDoProcesso(_jose.Id, PoloProcessual.Ativo)]);
        _c.Processos.Itens.Add(doJose);

        var resultado = await Criar(_c.Diretor, processo: doJose.Id);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O cliente da atividade deve ser um dos clientes do processo.");
        NadaFoiGravado();
    }

    [Fact]
    public async Task Cliente_inativo_e_Validacao_e_nada_e_gravado()
    {
        _maria.Inativar();

        var resultado = await Criar(_c.Diretor);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O cliente está inativo.");
        NadaFoiGravado();
    }

    [Fact]
    public async Task Dado_invalido_da_atividade_e_Validacao_e_nada_e_gravado_nem_vinculado()
    {
        var resultado = await Criar(_c.Diretor, titulo: " ", comentario: true, documentos: [_certidao.Id]);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O título da atividade é obrigatório.");
        NadaFoiGravado();
    }

    [Fact]
    public async Task Concorrencia_na_gravacao_vira_Conflito_de_alteracao()
    {
        _c.Unidade.Falha = new ExcecaoDeConcorrencia("outra gravação passou na frente");

        var resultado = await Criar(_c.Diretor);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be(Alterada);
    }

    [Fact]
    public async Task Unicidade_na_gravacao_vira_Conflito_de_ja_virou_atividade()
    {
        _c.Unidade.Falha = new ExcecaoDeConflito("valor único repetido", restricao: "ix_atividades_solicitacao_id");

        var resultado = await Criar(_c.Diretor);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be(JaVirouAtividade);
    }

    [Fact]
    public async Task SalvarAtividade_nao_troca_o_cliente_de_atividade_da_solicitacao()
    {
        var criada = await Criar(_c.Diretor);
        criada.Sucesso.Should().BeTrue(criada.Mensagem);

        foreach (var cliente in new Guid?[] { _jose.Id, null })
        {
            var edicao = await SalvarEdicao(criada.Valor, cliente);

            edicao.Erro.Should().Be(TipoErro.Validacao);
            edicao.Mensagem.Should().Be(Atividade.ClienteDaSolicitacaoTravado);
        }

        _c.Unidade.Salvamentos.Should().Be(1, "as edições recusadas não gravam");
        _c.Atividades.Itens.Single().ClienteId.Should().Be(_maria.Id);
    }

    [Fact]
    public async Task SalvarAtividade_mantem_o_cliente_de_atividade_da_solicitacao_ao_editar_o_resto()
    {
        var criada = await Criar(_c.Diretor);

        var edicao = await SalvarEdicao(criada.Valor, _maria.Id);

        edicao.Sucesso.Should().BeTrue(edicao.Mensagem);
        _c.Unidade.Salvamentos.Should().Be(2);
    }
}
