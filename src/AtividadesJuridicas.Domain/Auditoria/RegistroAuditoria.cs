namespace AtividadesJuridicas.Domain.Auditoria;

public enum OperacaoAuditada
{
    Criacao = 1,
    Alteracao = 2,
    Exclusao = 3,
    Evento = 4,
}

/// <summary>Somente inserção: não é uma Entidade, não tem carimbos nem exclusão lógica.</summary>
public sealed class RegistroAuditoria
{
    private RegistroAuditoria() { }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public DateTime DataHora { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public OperacaoAuditada Operacao { get; private set; }
    public string? Entidade { get; private set; }
    public Guid? EntidadeId { get; private set; }
    public string? Evento { get; private set; }
    public string? Dados { get; private set; }
    public string? Ip { get; private set; }
    public Guid? IdCorrelacao { get; private set; }

    public static RegistroAuditoria DeAlteracao(
        DateTime dataHoraUtc, Guid? usuarioId, OperacaoAuditada operacao, string entidade, Guid entidadeId,
        string? dados, string? ip, Guid? idCorrelacao)
    {
        if (operacao == OperacaoAuditada.Evento)
            throw new ArgumentException("Use DeEvento para eventos.", nameof(operacao));

        return new RegistroAuditoria
        {
            DataHora = ExigirUtc(dataHoraUtc), UsuarioId = usuarioId, Operacao = operacao,
            Entidade = entidade, EntidadeId = entidadeId, Dados = dados, Ip = ip, IdCorrelacao = idCorrelacao,
        };
    }

    public static RegistroAuditoria DeEvento(
        DateTime dataHoraUtc, Guid? usuarioId, string evento, string? entidade, Guid? entidadeId,
        string? dados, string? ip, Guid? idCorrelacao)
    {
        if (string.IsNullOrWhiteSpace(evento))
            throw new ArgumentException("O nome do evento é obrigatório.", nameof(evento));

        return new RegistroAuditoria
        {
            DataHora = ExigirUtc(dataHoraUtc), UsuarioId = usuarioId, Operacao = OperacaoAuditada.Evento,
            Evento = evento, Entidade = entidade, EntidadeId = entidadeId, Dados = dados, Ip = ip,
            IdCorrelacao = idCorrelacao,
        };
    }

    private static DateTime ExigirUtc(DateTime valor) =>
        valor.Kind == DateTimeKind.Utc
            ? valor
            : throw new ArgumentException("A data deve estar em UTC.", nameof(valor));
}
