using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Auditoria;

public static class ConsultaDeAuditoriaOpcoes
{
    public const int TamanhoDaPagina = 50;
}

/// <summary><paramref name="DeUtc"/> é inclusivo; <paramref name="AteUtc"/> é exclusivo.</summary>
public sealed record FiltroDeAuditoria(
    DateTime? DeUtc = null,
    DateTime? AteUtc = null,
    Guid? UsuarioId = null,
    string? Entidade = null,
    OperacaoAuditada? Operacao = null,
    Guid? EntidadeId = null,
    Guid? IdCorrelacao = null);

/// <summary><paramref name="DeUtc"/> é inclusivo; <paramref name="AteUtc"/> é exclusivo.</summary>
public sealed record FiltroDeAcessos(
    DateTime? DeUtc = null,
    DateTime? AteUtc = null,
    Guid? UsuarioId = null,
    string? TipoRecurso = null,
    AcaoDeAcesso? Acao = null);

public sealed record RegistroAuditoriaResumo(
    Guid Id, DateTime DataHora, Guid? UsuarioId, string? UsuarioNome, OperacaoAuditada Operacao,
    string? Entidade, Guid? EntidadeId, string? Evento, string? Dados, string? Ip,
    Guid? IdCorrelacao);

public sealed record RegistroAcessoResumo(
    Guid Id, DateTime DataHora, Guid UsuarioId, string? UsuarioNome, string TipoRecurso,
    Guid? RecursoId, AcaoDeAcesso Acao, string? Detalhe, string? Ip);

public interface IConsultaDeAuditoria
{
    Task<Pagina<RegistroAuditoriaResumo>> ConsultarAsync(
        FiltroDeAuditoria filtro, int pagina, int tamanho, CancellationToken ct);

    Task<Pagina<RegistroAcessoResumo>> ConsultarAcessosAsync(
        FiltroDeAcessos filtro, int pagina, int tamanho, CancellationToken ct);
}
