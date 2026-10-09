using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class ComentariosTests
{
    private const string NaoEncontrada = "Atividade não encontrada.";

    private readonly Cenario _c = new();
    private readonly Usuario _pedro, _ester;

    public ComentariosTests()
    {
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _ester = _c.NovoUsuario("Ester", "ester@x.com", _c.PerfilProducao);
    }

    private Usuario Diana => _c.Diretor;

    private async Task<Guid> DirecionadaAsync()
    {
        var criada = await new SalvarAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(null, "Elaborar contestação", "Prazo de 15 dias.", new DateOnly(2026, 10, 20), null, null, null, 1), default);
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        var definido = await new DefinirResponsaveisDaAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirResponsaveisDaAtividade(
                criada.Valor, [new ResponsavelEntrada(_pedro.Id, "Redigir")], Diana.Id), default);
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
        return criada.Valor;
    }

    private Task<Resultado<Guid>> Comentar(Usuario? ator, Guid id, string texto) =>
        new ComentarAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Comentarios, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new ComentarAtividade(id, texto), default);

    private Task<Resultado<IReadOnlyList<ComentarioDetalhe>>> Listar(Usuario? ator, Guid id) =>
        new ListarComentariosDaAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Comentarios, _c.Auditoria)
            .ManipularAsync(new ListarComentariosDaAtividade(id), default);

    [Fact]
    public async Task Diretor_e_responsavel_comentam_e_leem_em_ordem()
    {
        var id = await DirecionadaAsync();
        _c.Relogio.AgoraUtc = new DateTime(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc);
        (await Comentar(Diana, id, "Revisei o prazo.")).Sucesso.Should().BeTrue();
        _c.Relogio.AgoraUtc = new DateTime(2026, 10, 7, 10, 0, 0, DateTimeKind.Utc);
        var dele = await Comentar(_pedro, id, "  Começando agora.\nVolto à tarde.  ");
        dele.Sucesso.Should().BeTrue(dele.Mensagem);

        foreach (var leitor in new[] { Diana, _pedro })
        {
            var lista = await Listar(leitor, id);
            lista.Sucesso.Should().BeTrue(lista.Mensagem);
            lista.Valor!.Select(c => (c.UsuarioNome, c.Texto)).Should().Equal(
                ("Diana Diretora", "Revisei o prazo."), ("Pedro", "Começando agora.\nVolto à tarde."));
            lista.Valor![1].Id.Should().Be(dele.Valor);
            lista.Valor[1].UsuarioId.Should().Be(_pedro.Id);
            lista.Valor[1].DataHora.Should().Be(new DateTime(2026, 10, 7, 10, 0, 0, DateTimeKind.Utc));
        }

        _c.Comentarios.Itens.Should().HaveCount(2);
        _c.Unidade.Salvamentos.Should().Be(4);
    }

    [Fact]
    public async Task Producao_que_nao_e_responsavel_nao_comenta_nem_le_e_a_tentativa_fica_registrada()
    {
        var id = await DirecionadaAsync();
        _c.Auditoria.Eventos.Clear();

        var comentou = await Comentar(_ester, id, "Posso ajudar?");
        var leu = await Listar(_ester, id);

        comentou.Erro.Should().Be(TipoErro.NaoEncontrado);
        comentou.Mensagem.Should().Be(NaoEncontrada);
        leu.Erro.Should().Be(TipoErro.NaoEncontrado);
        leu.Mensagem.Should().Be(NaoEncontrada);
        _c.Comentarios.Itens.Should().BeEmpty();
        _c.Auditoria.Eventos.Should().HaveCount(2).And.OnlyContain(e =>
            e.Evento == EventosDeSeguranca.AcessoNegado && e.Entidade == "Atividade" && e.EntidadeId == id && e.Imediato);
    }

    [Fact]
    public async Task Atividade_inexistente_e_nao_encontrada_sem_evento()
    {
        var comentou = await Comentar(Diana, Guid.CreateVersion7(), "Oi");
        var leu = await Listar(Diana, Guid.CreateVersion7());

        comentou.Erro.Should().Be(TipoErro.NaoEncontrado);
        comentou.Mensagem.Should().Be(NaoEncontrada);
        leu.Erro.Should().Be(TipoErro.NaoEncontrado);
        leu.Mensagem.Should().Be(NaoEncontrada);
        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Sem_usuario_e_acesso_negado()
    {
        var id = await DirecionadaAsync();

        (await Comentar(null, id, "Oi")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Listar(null, id)).Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Comentarios.Itens.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "Escreva o comentário.")]
    [InlineData("   ", "Escreva o comentário.")]
    [InlineData("LONGO", "O comentário deve ter até 4000 caracteres.")]
    public async Task Comentario_vazio_ou_longo_e_validacao_e_nada_e_gravado(string texto, string mensagem)
    {
        var id = await DirecionadaAsync();
        var salvamentos = _c.Unidade.Salvamentos;

        var resultado = await Comentar(Diana, id, texto == "LONGO" ? new string('x', 4001) : texto);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(mensagem);
        _c.Comentarios.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(salvamentos);
    }

    [Fact]
    public async Task Comenta_se_em_qualquer_status()
    {
        var id = await DirecionadaAsync();
        var atividade = _c.Atividades.Itens.Single(a => a.Id == id);
        var vistos = new List<StatusAtividade>();

        async Task ComentaAsync()
        {
            vistos.Add(atividade.Status);
            (await Comentar(Diana, id, "Status " + atividade.Status)).Sucesso.Should().BeTrue();
        }

        await ComentaAsync(); // Direcionada
        var concluida = await new ConcluirMinhaParteManipulador(_c.Como(_pedro), _c.Atividades, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new ConcluirMinhaParte(id), default);
        concluida.Sucesso.Should().BeTrue(concluida.Mensagem);
        await ComentaAsync(); // Feita
        (await new AprovarAtividadeManipulador(_c.Como(Diana), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new AprovarAtividade(id), default)).Sucesso.Should().BeTrue();
        await ComentaAsync(); // Concluida

        vistos.Should().Equal(StatusAtividade.Direcionada, StatusAtividade.Feita, StatusAtividade.Concluida);
        _c.Comentarios.Itens.Should().HaveCount(3);
    }

    [Fact]
    public async Task Listagem_e_consulta_auditavel()
    {
        var id = await DirecionadaAsync();

        var consulta = new ListarComentariosDaAtividade(id);

        consulta.Should().BeAssignableTo<IConsultaAuditavel>();
        consulta.TipoRecurso.Should().Be("AtividadeComentario");
        consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
        consulta.Detalhe.Should().Contain(id.ToString());
    }
}
