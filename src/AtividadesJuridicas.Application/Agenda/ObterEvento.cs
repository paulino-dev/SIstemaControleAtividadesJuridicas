using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Agenda;

public sealed record ObterEvento(Guid Id) : IRequisicao<Resultado<EventoDetalhe>>, IConsultaAuditavel
{
    public string TipoRecurso => "Evento";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => Id;
}

public sealed class ObterEventoManipulador(
    IContextoDeAcesso contexto,
    IEventoRepositorio eventos,
    IUsuarioRepositorio usuarios,
    IClienteRepositorio clientes,
    IProcessoRepositorio processos,
    IAtividadeRepositorio atividades,
    IAuditoria auditoria) : IManipulador<ObterEvento, Resultado<EventoDetalhe>>
{
    public async Task<Resultado<EventoDetalhe>> ManipularAsync(ObterEvento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado<EventoDetalhe>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var evento = await eventos.ObterPorIdAsync(requisicao.Id, ct);
        if (evento is null)
            return Resultado<EventoDetalhe>.Falha(TipoErro.NaoEncontrado, AcessoAoEvento.NaoEncontrado);

        if (!AcessoAoEvento.PodeVer(ator, evento))
        {
            await AcessoAoEvento.RegistrarNegadoAsync(auditoria, evento.Id, nameof(ObterEvento));
            return Resultado<EventoDetalhe>.Falha(TipoErro.NaoEncontrado, AcessoAoEvento.NaoEncontrado);
        }

        var idsDeUsuarios = evento.Participantes.Select(p => p.UsuarioId).Append(evento.CriadorId).Distinct().ToList();
        var pessoas = (await usuarios.ObterVariosAsync(idsDeUsuarios, ct)).ToDictionary(u => u.Id);

        var cliente = evento.ClienteId is { } c ? await clientes.ObterPorIdAsync(c, ct) : null;
        var processo = evento.ProcessoId is { } p ? await processos.ObterPorIdAsync(p, ct) : null;

        // O título da atividade só aparece para quem pode ver a atividade; o Id continua.
        string? tituloDaAtividade = null;
        if (evento.AtividadeId is { } a && await atividades.ObterPorIdAsync(a, ct) is { } atividade
            && AcessoAAtividade.PodeVer(ator, atividade))
            tituloDaAtividade = atividade.Titulo;

        var participantes = evento.Participantes
            .Select(x => pessoas.TryGetValue(x.UsuarioId, out var u)
                ? new ParticipanteDetalhe(x.UsuarioId, u.Nome, u.Ativo)
                : new ParticipanteDetalhe(x.UsuarioId, null, false))
            .OrderBy(x => x.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var podeGerenciar = AcessoAoEvento.PodeGerenciar(ator, evento);

        return Resultado<EventoDetalhe>.Ok(new EventoDetalhe(
            evento.Id, evento.Titulo, evento.Descricao, evento.Local, evento.Tipo, evento.InicioEm,
            evento.DuracaoEstimadaMinutos, evento.FimEm, evento.ClienteId, cliente?.Nome, evento.ProcessoId,
            processo?.Titulo, evento.AtividadeId, tituloDaAtividade, evento.CriadorId,
            pessoas.TryGetValue(evento.CriadorId, out var criador) ? criador.Nome : null,
            participantes, new AcoesDoEvento(podeGerenciar, podeGerenciar), evento.Versao));
    }
}
