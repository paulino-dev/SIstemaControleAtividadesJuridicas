using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class PendenciasEAuditoriaPorRegistroTests(BancoFixture banco) : TesteComBanco(banco)
{
    private static readonly DateOnly Entrega = new(2090, 6, 15);

    private Guid _diana, _pedro, _paula;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _diana = await IdDoAdminAsync();
        _pedro = await NovoUsuarioAsync("Pedro", "pedro@x.com", TipoPerfil.Producao);
        _paula = await NovoUsuarioAsync("Paula", "paula@x.com", TipoPerfil.Producao);
    }

    private async Task<Guid> NovoUsuarioAsync(string nome, string email, TipoPerfil tipo)
    {
        var perfil = await IdDoPerfilAsync(tipo);
        var criado = await ExecutarAsync(_diana, new CriarUsuario(nome, email, "senha-do-" + nome[..3].ToLowerInvariant(), perfil, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    private async Task<Guid> CriarAtividadeAsync(string titulo)
    {
        var criada = await ExecutarAsync(_diana, new SalvarAtividade(null, titulo, "Descrição.", Entrega, null, null, null));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        return criada.Valor;
    }

    private async Task DirecionarAsync(Guid id, Guid revisor, params Guid[] responsaveis)
    {
        var definido = await ExecutarAsync(_diana, new DefinirResponsaveisDaAtividade(
            id, [.. responsaveis.Select(r => new ResponsavelEntrada(r, "Fazer"))], revisor));
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
    }

    private async Task<Guid> EventoAsync(string titulo, DateTime inicio, int minutos, params Guid[] participantes)
    {
        var salvo = await ExecutarAsync(_diana, new SalvarEvento(
            null, titulo, null, null, TipoEvento.Reuniao, inicio, minutos, null, null, null, participantes, null));
        salvo.Sucesso.Should().BeTrue(salvo.Mensagem);
        return salvo.Valor!.Id;
    }

    [Fact]
    public async Task Pendencias_do_usuario_contam_partes_revisoes_e_eventos()
    {
        var agora = DateTime.UtcNow;

        // Parte pendente dele (conta: 1 parte).
        var pendente = await CriarAtividadeAsync("Pendente");
        await DirecionarAsync(pendente, _diana, _pedro);

        // Ele já concluiu a sua parte, mas Paula não: a atividade segue direcionada (não conta).
        var concluida = await CriarAtividadeAsync("Parte concluída");
        await DirecionarAsync(concluida, _diana, _pedro, _paula);
        (await ExecutarAsync(_pedro, new ConcluirMinhaParte(concluida))).Sucesso.Should().BeTrue();

        // Feita, aguardando revisão por ele (conta: 1 revisão). Revisor precisa ser diretor: usa Davi.
        var davi = await NovoUsuarioAsync("Davi Diretor", "davi@x.com", TipoPerfil.Diretoria);
        var feita = await CriarAtividadeAsync("Aguarda revisão");
        await DirecionarAsync(feita, davi, _paula);
        (await ExecutarAsync(_paula, new ConcluirMinhaParte(feita))).Sucesso.Should().BeTrue();

        // Concluída, com Davi como revisor (não conta).
        var aprovada = await CriarAtividadeAsync("Aprovada");
        await DirecionarAsync(aprovada, davi, _paula);
        (await ExecutarAsync(_paula, new ConcluirMinhaParte(aprovada))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(davi, new AprovarAtividade(aprovada))).Sucesso.Should().BeTrue();

        // Excluída, com parte pendente dele (não conta).
        var excluida = await CriarAtividadeAsync("Excluída");
        await DirecionarAsync(excluida, _diana, _pedro);
        (await ExecutarAsync(_diana, new ExcluirAtividade(excluida))).Sucesso.Should().BeTrue();

        // Eventos: futuro (conta), em andamento (conta), passado, futuro excluído, futuro de que foi retirado.
        await EventoAsync("Futuro", agora.AddDays(3), 60, _pedro);
        await EventoAsync("Em andamento", agora.AddMinutes(-30), 120, _pedro);
        await EventoAsync("Passado", agora.AddDays(-3), 60, _pedro);
        var apagado = await EventoAsync("Futuro apagado", agora.AddDays(4), 60, _pedro);
        (await ExecutarAsync(_diana, new ExcluirEvento(apagado))).Sucesso.Should().BeTrue();
        var retirado = await EventoAsync("Futuro sem ele", agora.AddDays(5), 60, _pedro);
        var detalhe = (await ExecutarAsync(_diana, new ObterEvento(retirado))).Valor!;
        var retirada = await ExecutarAsync(_diana, new SalvarEvento(
            retirado, "Futuro sem ele", null, null, TipoEvento.Reuniao, detalhe.InicioEm, 60, null, null, null,
            [_paula], detalhe.Versao));
        retirada.Sucesso.Should().BeTrue(retirada.Mensagem);

        var pedro = await ExecutarAsync(_diana, new ObterPendenciasDoUsuario(_pedro));
        var paula = await ExecutarAsync(_diana, new ObterPendenciasDoUsuario(_paula));
        var diretor = await ExecutarAsync(_diana, new ObterPendenciasDoUsuario(davi));

        pedro.Sucesso.Should().BeTrue(pedro.Mensagem);
        pedro.Valor.Should().Be(new PendenciasDoUsuario(PartesPendentes: 1, RevisoesEmAberto: 0, EventosFuturos: 2),
            "só a parte da atividade pendente; o evento futuro e o que está em andamento");
        paula.Valor.Should().Be(new PendenciasDoUsuario(PartesPendentes: 1, RevisoesEmAberto: 0, EventosFuturos: 1),
            "a parte da atividade em que só Pedro concluiu; o evento do qual Pedro foi retirado");
        diretor.Valor.Should().Be(new PendenciasDoUsuario(0, 1, 0), "só a atividade feita aguarda a revisão dele");
    }

    [Fact]
    public async Task Pendencias_de_usuario_inexistente_e_acesso_de_producao()
    {
        (await ExecutarAsync(_diana, new ObterPendenciasDoUsuario(Guid.CreateVersion7()))).Erro
            .Should().Be(AtividadesJuridicas.Application.Comum.TipoErro.NaoEncontrado);
        (await ExecutarAsync(_pedro, new ObterPendenciasDoUsuario(_pedro))).Erro
            .Should().Be(AtividadesJuridicas.Application.Comum.TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Auditoria_filtra_por_registro_e_por_correlacao()
    {
        var atividade = await CriarAtividadeAsync("Auditada");
        await DirecionarAsync(atividade, _diana, _pedro);

        var doRegistro = (await ConsultarAsync(new FiltroDeAuditoria(EntidadeId: atividade))).Itens;

        doRegistro.Should().NotBeEmpty().And.OnlyContain(r => r.EntidadeId == atividade && r.Entidade == "Atividade");
        doRegistro.Should().Contain(r => r.Operacao == OperacaoAuditada.Criacao);

        // A linha de alteração do direcionamento (status) foi gravada junto com os responsáveis e o histórico.
        var alteracao = doRegistro.Single(r => r.Operacao == OperacaoAuditada.Alteracao && r.Dados!.Contains("Status"));
        alteracao.IdCorrelacao.Should().NotBeNull();

        var daGravacao = (await ConsultarAsync(new FiltroDeAuditoria(IdCorrelacao: alteracao.IdCorrelacao))).Itens;

        daGravacao.Should().OnlyContain(r => r.IdCorrelacao == alteracao.IdCorrelacao);
        daGravacao.Should().Contain(r => r.Id == alteracao.Id);
        daGravacao.Should().Contain(r => r.Entidade == "AtividadeResponsavel" && r.Operacao == OperacaoAuditada.Criacao);
        daGravacao.Should().Contain(r => r.Entidade == "AtividadeHistoricoStatus" && r.Operacao == OperacaoAuditada.Criacao);

        // Os dois filtros combinam entre si e com os existentes.
        var combinado = (await ConsultarAsync(new FiltroDeAuditoria(
            EntidadeId: atividade, IdCorrelacao: alteracao.IdCorrelacao, Entidade: "Atividade",
            Operacao: OperacaoAuditada.Alteracao, UsuarioId: _diana))).Itens;
        combinado.Should().ContainSingle().Which.Id.Should().Be(alteracao.Id);

        (await ConsultarAsync(new FiltroDeAuditoria(EntidadeId: atividade, Entidade: "AtividadeResponsavel"))).Itens
            .Should().BeEmpty("o registro da atividade não é de nenhuma entidade de responsável");
        (await ConsultarAsync(new FiltroDeAuditoria(EntidadeId: Guid.CreateVersion7()))).Itens.Should().BeEmpty();
        (await ConsultarAsync(new FiltroDeAuditoria(EntidadeId: atividade, IdCorrelacao: Guid.CreateVersion7()))).Itens
            .Should().BeEmpty();
    }

    private async Task<AtividadesJuridicas.Application.Comum.Pagina<RegistroAuditoriaResumo>> ConsultarAsync(FiltroDeAuditoria filtro)
    {
        var resultado = await ExecutarAsync(_diana, new ConsultarAuditoria(filtro));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor!;
    }
}
