using System.Text.Json;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Comum;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadesTests(BancoFixture banco) : TesteComBanco(banco)
{
    private static readonly DateOnly Entrega = new(2090, 6, 15);

    private Guid _diana, _davi, _pedro, _paula, _ester, _maria, _trabalhista, _processo;

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
    }

    private async Task<Guid> NovoUsuarioAsync(string nome, string email, TipoPerfil tipo)
    {
        var perfil = await IdDoPerfilAsync(tipo);
        var criado = await ExecutarAsync(_diana, new CriarUsuario(nome, email, "senha-do-" + nome[..3].ToLowerInvariant(), perfil, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    private async Task<Guid> CriarAsync(
        string titulo = "Elaborar contestação", DateOnly? entrega = null, Guid? cliente = null, Guid? processo = null)
    {
        var criada = await ExecutarAsync(_diana, new SalvarAtividade(
            null, titulo, "Prazo de 15 dias.", entrega ?? Entrega, cliente, processo, null));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        return criada.Valor;
    }

    private async Task DirecionarAsync(Guid id, Guid revisor, params (Guid Usuario, string? Direcionamento)[] responsaveis)
    {
        var definido = await ExecutarAsync(_diana, new DefinirResponsaveisDaAtividade(
            id, [.. responsaveis.Select(r => new ResponsavelEntrada(r.Usuario, r.Direcionamento))], revisor));
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
    }

    private async Task<Guid> DirecionadaAsync(string titulo = "Elaborar contestação")
    {
        var id = await CriarAsync(titulo);
        await DirecionarAsync(id, _diana, (_pedro, "Redigir"), (_paula, "Juntar documentos"));
        return id;
    }

    private async Task ConcluirAsync(Guid quem, Guid id) =>
        (await ExecutarAsync(quem, new ConcluirMinhaParte(id))).Sucesso.Should().BeTrue();

    private Task<Atividade> AtividadeNoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Atividades.IgnoreQueryFilters().AsNoTracking().SingleAsync(a => a.Id == id));

    private Task<List<AtividadeResponsavel>> ResponsaveisNoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Set<AtividadeResponsavel>().IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.AtividadeId == id).OrderBy(r => r.CriadoEm).ThenBy(r => r.Id).ToListAsync());

    private Task<List<AtividadeHistoricoStatus>> HistoricoNoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Set<AtividadeHistoricoStatus>().IgnoreQueryFilters().AsNoTracking()
            .Where(h => h.AtividadeId == id).OrderBy(h => h.Ordem).ToListAsync());

    private static PostgresException PostgresDe(Exception erro)
    {
        for (var e = erro; e is not null; e = e.InnerException)
            if (e is PostgresException pg) return pg;
        throw new InvalidOperationException("Nenhuma PostgresException na cadeia de exceções: " + erro);
    }

    [Fact]
    public async Task Atividade_vai_e_volta_do_banco_inteira_com_responsaveis_e_historico()
    {
        var id = await CriarAsync(cliente: _maria, processo: _processo);
        await DirecionarAsync(id, _diana, (_pedro, "Redigir"), (_paula, "Juntar documentos"));

        var detalhe = (await ExecutarAsync(_diana, new ObterAtividade(id))).Valor!;

        detalhe.Should().BeEquivalentTo(new
        {
            Id = id, Titulo = "Elaborar contestação", Descricao = "Prazo de 15 dias.", DataEntrega = (DateOnly?)Entrega,
            Status = StatusAtividade.Direcionada, Atrasada = false, ClienteId = (Guid?)_maria, ClienteNome = "Maria Silva",
            ProcessoId = (Guid?)_processo, ProcessoTitulo = "Reclamação trabalhista", AreaAtuacaoId = (Guid?)_trabalhista,
            AreaNome = "Trabalhista", TipoAutor = TipoAutor.Usuario, AutorNome = "Diana Diretora",
            RevisorId = (Guid?)_diana, RevisorNome = "Diana Diretora",
        });
        detalhe.ConcluidaEm.Should().BeNull();
        detalhe.Responsaveis.Should().BeEquivalentTo(
        [
            new ResponsavelDetalhe(_paula, "Paula", true, "Juntar documentos", null),
            new ResponsavelDetalhe(_pedro, "Pedro", true, "Redigir", null),
        ], opcoes => opcoes.WithStrictOrdering());
        detalhe.Historico.Select(h => (h.StatusAnterior, h.StatusNovo, h.UsuarioNome)).Should().Equal(
            ((StatusAtividade?)null, StatusAtividade.Criada, "Diana Diretora"),
            (StatusAtividade.Criada, StatusAtividade.Direcionada, "Diana Diretora"));
    }

    [Fact]
    public async Task Fluxo_completo_fica_no_historico_e_na_auditoria()
    {
        var id = await DirecionadaAsync();
        await ConcluirAsync(_pedro, id);
        await ConcluirAsync(_paula, id);
        (await ExecutarAsync(_diana, new DevolverAtividade(id, "Faltou a procuração", [_paula]))).Sucesso.Should().BeTrue();
        await ConcluirAsync(_paula, id);
        (await ExecutarAsync(_diana, new AprovarAtividade(id))).Sucesso.Should().BeTrue();
        var reaberta = await ExecutarAsync(_davi, new ReabrirAtividade(id, "Cliente pediu ajuste", [_pedro]));
        reaberta.Sucesso.Should().BeTrue(reaberta.Mensagem);

        var historico = await HistoricoNoBancoAsync(id);
        historico.Select(h => h.StatusNovo).Should().Equal(
            StatusAtividade.Criada, StatusAtividade.Direcionada, StatusAtividade.Feita, StatusAtividade.ParaCorrecao,
            StatusAtividade.Corrigida, StatusAtividade.Concluida, StatusAtividade.Reaberta);
        historico.Select(h => h.Motivo).Should().Equal(
            null, null, null, "Faltou a procuração", null, null, "Cliente pediu ajuste");
        historico.Select(h => h.UsuarioId).Should().Equal(
            _diana, _diana, _paula, _diana, _paula, _diana, _davi);
        (await AtividadeNoBancoAsync(id)).ConcluidaEm.Should().BeNull();

        var auditoria = await AuditoriaAsync();
        auditoria.Count(r => r.Entidade == "Atividade" && r.Operacao == OperacaoAuditada.Criacao).Should().Be(1);
        var alteracoes = auditoria.Where(r => r.Entidade == "Atividade" && r.EntidadeId == id
            && r.Operacao == OperacaoAuditada.Alteracao).ToList();
        alteracoes.Should().NotBeEmpty();
        alteracoes.Should().OnlyContain(r => Tem(r, "Status") || Tem(r, "ConcluidaEm"));
        var status = alteracoes.Where(r => Tem(r, "Status"))
            .Select(r => JsonDocument.Parse(r.Dados!).RootElement.GetProperty("Status")).ToList();
        status.Should().OnlyContain(s => Tem(s, "de") && Tem(s, "para"));
        status.Should().HaveCount(6);
        auditoria.Count(r => r.Entidade == "AtividadeHistoricoStatus" && r.Operacao == OperacaoAuditada.Criacao).Should().Be(7);
        auditoria.Should().Contain(r => r.Entidade == "AtividadeResponsavel" && r.Operacao == OperacaoAuditada.Alteracao
            && Tem(r, "ConcluidoEm"));
    }

    private static bool Tem(RegistroAuditoria registro, string propriedade) =>
        JsonDocument.Parse(registro.Dados!).RootElement.TryGetProperty(propriedade, out _);

    private static bool Tem(JsonElement elemento, string propriedade) => elemento.TryGetProperty(propriedade, out _);

    [Fact]
    public async Task Concluir_so_a_parte_carimba_a_atividade_sem_gerar_alteracao_dela()
    {
        var id = await DirecionadaAsync();
        var antes = await AtividadeNoBancoAsync(id);
        var alteracoesAntes = (await AuditoriaAsync()).Count(r => r.Entidade == "Atividade" && r.Operacao == OperacaoAuditada.Alteracao);

        await ConcluirAsync(_pedro, id);

        var depois = await AtividadeNoBancoAsync(id);
        depois.Status.Should().Be(StatusAtividade.Direcionada);
        depois.AtualizadoEm.Should().NotBeNull();
        depois.AtualizadoPor.Should().Be(_pedro);
        depois.VersaoDasPartes.Should().BeGreaterThan(antes.VersaoDasPartes);
        var auditoria = await AuditoriaAsync();
        auditoria.Count(r => r.Entidade == "Atividade" && r.Operacao == OperacaoAuditada.Alteracao).Should().Be(alteracoesAntes);
        auditoria.Should().Contain(r => r.Entidade == "AtividadeResponsavel" && r.Operacao == OperacaoAuditada.Alteracao);
    }

    [Fact]
    public async Task Duas_conclusoes_simultaneas_nao_deixam_a_atividade_presa()
    {
        var id = await DirecionadaAsync();

        // Dois escopos vivos ao mesmo tempo: o de Pedro lê primeiro, o de Paula lê e grava, o de Pedro grava por último.
        await EmEscopoAsync(_pedro, async spPedro =>
        {
            var atividade = (await spPedro.GetRequiredService<IAtividadeRepositorio>().ObterPorIdAsync(id, default))!;

            await ConcluirAsync(_paula, id);

            atividade.ConcluirParte(new PessoaDaAtividade(_pedro, TipoPerfil.Producao, true), DateTime.UtcNow);
            var salvar = () => spPedro.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            await salvar.Should().ThrowAsync<ExcecaoDeConcorrencia>();
            return 0;
        });

        (await AtividadeNoBancoAsync(id)).Status.Should().Be(StatusAtividade.Direcionada);

        // A conclusão de Pedro, refeita pelo caso de uso, passa e fecha a rodada.
        (await ExecutarAsync(_pedro, new ConcluirMinhaParte(id))).Sucesso.Should().BeTrue();
        (await AtividadeNoBancoAsync(id)).Status.Should().Be(StatusAtividade.Feita);
        (await ResponsaveisNoBancoAsync(id)).Should().OnlyContain(r => r.ConcluidoEm != null);
        (await HistoricoNoBancoAsync(id)).Select(h => h.StatusNovo).Should().Equal(
            StatusAtividade.Criada, StatusAtividade.Direcionada, StatusAtividade.Feita);
    }

    [Fact]
    public async Task Producao_so_lista_e_abre_as_atividades_em_que_e_responsavel()
    {
        var dosDois = await DirecionadaAsync("De Pedro e Paula");
        var daEster = await CriarAsync("Da Ester");
        await DirecionarAsync(daEster, _diana, (_ester, null));
        var semNinguem = await CriarAsync("Sem responsável");

        var dePedro = (await ExecutarAsync(_pedro, new ListarAtividades(ResponsavelId: _ester))).Valor!;
        dePedro.Itens.Select(a => a.Id).Should().Equal(dosDois);
        dePedro.Total.Should().Be(1);

        var negado = await ExecutarAsync(_pedro, new ObterAtividade(daEster));
        negado.Erro.Should().Be(TipoErro.NaoEncontrado);
        negado.Mensagem.Should().Be("Atividade não encontrada.");

        var eventos = (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado).ToList();
        var evento = eventos.Should().ContainSingle().Subject;
        evento.Entidade.Should().Be("Atividade");
        evento.EntidadeId.Should().Be(daEster);
        evento.UsuarioId.Should().Be(_pedro);

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso
            .AsNoTracking().Where(a => a.TipoRecurso == "Atividade").ToListAsync());
        acessos.Should().NotContain(a => a.Acao == AcaoDeAcesso.Visualizou && a.RecursoId == daEster);

        (await ExecutarAsync(_diana, new ListarAtividades())).Valor!.Itens.Select(a => a.Id)
            .Should().BeEquivalentTo([dosDois, daEster, semNinguem]);
    }

    [Fact]
    public async Task Responsavel_ve_so_o_proprio_direcionamento()
    {
        var id = await DirecionadaAsync();

        var detalhe = (await ExecutarAsync(_pedro, new ObterAtividade(id))).Valor!;

        detalhe.Responsaveis.Should().BeEquivalentTo(
        [
            new ResponsavelDetalhe(_paula, "Paula", false, null, null),
            new ResponsavelDetalhe(_pedro, "Pedro", true, "Redigir", null),
        ], opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public async Task Listagem_ordena_filtra_e_pagina_no_banco()
    {
        // 20 atividades no futuro distante e uma no passado distante, todas com data; mais uma sem data.
        var atrasada = await CriarAsync("Contestação atrasada", new DateOnly(2000, 1, 1));
        var datadas = new List<Guid>();
        for (var i = 1; i <= 20; i++)
            datadas.Add(await CriarAsync($"Tarefa {i:00}", new DateOnly(2090, 1, 1).AddDays(i),
                cliente: i == 1 ? _maria : null, processo: i == 1 ? _processo : null));
        var semData = (await ExecutarAsync(_diana, new SalvarAtividade(null, "Sem data", "Sem prazo.", null, null, null, null))).Valor;
        await DirecionarAsync(datadas[1], _diana, (_pedro, null));

        var primeira = (await ExecutarAsync(_diana, new ListarAtividades())).Valor!;
        primeira.Total.Should().Be(22);
        primeira.Tamanho.Should().Be(AtividadesOpcoes.TamanhoDaPagina).And.Be(20);
        primeira.Itens.Should().HaveCount(20);
        primeira.Itens.Select(a => a.Id).Should().Equal([atrasada, .. datadas.Take(19)]);
        primeira.Itens.Select(a => a.DataEntrega).Should().BeInAscendingOrder();

        var segunda = (await ExecutarAsync(_diana, new ListarAtividades(Pagina: 2))).Valor!;
        segunda.Itens.Select(a => a.Id).Should().Equal(datadas[19], semData);
        segunda.Itens[1].DataEntrega.Should().BeNull();

        async Task<IEnumerable<Guid>> IdsAsync(ListarAtividades consulta) =>
            (await ExecutarAsync(_diana, consulta)).Valor!.Itens.Select(a => a.Id);

        (await IdsAsync(new ListarAtividades(SomenteAtrasadas: true))).Should().Equal(atrasada);
        (await IdsAsync(new ListarAtividades(Busca: "contestacao"))).Should().Equal(atrasada);
        (await IdsAsync(new ListarAtividades(ClienteId: _maria))).Should().Equal(datadas[0]);
        (await IdsAsync(new ListarAtividades(ProcessoId: _processo))).Should().Equal(datadas[0]);
        (await IdsAsync(new ListarAtividades(Status: StatusAtividade.Direcionada))).Should().Equal(datadas[1]);
        (await IdsAsync(new ListarAtividades(ResponsavelId: _pedro))).Should().Equal(datadas[1]);
        (await ExecutarAsync(_diana, new ListarAtividades(Status: StatusAtividade.Criada))).Valor!.Total.Should().Be(21);

        var item = primeira.Itens.Single(a => a.Id == datadas[0]);
        item.Should().BeEquivalentTo(new { ClienteNome = "Maria Silva", ProcessoTitulo = "Reclamação trabalhista", AreaNome = "Trabalhista" });
        var direcionada = primeira.Itens.Single(a => a.Id == datadas[1]);
        direcionada.Responsaveis.Should().Equal("Pedro");
        direcionada.RevisorNome.Should().Be("Diana Diretora");
    }

    [Fact]
    public async Task Remover_responsavel_e_logico_e_o_evento_fica_na_mesma_transacao()
    {
        var id = await DirecionadaAsync();

        await DirecionarAsync(id, _diana, (_pedro, "Redigir"), (_ester, null));

        var entradas = await ResponsaveisNoBancoAsync(id);
        entradas.Should().HaveCount(3);
        var daPaula = entradas.Single(r => r.UsuarioId == _paula);
        daPaula.ExcluidoEm.Should().NotBeNull();
        entradas.Where(r => r.UsuarioId != _paula).Should().OnlyContain(r => r.ExcluidoEm == null);

        var auditoria = await AuditoriaAsync();
        var exclusao = auditoria.Single(r => r.Entidade == "AtividadeResponsavel" && r.Operacao == OperacaoAuditada.Exclusao);
        exclusao.EntidadeId.Should().Be(daPaula.Id);
        var daEster = entradas.Single(r => r.UsuarioId == _ester);
        auditoria.Should().ContainSingle(r => r.EntidadeId == daEster.Id && r.Operacao == OperacaoAuditada.Criacao);

        var evento = auditoria.Single(r => r.Evento == EventosDeAtividade.ResponsaveisAlterados
            && r.IdCorrelacao == exclusao.IdCorrelacao);
        evento.Entidade.Should().Be("Atividade");
        evento.EntidadeId.Should().Be(id);
        var dados = JsonDocument.Parse(evento.Dados!).RootElement;
        dados.GetProperty("adicionados").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_ester);
        dados.GetProperty("removidos").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_paula);
    }

    [Fact]
    public async Task Responsavel_removido_pode_voltar()
    {
        var id = await DirecionadaAsync();

        await DirecionarAsync(id, _diana, (_pedro, "Redigir"));
        await DirecionarAsync(id, _diana, (_pedro, "Redigir"), (_paula, "De volta"));

        var daPaula = (await ResponsaveisNoBancoAsync(id)).Where(r => r.UsuarioId == _paula).ToList();
        daPaula.Should().HaveCount(2);
        daPaula.Count(r => r.ExcluidoEm == null).Should().Be(1);
    }

    [Fact]
    public async Task Excluir_e_logico_para_a_atividade_os_responsaveis_e_o_historico()
    {
        var id = await DirecionadaAsync();

        (await ExecutarAsync(_diana, new ExcluirAtividade(id))).Sucesso.Should().BeTrue();

        (await AtividadeNoBancoAsync(id)).ExcluidoEm.Should().NotBeNull();
        (await ResponsaveisNoBancoAsync(id)).Should().HaveCount(2).And.OnlyContain(r => r.ExcluidoEm != null);
        (await HistoricoNoBancoAsync(id)).Should().HaveCount(2).And.OnlyContain(h => h.ExcluidoEm != null);
        (await ExecutarAsync(_diana, new ListarAtividades())).Valor!.Itens.Should().BeEmpty();
        (await ExecutarAsync(_diana, new ObterAtividade(id))).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    [Fact]
    public async Task Banco_recusa_apagar_fisicamente_uma_atividade_com_filhas()
    {
        var id = await DirecionadaAsync();

        var apagar = () => EmEscopoAsync(null, async sp =>
        {
            await sp.GetRequiredService<AppDbContext>().Database
                .ExecuteSqlInterpolatedAsync($"DELETE FROM atividades WHERE id = {id}");
            return 0;
        });

        var erro = await apagar.Should().ThrowAsync<Exception>();
        PostgresDe(erro.Which).SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        (await ExecutarAsync(_diana, new ObterAtividade(id))).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Banco_recusa_dois_responsaveis_vivos_iguais_na_mesma_atividade()
    {
        var id = await DirecionadaAsync();

        var duplicar = () => EmEscopoAsync(null, async sp =>
        {
            await sp.GetRequiredService<AppDbContext>().Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO atividades_responsaveis (id, atividade_id, usuario_id, criado_em) VALUES ({Guid.CreateVersion7()}, {id}, {_pedro}, now())");
            return 0;
        });

        var erro = await duplicar.Should().ThrowAsync<Exception>();
        var pg = PostgresDe(erro.Which);
        pg.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        pg.ConstraintName.Should().Be("ix_atividades_responsaveis_atividade_id_usuario_id");
        (await ResponsaveisNoBancoAsync(id)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Acessos_ficam_no_registro_de_consultas()
    {
        var id = await DirecionadaAsync();

        (await ExecutarAsync(_diana, new ListarAtividades(Busca: "contestação"))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_diana, new ObterAtividade(id))).Sucesso.Should().BeTrue();

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso
            .AsNoTracking().Where(a => a.UsuarioId == _diana && a.TipoRecurso == "Atividade").ToListAsync());
        acessos.Should().Contain(a => a.Acao == AcaoDeAcesso.Listou && a.RecursoId == null && a.Detalhe!.Contains("contestação"));
        acessos.Should().Contain(a => a.Acao == AcaoDeAcesso.Visualizou && a.RecursoId == id);
    }

    [Fact]
    public async Task Producao_nao_cria_nem_direciona_e_a_tentativa_fica_registrada()
    {
        var id = await DirecionadaAsync();

        var criar = await ExecutarAsync(_pedro, new SalvarAtividade(null, "Minha", "Texto.", Entrega, null, null, null));
        var direcionar = await ExecutarAsync(_pedro, new DefinirResponsaveisDaAtividade(
            id, [new ResponsavelEntrada(_pedro, "tudo comigo")], _diana));

        criar.Erro.Should().Be(TipoErro.AcessoNegado);
        direcionar.Erro.Should().Be(TipoErro.AcessoNegado);
        (await AuditoriaAsync()).Count(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == _pedro).Should().Be(2);
        (await ExecutarAsync(_diana, new ListarAtividades())).Valor!.Total.Should().Be(1);
    }

    [Fact]
    public async Task Calendario_do_escritorio_usa_o_fuso_configurado()
    {
        var fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        DateOnly Esperado() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fuso));

        var antes = Esperado();
        var hoje = await EmEscopoAsync(null, sp => Task.FromResult(sp.GetRequiredService<ICalendarioDoEscritorio>().Hoje));
        var depois = Esperado();

        hoje.Should().BeOneOf(antes, depois);

        var relogio = new RelogioDeTeste(new DateTime(2026, 10, 10, 2, 0, 0, DateTimeKind.Utc));
        new CalendarioDoEscritorio(fuso, relogio).Hoje.Should().Be(new DateOnly(2026, 10, 9));
    }

    // ---- Correções finais da fase 5A ----

    private async Task<uint> VersaoAsync(Guid id) => (await ExecutarAsync(_diana, new ObterAtividade(id))).Valor!.Versao;

    private static JsonElement DadosDe(RegistroAuditoria registro) => JsonDocument.Parse(registro.Dados!).RootElement;

    private Task<Resultado<Guid>> EditarAsync(Guid id, string titulo, DateOnly? entrega = null, uint? versao = null) =>
        ExecutarAsync(_diana, new SalvarAtividade(
            id, titulo, "Prazo de 15 dias.", entrega ?? Entrega, null, null, null, versao));

    [Fact]
    public async Task Editar_grava_no_banco_e_audita_so_o_que_mudou()
    {
        var id = await DirecionadaAsync();
        var antes = await AtividadeNoBancoAsync(id);
        var historicoAntes = await HistoricoNoBancoAsync(id);
        var novaEntrega = Entrega.AddDays(7);

        var editada = await EditarAsync(id, "Contestação revisada", novaEntrega, await VersaoAsync(id));

        editada.Sucesso.Should().BeTrue(editada.Mensagem);
        var depois = await AtividadeNoBancoAsync(id);
        depois.Titulo.Should().Be("Contestação revisada");
        depois.DataEntrega.Should().Be(novaEntrega);
        depois.Status.Should().Be(antes.Status);
        depois.AutorId.Should().Be(antes.AutorId);
        depois.TipoAutor.Should().Be(antes.TipoAutor);
        (await HistoricoNoBancoAsync(id)).Select(h => (h.Ordem, h.StatusNovo)).Should().Equal(
            historicoAntes.Select(h => (h.Ordem, h.StatusNovo)));

        var alteracao = (await AuditoriaAsync()).Where(r => r.Entidade == "Atividade" && r.EntidadeId == id
            && r.Operacao == OperacaoAuditada.Alteracao && Tem(r, "Titulo")).Should().ContainSingle().Subject;
        var dados = DadosDe(alteracao);
        dados.GetProperty("Titulo").GetProperty("de").GetString().Should().Be("Elaborar contestação");
        dados.GetProperty("Titulo").GetProperty("para").GetString().Should().Be("Contestação revisada");
        dados.TryGetProperty("DataEntrega", out var campoEntrega).Should().BeTrue();
        Tem(campoEntrega, "de").Should().BeTrue();
        Tem(campoEntrega, "para").Should().BeTrue();
        Tem(alteracao, "Status").Should().BeFalse();
        Tem(alteracao, "AutorId").Should().BeFalse();
        alteracao.UsuarioId.Should().Be(_diana);
    }

    [Fact]
    public async Task Trocar_o_revisor_grava_no_banco_e_audita_de_e_para()
    {
        var id = await DirecionadaAsync();

        var trocado = await ExecutarAsync(_diana, new TrocarRevisorDaAtividade(id, _davi, await VersaoAsync(id)));

        trocado.Sucesso.Should().BeTrue(trocado.Mensagem);
        (await AtividadeNoBancoAsync(id)).RevisorId.Should().Be(_davi);
        var troca = (await AuditoriaAsync()).Where(r => r.Entidade == "Atividade" && r.EntidadeId == id
                && r.Operacao == OperacaoAuditada.Alteracao && Tem(r, "RevisorId"))
            .Select(DadosDe).Single(d => d.GetProperty("RevisorId").GetProperty("para").GetGuid() == _davi);
        troca.GetProperty("RevisorId").GetProperty("de").GetGuid().Should().Be(_diana);
    }

    [Fact]
    public async Task Formulario_desatualizado_e_recusado_em_todas_as_acoes_e_nada_muda()
    {
        var id = await DirecionadaAsync();
        var velha = await VersaoAsync(id);
        (await EditarAsync(id, "Alterada por outra pessoa")).Sucesso.Should().BeTrue();
        var atual = await VersaoAsync(id);
        atual.Should().NotBe(velha);
        var antes = await AtividadeNoBancoAsync(id);
        var responsaveisAntes = await ResponsaveisNoBancoAsync(id);

        var definir = await ExecutarAsync(_diana, new DefinirResponsaveisDaAtividade(
            id, [new ResponsavelEntrada(_ester, null)], _diana, velha));
        var salvar = await EditarAsync(id, "Título velho", versao: velha);
        var revisor = await ExecutarAsync(_diana, new TrocarRevisorDaAtividade(id, _davi, velha));
        var excluir = await ExecutarAsync(_diana, new ExcluirAtividade(id, velha));

        (TipoErro? Erro, string? Mensagem)[] recusas =
            [(definir.Erro, definir.Mensagem), (salvar.Erro, salvar.Mensagem), (revisor.Erro, revisor.Mensagem), (excluir.Erro, excluir.Mensagem)];
        recusas.Should().OnlyContain(r => r.Erro == TipoErro.Conflito
            && r.Mensagem == "A atividade foi alterada por outra pessoa. Recarregue e tente novamente.");

        var depois = await AtividadeNoBancoAsync(id);
        depois.Titulo.Should().Be("Alterada por outra pessoa");
        depois.RevisorId.Should().Be(antes.RevisorId);
        depois.ExcluidoEm.Should().BeNull();
        depois.Versao.Should().Be(antes.Versao);
        (await ResponsaveisNoBancoAsync(id)).Select(r => (r.UsuarioId, r.ExcluidoEm)).Should().Equal(
            responsaveisAntes.Select(r => (r.UsuarioId, r.ExcluidoEm)));

        // Com a versão atual, a ação passa.
        var certa = await ExecutarAsync(_diana, new TrocarRevisorDaAtividade(id, _davi, await VersaoAsync(id)));
        certa.Sucesso.Should().BeTrue(certa.Mensagem);
        (await AtividadeNoBancoAsync(id)).RevisorId.Should().Be(_davi);
    }

    [Fact]
    public async Task Revisor_que_e_o_unico_responsavel_conclui_a_atividade_numa_so_gravacao()
    {
        var id = await CriarAsync();
        await DirecionarAsync(id, _diana, (_diana, "Eu mesma"));

        await ConcluirAsync(_diana, id);

        var atividade = await AtividadeNoBancoAsync(id);
        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().NotBeNull();
        var historico = await HistoricoNoBancoAsync(id);
        var ultimas = historico.TakeLast(2).ToList();
        ultimas.Select(h => h.StatusNovo).Should().Equal(StatusAtividade.Feita, StatusAtividade.Concluida);
        (ultimas[1].Ordem - ultimas[0].Ordem).Should().Be(1);
        ultimas[1].DataHora.Should().Be(ultimas[0].DataHora);
        ultimas.Should().OnlyContain(h => h.UsuarioId == _diana);
    }

    [Fact]
    public async Task Responsavel_removido_perde_o_acesso_na_hora()
    {
        var id = await DirecionadaAsync();
        (await ExecutarAsync(_paula, new ObterAtividade(id))).Sucesso.Should().BeTrue();

        await DirecionarAsync(id, _diana, (_pedro, "Redigir"));

        (await ExecutarAsync(_paula, new ListarAtividades())).Valor!.Itens.Should().BeEmpty();
        var obter = await ExecutarAsync(_paula, new ObterAtividade(id));
        var concluir = await ExecutarAsync(_paula, new ConcluirMinhaParte(id));
        obter.Erro.Should().Be(TipoErro.NaoEncontrado);
        concluir.Erro.Should().Be(TipoErro.NaoEncontrado);

        var negados = (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == _paula).ToList();
        negados.Should().HaveCount(2).And.OnlyContain(r => r.Entidade == "Atividade" && r.EntidadeId == id);
        negados.Select(r => DadosDe(r).GetProperty("requisicao").GetString()).Should()
            .BeEquivalentTo(nameof(ObterAtividade), nameof(ConcluirMinhaParte));
    }

    [Fact]
    public async Task Busca_trata_porcentagem_e_sublinhado_como_texto()
    {
        var cem = await CriarAsync("100% pronto");
        var ab = await CriarAsync("a_b");
        await CriarAsync("abc qualquer");

        (await ExecutarAsync(_diana, new ListarAtividades(Busca: "%"))).Valor!.Itens.Select(a => a.Id).Should().Equal(cem);
        (await ExecutarAsync(_diana, new ListarAtividades(Busca: "_"))).Valor!.Itens.Select(a => a.Id).Should().Equal(ab);
    }

    [Fact]
    public async Task Atraso_no_banco_so_vale_para_data_anterior_a_hoje_e_nao_concluida()
    {
        var hoje = await EmEscopoAsync(null, sp => Task.FromResult(sp.GetRequiredService<ICalendarioDoEscritorio>().Hoje));
        var vencendoHoje = await CriarAsync("Vence hoje", hoje);
        var vencidaOntem = await CriarAsync("Venceu ontem", hoje.AddDays(-1));
        var concluidaVencida = await CriarAsync("Concluída vencida", hoje.AddDays(-1));
        await DirecionarAsync(concluidaVencida, _diana, (_diana, null));
        await ConcluirAsync(_diana, concluidaVencida);

        var atrasadas = (await ExecutarAsync(_diana, new ListarAtividades(SomenteAtrasadas: true))).Valor!;
        var todas = (await ExecutarAsync(_diana, new ListarAtividades())).Valor!;

        atrasadas.Itens.Select(a => a.Id).Should().Equal(vencidaOntem);
        todas.Itens.Single(a => a.Id == vencendoHoje).Atrasada.Should().BeFalse();
        todas.Itens.Single(a => a.Id == vencidaOntem).Atrasada.Should().BeTrue();
        todas.Itens.Single(a => a.Id == concluidaVencida).Atrasada.Should().BeFalse();
        (await ExecutarAsync(_diana, new ObterAtividade(vencendoHoje))).Valor!.Atrasada.Should().BeFalse();
        (await ExecutarAsync(_diana, new ObterAtividade(vencidaOntem))).Valor!.Atrasada.Should().BeTrue();
    }

    [Fact]
    public async Task Mesma_data_de_entrega_lista_a_mais_recente_primeiro()
    {
        var primeira = await CriarAsync("Primeira");
        var segunda = await CriarAsync("Segunda");
        var terceira = await CriarAsync("Terceira");

        (await ExecutarAsync(_diana, new ListarAtividades())).Valor!.Itens.Select(a => a.Id)
            .Should().Equal(terceira, segunda, primeira);
    }

    [Fact]
    public async Task Excluir_registra_quem_excluiu_a_atividade_e_as_filhas()
    {
        var id = await DirecionadaAsync();

        (await ExecutarAsync(_diana, new ExcluirAtividade(id))).Sucesso.Should().BeTrue();

        var exclusoes = (await AuditoriaAsync()).Where(r => r.Operacao == OperacaoAuditada.Exclusao).ToList();
        exclusoes.Count(r => r.Entidade == "Atividade" && r.EntidadeId == id).Should().Be(1);
        exclusoes.Count(r => r.Entidade == "AtividadeResponsavel").Should().Be(2);
        exclusoes.Count(r => r.Entidade == "AtividadeHistoricoStatus").Should().Be(2);
        exclusoes.Should().OnlyContain(r => r.UsuarioId == _diana);
    }

    [Fact]
    public async Task Recusas_do_dominio_chegam_como_validacao()
    {
        var feita = await DirecionadaAsync("Já feita");
        await ConcluirAsync(_pedro, feita);
        await ConcluirAsync(_paula, feita);
        var comDiretoraUnica = await CriarAsync("Com diretora única");
        await DirecionarAsync(comDiretoraUnica, _diana, (_diana, null), (_pedro, null));

        var devolver = await ExecutarAsync(_davi, new DevolverAtividade(feita, "Ajustar", [_pedro]));
        var inexistente = await ExecutarAsync(_diana, new TrocarRevisorDaAtividade(feita, Guid.CreateVersion7()));
        var travado = await ExecutarAsync(_diana, new TrocarRevisorDaAtividade(comDiretoraUnica, _davi));

        devolver.Erro.Should().Be(TipoErro.Validacao);
        devolver.Mensagem.Should().Be(Atividade.SoORevisor);
        inexistente.Erro.Should().Be(TipoErro.Validacao);
        inexistente.Mensagem.Should().Be("Usuário não encontrado.");
        travado.Erro.Should().Be(TipoErro.Validacao);
        travado.Mensagem.Should().Be(Atividade.RevisorTravado);
        (await AtividadeNoBancoAsync(feita)).Status.Should().Be(StatusAtividade.Feita);
        (await AtividadeNoBancoAsync(comDiretoraUnica)).RevisorId.Should().Be(_diana);
    }

    [Fact]
    public async Task Remover_o_ultimo_pendente_grava_o_motivo_no_historico()
    {
        var id = await DirecionadaAsync();
        await ConcluirAsync(_pedro, id);

        await DirecionarAsync(id, _diana, (_pedro, "Redigir"));

        var historico = await HistoricoNoBancoAsync(id);
        historico.Last().StatusNovo.Should().Be(StatusAtividade.Feita);
        historico.Last().Motivo.Should().Be("Responsável pendente removido.");
        historico.Last().UsuarioId.Should().Be(_diana);
        historico.Take(historico.Count - 1).Should().OnlyContain(h => h.Motivo == null);
    }

    [Fact]
    public async Task Outro_diretor_removendo_o_ultimo_pendente_nao_conclui_pelo_revisor()
    {
        var id = await CriarAsync();
        await DirecionarAsync(id, _diana, (_diana, null), (_pedro, null));
        await ConcluirAsync(_diana, id);

        var definido = await ExecutarAsync(_davi, new DefinirResponsaveisDaAtividade(id, [new ResponsavelEntrada(_diana, null)], null));

        definido.Sucesso.Should().BeTrue(definido.Mensagem);
        (await AtividadeNoBancoAsync(id)).Status.Should().Be(StatusAtividade.Feita);
        var historico = await HistoricoNoBancoAsync(id);
        historico.Last().UsuarioId.Should().Be(_davi);
        historico.Last().Motivo.Should().Be("Responsável pendente removido.");
    }

    [Fact]
    public async Task Listagem_filtra_pelo_revisor_no_banco_e_producao_continua_so_com_as_suas()
    {
        var daDiana = await DirecionadaAsync("Revisada pela Diana");
        var doDavi = await CriarAsync("Revisada pelo Davi");
        await DirecionarAsync(doDavi, _davi, (_pedro, null));
        var deOutro = await CriarAsync("Da Ester");
        await DirecionarAsync(deOutro, _davi, (_ester, null));

        async Task<IEnumerable<Guid>> IdsAsync(Guid? ator, ListarAtividades consulta) =>
            (await ExecutarAsync(ator, consulta)).Valor!.Itens.Select(a => a.Id);

        (await IdsAsync(_diana, new ListarAtividades(RevisorId: _diana))).Should().Equal(daDiana);
        (await IdsAsync(_diana, new ListarAtividades(RevisorId: _davi))).Should().BeEquivalentTo([doDavi, deOutro]);
        (await IdsAsync(_diana, new ListarAtividades(RevisorId: _davi, ResponsavelId: _ester))).Should().Equal(deOutro);
        (await IdsAsync(_pedro, new ListarAtividades(RevisorId: _davi))).Should().Equal(doDavi);
        (await IdsAsync(_pedro, new ListarAtividades(RevisorId: _diana, ResponsavelId: _ester))).Should().Equal(daDiana);
    }

    private sealed class RelogioDeTeste(DateTime agora) : IRelogio
    {
        public DateTime AgoraUtc => agora;
    }
}
