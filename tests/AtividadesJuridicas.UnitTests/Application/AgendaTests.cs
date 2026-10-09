using System.Text.Json;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class AgendaTests
{
    private const string NaoEncontrado = "Evento não encontrado.";
    private const string Alterado = "O evento foi alterado por outra pessoa. Recarregue e tente novamente.";

    private static readonly DateTime Inicio = new(2026, 10, 20, 13, 0, 0, DateTimeKind.Utc);

    private readonly Cenario _c = new();
    private readonly Usuario _pedro, _paula, _ester, _inativo;
    private readonly ClientePessoaFisica _maria, _jose;
    private readonly AreaAtuacao _trabalhista = AreaAtuacao.Criar("Trabalhista");
    private readonly Processo _processo;

    public AgendaTests()
    {
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _paula = _c.NovoUsuario("Paula", "paula@x.com", _c.PerfilProducao);
        _ester = _c.NovoUsuario("Ester", "ester@x.com", _c.PerfilProducao);
        _inativo = _c.NovoUsuario("Ivo Inativo", "ivo@x.com", _c.PerfilProducao);
        _inativo.Inativar();
        _maria = ClientePessoaFisica.Criar(new DadosDoCliente("Maria Silva", null, null, Endereco.Vazio, null), Cpf.Criar("529.982.247-25"), null, null);
        _jose = ClientePessoaFisica.Criar(new DadosDoCliente("José Souza", null, null, Endereco.Vazio, null), Cpf.Criar("111.444.777-35"), null, null);
        _c.Clientes.Itens.AddRange([_maria, _jose]);
        _c.Areas.Itens.Add(_trabalhista);
        _processo = Processo.Criar(
            new DadosDoProcesso(null, "Reclamação trabalhista", _trabalhista.Id, null, null, null, null, null, null),
            [new ClienteDoProcesso(_maria.Id, PoloProcessual.Ativo)]);
        _c.Processos.Itens.Add(_processo);
    }

    private Usuario Diana => _c.Diretor;

    private Task<Resultado<EventoSalvo>> Salvar(
        Usuario? ator, Guid? id = null, string titulo = "Audiência de instrução", DateTime? inicio = null, int minutos = 60,
        Guid? cliente = null, Guid? processo = null, Guid? atividade = null, Usuario[]? participantes = null,
        uint? versao = null, TipoEvento tipo = TipoEvento.Audiencia) =>
        new SalvarEventoManipulador(
                _c.Como(ator), _c.Eventos, _c.Usuarios, _c.Clientes, _c.Processos, _c.Atividades, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new SalvarEvento(
                id, titulo, "Levar os documentos.", "Fórum", tipo, inicio ?? Inicio, minutos, cliente, processo, atividade,
                [.. (participantes ?? []).Select(u => u.Id)], versao), default);

    private Task<Resultado> Excluir(Usuario? ator, Guid id, uint? versao = null) =>
        new ExcluirEventoManipulador(_c.Como(ator), _c.Eventos, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new ExcluirEvento(id, versao), default);

    private Task<Resultado<EventoDetalhe>> Obter(Usuario? ator, Guid id) =>
        new ObterEventoManipulador(
                _c.Como(ator), _c.Eventos, _c.Usuarios, _c.Clientes, _c.Processos, _c.Atividades, _c.Auditoria)
            .ManipularAsync(new ObterEvento(id), default);

    private Task<Resultado<IReadOnlyList<EventoResumo>>> Listar(
        Usuario? ator, DateTime? de = null, DateTime? ate = null, Guid? participante = null, TipoEvento? tipo = null) =>
        new ListarEventosManipulador(_c.Como(ator), _c.Eventos)
            .ManipularAsync(new ListarEventos(
                de ?? Inicio.Date, ate ?? Inicio.Date.AddDays(1), participante, tipo), default);

    private Task<Resultado<IReadOnlyList<UsuarioDaAgenda>>> ListarUsuarios(Usuario? ator) =>
        new ListarUsuariosParaAgendaManipulador(_c.Como(ator), _c.Usuarios)
            .ManipularAsync(new ListarUsuariosParaAgenda(), default);

    private async Task<Guid> CriadoAsync(
        Usuario criador, string titulo = "Audiência de instrução", DateTime? inicio = null, int minutos = 60,
        params Usuario[] participantes)
    {
        var resultado = await Salvar(criador, titulo: titulo, inicio: inicio, minutos: minutos, participantes: participantes);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor!.Id;
    }

    private Evento NoRepositorio(Guid id) => _c.Eventos.Itens.Single(e => e.Id == id);

    private async Task<Guid> AtividadeDoPedroAsync()
    {
        var criada = await new SalvarAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(null, "Contestação sigilosa", "Texto.", new DateOnly(2026, 10, 30), null, null, null), default);
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        var definido = await new DefinirResponsaveisDaAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirResponsaveisDaAtividade(
                criada.Valor, [new ResponsavelEntrada(_pedro.Id, "Redigir")], Diana.Id), default);
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
        return criada.Valor;
    }

    // ---- Criar ----

    [Fact]
    public async Task Qualquer_usuario_cria_evento_e_entra_como_participante()
    {
        var resultado = await Salvar(_pedro, participantes: [_paula]);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        resultado.Valor!.Sobreposicoes.Should().BeEmpty();
        var evento = NoRepositorio(resultado.Valor.Id);
        evento.CriadorId.Should().Be(_pedro.Id);
        evento.Participantes.Select(p => p.UsuarioId).Should().BeEquivalentTo([_pedro.Id, _paula.Id]);
        evento.Titulo.Should().Be("Audiência de instrução");
        evento.InicioEm.Should().Be(Inicio);
        evento.DuracaoEstimadaMinutos.Should().Be(60);
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Sem_usuario_tudo_e_acesso_negado()
    {
        var id = await CriadoAsync(Diana);

        (await Salvar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(null, id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Excluir(null, id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Obter(null, id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await ListarUsuarios(null)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    // ---- Quem vê ----

    [Fact]
    public async Task Producao_ve_so_os_eventos_em_que_participa_ou_que_criou_qualquer_que_seja_o_filtro()
    {
        var comPedro = await CriadoAsync(Diana, "Da Diana com Pedro", Inicio, 60, _pedro);
        var criadoPorPedro = await CriadoAsync(_pedro, "Criado por Pedro", Inicio.AddHours(2), 60, _paula);
        // Pedro sai da lista de participantes do que criou, e continua vendo.
        (await Salvar(Diana, criadoPorPedro, titulo: "Criado por Pedro", inicio: Inicio.AddHours(2), participantes: [_paula]))
            .Sucesso.Should().BeTrue();
        await CriadoAsync(_paula, "Só da Paula", Inicio.AddHours(4), 60);

        var seus = await Listar(_pedro);
        seus.Valor!.Select(e => e.Id).Should().Equal(comPedro, criadoPorPedro);

        // O filtro por participante só restringe mais; nunca revela o que Pedro não vê.
        (await Listar(_pedro, participante: _paula.Id)).Valor!.Select(e => e.Id).Should().Equal(criadoPorPedro);
        (await Listar(_pedro, participante: _ester.Id)).Valor!.Should().BeEmpty();
        (await Listar(_pedro, participante: Diana.Id)).Valor!.Select(e => e.Id).Should().Equal(comPedro);
        (await Listar(_pedro, tipo: TipoEvento.Prazo)).Valor!.Should().BeEmpty();
    }

    [Fact]
    public async Task Diretoria_ve_todos_e_filtra_por_participante_e_tipo()
    {
        var a = await CriadoAsync(_pedro, "A", Inicio, 60);
        var b = (await Salvar(_paula, titulo: "B", inicio: Inicio.AddHours(2), tipo: TipoEvento.Reuniao, participantes: [_ester])).Valor!.Id;
        var c = (await Salvar(Diana, titulo: "C", inicio: Inicio.AddHours(4), tipo: TipoEvento.Prazo)).Valor!.Id;

        (await Listar(Diana)).Valor!.Select(e => e.Id).Should().Equal(a, b, c);
        (await Listar(Diana, participante: _ester.Id)).Valor!.Select(e => e.Id).Should().Equal(b);
        (await Listar(Diana, participante: _paula.Id)).Valor!.Select(e => e.Id).Should().Equal(b);
        (await Listar(Diana, tipo: TipoEvento.Prazo)).Valor!.Select(e => e.Id).Should().Equal(c);
        (await Listar(Diana, participante: _ester.Id, tipo: TipoEvento.Prazo)).Valor!.Should().BeEmpty();

        var resumo = (await Listar(Diana)).Valor!.Single(e => e.Id == b);
        resumo.Participantes.Should().Equal("Ester", "Paula");
        resumo.FimEm.Should().Be(Inicio.AddHours(3));
    }

    [Fact]
    public async Task Evento_que_o_usuario_nao_ve_responde_nao_encontrado_e_fica_registrado()
    {
        var id = await CriadoAsync(Diana, participantes: _pedro);
        _c.Auditoria.Eventos.Clear();

        var obter = await Obter(_ester, id);
        var editar = await Salvar(_ester, id, titulo: "Invadido");
        var excluir = await Excluir(_ester, id);

        foreach (var resultado in new Resultado[] { obter, editar, excluir })
        {
            resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
            resultado.Mensagem.Should().Be(NaoEncontrado);
        }

        _c.Auditoria.Eventos.Should().HaveCount(3).And.OnlyContain(e =>
            e.Evento == EventosDeSeguranca.AcessoNegado && e.Entidade == "Evento" && e.EntidadeId == id && e.Imediato);
        var requisicoes = _c.Auditoria.Eventos
            .Select(e => JsonSerializer.SerializeToElement(e.Dados))
            .Select(d => (d.GetProperty("requisicao").GetString(), d.GetProperty("resposta").GetString()))
            .ToList();
        requisicoes.Should().BeEquivalentTo(
        [
            (nameof(ObterEvento), "nao_encontrado"),
            (nameof(SalvarEvento), "nao_encontrado"),
            (nameof(ExcluirEvento), "nao_encontrado"),
        ]);
        NoRepositorio(id).Titulo.Should().Be("Audiência de instrução");
    }

    [Fact]
    public async Task Evento_inexistente_responde_o_mesmo_nao_encontrado_sem_registrar()
    {
        _c.Auditoria.Eventos.Clear();
        var inexistente = Guid.NewGuid();

        var obter = await Obter(_ester, inexistente);
        var editar = await Salvar(_ester, inexistente);
        var excluir = await Excluir(_ester, inexistente);

        foreach (var resultado in new Resultado[] { obter, editar, excluir })
        {
            resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
            resultado.Mensagem.Should().Be(NaoEncontrado);
        }

        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Participante_que_nao_criou_ve_mas_nao_edita_nem_exclui()
    {
        var id = await CriadoAsync(Diana, participantes: _paula);

        (await Obter(_paula, id)).Sucesso.Should().BeTrue();
        var editar = await Salvar(_paula, id, titulo: "Mudei");
        var excluir = await Excluir(_paula, id);

        editar.Erro.Should().Be(TipoErro.AcessoNegado);
        excluir.Erro.Should().Be(TipoErro.AcessoNegado);
        NoRepositorio(id).Titulo.Should().Be("Audiência de instrução");
        _c.Auditoria.Eventos.Should().NotContain(e => e.Evento == EventosDeSeguranca.AcessoNegado && e.Entidade == "Evento");
    }

    [Fact]
    public async Task Criador_retirado_dos_participantes_continua_vendo_e_gerenciando()
    {
        var id = await CriadoAsync(_pedro, participantes: _paula);
        (await Salvar(Diana, id, participantes: [_paula])).Sucesso.Should().BeTrue();
        NoRepositorio(id).Participa(_pedro.Id).Should().BeFalse();

        (await Listar(_pedro)).Valor!.Select(e => e.Id).Should().Equal(id);
        var detalhe = await Obter(_pedro, id);
        detalhe.Sucesso.Should().BeTrue();
        detalhe.Valor!.Acoes.Should().Be(new AcoesDoEvento(true, true));
        (await Salvar(_pedro, id, titulo: "Remarcada", participantes: [_paula])).Sucesso.Should().BeTrue();
        (await Excluir(_pedro, id)).Sucesso.Should().BeTrue();
    }

    // ---- Sobreposição ----

    [Fact]
    public async Task Sobreposicao_e_avisada_e_nao_impede()
    {
        await CriadoAsync(Diana, "Reunião da diretoria", Inicio, 60, _paula, _ester);

        var resultado = await Salvar(_pedro, titulo: "Outro", inicio: Inicio.AddMinutes(30), participantes: [_paula, _ester]);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        resultado.Valor!.Sobreposicoes.Should().Equal(
            new SobreposicaoDeAgenda(_ester.Id, "Ester", Inicio, Inicio.AddHours(1)),
            new SobreposicaoDeAgenda(_paula.Id, "Paula", Inicio, Inicio.AddHours(1)));
        _c.Eventos.Itens.Should().HaveCount(2);
    }

    [Fact]
    public async Task Eventos_encostados_nao_sao_sobreposicao()
    {
        await CriadoAsync(Diana, "Primeiro", Inicio, 60, _paula);

        var depois = await Salvar(_pedro, inicio: Inicio.AddHours(1), participantes: [_paula]);
        var antes = await Salvar(_pedro, inicio: Inicio.AddHours(-1), participantes: [_paula]);

        depois.Valor!.Sobreposicoes.Should().BeEmpty();
        antes.Valor!.Sobreposicoes.Should().BeEmpty();
    }

    [Fact]
    public async Task Editar_nao_conflita_consigo_mesmo()
    {
        var id = await CriadoAsync(_pedro, participantes: _paula);

        var resultado = await Salvar(_pedro, id, inicio: Inicio.AddMinutes(10), participantes: [_paula]);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        resultado.Valor!.Id.Should().Be(id);
        resultado.Valor.Sobreposicoes.Should().BeEmpty();
    }

    [Fact]
    public async Task Evento_excluido_nao_gera_aviso()
    {
        var id = await CriadoAsync(Diana, participantes: _paula);
        (await Excluir(Diana, id)).Sucesso.Should().BeTrue();

        var resultado = await Salvar(_pedro, participantes: [_paula]);

        resultado.Valor!.Sobreposicoes.Should().BeEmpty();
    }

    [Fact]
    public async Task Aviso_de_sobreposicao_nao_revela_o_titulo_do_outro_evento()
    {
        await CriadoAsync(Diana, "Estratégia confidencial", Inicio, 60, _paula);

        var resultado = await Salvar(_pedro, inicio: Inicio, participantes: [_paula]);

        typeof(SobreposicaoDeAgenda).GetProperties().Select(p => p.Name)
            .Should().BeEquivalentTo(nameof(SobreposicaoDeAgenda.UsuarioId), nameof(SobreposicaoDeAgenda.UsuarioNome),
                nameof(SobreposicaoDeAgenda.InicioEm), nameof(SobreposicaoDeAgenda.FimEm));
        resultado.Valor!.Sobreposicoes.Should().ContainSingle();
        JsonSerializer.Serialize(resultado.Valor).Should().NotContain("Estratégia");
    }

    // ---- Validações ----

    [Fact]
    public async Task Vinculos_sao_validados()
    {
        var daoutra = await new SalvarAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(null, "Da Diana", "Texto.", null, null, null, null), default);
        var doPedro = await AtividadeDoPedroAsync();

        static Task Recusa(Resultado<EventoSalvo> resultado, string mensagem)
        {
            resultado.Erro.Should().Be(TipoErro.Validacao);
            resultado.Mensagem.Should().Be(mensagem);
            return Task.CompletedTask;
        }

        await Recusa(await Salvar(_pedro, cliente: Guid.NewGuid()), "Cliente não encontrado.");
        await Recusa(await Salvar(_pedro, processo: Guid.NewGuid()), "Processo não encontrado.");
        await Recusa(await Salvar(_pedro, cliente: _jose.Id, processo: _processo.Id),
            "O cliente do evento deve ser um dos clientes do processo.");
        _jose.Inativar();
        await Recusa(await Salvar(_pedro, cliente: _jose.Id), "O cliente está inativo.");
        await Recusa(await Salvar(_pedro, atividade: Guid.NewGuid()), "Atividade não encontrada.");
        // Existente, mas que Pedro não vê: a mesma mensagem.
        await Recusa(await Salvar(_pedro, atividade: daoutra.Valor), "Atividade não encontrada.");

        // O que é válido passa: cliente do processo, atividade de que Pedro é responsável.
        var certo = await Salvar(_pedro, cliente: _maria.Id, processo: _processo.Id, atividade: doPedro);
        certo.Sucesso.Should().BeTrue(certo.Mensagem);
        var evento = NoRepositorio(certo.Valor!.Id);
        (evento.ClienteId, evento.ProcessoId, evento.AtividadeId).Should().Be((_maria.Id, _processo.Id, doPedro));

        // A Diretoria vê qualquer atividade.
        (await Salvar(Diana, atividade: daoutra.Valor)).Sucesso.Should().BeTrue();

        // Cliente inativo que já estava no evento permanece; trocar para outro inativo é recusado.
        _maria.Inativar();
        var mantido = await Salvar(_pedro, certo.Valor.Id, titulo: "Remarcada", cliente: _maria.Id, processo: _processo.Id, atividade: doPedro, participantes: [_pedro]);
        mantido.Sucesso.Should().BeTrue(mantido.Mensagem);
        var trocado = await Salvar(_pedro, certo.Valor.Id, cliente: _jose.Id, participantes: [_pedro]);
        trocado.Erro.Should().Be(TipoErro.Validacao);
        trocado.Mensagem.Should().Be("O cliente está inativo.");
        NoRepositorio(certo.Valor.Id).ClienteId.Should().Be(_maria.Id);
    }

    [Fact]
    public async Task Atividade_que_o_usuario_deixou_de_ver_so_e_revalidada_quando_muda()
    {
        var doPedro = await AtividadeDoPedroAsync();
        var id = (await Salvar(_pedro, atividade: doPedro)).Valor!.Id;

        // Pedro deixa de ser responsável; o evento, que já tinha a atividade, continua editável sem trocá-la.
        var atividade = _c.Atividades.Itens.Single(a => a.Id == doPedro);
        var definido = await new DefinirResponsaveisDaAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirResponsaveisDaAtividade(
                doPedro, [new ResponsavelEntrada(_paula.Id, null)], Diana.Id), default);
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
        atividade.EhResponsavel(_pedro.Id).Should().BeFalse();

        (await Salvar(_pedro, id, titulo: "Remarcada", atividade: doPedro, participantes: [_pedro])).Sucesso.Should().BeTrue();
        var outra = await Salvar(_pedro, id, atividade: Guid.NewGuid(), participantes: [_pedro]);
        outra.Erro.Should().Be(TipoErro.Validacao);
        outra.Mensagem.Should().Be("Atividade não encontrada.");
    }

    [Fact]
    public async Task Participante_inexistente_ou_inativo_e_recusado()
    {
        var id = await CriadoAsync(_pedro, participantes: _paula);

        var inexistente = await new SalvarEventoManipulador(
                _c.Como(_pedro), _c.Eventos, _c.Usuarios, _c.Clientes, _c.Processos, _c.Atividades, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new SalvarEvento(
                null, "X", null, null, TipoEvento.Reuniao, Inicio, 30, null, null, null, [Guid.NewGuid()]), default);
        var inativoNaCriacao = await Salvar(_pedro, participantes: [_inativo]);
        var inativoNaEdicao = await Salvar(_pedro, id, participantes: [_paula, _inativo]);

        inexistente.Erro.Should().Be(TipoErro.Validacao);
        inexistente.Mensagem.Should().Be("Usuário não encontrado.");
        inativoNaCriacao.Erro.Should().Be(TipoErro.Validacao);
        inativoNaCriacao.Mensagem.Should().Be("Usuário inativo não pode ser incluído como participante.");
        inativoNaEdicao.Erro.Should().Be(TipoErro.Validacao);
        _c.Eventos.Itens.Should().ContainSingle();
        NoRepositorio(id).Participantes.Select(p => p.UsuarioId).Should().BeEquivalentTo([_pedro.Id, _paula.Id]);
    }

    [Fact]
    public async Task Participante_que_ja_estava_e_ficou_inativo_permanece()
    {
        var id = await CriadoAsync(_pedro, participantes: _paula);
        _paula.Inativar();

        var resultado = await Salvar(_pedro, id, titulo: "Remarcada", participantes: [_pedro, _paula]);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        NoRepositorio(id).Participa(_paula.Id).Should().BeTrue();
    }

    [Fact]
    public async Task Recusas_do_dominio_chegam_como_validacao()
    {
        (await Salvar(_pedro, titulo: " ")).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_pedro, minutos: 0)).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_pedro, minutos: Evento.DuracaoMaximaMinutos + 1)).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_pedro, inicio: DateTime.SpecifyKind(Inicio, DateTimeKind.Unspecified))).Erro.Should().Be(TipoErro.Validacao);
        _c.Eventos.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Versao_esperada_diferente_e_conflito_em_editar_e_excluir()
    {
        var id = await CriadoAsync(_pedro, participantes: _paula);
        var versao = NoRepositorio(id).Versao;
        var outra = versao + 1;

        // A conferência vem antes de qualquer validação: um título inválido não a esconde.
        var editar = await Salvar(_pedro, id, titulo: " ", versao: outra, participantes: [_paula]);
        var excluir = await Excluir(_pedro, id, outra);

        editar.Erro.Should().Be(TipoErro.Conflito);
        editar.Mensagem.Should().Be(Alterado);
        excluir.Erro.Should().Be(TipoErro.Conflito);
        excluir.Mensagem.Should().Be(Alterado);
        NoRepositorio(id).Titulo.Should().Be("Audiência de instrução");

        (await Salvar(_pedro, id, titulo: "Atual", versao: versao, participantes: [_paula])).Sucesso.Should().BeTrue();
        (await Excluir(_pedro, id, versao)).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Falha_de_concorrencia_ou_de_unicidade_ao_gravar_e_conflito()
    {
        var id = await CriadoAsync(_pedro);

        _c.Unidade.Falha = new ExcecaoDeConcorrencia("alterado");
        var salvar = await Salvar(_pedro, id, titulo: "Outro", participantes: [_pedro]);
        var excluir = await Excluir(_pedro, id);
        _c.Unidade.Falha = new ExcecaoDeConflito("repetido");
        var repetido = await Salvar(_pedro, titulo: "Novo");

        foreach (var resultado in new Resultado[] { salvar, excluir, repetido })
        {
            resultado.Erro.Should().Be(TipoErro.Conflito);
            resultado.Mensagem.Should().Be(Alterado);
        }
    }

    [Fact]
    public async Task Periodo_invalido_ou_longo_demais_e_recusado()
    {
        (await Listar(Diana, Inicio, Inicio)).Mensagem.Should().Be("Período inválido.");
        (await Listar(Diana, Inicio, Inicio.AddDays(-1))).Mensagem.Should().Be("Período inválido.");
        (await Listar(Diana, DateTime.SpecifyKind(Inicio, DateTimeKind.Unspecified), Inicio.AddDays(1)))
            .Mensagem.Should().Be("Período inválido.");

        var longo = await Listar(Diana, Inicio, Inicio.AddDays(92).AddMinutes(1));
        longo.Erro.Should().Be(TipoErro.Validacao);
        longo.Mensagem.Should().Be("O período deve ter no máximo 92 dias.");

        (await Listar(Diana, Inicio, Inicio.AddDays(92))).Sucesso.Should().BeTrue();
    }

    // ---- Listagem e ficha ----

    [Fact]
    public async Task Listagem_traz_os_eventos_que_tocam_o_periodo_em_ordem()
    {
        var de = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        var ate = de.AddDays(1);
        var terminaDentro = await CriadoAsync(Diana, "Começa antes", de.AddHours(-2), 180);
        var dentro = await CriadoAsync(Diana, "Dentro", de.AddHours(10), 60);
        var contem = await CriadoAsync(Diana, "Contém", de.AddDays(-1), 3 * 24 * 60);
        var comecaDentro = await CriadoAsync(Diana, "Começa dentro", ate.AddHours(-1), 120);
        await CriadoAsync(Diana, "Encostado antes", de.AddHours(-1), 60);
        await CriadoAsync(Diana, "Encostado depois", ate, 60);
        await CriadoAsync(Diana, "Bem depois", ate.AddDays(2), 60);

        var lista = (await Listar(Diana, de, ate)).Valor!;

        lista.Select(e => e.Id).Should().Equal(contem, terminaDentro, dentro, comecaDentro);
        lista.Select(e => e.Titulo).Should().Equal("Contém", "Começa antes", "Dentro", "Começa dentro");
    }

    [Fact]
    public async Task Ficha_do_evento_traz_os_nomes_dos_vinculos_e_das_pessoas()
    {
        var doPedro = await AtividadeDoPedroAsync();
        var criado = await Salvar(_pedro, cliente: _maria.Id, processo: _processo.Id, atividade: doPedro, participantes: [_paula]);
        _paula.Inativar();

        var detalhe = (await Obter(_pedro, criado.Valor!.Id)).Valor!;

        detalhe.ClienteNome.Should().Be("Maria Silva");
        detalhe.ProcessoTitulo.Should().Be("Reclamação trabalhista");
        detalhe.AtividadeTitulo.Should().Be("Contestação sigilosa");
        detalhe.CriadorNome.Should().Be("Pedro");
        detalhe.CriadorId.Should().Be(_pedro.Id);
        detalhe.Participantes.Should().Equal(
            new ParticipanteDetalhe(_paula.Id, "Paula", false), new ParticipanteDetalhe(_pedro.Id, "Pedro", true));
        detalhe.FimEm.Should().Be(Inicio.AddHours(1));
        detalhe.DuracaoEstimadaMinutos.Should().Be(60);
        detalhe.Versao.Should().Be(NoRepositorio(detalhe.Id).Versao);
    }

    [Fact]
    public async Task Ficha_do_evento_esconde_o_titulo_da_atividade_que_o_usuario_nao_ve()
    {
        var doPedro = await AtividadeDoPedroAsync();
        var id = (await Salvar(Diana, atividade: doPedro, participantes: [_paula])).Valor!.Id;

        var paula = (await Obter(_paula, id)).Valor!;
        var diana = (await Obter(Diana, id)).Valor!;

        paula.AtividadeId.Should().Be(doPedro);
        paula.AtividadeTitulo.Should().BeNull();
        diana.AtividadeTitulo.Should().Be("Contestação sigilosa");
    }

    [Fact]
    public async Task Acoes_do_evento_seguem_o_papel()
    {
        var id = await CriadoAsync(_pedro, participantes: _paula);

        (await Obter(_pedro, id)).Valor!.Acoes.Should().Be(new AcoesDoEvento(true, true));
        (await Obter(Diana, id)).Valor!.Acoes.Should().Be(new AcoesDoEvento(true, true));
        (await Obter(_paula, id)).Valor!.Acoes.Should().Be(new AcoesDoEvento(false, false));
    }

    [Fact]
    public async Task Lista_de_usuarios_para_a_agenda_traz_so_ativos_com_id_e_nome_para_qualquer_usuario()
    {
        var comoProducao = await ListarUsuarios(_pedro);
        var comoDiretoria = await ListarUsuarios(Diana);

        var esperado = new[]
        {
            new UsuarioDaAgenda(Diana.Id, "Diana Diretora"), new UsuarioDaAgenda(_ester.Id, "Ester"),
            new UsuarioDaAgenda(_paula.Id, "Paula"), new UsuarioDaAgenda(_pedro.Id, "Pedro"),
        };
        comoProducao.Valor!.Should().Equal(esperado);
        comoDiretoria.Valor!.Should().Equal(esperado);
        typeof(UsuarioDaAgenda).GetProperties().Select(p => p.Name).Should().BeEquivalentTo("Id", "Nome");
    }

    [Fact]
    public async Task Consultas_sao_auditaveis()
    {
        var id = await CriadoAsync(Diana);
        var de = Inicio.Date;
        var ate = de.AddDays(2);

        var listar = new ListarEventos(de, ate, _paula.Id, TipoEvento.Reuniao);
        var obter = new ObterEvento(id);

        listar.TipoRecurso.Should().Be("Evento");
        listar.Acao.Should().Be(AcaoDeAcesso.Listou);
        ((IConsultaAuditavel)listar).RecursoId.Should().BeNull();
        var detalhe = JsonDocument.Parse(listar.Detalhe!).RootElement;
        detalhe.GetProperty("ParticipanteId").GetGuid().Should().Be(_paula.Id);
        detalhe.GetProperty("Tipo").GetInt32().Should().Be((int)TipoEvento.Reuniao);
        detalhe.GetProperty("DeUtc").GetDateTime().Should().Be(de);
        detalhe.GetProperty("AteUtc").GetDateTime().Should().Be(ate);
        obter.TipoRecurso.Should().Be("Evento");
        obter.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        obter.RecursoId.Should().Be(id);
        typeof(ListarUsuariosParaAgenda).GetInterfaces().Should().NotContain(typeof(IConsultaAuditavel));
    }

    // ---- Onda final: auditoria dos participantes ----

    private static (Guid[] Adicionados, Guid[] Removidos) ListasAnotadas(EventoAnotado anotado)
    {
        var d = JsonSerializer.SerializeToElement(anotado.Dados);
        return ([.. d.GetProperty("adicionados").EnumerateArray().Select(x => x.GetGuid())],
            [.. d.GetProperty("removidos").EnumerateArray().Select(x => x.GetGuid())]);
    }

    [Fact]
    public async Task Edicao_que_muda_participantes_anota_o_evento_com_adicionados_e_removidos()
    {
        var id = await CriadoAsync(_pedro, participantes: _paula);
        _c.Auditoria.Eventos.Clear();

        var resultado = await Salvar(_pedro, id, participantes: [_pedro, _ester]);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var anotado = _c.Auditoria.Eventos.Single(e => e.Evento == "evento_participantes_alterados");
        anotado.Entidade.Should().Be("Evento");
        anotado.EntidadeId.Should().Be(id);
        anotado.Imediato.Should().BeFalse();
        var (adicionados, removidos) = ListasAnotadas(anotado);
        adicionados.Should().Equal(_ester.Id);
        removidos.Should().Equal(_paula.Id);
    }

    [Fact]
    public async Task Edicao_que_nao_muda_participantes_e_a_criacao_nao_anotam_nada()
    {
        _c.Auditoria.Eventos.Clear();
        var id = await CriadoAsync(_pedro, participantes: _paula);
        _c.Auditoria.Eventos.Should().NotContain(e => e.Evento == "evento_participantes_alterados");

        var resultado = await Salvar(_pedro, id, titulo: "Remarcada", participantes: [_pedro, _paula]);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _c.Auditoria.Eventos.Should().NotContain(e => e.Evento == "evento_participantes_alterados");
    }

    // ---- Onda final: vínculo só é revalidado quando muda ----

    [Fact]
    public async Task Evento_continua_editavel_quando_o_cliente_saiu_do_processo()
    {
        var id = (await Salvar(_pedro, cliente: _maria.Id, processo: _processo.Id, participantes: [_pedro])).Valor!.Id;
        _processo.DefinirClientes([new ClienteDoProcesso(_jose.Id, PoloProcessual.Ativo)]);

        var remarcado = await Salvar(
            _pedro, id, inicio: Inicio.AddHours(2), cliente: _maria.Id, processo: _processo.Id, participantes: [_pedro]);

        remarcado.Sucesso.Should().BeTrue(remarcado.Mensagem);
        var evento = NoRepositorio(id);
        evento.InicioEm.Should().Be(Inicio.AddHours(2));
        (evento.ClienteId, evento.ProcessoId).Should().Be((_maria.Id, _processo.Id));

        // Trocar o cliente por outro que não é do processo continua recusado.
        var outro = ClientePessoaFisica.Criar(new DadosDoCliente("Otávio Lima", null, null, Endereco.Vazio, null), Cpf.Criar("390.533.447-05"), null, null);
        _c.Clientes.Itens.Add(outro);
        var trocado = await Salvar(_pedro, id, cliente: outro.Id, processo: _processo.Id, participantes: [_pedro]);
        trocado.Erro.Should().Be(TipoErro.Validacao);
        trocado.Mensagem.Should().Be("O cliente do evento deve ser um dos clientes do processo.");
        NoRepositorio(id).ClienteId.Should().Be(_maria.Id);
    }

    [Fact]
    public async Task Evento_continua_editavel_quando_o_processo_deixou_de_existir()
    {
        var id = (await Salvar(_pedro, cliente: _maria.Id, processo: _processo.Id, participantes: [_pedro])).Valor!.Id;
        _c.Processos.Itens.Remove(_processo);

        var remarcado = await Salvar(
            _pedro, id, inicio: Inicio.AddHours(2), cliente: _maria.Id, processo: _processo.Id, participantes: [_pedro]);

        remarcado.Sucesso.Should().BeTrue(remarcado.Mensagem);
        NoRepositorio(id).ProcessoId.Should().Be(_processo.Id);
    }

    [Fact]
    public async Task Evento_com_atividade_que_o_editor_nao_ve_mais_so_muda_o_horario_e_nao_aponta_para_outra_invisivel()
    {
        var doPedro = await AtividadeDoPedroAsync();
        var daDiana = (await new SalvarAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(null, "Só da Diana", "Texto.", null, null, null, null), default)).Valor;
        var id = (await Salvar(_pedro, atividade: doPedro, participantes: [_pedro])).Valor!.Id;
        (await new DefinirResponsaveisDaAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirResponsaveisDaAtividade(
                doPedro, [new ResponsavelEntrada(_paula.Id, null)], Diana.Id), default)).Sucesso.Should().BeTrue();

        var horario = await Salvar(_pedro, id, inicio: Inicio.AddHours(3), atividade: doPedro, participantes: [_pedro]);
        var outra = await Salvar(_pedro, id, atividade: daDiana, participantes: [_pedro]);

        horario.Sucesso.Should().BeTrue(horario.Mensagem);
        NoRepositorio(id).AtividadeId.Should().Be(doPedro);
        outra.Erro.Should().Be(TipoErro.Validacao);
        outra.Mensagem.Should().Be("Atividade não encontrada.");

        // Atividade excluída depois: o horário ainda muda.
        _c.Atividades.Itens.RemoveAll(a => a.Id == doPedro);
        (await Salvar(_pedro, id, inicio: Inicio.AddHours(4), atividade: doPedro, participantes: [_pedro]))
            .Sucesso.Should().BeTrue();
    }

    // ---- Onda final: versão esperada de quem não vê / não gerencia ----

    [Fact]
    public async Task Versao_esperada_velha_de_quem_nao_ve_o_evento_continua_nao_encontrado()
    {
        var id = await CriadoAsync(Diana, participantes: _pedro);
        var velha = NoRepositorio(id).Versao + 7;

        var editar = await Salvar(_ester, id, versao: velha);
        var excluir = await Excluir(_ester, id, velha);

        foreach (var resultado in new Resultado[] { editar, excluir })
        {
            resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
            resultado.Mensagem.Should().Be(NaoEncontrado);
        }
    }

    [Fact]
    public async Task Versao_esperada_velha_de_participante_que_nao_gerencia_continua_acesso_negado()
    {
        var id = await CriadoAsync(Diana, participantes: _paula);
        var velha = NoRepositorio(id).Versao + 7;

        var editar = await Salvar(_paula, id, versao: velha);
        var excluir = await Excluir(_paula, id, velha);

        editar.Erro.Should().Be(TipoErro.AcessoNegado);
        excluir.Erro.Should().Be(TipoErro.AcessoNegado);
    }

    // ---- Onda final: ordem estável e datas extremas ----

    [Fact]
    public async Task Eventos_que_comecam_juntos_saem_em_ordem_de_id()
    {
        var a = await CriadoAsync(Diana, "A", Inicio, 60);
        var b = await CriadoAsync(Diana, "B", Inicio, 60);
        var c = await CriadoAsync(Diana, "C", Inicio, 60);

        var lista = (await Listar(Diana)).Valor!.Select(e => e.Id).ToList();

        lista.Should().Equal(new[] { a, b, c }.Order());
        lista.Should().Equal((await Listar(Diana)).Valor!.Select(e => e.Id));
    }

    [Fact]
    public async Task Datas_extremas_e_participantes_nulos_sao_validacao_nunca_excecao()
    {
        (await Salvar(_pedro, inicio: DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc))).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_pedro, inicio: new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc))).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_pedro, inicio: new DateTime(1999, 12, 31, 23, 0, 0, DateTimeKind.Utc))).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_pedro, inicio: new DateTime(2101, 1, 1, 0, 0, 0, DateTimeKind.Utc))).Erro.Should().Be(TipoErro.Validacao);

        var nulos = await new SalvarEventoManipulador(
                _c.Como(_pedro), _c.Eventos, _c.Usuarios, _c.Clientes, _c.Processos, _c.Atividades, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new SalvarEvento(
                null, "X", null, null, TipoEvento.Reuniao, Inicio, 30, null, null, null, null!), default);
        nulos.Erro.Should().Be(TipoErro.Validacao);
        _c.Eventos.Itens.Should().BeEmpty();

        (await Salvar(_pedro, inicio: new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc))).Sucesso.Should().BeTrue();
        (await Salvar(_pedro, inicio: new DateTime(2100, 12, 31, 23, 59, 0, DateTimeKind.Utc))).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Periodo_com_limites_extremos_e_validacao_nunca_excecao()
    {
        var minimo = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maximo = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        var extremos = new (DateTime De, DateTime Ate)[]
        {
            (minimo, minimo.AddDays(1)),
            (maximo.AddDays(-1), maximo),
        };

        foreach (var (de, ate) in extremos)
        {
            var resultado = await Listar(Diana, de, ate);
            resultado.Erro.Should().Be(TipoErro.Validacao);
            resultado.Mensagem.Should().Be("Período inválido.");
        }
    }

    // ---- Onda final: atividade excluída x sem acesso na ficha ----

    [Fact]
    public async Task Ficha_so_diz_que_a_atividade_foi_excluida_a_quem_poderia_te_la_visto()
    {
        var doPedro = await AtividadeDoPedroAsync();
        var daDiana = (await new SalvarAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(null, "Só da Diana", "Texto.", null, null, null, null), default)).Valor;
        var comExcluida = (await Salvar(Diana, atividade: doPedro, participantes: [_pedro])).Valor!.Id;
        var comOculta = (await Salvar(Diana, atividade: daDiana, participantes: [_pedro])).Valor!.Id;
        (await Obter(Diana, comExcluida)).Valor!.AtividadeExcluida.Should().BeFalse();

        _c.Atividades.Itens.RemoveAll(a => a.Id == doPedro);

        var excluidaParaDiana = (await Obter(Diana, comExcluida)).Valor!;
        var excluidaParaPedro = (await Obter(_pedro, comExcluida)).Valor!;
        var ocultaParaPedro = (await Obter(_pedro, comOculta)).Valor!;
        var ocultaParaDiana = (await Obter(Diana, comOculta)).Valor!;

        excluidaParaDiana.AtividadeExcluida.Should().BeTrue();
        excluidaParaDiana.AtividadeTitulo.Should().BeNull();
        // Quem não poderia ver a atividade recebe o mesmo para a excluída e para a existente que não vê.
        excluidaParaPedro.AtividadeExcluida.Should().BeFalse();
        excluidaParaPedro.AtividadeTitulo.Should().BeNull();
        ocultaParaPedro.AtividadeExcluida.Should().BeFalse();
        ocultaParaPedro.AtividadeTitulo.Should().BeNull();
        excluidaParaPedro.AtividadeId.Should().Be(doPedro);
        ocultaParaDiana.AtividadeExcluida.Should().BeFalse();
        ocultaParaDiana.AtividadeTitulo.Should().Be("Só da Diana");
    }
}
