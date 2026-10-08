using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Agenda;

/// <summary>Um participante do evento.</summary>
public sealed class EventoParticipante : Entidade
{
    private EventoParticipante() { }

    internal EventoParticipante(Guid eventoId, Guid usuarioId)
    {
        EventoId = eventoId;
        UsuarioId = usuarioId;
    }

    public Guid EventoId { get; private set; }
    public Guid UsuarioId { get; private set; }
}
