using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Agenda;

/// <summary>Os eventos que tocam o período <c>[DeUtc, AteUtc)</c>, em UTC, de no máximo 92 dias.</summary>
public sealed record ListarEventos(DateTime DeUtc, DateTime AteUtc, Guid? ParticipanteId = null, TipoEvento? Tipo = null)
    : IRequisicao<Resultado<IReadOnlyList<EventoResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Evento";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { DeUtc, AteUtc, ParticipanteId, Tipo });
}

public sealed class ListarEventosManipulador(IContextoDeAcesso contexto, IEventoRepositorio eventos)
    : IManipulador<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>
{
    public async Task<Resultado<IReadOnlyList<EventoResumo>>> ManipularAsync(ListarEventos requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (requisicao.DeUtc.Kind != DateTimeKind.Utc || requisicao.AteUtc.Kind != DateTimeKind.Utc
            || requisicao.DeUtc >= requisicao.AteUtc)
            return Falha(TipoErro.Validacao, "Período inválido.");
        if (requisicao.AteUtc - requisicao.DeUtc > TimeSpan.FromDays(AgendaOpcoes.PeriodoMaximoEmDias))
            return Falha(TipoErro.Validacao, $"O período deve ter no máximo {AgendaOpcoes.PeriodoMaximoEmDias} dias.");

        // Fora da Diretoria só se listam os eventos em que se participa ou que se criou, qualquer que seja o filtro.
        var filtro = new FiltroDeEventos(
            requisicao.DeUtc, requisicao.AteUtc, requisicao.ParticipanteId, requisicao.Tipo,
            ator.EhDiretoria ? null : ator.Id);

        return Resultado<IReadOnlyList<EventoResumo>>.Ok(await eventos.ListarAsync(filtro, ct));
    }

    private static Resultado<IReadOnlyList<EventoResumo>> Falha(TipoErro erro, string mensagem) =>
        Resultado<IReadOnlyList<EventoResumo>>.Falha(erro, mensagem);
}
