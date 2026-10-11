namespace AtividadesJuridicas.Web.Infra;

/// <summary>A quantidade com o nome do item no número certo: "1 documento", "8 documentos".</summary>
public static class Contagem
{
    public static string De(int quantidade, string singular, string plural) =>
        $"{quantidade} {(quantidade == 1 ? singular : plural)}";
}
