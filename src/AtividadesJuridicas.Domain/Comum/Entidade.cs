namespace AtividadesJuridicas.Domain.Comum;

public abstract class Entidade
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public DateTime CriadoEm { get; private set; }
    public Guid? CriadoPor { get; private set; }
    public DateTime? AtualizadoEm { get; private set; }
    public Guid? AtualizadoPor { get; private set; }
    public DateTime? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public bool Excluido => ExcluidoEm is not null;

    public void CarimbarCriacao(DateTime agoraUtc, Guid? usuarioId)
    {
        CriadoEm = ExigirUtc(agoraUtc);
        CriadoPor = usuarioId;
    }

    public void CarimbarAtualizacao(DateTime agoraUtc, Guid? usuarioId)
    {
        AtualizadoEm = ExigirUtc(agoraUtc);
        AtualizadoPor = usuarioId;
    }

    public void MarcarExcluido(DateTime agoraUtc, Guid? usuarioId)
    {
        if (Excluido) return;
        ExcluidoEm = ExigirUtc(agoraUtc);
        ExcluidoPor = usuarioId;
    }

    private static DateTime ExigirUtc(DateTime valor) =>
        valor.Kind == DateTimeKind.Utc
            ? valor
            : throw new ArgumentException("A data deve estar em UTC.", nameof(valor));
}
