namespace AtividadesJuridicas.Web.Infra;

public static class UrlLocal
{
    /// <summary>
    /// Devolve <paramref name="url"/> apenas quando é um caminho deste site; caso contrário "/".
    /// Impede que o login seja usado para redirecionar a outro domínio.
    /// </summary>
    public static string Normalizar(string? url)
    {
        if (string.IsNullOrEmpty(url) || url[0] != '/') return "/";
        // Navegadores descartam TAB/CR/LF: "/	/x" viraria "//x".
        if (url.Any(char.IsControl)) return "/";
        if (url.Length > 1 && (url[1] == '/' || url[1] == '\\')) return "/";
        if (url.StartsWith("/conta/", StringComparison.OrdinalIgnoreCase)) return "/";
        return url;
    }
}
