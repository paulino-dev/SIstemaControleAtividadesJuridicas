using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AgendaTests(BancoFixture banco) : TesteComBanco(banco)
{
    private const string Alterado = "O evento foi alterado por outra pessoa. Recarregue e tente novamente.";

    // Horários no futuro distante, em UTC, para não depender do relógio.
    private static readonly DateTime Inicio = new(2090, 6, 15, 13, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DiaInicio = new(2090, 6, 15, 0, 0, 0, DateTimeKind.Utc);

    private Guid _diana, _davi, _pedro, _paula, _ester, _maria, _trabalhista, _processo, _atividade;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _diana = await IdDoAdminAsync();
        _davi = await NovoUsuarioAsync("Davi Diretor", "davi@x.com", TipoPerfil.Diretoria);
        _pedro = await NovoUsuarioAsync("Pedro", "pedro@x.com", TipoPerfil.Producao);
        _paula = await NovoUsuarioAsync("Paula", "paula@x.com", TipoPerfil.Producao);
        _ester = await NovoUsuarioAsync("Ester", "ester@x.com", TipoPerfil.Producao);
        _trabalhista = (await ExecutarAsync(_diana, new SalvarAreaAtuacao(null, "Trabalhista"))).Valor;
        _maria = (await ExecutarAsync(_diana, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null))).Valor;
        _processo = (await ExecutarAsync(_diana, new SalvarProcesso(
            null,
            new DadosDoProcessoEntrada(null, "Reclamação trabalhista", _trabalhista, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(_maria, PoloProcessual.Ativo)]))).Valor;
        var atividade = await ExecutarAsync(_diana, new SalvarAtividade(
            null, "Elaborar contestação", "Prazo de 15 dias.", new DateOnly(2090, 6, 20), null, null, null));
        atividade.Sucesso.Should().BeTrue(atividade.Mensagem);
        _atividade = atividade.Valor;
        (await ExecutarAsync(_diana, new DefinirResponsaveisDaAtividade(
            _atividade, [new ResponsavelEntrada(_pedro, "Redigir")], _diana))).Sucesso.Should().BeTrue();
    }

    private async Task<Guid> NovoUsuarioAsync(string nome, string email, TipoPerfil tipo)
    {
        var perfil = await IdDoPerfilAsync(tipo);
        var criado = await ExecutarAsync(_diana, new CriarUsuario(nome, email, "senha-do-" + nome[..3].ToLowerInvariant(), perfil, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    private Task<Resultado<EventoSalvo>> SalvarAsync(
        Guid quem, string titulo = "Audiência de instrução", DateTime? inicio = null, int minutos = 60, Guid? id = null,
        Guid? cliente = null, Guid? processo = null, Guid? atividade = null, Guid[]? participantes = null,
        uint? versao = null, TipoEvento tipo = TipoEvento.Audiencia) =>
        ExecutarAsync(quem, new SalvarEvento(
            id, titulo, "Levar os documentos.", "Fórum", tipo, inicio ?? Inicio, minutos, cliente, processo, atividade,
            participantes ?? [], versao));

    private async Task<Guid> CriarAsync(
        Guid quem, string titulo = "Audiência de instrução", DateTime? inicio = null, int minutos = 60,
        params Guid[] participantes)
    {
        var criado = await SalvarAsync(quem, titulo, inicio, minutos, participantes: participantes);
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor!.Id;
    }

    private async Task<uint> VersaoAsync(Guid id) => (await ExecutarAsync(_diana, new ObterEvento(id))).Valor!.Versao;

    private Task<Evento> EventoNoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Eventos.IgnoreQueryFilters().AsNoTracking().SingleAsync(e => e.Id == id));

    private Task<List<EventoParticipante>> ParticipantesNoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Set<EventoParticipante>().IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.EventoId == id).OrderBy(p => p.CriadoEm).ThenBy(p => p.Id).ToListAsync());

    private static PostgresException PostgresDe(Exception erro)
    {
        for (var e = erro; e is not null; e = e.InnerException)
            if (e is PostgresException pg) return pg;
        throw new InvalidOperationException("Nenhuma PostgresException na cadeia de exceções: " + erro);
    }

    [Fact]
    public async Task Evento_vai_e_volta_do_banco_com_participantes_e_vinculos()
    {
        var criado = await SalvarAsync(
            _diana, cliente: _maria, processo: _processo, atividade: _atividade, participantes: [_pedro, _paula]);
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        criado.Valor!.Sobreposicoes.Should().BeEmpty();

        var detalhe = (await ExecutarAsync(_diana, new ObterEvento(criado.Valor.Id))).Valor!;

        detalhe.Should().BeEquivalentTo(new
        {
            Id = criado.Valor.Id, Titulo = "Audiência de instrução", Descricao = "Levar os documentos.", Local = "Fórum",
            Tipo = TipoEvento.Audiencia, InicioEm = Inicio, DuracaoEstimadaMinutos = 60, FimEm = Inicio.AddHours(1),
            ClienteId = (Guid?)_maria, ClienteNome = "Maria Silva", ProcessoId = (Guid?)_processo,
            ProcessoTitulo = "Reclamação trabalhista", AtividadeId = (Guid?)_atividade, AtividadeTitulo = "Elaborar contestação",
            CriadorId = _diana, CriadorNome = "Diana Diretora", Acoes = new AcoesDoEvento(true, true),
        });
        detalhe.InicioEm.Kind.Should().Be(DateTimeKind.Utc);
        detalhe.Participantes.Should().BeEquivalentTo(
        [
            new ParticipanteDetalhe(_diana, "Diana Diretora", true),
            new ParticipanteDetalhe(_paula, "Paula", true),
            new ParticipanteDetalhe(_pedro, "Pedro", true),
        ], opcoes => opcoes.WithStrictOrdering());
        detalhe.Versao.Should().NotBe(0u);

        var noBanco = await EventoNoBancoAsync(criado.Valor.Id);
        noBanco.InicioEm.Should().Be(Inicio);
        noBanco.CriadorId.Should().Be(_diana);
        (await ParticipantesNoBancoAsync(criado.Valor.Id)).Should().HaveCount(3).And.OnlyContain(p => p.ExcluidoEm == null);
    }

    [Fact]
    public async Task Listagem_por_periodo_no_banco()
    {
        var de = DiaInicio;
        var ate = de.AddDays(1);
        var terminaDentro = await CriarAsync(_diana, "Começa antes", de.AddHours(-2), 180);
        var dentro = await CriarAsync(_diana, "Dentro", de.AddHours(10), 60);
        var contem = await CriarAsync(_diana, "Contém", de.AddDays(-1), 3 * 24 * 60);
        var comecaDentro = await CriarAsync(_diana, "Começa dentro", ate.AddHours(-1), 120);
        await CriarAsync(_diana, "Encostado antes", de.AddHours(-1), 60);
        await CriarAsync(_diana, "Encostado depois", ate, 60);
        await CriarAsync(_diana, "Bem depois", ate.AddDays(2), 60);
        // Na borda da janela ampliada: dura 7 dias e termina exatamente no início do período (fora) ou um minuto depois (dentro).
        await CriarAsync(_diana, "Sete dias que terminam no início", de.AddDays(-7), Evento.DuracaoMaximaMinutos);
        var seteDiasQueTocam = await CriarAsync(_diana, "Sete dias que tocam", de.AddDays(-7).AddMinutes(1), Evento.DuracaoMaximaMinutos);

        var lista = (await ExecutarAsync(_diana, new ListarEventos(de, ate))).Valor!;

        lista.Select(e => e.Id).Should().Equal(seteDiasQueTocam, contem, terminaDentro, dentro, comecaDentro);
        lista.Select(e => e.Titulo).Should().Equal(
            "Sete dias que tocam", "Contém", "Começa antes", "Dentro", "Começa dentro");
        lista.Single(e => e.Id == dentro).FimEm.Should().Be(de.AddHours(11));
        lista.Single(e => e.Id == dentro).Participantes.Should().Equal("Diana Diretora");
    }

    [Fact]
    public async Task Producao_so_lista_e_abre_os_seus_no_banco()
    {
        var dePedro = await CriarAsync(_diana, "De Pedro", Inicio, 60, _pedro);
        var daEster = await CriarAsync(_diana, "Da Ester", Inicio.AddHours(2), 60, _ester);
        var criadoPorPedro = await CriarAsync(_pedro, "Criado por Pedro", Inicio.AddHours(4), 60);

        var lista = await ExecutarAsync(_pedro, new ListarEventos(DiaInicio, DiaInicio.AddDays(1)));
        lista.Valor!.Select(e => e.Id).Should().Equal(dePedro, criadoPorPedro);
        (await ExecutarAsync(_pedro, new ListarEventos(DiaInicio, DiaInicio.AddDays(1), ParticipanteId: _ester)))
            .Valor!.Should().BeEmpty();
        (await ExecutarAsync(_pedro, new ListarEventos(DiaInicio, DiaInicio.AddDays(1), ParticipanteId: _diana)))
            .Valor!.Select(e => e.Id).Should().Equal(dePedro);

        var negado = await ExecutarAsync(_pedro, new ObterEvento(daEster));
        negado.Erro.Should().Be(TipoErro.NaoEncontrado);
        negado.Mensagem.Should().Be("Evento não encontrado.");
        (await ExecutarAsync(_pedro, new ObterEvento(dePedro))).Sucesso.Should().BeTrue();

        var eventos = (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado).ToList();
        var evento = eventos.Should().ContainSingle().Subject;
        evento.Entidade.Should().Be("Evento");
        evento.EntidadeId.Should().Be(daEster);
        evento.UsuarioId.Should().Be(_pedro);

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso
            .AsNoTracking().Where(a => a.TipoRecurso == "Evento").ToListAsync());
        acessos.Should().NotContain(a => a.Acao == AcaoDeAcesso.Visualizou && a.RecursoId == daEster);
        acessos.Should().Contain(a => a.Acao == AcaoDeAcesso.Visualizou && a.RecursoId == dePedro && a.UsuarioId == _pedro);

        (await ExecutarAsync(_diana, new ListarEventos(DiaInicio, DiaInicio.AddDays(1)))).Valor!.Select(e => e.Id)
            .Should().Equal(dePedro, daEster, criadoPorPedro);
    }

    [Fact]
    public async Task Sobreposicao_e_calculada_no_banco()
    {
        await CriarAsync(_diana, "Reunião da Paula", Inicio, 60, _paula);
        await CriarAsync(_diana, "Base da Ester", Inicio.AddHours(20), 60, _ester);

        // Conflito parcial: aviso com a Paula e o intervalo do outro evento, sem o título.
        var parcial = await SalvarAsync(_pedro, "Outro", Inicio.AddMinutes(30), 60, participantes: [_paula]);
        parcial.Sucesso.Should().BeTrue(parcial.Mensagem);
        parcial.Valor!.Sobreposicoes.Should().Equal(new SobreposicaoDeAgenda(_paula, "Paula", Inicio, Inicio.AddHours(1)));

        // Encostados, antes e depois: sem aviso.
        (await SalvarAsync(_pedro, "Depois", Inicio.AddHours(21), 60, participantes: [_ester])).Valor!.Sobreposicoes.Should().BeEmpty();
        (await SalvarAsync(_pedro, "Antes", Inicio.AddHours(19), 60, participantes: [_ester])).Valor!.Sobreposicoes.Should().BeEmpty();

        // Quem não participa não gera aviso.
        (await SalvarAsync(_ester, "Sem a Paula", Inicio, 60)).Valor!.Sobreposicoes.Should().BeEmpty();

        // Evento excluído não conta.
        var apagado = await CriarAsync(_diana, "Apagado", Inicio.AddHours(5), 60, _paula);
        (await ExecutarAsync(_diana, new ExcluirEvento(apagado))).Sucesso.Should().BeTrue();
        (await SalvarAsync(_pedro, "No horário do apagado", Inicio.AddHours(5), 60, participantes: [_paula]))
            .Valor!.Sobreposicoes.Should().BeEmpty();

        // Participante retirado não conta.
        var retirada = await CriarAsync(_diana, "Sem a Paula depois", Inicio.AddHours(8), 60, _paula);
        (await SalvarAsync(_diana, "Sem a Paula depois", Inicio.AddHours(8), 60, retirada, participantes: [_diana])).Sucesso.Should().BeTrue();
        (await SalvarAsync(_pedro, "No horário da retirada", Inicio.AddHours(8), 60, participantes: [_paula]))
            .Valor!.Sobreposicoes.Should().BeEmpty();

        // Na edição, o próprio evento não conflita consigo (mesmo mudando de horário para dentro do antigo).
        var paraEditar = await CriarAsync(_diana, "Para editar", Inicio.AddHours(12), 60, _paula);
        var editado = await SalvarAsync(_diana, "Para editar", Inicio.AddHours(12).AddMinutes(10), 60, paraEditar, participantes: [_diana, _paula]);
        editado.Sucesso.Should().BeTrue(editado.Mensagem);
        editado.Valor!.Sobreposicoes.Should().BeEmpty();
    }

    [Fact]
    public async Task Trocar_participantes_e_logico_e_auditado()
    {
        var id = await CriarAsync(_diana, "Reunião", Inicio, 60, _pedro, _paula);
        var antes = await ParticipantesNoBancoAsync(id);
        var daPaula = antes.Single(p => p.UsuarioId == _paula);

        var trocado = await SalvarAsync(_diana, "Reunião", Inicio, 60, id, participantes: [_diana, _pedro, _ester]);
        trocado.Sucesso.Should().BeTrue(trocado.Mensagem);

        var entradas = await ParticipantesNoBancoAsync(id);
        entradas.Should().HaveCount(4);
        entradas.Single(p => p.Id == daPaula.Id).ExcluidoEm.Should().NotBeNull();
        entradas.Where(p => p.UsuarioId != _paula).Should().OnlyContain(p => p.ExcluidoEm == null);
        var daEster = entradas.Single(p => p.UsuarioId == _ester);

        var auditoria = await AuditoriaAsync();
        auditoria.Should().ContainSingle(r => r.Entidade == "EventoParticipante" && r.EntidadeId == daPaula.Id
            && r.Operacao == OperacaoAuditada.Exclusao);
        auditoria.Should().ContainSingle(r => r.Entidade == "EventoParticipante" && r.EntidadeId == daEster.Id
            && r.Operacao == OperacaoAuditada.Criacao);
        auditoria.Where(r => r.Entidade == "EventoParticipante" && r.EntidadeId == daPaula.Id)
            .Select(r => r.Operacao).Should().BeEquivalentTo([OperacaoAuditada.Criacao, OperacaoAuditada.Exclusao]);

        // Quem foi retirado pode voltar: nova entrada, a antiga continua excluída.
        (await SalvarAsync(_diana, "Reunião", Inicio, 60, id, participantes: [_diana, _pedro, _ester, _paula]))
            .Sucesso.Should().BeTrue();
        var daPaulaDeNovo = (await ParticipantesNoBancoAsync(id)).Where(p => p.UsuarioId == _paula).ToList();
        daPaulaDeNovo.Should().HaveCount(2);
        daPaulaDeNovo.Count(p => p.ExcluidoEm == null).Should().Be(1);
        (await AuditoriaAsync()).Count(r => r.Entidade == "EventoParticipante" && r.Operacao == OperacaoAuditada.Criacao
            && daPaulaDeNovo.Select(p => p.Id).Contains(r.EntidadeId!.Value)).Should().Be(2);
    }

    [Fact]
    public async Task Duas_edicoes_simultaneas_a_segunda_recebe_conflito()
    {
        var id = await CriarAsync(_diana, "Reunião", Inicio, 60, _pedro);

        // Dois escopos vivos ao mesmo tempo: o primeiro lê, o segundo lê e grava, o primeiro grava por último.
        await EmEscopoAsync(_diana, async spPrimeiro =>
        {
            var evento = (await spPrimeiro.GetRequiredService<IEventoRepositorio>().ObterPorIdAsync(id, default))!;

            var outra = await SalvarAsync(_diana, "Reunião remarcada", Inicio, 60, id, participantes: [_diana, _pedro]);
            outra.Sucesso.Should().BeTrue(outra.Mensagem);

            // A edição do primeiro mexe só nos participantes: a linha do evento tem de ser gravada mesmo assim.
            evento.DefinirParticipantes([new PessoaDoEvento(_diana, true), new PessoaDoEvento(_paula, true)]);
            var salvar = () => spPrimeiro.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            await salvar.Should().ThrowAsync<ExcecaoDeConcorrencia>();
            return 0;
        });

        (await EventoNoBancoAsync(id)).Titulo.Should().Be("Reunião remarcada");
        (await ParticipantesNoBancoAsync(id)).Where(p => p.ExcluidoEm == null).Select(p => p.UsuarioId)
            .Should().BeEquivalentTo([_diana, _pedro]);

        // Refeita pelo caso de uso, com a versão atual, a troca passa.
        var refeita = await SalvarAsync(
            _diana, "Reunião remarcada", Inicio, 60, id, participantes: [_diana, _paula], versao: await VersaoAsync(id));
        refeita.Sucesso.Should().BeTrue(refeita.Mensagem);
    }

    [Fact]
    public async Task Formulario_desatualizado_e_recusado()
    {
        var id = await CriarAsync(_diana, "Reunião", Inicio, 60, _pedro);
        var velha = await VersaoAsync(id);
        (await SalvarAsync(_diana, "Alterada por outra pessoa", Inicio, 60, id, participantes: [_diana, _pedro])).Sucesso.Should().BeTrue();
        var atual = await VersaoAsync(id);
        atual.Should().NotBe(velha);
        var antes = await EventoNoBancoAsync(id);

        var editar = await SalvarAsync(_diana, "Título velho", Inicio, 60, id, participantes: [_diana, _pedro], versao: velha);
        var excluir = await ExecutarAsync(_diana, new ExcluirEvento(id, velha));

        editar.Erro.Should().Be(TipoErro.Conflito);
        editar.Mensagem.Should().Be(Alterado);
        excluir.Erro.Should().Be(TipoErro.Conflito);
        excluir.Mensagem.Should().Be(Alterado);
        var depois = await EventoNoBancoAsync(id);
        depois.Titulo.Should().Be("Alterada por outra pessoa");
        depois.ExcluidoEm.Should().BeNull();
        depois.Versao.Should().Be(antes.Versao);

        // Com a versão atual, as duas ações passam.
        var certa = await SalvarAsync(_diana, "Título novo", Inicio, 60, id, participantes: [_diana, _pedro], versao: atual);
        certa.Sucesso.Should().BeTrue(certa.Mensagem);
        (await ExecutarAsync(_diana, new ExcluirEvento(id, await VersaoAsync(id)))).Sucesso.Should().BeTrue();
        (await EventoNoBancoAsync(id)).ExcluidoEm.Should().NotBeNull();
    }

    [Fact]
    public async Task Excluir_e_logico_para_o_evento_e_os_participantes()
    {
        var id = await CriarAsync(_diana, "Reunião", Inicio, 60, _pedro, _paula);

        (await ExecutarAsync(_diana, new ExcluirEvento(id))).Sucesso.Should().BeTrue();

        (await EventoNoBancoAsync(id)).ExcluidoEm.Should().NotBeNull();
        var participantes = await ParticipantesNoBancoAsync(id);
        participantes.Should().HaveCount(3).And.OnlyContain(p => p.ExcluidoEm != null);
        (await ExecutarAsync(_diana, new ListarEventos(DiaInicio, DiaInicio.AddDays(1)))).Valor!.Should().BeEmpty();
        (await ExecutarAsync(_diana, new ObterEvento(id))).Erro.Should().Be(TipoErro.NaoEncontrado);

        var auditoria = await AuditoriaAsync();
        auditoria.Should().ContainSingle(r => r.Entidade == "Evento" && r.EntidadeId == id && r.Operacao == OperacaoAuditada.Exclusao);
        foreach (var participante in participantes)
            auditoria.Should().ContainSingle(r => r.Entidade == "EventoParticipante" && r.EntidadeId == participante.Id
                && r.Operacao == OperacaoAuditada.Exclusao);
    }

    [Fact]
    public async Task Banco_recusa_apagar_fisicamente_evento_com_participantes()
    {
        var id = await CriarAsync(_diana, "Reunião", Inicio, 60, _pedro);

        var apagar = () => EmEscopoAsync(null, async sp =>
        {
            await sp.GetRequiredService<AppDbContext>().Database
                .ExecuteSqlInterpolatedAsync($"DELETE FROM eventos WHERE id = {id}");
            return 0;
        });

        var erro = await apagar.Should().ThrowAsync<Exception>();
        PostgresDe(erro.Which).SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        (await ExecutarAsync(_diana, new ObterEvento(id))).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Banco_recusa_participante_repetido_vivo()
    {
        var id = await CriarAsync(_diana, "Reunião", Inicio, 60, _pedro);

        var duplicar = () => EmEscopoAsync(null, async sp =>
        {
            await sp.GetRequiredService<AppDbContext>().Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO eventos_participantes (id, evento_id, usuario_id, criado_em) VALUES ({Guid.CreateVersion7()}, {id}, {_pedro}, now())");
            return 0;
        });

        var erro = await duplicar.Should().ThrowAsync<Exception>();
        var pg = PostgresDe(erro.Which);
        pg.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        pg.ConstraintName.Should().Be("ix_eventos_participantes_evento_id_usuario_id");
        (await ParticipantesNoBancoAsync(id)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Lista_de_usuarios_para_a_agenda_traz_so_os_ativos()
    {
        (await ExecutarAsync(_diana, new DefinirAtivoUsuario(_ester, false))).Sucesso.Should().BeTrue();

        var comoProducao = (await ExecutarAsync(_pedro, new ListarUsuariosParaAgenda())).Valor!;

        comoProducao.Select(u => u.Nome).Should().Equal("Davi Diretor", "Diana Diretora", "Paula", "Pedro");
        comoProducao.Select(u => u.Id).Should().BeEquivalentTo([_davi, _diana, _paula, _pedro]);
    }

    [Fact]
    public async Task Acessos_ficam_no_registro_de_consultas()
    {
        var id = await CriarAsync(_diana, "Reunião", Inicio, 60, _pedro);

        (await ExecutarAsync(_diana, new ListarEventos(DiaInicio, DiaInicio.AddDays(1), Tipo: TipoEvento.Audiencia)))
            .Sucesso.Should().BeTrue();
        (await ExecutarAsync(_diana, new ObterEvento(id))).Sucesso.Should().BeTrue();

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso
            .AsNoTracking().Where(a => a.UsuarioId == _diana && a.TipoRecurso == "Evento").ToListAsync());
        acessos.Should().Contain(a => a.Acao == AcaoDeAcesso.Listou && a.RecursoId == null && a.Detalhe!.Contains("Tipo"));
        acessos.Should().Contain(a => a.Acao == AcaoDeAcesso.Visualizou && a.RecursoId == id);
    }

    // ---- Onda final ----

    [Fact]
    public async Task Troca_so_dos_participantes_deixa_registro_no_proprio_evento_na_auditoria()
    {
        var id = await CriarAsync(_pedro, "Só participantes", Inicio, 60, _paula);

        var edicao = await SalvarAsync(_pedro, "Só participantes", Inicio, 60, id, participantes: [_pedro, _ester]);
        edicao.Sucesso.Should().BeTrue(edicao.Mensagem);

        var consulta = await ExecutarAsync(_diana, new ConsultarAuditoria(new FiltroDeAuditoria(EntidadeId: id)));
        consulta.Sucesso.Should().BeTrue(consulta.Mensagem);
        var anotado = consulta.Valor!.Itens.Single(r => r.Evento == "evento_participantes_alterados");
        anotado.Entidade.Should().Be("Evento");
        anotado.EntidadeId.Should().Be(id);
        anotado.Operacao.Should().Be(OperacaoAuditada.Evento);
        using var dados = System.Text.Json.JsonDocument.Parse(anotado.Dados!);
        dados.RootElement.GetProperty("adicionados").EnumerateArray().Select(x => x.GetGuid()).Should().Equal(_ester);
        dados.RootElement.GetProperty("removidos").EnumerateArray().Select(x => x.GetGuid()).Should().Equal(_paula);

        // Sem mudança nos participantes, nenhuma marca nova.
        (await SalvarAsync(_pedro, "Só participantes", Inicio, 60, id, participantes: [_pedro, _ester])).Sucesso.Should().BeTrue();
        var depois = await ExecutarAsync(_diana, new ConsultarAuditoria(new FiltroDeAuditoria(EntidadeId: id)));
        depois.Valor!.Itens.Count(r => r.Evento == "evento_participantes_alterados").Should().Be(1);
    }

    [Fact]
    public async Task Visibilidade_na_listagem_criador_retirado_ainda_ve_participante_retirado_nao_e_estranho_nunca()
    {
        // (a) Quem criou continua vendo mesmo retirado dos participantes.
        var criadoPorPedro = await CriarAsync(_pedro, "Criado pelo Pedro", Inicio, 60, _paula);
        (await SalvarAsync(_diana, "Criado pelo Pedro", Inicio, 60, criadoPorPedro, participantes: [_paula])).Sucesso.Should().BeTrue();

        // (b) Participante retirado deixa de ver.
        var daDiana = await CriarAsync(_diana, "Da Diana", Inicio.AddHours(2), 60, _paula);
        (await ExecutarAsync(_paula, new ListarEventos(DiaInicio, DiaInicio.AddDays(1)))).Valor!.Select(e => e.Id)
            .Should().Contain(daDiana);
        (await SalvarAsync(_diana, "Da Diana", Inicio.AddHours(2), 60, daDiana, participantes: [_diana])).Sucesso.Should().BeTrue();

        var doPedro = (await ExecutarAsync(_pedro, new ListarEventos(DiaInicio, DiaInicio.AddDays(1)))).Valor!.Select(e => e.Id).ToList();
        var daPaula = (await ExecutarAsync(_paula, new ListarEventos(DiaInicio, DiaInicio.AddDays(1)))).Valor!.Select(e => e.Id).ToList();
        var daEster = (await ExecutarAsync(_ester, new ListarEventos(DiaInicio, DiaInicio.AddDays(1)))).Valor!.Select(e => e.Id).ToList();

        doPedro.Should().Contain(criadoPorPedro);
        daPaula.Should().Contain(criadoPorPedro).And.NotContain(daDiana);
        // (c) Quem nem criou nem participa não lista nenhum dos dois.
        daEster.Should().NotContain([criadoPorPedro, daDiana]);
    }

    [Fact]
    public async Task Evento_longo_que_contem_o_horario_do_novo_evento_gera_aviso()
    {
        // 3 dias, começando um dia antes do dia do novo evento; o novo cai bem no meio.
        await CriarAsync(_diana, "Três dias", Inicio.AddDays(-1), 3 * 24 * 60, _paula);

        var novo = await SalvarAsync(_pedro, "Uma hora no meio", Inicio, 60, participantes: [_paula]);

        novo.Sucesso.Should().BeTrue(novo.Mensagem);
        novo.Valor!.Sobreposicoes.Should().ContainSingle().Which.UsuarioId.Should().Be(_paula);
    }

    [Fact]
    public async Task Eventos_que_comecam_juntos_saem_em_ordem_de_id_no_banco()
    {
        var a = await CriarAsync(_diana, "A", Inicio, 60);
        var b = await CriarAsync(_diana, "B", Inicio, 60);
        var c = await CriarAsync(_diana, "C", Inicio, 60);

        var lista = (await ExecutarAsync(_diana, new ListarEventos(DiaInicio, DiaInicio.AddDays(1)))).Valor!;

        lista.Select(e => e.Id).Should().Equal(new[] { a, b, c }.Order());
    }
}
