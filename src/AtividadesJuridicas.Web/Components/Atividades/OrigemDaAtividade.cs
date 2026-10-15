namespace AtividadesJuridicas.Web.Components.Atividades;

/// <summary>
/// De onde a ficha da atividade foi aberta (<c>?de=produtores&amp;produtor=</c>) e como as telas de ação (editar,
/// responsáveis, devolver, reabrir) devolvem essa origem à ficha: nos links que saem dela e no voltar de volta.
/// </summary>
public static class OrigemDaAtividade
{
    /// <summary>
    /// A query que carrega a origem: vazia fora de Produtores; <c>?de=produtores</c> com <c>&amp;produtor={id}</c> só
    /// quando o produtor é um id válido (texto que não é um id é descartado, como a ficha faz).
    /// </summary>
    public static string Consulta(string? de, string? produtor)
    {
        if (!string.Equals(de, "produtores", StringComparison.Ordinal)) return string.Empty;

        return Guid.TryParse(produtor, out var id) ? $"?de=produtores&produtor={id}" : "?de=produtores";
    }

    /// <summary>O endereço da ficha da atividade, com a origem de Produtores quando houver.</summary>
    public static string Ficha(Guid atividadeId, string? de, string? produtor) =>
        $"/atividades/{atividadeId}{Consulta(de, produtor)}";
}
