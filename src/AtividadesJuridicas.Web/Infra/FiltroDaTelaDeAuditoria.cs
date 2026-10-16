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

    /// <summary>Uma cópia dos campos: o filtro aplicado não muda com o que se digita depois.</summary>
    public FiltroDaTelaDeAuditoria Copia() => (FiltroDaTelaDeAuditoria)MemberwiseClone();

    /// <summary>
    /// Fechar o painel sem filtrar: os campos do painel (usuário, operação, entidade, registro, gravação, ação e recurso)
    /// voltam ao filtro aplicado. O período, que fica fora do painel, não muda.
    /// </summary>
    public void VoltarOsDoPainel(FiltroDaTelaDeAuditoria aplicado)
    {
        ArgumentNullException.ThrowIfNull(aplicado);
        UsuarioId = aplicado.UsuarioId;
        Entidade = aplicado.Entidade;
        EntidadeId = aplicado.EntidadeId;
        IdCorrelacao = aplicado.IdCorrelacao;
        Operacao = aplicado.Operacao;
        TipoRecurso = aplicado.TipoRecurso;
        Acao = aplicado.Acao;
    }

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
