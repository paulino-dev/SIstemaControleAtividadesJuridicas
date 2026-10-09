using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class AndamentoTests
{
    private static readonly DateOnly Entrega = new(2026, 10, 20);

    private readonly Cenario _c = new();
    private readonly Cliente _maria;
    private readonly Cliente _jose;
    private readonly Usuario _pedro;
    private readonly Usuario _carla;

    public AndamentoTests()
    {
        _maria = _c.NovoCliente("Maria Silva", "529.982.247-25");
        _jose = _c.NovoCliente("José Souza", "111.444.777-35");
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _carla = _c.NovoUsuarioDeCliente("Carla Cliente", "carla@x.com", _maria);
    }

    private Task<Resultado<AndamentoDoCliente>> Consultar(Usuario? ator, ListarMeuAndamento? consulta = null) =>
        new ListarMeuAndamentoManipulador(_c.Como(ator), _c.Atividades)
            .ManipularAsync(consulta ?? new ListarMeuAndamento(), default);

    private async Task<Guid> CriadaAsync(string titulo, Cliente? cliente)
    {
        var resultado = await new SalvarAtividadeManipulador(
                _c.Como(_c.Diretor), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(null, titulo, "Texto.", Entrega, cliente?.Id, null, null, 1), default);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private async Task DirecionarAsync(Guid id)
    {
        var resultado = await new DefinirResponsaveisDaAtividadeManipulador(
                _c.Como(_c.Diretor), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirResponsaveisDaAtividade(id, [new ResponsavelEntrada(_pedro.Id, "Redigir")], _c.Diretor.Id), default);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
    }

    private async Task ConcluirAsync(Guid id)
    {
        await DirecionarAsync(id);
        (await new ConcluirMinhaParteManipulador(_c.Como(_pedro), _c.Atividades, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new ConcluirMinhaParte(id), default)).Sucesso.Should().BeTrue();
        (await new AprovarAtividadeManipulador(_c.Como(_c.Diretor), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new AprovarAtividade(id), default)).Sucesso.Should().BeTrue();
    }

    /// <summary>Da Maria: uma criada, uma direcionada, uma concluída. Do José: uma. Avulsa: uma.</summary>
    private async Task MontarAsync()
    {
        await CriadaAsync("Criada da Maria", _maria);
        await DirecionarAsync(await CriadaAsync("Direcionada da Maria", _maria));
        await ConcluirAsync(await CriadaAsync("Concluída da Maria", _maria));
        await CriadaAsync("Do José", _jose);
        await CriadaAsync("Avulsa", null);
    }

    [Theory]
    [InlineData(StatusAtividade.Criada, SituacaoDoAndamento.EmAndamento)]
    [InlineData(StatusAtividade.Direcionada, SituacaoDoAndamento.EmAndamento)]
    [InlineData(StatusAtividade.Feita, SituacaoDoAndamento.EmAndamento)]
    [InlineData(StatusAtividade.ParaCorrecao, SituacaoDoAndamento.EmAndamento)]
    [InlineData(StatusAtividade.Corrigida, SituacaoDoAndamento.EmAndamento)]
    [InlineData(StatusAtividade.Concluida, SituacaoDoAndamento.Concluida)]
    [InlineData(StatusAtividade.Reaberta, SituacaoDoAndamento.EmAndamento)]
    public void Situacao_do_andamento_para_cada_status(StatusAtividade status, SituacaoDoAndamento esperada)
    {
        Andamento.SituacaoDe(status).Should().Be(esperada);
        Enum.GetValues<StatusAtividade>().Should().HaveCount(7);
    }

    [Fact]
    public async Task Cliente_ve_so_as_atividades_do_proprio_cliente_com_os_totais()
    {
        await MontarAsync();

        var andamento = (await Consultar(_carla)).Valor!;

        andamento.EmAndamento.Should().Be(2);
        andamento.Concluidas.Should().Be(1);
        andamento.Atividades.Total.Should().Be(3);
        andamento.Atividades.Itens.Select(a => a.Titulo).Should().Equal(
            "Direcionada da Maria", "Criada da Maria", "Concluída da Maria");
        andamento.Atividades.Itens.Select(a => a.Situacao).Should().Equal(
            SituacaoDoAndamento.EmAndamento, SituacaoDoAndamento.EmAndamento, SituacaoDoAndamento.Concluida);
        andamento.Atividades.Itens.Select(a => a.ConcluidaEm.HasValue).Should().Equal(false, false, true);
    }

    [Fact]
    public async Task Andamento_e_so_para_o_perfil_cliente()
    {
        await MontarAsync();

        foreach (var ator in new Usuario?[] { _c.Diretor, _pedro, null })
            (await Consultar(ator)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Filtro_por_situacao_nao_muda_os_totais()
    {
        await MontarAsync();

        var concluidas = (await Consultar(_carla, new ListarMeuAndamento(SituacaoDoAndamento.Concluida))).Valor!;
        var emAndamento = (await Consultar(_carla, new ListarMeuAndamento(SituacaoDoAndamento.EmAndamento))).Valor!;

        concluidas.Atividades.Itens.Select(a => a.Titulo).Should().Equal("Concluída da Maria");
        concluidas.Atividades.Total.Should().Be(1);
        emAndamento.Atividades.Itens.Select(a => a.Titulo).Should().Equal("Direcionada da Maria", "Criada da Maria");
        emAndamento.Atividades.Total.Should().Be(2);
        foreach (var andamento in new[] { concluidas, emAndamento })
        {
            andamento.EmAndamento.Should().Be(2);
            andamento.Concluidas.Should().Be(1);
        }
    }

    [Fact]
    public async Task Situacao_invalida_e_recusada()
    {
        await MontarAsync();

        var resultado = await Consultar(_carla, new ListarMeuAndamento((SituacaoDoAndamento)99));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Situação inválida.");
    }

    [Fact]
    public void Linha_do_andamento_tem_exatamente_os_cinco_campos()
    {
        typeof(AtividadeDoAndamento).GetProperties().Select(p => p.Name).Order().Should().Equal(
            ["AreaNome", "ConcluidaEm", "Processo", "Situacao", "Titulo"],
            "acrescentar um campo ao que o cliente lê é uma decisão: acrescente-o aqui e na spec do portal");
    }

    [Fact]
    public void Consulta_e_auditavel_e_nao_tem_campo_de_cliente()
    {
        IConsultaAuditavel consulta = new ListarMeuAndamento(SituacaoDoAndamento.Concluida, 2);

        consulta.TipoRecurso.Should().Be("Atividade");
        consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
        consulta.Detalhe.Should().Contain("Concluida").And.Contain("2");
        typeof(ListarMeuAndamento).GetProperties().Select(p => p.Name).Should().NotContain(n => n.Contains("Cliente"));
    }
}
