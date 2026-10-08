using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class AtividadesTests
{
    private const string NaoEncontrada = "Atividade não encontrada.";
    private static readonly DateOnly Entrega = new(2026, 10, 20);

    private readonly Cenario _c = new();
    private readonly Usuario _davi, _pedro, _paula, _ester;
    private readonly ClientePessoaFisica _maria, _jose;
    private readonly AreaAtuacao _trabalhista = AreaAtuacao.Criar("Trabalhista");
    private readonly AreaAtuacao _civel = AreaAtuacao.Criar("Cível");
    private readonly Processo _processo;

    public AtividadesTests()
    {
        _davi = _c.NovoUsuario("Davi Diretor", "davi@x.com", _c.PerfilDiretor);
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _paula = _c.NovoUsuario("Paula", "paula@x.com", _c.PerfilProducao);
        _ester = _c.NovoUsuario("Ester", "ester@x.com", _c.PerfilProducao);
        _maria = ClientePessoaFisica.Criar(new DadosDoCliente("Maria Silva", null, null, Endereco.Vazio, null), Cpf.Criar("529.982.247-25"), null, null);
        _jose = ClientePessoaFisica.Criar(new DadosDoCliente("José Souza", null, null, Endereco.Vazio, null), Cpf.Criar("111.444.777-35"), null, null);
        _c.Clientes.Itens.AddRange([_maria, _jose]);
        _c.Areas.Itens.AddRange([_trabalhista, _civel]);
        _processo = Processo.Criar(
            new DadosDoProcesso(null, "Reclamação trabalhista", _trabalhista.Id, null, null, null, null, null, null),
            [new ClienteDoProcesso(_maria.Id, PoloProcessual.Ativo)]);
        _c.Processos.Itens.Add(_processo);
    }

    private Usuario Diana => _c.Diretor;

    private Task<Resultado<Guid>> Salvar(Usuario? ator, Guid? id = null, string titulo = "Elaborar contestação",
        DateOnly? entrega = null, bool semEntrega = false, Guid? cliente = null, Guid? processo = null, Guid? area = null) =>
        new SalvarAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade)
            .ManipularAsync(new SalvarAtividade(id, titulo, "Prazo de 15 dias.", semEntrega ? null : entrega ?? Entrega, cliente, processo, area), default);

    private Task<Resultado> Definir(Usuario? ator, Guid id, Guid? revisor, params (Usuario Usuario, string? Direcionamento)[] responsaveis) =>
        new DefinirResponsaveisDaAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirResponsaveisDaAtividade(
                id, [.. responsaveis.Select(r => new ResponsavelEntrada(r.Usuario.Id, r.Direcionamento))], revisor), default);

    private Task<Resultado> Concluir(Usuario? ator, Guid id) =>
        new ConcluirMinhaParteManipulador(_c.Como(ator), _c.Atividades, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new ConcluirMinhaParte(id), default);

    private Task<Resultado> Aprovar(Usuario? ator, Guid id) =>
        new AprovarAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new AprovarAtividade(id), default);

    private Task<Resultado> Devolver(Usuario? ator, Guid id, string motivo, params Usuario[] responsaveis) =>
        new DevolverAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new DevolverAtividade(id, motivo, [.. responsaveis.Select(u => u.Id)]), default);

    private Task<Resultado> Reabrir(Usuario? ator, Guid id, string motivo, params Usuario[] responsaveis) =>
        new ReabrirAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new ReabrirAtividade(id, motivo, [.. responsaveis.Select(u => u.Id)]), default);

    private Task<Resultado> TrocarRevisor(Usuario? ator, Guid id, Usuario revisor) =>
        new TrocarRevisorDaAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Unidade)
            .ManipularAsync(new TrocarRevisorDaAtividade(id, revisor.Id), default);

    private Task<Resultado> Excluir(Usuario? ator, Guid id) =>
        new ExcluirAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Unidade).ManipularAsync(new ExcluirAtividade(id), default);

    private Task<Resultado<AtividadeDetalhe>> Obter(Usuario? ator, Guid id) =>
        new ObterAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Usuarios, _c.Clientes, _c.Processos, _c.Areas, _c.Calendario, _c.Auditoria)
            .ManipularAsync(new ObterAtividade(id), default);

    private Task<Resultado<Pagina<AtividadeResumo>>> Listar(Usuario? ator, ListarAtividades? consulta = null) =>
        new ListarAtividadesManipulador(_c.Como(ator), _c.Atividades, _c.Calendario)
            .ManipularAsync(consulta ?? new ListarAtividades(), default);

    private Atividade NoRepositorio(Guid id) => _c.Atividades.Itens.Single(a => a.Id == id);

    private async Task<Guid> CriadaAsync(string titulo = "Elaborar contestação", DateOnly? entrega = null)
    {
        var resultado = await Salvar(Diana, titulo: titulo, entrega: entrega);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    /// <summary>Direcionada a Pedro e Paula, com Diana de revisora.</summary>
    private async Task<Guid> DirecionadaAsync(string titulo = "Elaborar contestação", DateOnly? entrega = null)
    {
        var id = await CriadaAsync(titulo, entrega);
        var definido = await Definir(Diana, id, Diana.Id, (_pedro, "Redigir"), (_paula, "Juntar documentos"));
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
        return id;
    }

    private async Task<Guid> FeitaAsync()
    {
        var id = await DirecionadaAsync();
        (await Concluir(_pedro, id)).Sucesso.Should().BeTrue();
        (await Concluir(_paula, id)).Sucesso.Should().BeTrue();
        return id;
    }

    // ---- Salvar ----

    [Fact]
    public async Task Diretor_cria_atividade_avulsa_e_e_o_autor()
    {
        var resultado = await Salvar(Diana);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var atividade = NoRepositorio(resultado.Valor);
        atividade.Status.Should().Be(StatusAtividade.Criada);
        atividade.TipoAutor.Should().Be(TipoAutor.Usuario);
        atividade.AutorId.Should().Be(Diana.Id);
        atividade.ClienteId.Should().BeNull();
        atividade.Historico.Single().DataHora.Should().Be(_c.Relogio.AgoraUtc);
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Com_processo_a_area_e_a_do_processo_e_o_cliente_tem_de_ser_dele()
    {
        var certo = await Salvar(Diana, cliente: _maria.Id, processo: _processo.Id, area: _civel.Id);
        var clienteDeFora = await Salvar(Diana, cliente: _jose.Id, processo: _processo.Id);
        var semCliente = await Salvar(Diana, processo: _processo.Id);

        certo.Sucesso.Should().BeTrue(certo.Mensagem);
        NoRepositorio(certo.Valor).AreaAtuacaoId.Should().Be(_trabalhista.Id);
        clienteDeFora.Erro.Should().Be(TipoErro.Validacao);
        clienteDeFora.Mensagem.Should().Be("O cliente da atividade deve ser um dos clientes do processo.");
        semCliente.Erro.Should().Be(TipoErro.Validacao);
        _c.Atividades.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Cliente_processo_e_area_informados_tem_de_existir()
    {
        (await Salvar(Diana, cliente: Guid.CreateVersion7())).Mensagem.Should().Be("Cliente não encontrado.");
        (await Salvar(Diana, cliente: _maria.Id, processo: Guid.CreateVersion7())).Mensagem.Should().Be("Processo não encontrado.");
        (await Salvar(Diana, area: Guid.CreateVersion7())).Mensagem.Should().Be("Área de atuação não encontrada.");
        _c.Atividades.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Cliente_e_area_inativos_nao_entram_em_vinculo_novo_mas_permanecem_no_que_ja_existia()
    {
        var id = (await Salvar(Diana, cliente: _maria.Id, area: _civel.Id)).Valor;
        _maria.Inativar();
        _civel.Inativar();

        var manter = await Salvar(Diana, id, titulo: "Título novo", cliente: _maria.Id, area: _civel.Id);
        var novaComInativo = await Salvar(Diana, cliente: _maria.Id);
        var novaComAreaInativa = await Salvar(Diana, area: _civel.Id);

        manter.Sucesso.Should().BeTrue(manter.Mensagem);
        NoRepositorio(id).Titulo.Should().Be("Título novo");
        novaComInativo.Mensagem.Should().Be("O cliente está inativo.");
        novaComAreaInativa.Mensagem.Should().Be("A área de atuação está inativa.");
    }

    [Fact]
    public async Task Editar_valida_e_nao_encontra_o_que_nao_existe()
    {
        var id = await DirecionadaAsync();

        (await Salvar(Diana, id, semEntrega: true)).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(Diana, id, titulo: "  ")).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(Diana, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
        NoRepositorio(id).Titulo.Should().Be("Elaborar contestação");
    }

    // ---- Direcionar ----

    [Fact]
    public async Task Definir_responsaveis_direciona_e_anota_o_evento()
    {
        var id = await CriadaAsync();

        var resultado = await Definir(Diana, id, Diana.Id, (_pedro, "Redigir"), (_paula, null));

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var atividade = NoRepositorio(id);
        atividade.Status.Should().Be(StatusAtividade.Direcionada);
        atividade.RevisorId.Should().Be(Diana.Id);
        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeAtividade.ResponsaveisAlterados);
        evento.Entidade.Should().Be("Atividade");
        evento.EntidadeId.Should().Be(id);
        evento.Imediato.Should().BeFalse();
        evento.Dados.Should().BeEquivalentTo(new
        {
            adicionados = new[] { _pedro.Id, _paula.Id },
            removidos = Array.Empty<Guid>(),
            direcionamentoAlterado = Array.Empty<Guid>(),
        });
    }

    [Fact]
    public async Task Definir_os_mesmos_responsaveis_nao_anota_evento()
    {
        var id = await DirecionadaAsync();
        _c.Auditoria.Eventos.Clear();

        (await Definir(Diana, id, Diana.Id, (_pedro, "Redigir"), (_paula, "Juntar documentos"))).Sucesso.Should().BeTrue();

        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Responsavel_de_diretoria_unico_vira_o_revisor_e_outro_e_recusado()
    {
        var id = await CriadaAsync();

        var outro = await Definir(Diana, id, Diana.Id, (_davi, null), (_pedro, null));
        var travado = await Definir(Diana, id, null, (_davi, null), (_pedro, null));

        outro.Erro.Should().Be(TipoErro.Validacao);
        outro.Mensagem.Should().Be(Atividade.RevisorTravado);
        travado.Sucesso.Should().BeTrue(travado.Mensagem);
        NoRepositorio(id).RevisorId.Should().Be(_davi.Id);
    }

    [Fact]
    public async Task Usuario_inexistente_ou_inativo_nao_entra_como_responsavel_nem_revisor()
    {
        var id = await CriadaAsync();
        _ester.Inativar();
        var fantasma = Usuario.Criar("Fantasma", Email.Criar("f@x.com"), "hash", _c.PerfilProducao);

        (await Definir(Diana, id, Diana.Id, (fantasma, null))).Mensagem.Should().Be("Usuário não encontrado.");
        (await Definir(Diana, id, Diana.Id, (_ester, null))).Mensagem.Should().Be("Usuário inativo não pode ser incluído como responsável.");
        (await Definir(Diana, id, _pedro.Id, (_paula, null))).Mensagem.Should().Be("O revisor deve ser um usuário ativo de Diretoria.");
        (await Definir(Diana, id, fantasma.Id, (_paula, null))).Mensagem.Should().Be("Usuário não encontrado.");
        NoRepositorio(id).Status.Should().Be(StatusAtividade.Criada);
    }

    [Fact]
    public async Task Trocar_revisor_respeita_a_regra_e_o_acesso()
    {
        var id = await FeitaAsync();

        (await TrocarRevisor(_pedro, id, _davi)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await TrocarRevisor(Diana, id, _pedro)).Mensagem.Should().Be("O revisor deve ser um usuário ativo de Diretoria.");
        (await TrocarRevisor(Diana, id, _davi)).Sucesso.Should().BeTrue();
        NoRepositorio(id).RevisorId.Should().Be(_davi.Id);
    }

    // ---- Concluir, aprovar, devolver, reabrir ----

    [Fact]
    public async Task Responsaveis_concluem_revisor_aprova_e_o_percurso_fica_no_historico()
    {
        var id = await FeitaAsync();
        NoRepositorio(id).Status.Should().Be(StatusAtividade.Feita);

        (await Aprovar(Diana, id)).Sucesso.Should().BeTrue();

        var atividade = NoRepositorio(id);
        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(_c.Relogio.AgoraUtc);
        atividade.Historico.Select(h => h.StatusNovo).Should().Equal(
            StatusAtividade.Criada, StatusAtividade.Direcionada, StatusAtividade.Feita, StatusAtividade.Concluida);
    }

    [Fact]
    public async Task So_o_revisor_de_diretoria_aprova_e_devolve()
    {
        var id = await FeitaAsync();

        (await Aprovar(_pedro, id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Devolver(_pedro, id, "motivo", _pedro)).Erro.Should().Be(TipoErro.AcessoNegado);
        var outroDiretor = await Aprovar(_davi, id);
        outroDiretor.Erro.Should().Be(TipoErro.Validacao);
        outroDiretor.Mensagem.Should().Be(Atividade.SoORevisor);
        NoRepositorio(id).Status.Should().Be(StatusAtividade.Feita);
    }

    [Fact]
    public async Task Devolver_e_reabrir_seguem_o_fluxo()
    {
        var id = await FeitaAsync();

        (await Devolver(Diana, id, "Faltou a procuração", _paula)).Sucesso.Should().BeTrue();
        NoRepositorio(id).Status.Should().Be(StatusAtividade.ParaCorrecao);
        (await Concluir(_pedro, id)).Mensagem.Should().Be("Sua parte já está concluída.");
        (await Concluir(_paula, id)).Sucesso.Should().BeTrue();
        NoRepositorio(id).Status.Should().Be(StatusAtividade.Corrigida);
        (await Aprovar(Diana, id)).Sucesso.Should().BeTrue();

        (await Reabrir(_pedro, id, "Ajuste", _pedro)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Reabrir(_davi, id, "  ", _pedro)).Mensagem.Should().Be("Informe o motivo.");
        (await Reabrir(_davi, id, "Cliente pediu ajuste", _pedro)).Sucesso.Should().BeTrue();
        var atividade = NoRepositorio(id);
        atividade.Status.Should().Be(StatusAtividade.Reaberta);
        atividade.Historico.Last().UsuarioId.Should().Be(_davi.Id);
        atividade.Historico.Last().Motivo.Should().Be("Cliente pediu ajuste");
    }

    [Fact]
    public async Task Concluir_a_parte_de_atividade_que_nao_e_sua_responde_nao_encontrada_e_fica_registrado()
    {
        var id = await DirecionadaAsync();
        _c.Auditoria.Eventos.Clear();

        var resultado = await Concluir(_ester, id);

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be(NaoEncontrada);
        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
        evento.Entidade.Should().Be("Atividade");
        evento.EntidadeId.Should().Be(id);
        evento.Imediato.Should().BeTrue();
    }

    [Fact]
    public async Task Diretor_que_nao_e_responsavel_nao_conclui_parte_alguma()
    {
        var id = await DirecionadaAsync();

        var resultado = await Concluir(_davi, id);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Você não é responsável por esta atividade.");
    }

    [Fact]
    public async Task Conflito_de_concorrencia_vira_mensagem_de_recarregar()
    {
        var id = await DirecionadaAsync();
        _c.Unidade.Falha = new ExcecaoDeConcorrencia("alterado");

        var resultado = await Concluir(_pedro, id);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("A atividade foi alterada por outra pessoa. Recarregue e tente novamente.");
    }

    // ---- Excluir ----

    [Fact]
    public async Task So_a_diretoria_exclui()
    {
        var id = await DirecionadaAsync();

        (await Excluir(_pedro, id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Excluir(Diana, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await Excluir(Diana, id)).Sucesso.Should().BeTrue();
        _c.Atividades.Itens.Should().BeEmpty();
    }

    // ---- Acesso dos comandos ----

    [Fact]
    public async Task Producao_e_anonimo_nao_criam_editam_nem_direcionam()
    {
        var id = await DirecionadaAsync();

        (await Salvar(_pedro)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(_pedro, id, titulo: "Meu título")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Definir(_pedro, id, _pedro.Id, (_pedro, "tudo comigo"))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Concluir(null, id)).Erro.Should().Be(TipoErro.AcessoNegado);
        NoRepositorio(id).Titulo.Should().Be("Elaborar contestação");
        _c.Atividades.Itens.Should().ContainSingle();
    }

    // ---- Ver ----

    [Fact]
    public async Task Diretor_ve_a_atividade_inteira_com_todos_os_direcionamentos_e_as_acoes()
    {
        var id = (await Salvar(Diana, cliente: _maria.Id, processo: _processo.Id)).Valor;
        (await Definir(Diana, id, Diana.Id, (_pedro, "Redigir"), (_paula, "Juntar documentos"))).Sucesso.Should().BeTrue();

        var detalhe = (await Obter(Diana, id)).Valor!;

        detalhe.Should().BeEquivalentTo(new
        {
            Id = id, Titulo = "Elaborar contestação", Descricao = "Prazo de 15 dias.", DataEntrega = (DateOnly?)Entrega,
            Status = StatusAtividade.Direcionada, Atrasada = false, ClienteNome = "Maria Silva",
            ProcessoTitulo = "Reclamação trabalhista", AreaNome = "Trabalhista", TipoAutor = TipoAutor.Usuario,
            AutorNome = "Diana Diretora", RevisorNome = "Diana Diretora",
        });
        detalhe.Responsaveis.Should().BeEquivalentTo(
        [
            new ResponsavelDetalhe(_paula.Id, "Paula", true, "Juntar documentos", null),
            new ResponsavelDetalhe(_pedro.Id, "Pedro", true, "Redigir", null),
        ], opcoes => opcoes.WithStrictOrdering());
        detalhe.Historico.Select(h => (h.StatusNovo, h.UsuarioNome)).Should().Equal(
            (StatusAtividade.Criada, "Diana Diretora"), (StatusAtividade.Direcionada, "Diana Diretora"));
        detalhe.Acoes.Should().Be(new AcoesDaAtividade(
            Editar: true, DefinirResponsaveis: true, TrocarRevisor: true, ConcluirMinhaParte: false,
            AprovarOuDevolver: false, Reabrir: false, Excluir: true));
    }

    [Fact]
    public async Task Responsavel_de_producao_ve_os_nomes_dos_colegas_mas_so_o_proprio_direcionamento()
    {
        var id = await DirecionadaAsync();

        var detalhe = (await Obter(_pedro, id)).Valor!;

        detalhe.Responsaveis.Should().BeEquivalentTo(
        [
            new ResponsavelDetalhe(_paula.Id, "Paula", false, null, null),
            new ResponsavelDetalhe(_pedro.Id, "Pedro", true, "Redigir", null),
        ], opcoes => opcoes.WithStrictOrdering());
        detalhe.Acoes.Should().Be(new AcoesDaAtividade(
            Editar: false, DefinirResponsaveis: false, TrocarRevisor: false, ConcluirMinhaParte: true,
            AprovarOuDevolver: false, Reabrir: false, Excluir: false));
    }

    [Fact]
    public async Task Producao_que_nao_e_responsavel_recebe_nao_encontrada_e_a_tentativa_fica_registrada()
    {
        var id = await DirecionadaAsync();
        _c.Auditoria.Eventos.Clear();

        var resultado = await Obter(_ester, id);
        var inexistente = await Obter(_ester, Guid.CreateVersion7());

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be(NaoEncontrada);
        inexistente.Erro.Should().Be(TipoErro.NaoEncontrado);
        inexistente.Mensagem.Should().Be(NaoEncontrada);
        _c.Auditoria.Eventos.Should().ContainSingle(e => e.Evento == EventosDeSeguranca.AcessoNegado && e.EntidadeId == id);
        (await Obter(null, id)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Acoes_acompanham_o_status_e_o_papel()
    {
        var id = await FeitaAsync();

        (await Obter(Diana, id)).Valor!.Acoes.Should().Be(new AcoesDaAtividade(
            Editar: true, DefinirResponsaveis: false, TrocarRevisor: true, ConcluirMinhaParte: false,
            AprovarOuDevolver: true, Reabrir: false, Excluir: true));
        (await Obter(_davi, id)).Valor!.Acoes.AprovarOuDevolver.Should().BeFalse("Davi é diretor, mas não é o revisor");
        (await Obter(_pedro, id)).Valor!.Acoes.ConcluirMinhaParte.Should().BeFalse("a parte dele já está concluída");

        (await Aprovar(Diana, id)).Sucesso.Should().BeTrue();
        (await Obter(_davi, id)).Valor!.Acoes.Should().Be(new AcoesDaAtividade(
            Editar: true, DefinirResponsaveis: false, TrocarRevisor: false, ConcluirMinhaParte: false,
            AprovarOuDevolver: false, Reabrir: true, Excluir: true));
    }

    [Fact]
    public async Task Atrasada_usa_a_data_do_escritorio()
    {
        var id = await DirecionadaAsync(entrega: new DateOnly(2026, 10, 9));

        _c.Calendario.Hoje = new DateOnly(2026, 10, 9);
        (await Obter(Diana, id)).Valor!.Atrasada.Should().BeFalse();

        _c.Calendario.Hoje = new DateOnly(2026, 10, 10);
        (await Obter(Diana, id)).Valor!.Atrasada.Should().BeTrue();
    }

    [Fact]
    public void Ver_e_consulta_auditavel_da_atividade()
    {
        var id = Guid.CreateVersion7();
        IConsultaAuditavel consulta = new ObterAtividade(id);

        consulta.TipoRecurso.Should().Be("Atividade");
        consulta.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        consulta.RecursoId.Should().Be(id);
    }

    // ---- Listar ----

    [Fact]
    public async Task Diretoria_lista_todas_e_producao_so_as_suas_qualquer_que_seja_o_filtro_pedido()
    {
        var dosDois = await DirecionadaAsync("De Pedro e Paula");
        var daEster = await CriadaAsync("Da Ester");
        (await Definir(Diana, daEster, Diana.Id, (_ester, null))).Sucesso.Should().BeTrue();
        var semNinguem = await CriadaAsync("Sem responsável");

        (await Listar(Diana)).Valor!.Itens.Select(a => a.Id).Should().BeEquivalentTo([dosDois, daEster, semNinguem]);
        (await Listar(_pedro)).Valor!.Itens.Select(a => a.Id).Should().Equal(dosDois);
        (await Listar(_pedro, new ListarAtividades(ResponsavelId: _ester.Id))).Valor!.Itens.Select(a => a.Id)
            .Should().Equal(new[] { dosDois }, "Produção não lista as atividades de outro");
        (await Listar(Diana, new ListarAtividades(ResponsavelId: _ester.Id))).Valor!.Itens.Select(a => a.Id).Should().Equal(daEster);
        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Listagem_filtra_por_status_cliente_atraso_e_busca_e_traz_os_nomes()
    {
        var atrasada = await DirecionadaAsync("Contestação", new DateOnly(2026, 10, 1));
        var noPrazo = (await Salvar(Diana, titulo: "Recurso", cliente: _maria.Id)).Valor;
        _c.Calendario.Hoje = new DateOnly(2026, 10, 9);

        (await Listar(Diana, new ListarAtividades(SomenteAtrasadas: true))).Valor!.Itens.Select(a => a.Id).Should().Equal(atrasada);
        (await Listar(Diana, new ListarAtividades(Status: StatusAtividade.Criada))).Valor!.Itens.Select(a => a.Id).Should().Equal(noPrazo);
        (await Listar(Diana, new ListarAtividades(ClienteId: _maria.Id))).Valor!.Itens.Select(a => a.Id).Should().Equal(noPrazo);
        (await Listar(Diana, new ListarAtividades(Busca: "contest"))).Valor!.Itens.Select(a => a.Id).Should().Equal(atrasada);

        var item = (await Listar(Diana, new ListarAtividades(SomenteAtrasadas: true))).Valor!.Itens.Single();
        item.Atrasada.Should().BeTrue();
        item.Responsaveis.Should().Equal("Paula", "Pedro");
        item.RevisorNome.Should().Be("Diana Diretora");
    }

    [Fact]
    public void Listagem_e_consulta_auditavel_com_os_filtros_no_detalhe()
    {
        IConsultaAuditavel consulta = new ListarAtividades(Status: StatusAtividade.Feita, Busca: "contestação", Pagina: 2);

        consulta.TipoRecurso.Should().Be("Atividade");
        consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
        consulta.RecursoId.Should().BeNull();
        consulta.Detalhe.Should().Contain("contestação").And.Contain("\"Pagina\":2");
    }
}
