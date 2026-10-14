namespace AtividadesJuridicas.Web.Infra;

/// <summary>O começo de um identificador longo, para caber numa célula: "0199a3f2…".</summary>
public static class IdentificadorCurto
{
    public static string De(Guid id) => $"{id.ToString()[..8]}…";
}
