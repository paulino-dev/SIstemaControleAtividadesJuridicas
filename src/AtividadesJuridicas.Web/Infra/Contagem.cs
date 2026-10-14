namespace AtividadesJuridicas.Web.Infra;

/// <summary>A quantidade com o nome do item no número certo: "1 documento", "8 documentos".</summary>
public static class Contagem
{
    public static string De(int quantidade, string singular, string plural) =>
        $"{quantidade} {(quantidade == 1 ? singular : plural)}";

    /// <summary>"5 áreas · 1 inativa": a quantidade e, quando houver, quantos estão inativos.</summary>
    public static string ComInativos(
        int total, int inativos, string singular, string plural, string inativoNoSingular, string inativoNoPlural) =>
        inativos > 0
            ? $"{De(total, singular, plural)} · {De(inativos, inativoNoSingular, inativoNoPlural)}"
            : De(total, singular, plural);
}
