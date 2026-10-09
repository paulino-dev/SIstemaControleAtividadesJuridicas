using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class ProdutividadeTests
{
    // Outubro de 2026 no fuso do escritório (UTC-3): de 01/10 00:00 a 01/11 00:00.
    private static readonly DateTime Outubro = new(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Novembro = new(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Entrega = new(2026, 12, 20);

    private readonly Cenario _c = new();
    private readonly Usuario _davi, _pedro, _paula;
    private readonly ClientePessoaFisica _maria;

    public ProdutividadeTests()
    {
        _davi = _c.NovoUsuario("Davi Diretor", "davi@x.com", _c.PerfilDiretor);
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _paula = _c.NovoUsuario("Paula", "paula@x.com", _c.PerfilProducao);
        _maria = ClientePessoaFisica.Criar(
            new DadosDoCliente("Maria Silva", null, null, Endereco.Vazio, null), Cpf.Criar("529.982.247-25"), null, null);
        _c.Clientes.Itens.Add(_maria);
    }

    private Usuario Diana => _c.Diretor;

    private static DateTime Em(int mes, int dia) => new(2026, mes, dia, 15, 0, 0, DateTimeKind.Utc);

    private Task<Resultado<ProdutividadeDoProdutor>> Obter(Usuario? ator, Guid produtor, DateTime? de = null, DateTime? ate = null) =>
        new ObterProdutividadeDoProdutorManipulador(_c.Como(ator), _c.Usuarios, _c.Perfis, _c.Atividades)
            .ManipularAsync(new ObterProdutividadeDoProdutor(produtor, de ?? Outubro, ate ?? Novembro), default);

    private Task<Resultado<Guid>> Salvar(Guid? id, string titulo, int complexidade, Guid? cliente) =>
        new SalvarAtividadeManipulador(_c.Como(Diana), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(id, titulo, "Descrição.", Entrega, cliente, null, null, complexidade), default);

    private Task<Resultado> Concluir(Usuario quem, Guid id) =>
        new ConcluirMinhaParteManipulador(_c.Como(quem), _c.Atividades, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new ConcluirMinhaParte(id), default);

    private Task<Resultado> Aprovar(Guid id) =>
        new AprovarAtividadeManipulador(_c.Como(Diana), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new AprovarAtividade(id), default);

    /// <summary>Direcionada aos responsáveis informados, com Diana de revisora.</summary>
    private async Task<Guid> DirecionadaAsync(string titulo, int complexidade, Guid? cliente, params Usuario[] responsaveis)
    {
        var salva = await Salvar(null, titulo, complexidade, cliente);
        salva.Sucesso.Should().BeTrue(salva.Mensagem);
        var definida = await new DefinirResponsaveisDaAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirResponsaveisDaAtividade(
                salva.Valor, [.. responsaveis.Select(u => new ResponsavelEntrada(u.Id, null))], Diana.Id), default);
        definida.Sucesso.Should().BeTrue(definida.Mensagem);
        return salva.Valor;
    }

    /// <summary>Todo o percurso até a aprovação acontece no instante informado, que é o da conclusão.</summary>
    private async Task<Guid> ConcluidaAsync(string titulo, int complexidade, DateTime concluidaEm, Guid? cliente, params Usuario[] responsaveis)
    {
        _c.Relogio.AgoraUtc = concluidaEm;
        var id = await DirecionadaAsync(titulo, complexidade, cliente, responsaveis);
        foreach (var responsavel in responsaveis) (await Concluir(responsavel, id)).Sucesso.Should().BeTrue();
        (await Aprovar(id)).Sucesso.Should().BeTrue();
        return id;
    }

    private static (int Complexidade, int Total)[] Totais(ProdutividadeDoProdutor produtividade) =>
        [.. produtividade.Grupos.Select(g => (g.Complexidade, g.Total))];

    // ---- Acesso e validação ----

    [Fact]
    public async Task So_a_diretoria_consulta()
    {
        (await Obter(_pedro, _pedro.Id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Obter(null, _pedro.Id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Obter(Diana, _pedro.Id)).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Quem_nao_e_de_producao_responde_como_inexistente()
    {
        var diretor = await Obter(Diana, _davi.Id);
        var ninguem = await Obter(Diana, Guid.CreateVersion7());

        diretor.Erro.Should().Be(TipoErro.NaoEncontrado);
        diretor.Mensagem.Should().Be("Produtor não encontrado.");
        ninguem.Erro.Should().Be(TipoErro.NaoEncontrado);
        ninguem.Mensagem.Should().Be("Produtor não encontrado.");
    }

    [Fact]
    public async Task Periodo_invalido_e_recusado_antes_de_procurar_o_produtor()
    {
        var semUtc = await Obter(Diana, _pedro.Id, new DateTime(2026, 10, 1), Novembro);
        var invertido = await Obter(Diana, _pedro.Id, Novembro, Outubro);
        var vazio = await Obter(Diana, _pedro.Id, Outubro, Outubro);
        var longo = await Obter(Diana, _pedro.Id, Outubro, Outubro.AddDays(92).AddTicks(1));
        var noLimite = await Obter(Diana, _pedro.Id, Outubro, Outubro.AddDays(92));
        var invalidoDeQuemNaoExiste = await Obter(Diana, Guid.CreateVersion7(), Novembro, Outubro);

        semUtc.Erro.Should().Be(TipoErro.Validacao);
        semUtc.Mensagem.Should().Be("Período inválido.");
        invertido.Erro.Should().Be(TipoErro.Validacao);
        vazio.Erro.Should().Be(TipoErro.Validacao);
        longo.Erro.Should().Be(TipoErro.Validacao);
        longo.Mensagem.Should().Be("O período deve ter no máximo 92 dias.");
        noLimite.Sucesso.Should().BeTrue(noLimite.Mensagem);
        invalidoDeQuemNaoExiste.Erro.Should().Be(TipoErro.Validacao);
    }

    [Fact]
    public void E_consulta_auditavel_com_o_produtor_e_o_periodo()
    {
        IConsultaAuditavel consulta = new ObterProdutividadeDoProdutor(_pedro.Id, Outubro, Novembro);

        consulta.TipoRecurso.Should().Be("Produtividade");
        consulta.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        consulta.RecursoId.Should().Be(_pedro.Id);
        consulta.Detalhe.Should().Contain("2026-10-01T03:00:00Z").And.Contain("2026-11-01T03:00:00Z");
    }

    // ---- O que conta ----

    [Fact]
    public async Task Sem_nada_concluido_vem_os_tres_grupos_vazios()
    {
        var produtividade = (await Obter(Diana, _pedro.Id)).Valor!;

        produtividade.ProdutorId.Should().Be(_pedro.Id);
        produtividade.ProdutorNome.Should().Be("Pedro");
        Totais(produtividade).Should().Equal((1, 0), (2, 0), (3, 0));
        produtividade.Total.Should().Be(0);
        produtividade.PrimeiraConclusaoEm.Should().BeNull();
    }

    [Fact]
    public async Task Agrupa_por_complexidade_as_concluidas_do_periodo_em_que_o_produtor_e_responsavel()
    {
        var simples = await ConcluidaAsync("Petição simples", 1, Em(10, 5), null, _pedro);
        var dosDois = await ConcluidaAsync("Recurso dos dois", 3, Em(10, 10), _maria.Id, _pedro, _paula);
        var daPaula = await ConcluidaAsync("Só da Paula", 2, Em(10, 7), null, _paula);
        await ConcluidaAsync("De setembro", 2, Em(9, 15), null, _pedro);
        await DirecionadaAsync("Ainda em andamento", 1, null, _pedro);

        var dePedro = (await Obter(Diana, _pedro.Id)).Valor!;
        var dePaula = (await Obter(Diana, _paula.Id)).Valor!;

        Totais(dePedro).Should().Equal((1, 1), (2, 0), (3, 1));
        dePedro.Total.Should().Be(2);
        dePedro.Grupos[0].Atividades.Should().Equal(new AtividadeRealizada(simples, "Petição simples", 1, Em(10, 5), null));
        dePedro.Grupos[2].Atividades.Should().Equal(new AtividadeRealizada(dosDois, "Recurso dos dois", 3, Em(10, 10), "Maria Silva"));
        dePedro.PrimeiraConclusaoEm.Should().Be(Em(9, 15));

        // A atividade dos dois aparece inteira para cada um, uma vez só.
        Totais(dePaula).Should().Equal((1, 0), (2, 1), (3, 1));
        dePaula.Grupos[1].Atividades.Select(a => a.Id).Should().Equal(daPaula);
        dePaula.Grupos[2].Atividades.Select(a => a.Id).Should().Equal(dosDois);
    }

    [Fact]
    public async Task Dentro_do_grupo_vem_da_conclusao_mais_antiga_para_a_mais_recente()
    {
        var depois = await ConcluidaAsync("Depois", 2, Em(10, 20), null, _pedro);
        var antes = await ConcluidaAsync("Antes", 2, Em(10, 2), null, _pedro);

        (await Obter(Diana, _pedro.Id)).Valor!.Grupos[1].Atividades.Select(a => a.Id).Should().Equal(antes, depois);
    }

    [Fact]
    public async Task O_periodo_e_semiaberto_o_inicio_conta_e_o_fim_nao()
    {
        var noInicio = await ConcluidaAsync("No primeiro instante", 1, Outubro, null, _pedro);
        await ConcluidaAsync("No primeiro instante do mês seguinte", 1, Novembro, null, _pedro);
        var noFim = await ConcluidaAsync("No último instante", 1, Novembro.AddTicks(-1), null, _pedro);

        (await Obter(Diana, _pedro.Id)).Valor!.Grupos[0].Atividades.Select(a => a.Id).Should().Equal(noInicio, noFim);
    }

    [Fact]
    public async Task Reaberta_deixa_de_contar_e_volta_no_mes_da_nova_conclusao()
    {
        var id = await ConcluidaAsync("Parecer", 2, Em(10, 5), null, _pedro);

        (await new ReabrirAtividadeManipulador(_c.Como(Diana), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new ReabrirAtividade(id, "Cliente pediu ajuste", [_pedro.Id]), default)).Sucesso.Should().BeTrue();

        var reaberta = (await Obter(Diana, _pedro.Id)).Valor!;
        reaberta.Total.Should().Be(0);
        reaberta.PrimeiraConclusaoEm.Should().BeNull();

        _c.Relogio.AgoraUtc = Em(11, 3);
        (await Concluir(_pedro, id)).Sucesso.Should().BeTrue();
        (await Aprovar(id)).Sucesso.Should().BeTrue();

        (await Obter(Diana, _pedro.Id)).Valor!.Total.Should().Be(0);
        (await Obter(Diana, _pedro.Id, Novembro, Novembro.AddDays(30))).Valor!
            .Grupos[1].Atividades.Select(a => a.Id).Should().Equal(id);
    }

    [Fact]
    public async Task Vale_a_complexidade_atual_da_atividade()
    {
        var id = await ConcluidaAsync("Parecer", 1, Em(10, 5), null, _pedro);

        (await Salvar(id, "Parecer", 3, null)).Sucesso.Should().BeTrue();

        Totais((await Obter(Diana, _pedro.Id)).Valor!).Should().Equal((1, 0), (2, 0), (3, 1));
    }

    [Fact]
    public async Task Excluida_deixa_de_contar()
    {
        var id = await ConcluidaAsync("Parecer", 1, Em(10, 5), null, _pedro);

        (await new ExcluirAtividadeManipulador(_c.Como(Diana), _c.Atividades, _c.Unidade)
            .ManipularAsync(new ExcluirAtividade(id), default)).Sucesso.Should().BeTrue();

        (await Obter(Diana, _pedro.Id)).Valor!.Total.Should().Be(0);
    }

    [Fact]
    public async Task Inativo_continua_consultavel_e_quem_virou_diretoria_deixa_de_ser_produtor()
    {
        await ConcluidaAsync("Parecer", 1, Em(10, 5), null, _pedro, _paula);

        _pedro.Inativar();
        _paula.AlterarPerfil(_c.PerfilDiretor);

        (await Obter(Diana, _pedro.Id)).Valor!.Total.Should().Be(1);
        (await Obter(Diana, _paula.Id)).Erro.Should().Be(TipoErro.NaoEncontrado);
    }
}
