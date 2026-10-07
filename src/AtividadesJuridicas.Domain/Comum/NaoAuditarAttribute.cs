namespace AtividadesJuridicas.Domain.Comum;

/// <summary>Propriedade que nunca aparece nos registros de auditoria: segredo ou controle técnico.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NaoAuditarAttribute : Attribute;
