using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Application.Solicitacoes;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.UnitTests.Application;

public class SolicitacoesDaDiretoriaTests
{
    private const string Descricao = "Preciso de uma certidão atualizada.";

    private readonly Cenario _c = new();
    private readonly Cliente _maria;
    private readonly Cliente _jose;
    private readonly Usuario _pedro;
    private readonly Usuario _carla;
    private readonly Usuario _joao;

    public SolicitacoesDaDiretoriaTests()
    {
        _maria = _c.NovoCliente("Maria Silva", "529.982.247-25");
        _jose = _c.NovoCliente("José Souza", "111.444.777-35");
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _carla = _c.NovoUsuarioDeCliente("Carla Cliente", "carla@x.com", _maria);
        _joao = _c.NovoUsuarioDeCliente("João Cliente", "joao@x.com", _jose);
    }

    private Task<Resultado<Pagina<SolicitacaoResumo>>> Listar(Usuario? ator, ListarSolicitacoes? consulta = null) =>
        new ListarSolicitacoesManipulador(_c.Como(ator), _c.Solicitacoes)
            .ManipularAsync(consulta ?? new ListarSolicitacoes(), default);

    private Task<Resultado<SolicitacaoDetalhe>> Obter(Usuario? ator, Guid id) =>
        new ObterSolicitacaoManipulador(_c.Como(ator), _c.Solicitacoes, _c.Documentos)
            .ManipularAsync(new ObterSolicitacao(id), default);

    private async Task<Guid> AbertaAsync(Usuario autor, string descricao = Descricao)
    {
        var resultado = await new AbrirSolicitacaoManipulador(
                _c.Como(autor), _c.Solicitacoes, _c.Documentos, _c.Armazenamento, _c.Unidade)
            .ManipularAsync(new AbrirSolicitacao(descricao, UrgenciaDaSolicitacao.Media, []), default);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private SolicitacaoDoCliente Solicitacao(Guid id) => _c.Solicitacoes.Itens.Single(s => s.Id == id);

    /// <summary>Uma atividade da Maria criada a partir da solicitação, já no repositório.</summary>
    private Atividade AtividadeDa(Guid solicitacao, string titulo)
    {
        var atividade = Atividade.Criar(
            new DadosDaAtividade(titulo, "Texto.", new DateOnly(2026, 10, 20), 1),
            VinculoDaAtividade.Criar(_maria.Id, null, null), TipoAutor.Usuario, _c.Diretor.Id, _c.Relogio.AgoraUtc, solicitacao);
        _c.Atividades.Adicionar(atividade);
        return atividade;
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_nas_duas()
    {
        var id = await AbertaAsync(_carla);

        var lista = await Listar(_pedro);
        var ficha = await Obter(_pedro, id);

        lista.Erro.Should().Be(TipoErro.AcessoNegado);
        ficha.Erro.Should().Be(TipoErro.AcessoNegado);
        lista.Valor.Should().BeNull();
        ficha.Valor.Should().BeNull();
    }

    [Fact]
    public async Task Cliente_e_anonimo_tambem_recebem_acesso_negado()
    {
        var id = await AbertaAsync(_carla);

        foreach (var ator in new Usuario?[] { _carla, _joao, null })
        {
            (await Listar(ator)).Erro.Should().Be(TipoErro.AcessoNegado);
            (await Obter(ator, id)).Erro.Should().Be(TipoErro.AcessoNegado);
        }
    }

    [Fact]
    public void As_requisicoes_da_diretoria_nao_sao_abertas_ao_cliente()
    {
        typeof(IRequisicaoAbertaAoCliente).IsAssignableFrom(typeof(ListarSolicitacoes)).Should().BeFalse();
        typeof(IRequisicaoAbertaAoCliente).IsAssignableFrom(typeof(ObterSolicitacao)).Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task Situacao_invalida_e_recusada(int situacao)
    {
        var resultado = await Listar(_c.Diretor, new ListarSolicitacoes((SituacaoDaSolicitacao)situacao));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Situação inválida.");
    }

    [Fact]
    public void Consultas_da_diretoria_sao_auditaveis_no_recurso_Solicitacao()
    {
        var id = Guid.CreateVersion7();
        var cliente = Guid.Parse("11111111-2222-3333-4444-555555555555");

        IConsultaAuditavel lista = new ListarSolicitacoes(SituacaoDaSolicitacao.Cancelada, cliente, 2);
        IConsultaAuditavel semFiltro = new ListarSolicitacoes();
        IConsultaAuditavel ficha = new ObterSolicitacao(id);

        lista.TipoRecurso.Should().Be("Solicitacao");
        lista.Acao.Should().Be(AcaoDeAcesso.Listou);
        lista.RecursoId.Should().BeNull();
        lista.Detalhe.Should().Be(
            """{"Situacao":"Cancelada","ClienteId":"11111111-2222-3333-4444-555555555555","Pagina":2}""");
        semFiltro.Detalhe.Should().Be("""{"Situacao":null,"ClienteId":null,"Pagina":1}""");
        ficha.TipoRecurso.Should().Be("Solicitacao");
        ficha.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        ficha.RecursoId.Should().Be(id);
    }

    [Fact]
    public async Task Listar_traz_todas_da_mais_antiga_para_a_mais_nova_com_os_filtros()
    {
        var daMaria = await AbertaAsync(_carla, "Primeira, da Maria.");
        var doJose = await AbertaAsync(_joao, "Segunda, do José.");
        var cancelada = await AbertaAsync(_carla, "Terceira, da Maria, cancelada.");
        Solicitacao(cancelada).Cancelar(DateTime.UtcNow);

        var todas = (await Listar(_c.Diretor)).Valor!;
        var aguardando = (await Listar(_c.Diretor, new ListarSolicitacoes(SituacaoDaSolicitacao.Aguardando))).Valor!;
        var canceladas = (await Listar(_c.Diretor, new ListarSolicitacoes(SituacaoDaSolicitacao.Cancelada))).Valor!;
        var deJose = (await Listar(_c.Diretor, new ListarSolicitacoes(ClienteId: _jose.Id))).Valor!;
        var canceladasDaMaria = (await Listar(
            _c.Diretor, new ListarSolicitacoes(SituacaoDaSolicitacao.Cancelada, _maria.Id))).Valor!;
        var canceladasDoJose = (await Listar(
            _c.Diretor, new ListarSolicitacoes(SituacaoDaSolicitacao.Cancelada, _jose.Id))).Valor!;

        todas.Itens.Select(s => s.Id).Should().Equal(daMaria, doJose, cancelada);
        todas.Total.Should().Be(3);
        aguardando.Itens.Select(s => s.Id).Should().Equal(daMaria, doJose);
        canceladas.Itens.Select(s => s.Id).Should().Equal(cancelada);
        deJose.Itens.Select(s => s.Id).Should().Equal(doJose);
        canceladasDaMaria.Itens.Select(s => s.Id).Should().Equal(cancelada);
        canceladasDoJose.Itens.Should().BeEmpty();

        var linha = todas.Itens[1];
        linha.ClienteId.Should().Be(_jose.Id);
        linha.ClienteNome.Should().Be("José Souza");
        linha.SolicitanteNome.Should().Be("João Cliente");
        linha.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        todas.Itens[2].Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);
    }

    [Fact]
    public async Task Listar_pagina_de_vinte_e_normaliza_a_pagina_pedida()
    {
        var ids = new List<Guid>();
        for (var i = 0; i <= SolicitacoesOpcoes.TamanhoDaPagina; i++) ids.Add(await AbertaAsync(_carla, $"Pedido {i}."));

        var primeira = (await Listar(_c.Diretor)).Valor!;
        var segunda = (await Listar(_c.Diretor, new ListarSolicitacoes(Pagina: 2))).Valor!;
        var zero = (await Listar(_c.Diretor, new ListarSolicitacoes(Pagina: 0))).Valor!;

        primeira.Itens.Select(s => s.Id).Should().Equal(ids.Take(SolicitacoesOpcoes.TamanhoDaPagina));
        primeira.Total.Should().Be(SolicitacoesOpcoes.TamanhoDaPagina + 1);
        segunda.Itens.Select(s => s.Id).Should().Equal(ids[^1]);
        segunda.Numero.Should().Be(2);
        zero.Numero.Should().Be(1);
    }

    [Fact]
    public async Task Listar_sem_nenhuma_solicitacao_traz_pagina_vazia()
    {
        var pagina = (await Listar(_c.Diretor)).Valor!;

        pagina.Itens.Should().BeEmpty();
        pagina.Total.Should().Be(0);
    }

    [Fact]
    public async Task Obter_traz_a_ficha_com_cliente_solicitante_e_versao()
    {
        var id = await AbertaAsync(_carla);

        var ficha = (await Obter(_c.Diretor, id)).Valor!;

        ficha.Id.Should().Be(id);
        ficha.ClienteId.Should().Be(_maria.Id);
        ficha.ClienteNome.Should().Be("Maria Silva");
        ficha.SolicitanteId.Should().Be(_carla.Id);
        ficha.SolicitanteNome.Should().Be("Carla Cliente");
        ficha.Descricao.Should().Be(Descricao);
        ficha.Urgencia.Should().Be(UrgenciaDaSolicitacao.Media);
        ficha.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        ficha.CanceladaEm.Should().BeNull();
        ficha.AtividadeId.Should().BeNull();
        ficha.AtividadeTitulo.Should().BeNull();
        ficha.Documentos.Should().BeEmpty();
        ficha.Versao.Should().Be(Solicitacao(id).Versao);
    }

    [Fact]
    public async Task Obter_traz_a_atividade_viva_vinculada_e_a_data_do_cancelamento()
    {
        var id = await AbertaAsync(_carla);
        var cancelamento = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
        Solicitacao(id).Cancelar(cancelamento);
        var atividade = AtividadeDa(id, "Certidão da Maria");
        Solicitacao(id).VincularAtividade(atividade, confirmouCancelada: true);

        var ficha = (await Obter(_c.Diretor, id)).Valor!;

        ficha.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);
        ficha.CanceladaEm.Should().Be(cancelamento, "o cancelamento fica preservado quando a solicitação vira atividade");
        ficha.AtividadeId.Should().Be(atividade.Id);
        ficha.AtividadeTitulo.Should().Be("Certidão da Maria");

        // A atividade excluída não tem título: o vínculo continua como está gravado.
        _c.Atividades.Remover(atividade);
        var semAtividade = (await Obter(_c.Diretor, id)).Valor!;

        semAtividade.AtividadeId.Should().Be(atividade.Id);
        semAtividade.AtividadeTitulo.Should().BeNull();
    }

    [Fact]
    public async Task Obter_inexistente_responde_nao_encontrado()
    {
        var resultado = await Obter(_c.Diretor, Guid.CreateVersion7());

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be("Solicitação não encontrada.");
    }
}
