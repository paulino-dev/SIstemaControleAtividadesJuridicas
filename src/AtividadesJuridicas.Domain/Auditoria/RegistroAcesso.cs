namespace AtividadesJuridicas.Domain.Auditoria;

public enum AcaoDeAcesso
{
    Listou = 1,
    Visualizou = 2,
    Baixou = 3,
}

/// <summary>Uma consulta feita por um usuário. Somente inserção.</summary>
public sealed class RegistroAcesso
{
    private RegistroAcesso() { }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public DateTime DataHora { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string TipoRecurso { get; private set; } = null!;
    public Guid? RecursoId { get; private set; }
    public AcaoDeAcesso Acao { get; private set; }
    public string? Detalhe { get; private set; }
    public string? Ip { get; private set; }

    public static RegistroAcesso Criar(
        DateTime dataHoraUtc, Guid usuarioId, string tipoRecurso, Guid? recursoId, AcaoDeAcesso acao,
        string? detalhe, string? ip)
    {
        if (dataHoraUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A data deve estar em UTC.", nameof(dataHoraUtc));
        if (string.IsNullOrWhiteSpace(tipoRecurso))
            throw new ArgumentException("O tipo do recurso é obrigatório.", nameof(tipoRecurso));

        return new RegistroAcesso
        {
            DataHora = dataHoraUtc, UsuarioId = usuarioId, TipoRecurso = tipoRecurso, RecursoId = recursoId,
            Acao = acao, Detalhe = detalhe, Ip = ip,
        };
    }
}
