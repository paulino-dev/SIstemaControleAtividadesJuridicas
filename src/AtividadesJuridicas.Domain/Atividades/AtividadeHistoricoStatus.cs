using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Atividades;

/// <summary>
/// Uma transição de status. É dado de negócio, exibido na atividade; nunca é alterado depois de criado.
/// <see cref="UsuarioId"/> nulo indica transição feita pelo sistema.
/// </summary>
public sealed class AtividadeHistoricoStatus : Entidade
{
    public const int TamanhoMaximoMotivo = 2000;

    private AtividadeHistoricoStatus() { }

    internal AtividadeHistoricoStatus(
        Guid atividadeId, int ordem, StatusAtividade? statusAnterior, StatusAtividade statusNovo, Guid? usuarioId,
        DateTime dataHoraUtc, string? motivo)
    {
        AtividadeId = atividadeId;
        Ordem = ordem;
        StatusAnterior = statusAnterior;
        StatusNovo = statusNovo;
        UsuarioId = usuarioId;
        DataHora = dataHoraUtc;
        Motivo = motivo;
    }

    public Guid AtividadeId { get; private set; }

    /// <summary>Posição da transição dentro da atividade: duas transições do mesmo instante continuam ordenadas.</summary>
    public int Ordem { get; private set; }

    public StatusAtividade? StatusAnterior { get; private set; }
    public StatusAtividade StatusNovo { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public DateTime DataHora { get; private set; }
    public string? Motivo { get; private set; }
}
