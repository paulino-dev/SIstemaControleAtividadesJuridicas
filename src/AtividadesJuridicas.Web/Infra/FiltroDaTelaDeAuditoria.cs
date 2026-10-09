using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>Os campos de filtro da tela de auditoria, como digitados (os identificadores ainda são texto).</summary>
public sealed class FiltroDaTelaDeAuditoria
{
    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }
    public Guid? UsuarioId { get; set; }
    public string? Entidade { get; set; }
    public string? EntidadeId { get; set; }
    public string? IdCorrelacao { get; set; }
    public OperacaoAuditada? Operacao { get; set; }
    public string? TipoRecurso { get; set; }
    public AcaoDeAcesso? Acao { get; set; }

    /// <summary>
    /// O botão "ver tudo deste registro" de uma linha: só o período e o registro. Os demais filtros saem, para que a
    /// resposta seja tudo o que há sobre o registro, e não um recorte escondido.
    /// </summary>
    public static FiltroDaTelaDeAuditoria PorRegistro(FiltroDaTelaDeAuditoria atual, Guid entidadeId)
    {
        ArgumentNullException.ThrowIfNull(atual);
        return new FiltroDaTelaDeAuditoria { De = atual.De, Ate = atual.Ate, EntidadeId = entidadeId.ToString() };
    }

    /// <summary>O botão "ver o que foi gravado junto" de uma linha: só o período e a gravação.</summary>
    public static FiltroDaTelaDeAuditoria PorCorrelacao(FiltroDaTelaDeAuditoria atual, Guid idCorrelacao)
    {
        ArgumentNullException.ThrowIfNull(atual);
        return new FiltroDaTelaDeAuditoria { De = atual.De, Ate = atual.Ate, IdCorrelacao = idCorrelacao.ToString() };
    }
}
