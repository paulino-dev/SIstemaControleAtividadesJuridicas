namespace AtividadesJuridicas.Web.Infra;

/// <summary>As páginas que um usuário de perfil Cliente pode abrir. Fechado por padrão: página nova é interna.</summary>
public static class RotasDoCliente
{
    private static readonly HashSet<string> Abertas =
        new(StringComparer.OrdinalIgnoreCase) { "", "minha-senha", "nao-encontrada", "andamento", "meus-documentos", "documentos-comuns" };

    // Só do portal: não são montadas para quem não é cliente. Documentos comuns fica de fora, porque a Diretoria usa.
    private static readonly HashSet<string> SoDoPortal =
        new(StringComparer.OrdinalIgnoreCase) { "andamento", "meus-documentos" };

    /// <summary><paramref name="caminhoRelativo"/>: o endereço sem a base, como em <c>NavigationManager.ToBaseRelativePath</c>.</summary>
    public static bool Permite(string caminhoRelativo) => Abertas.Contains(Normalizar(caminhoRelativo));

    /// <summary>Verdadeiro para a página que só o cliente abre.</summary>
    public static bool Exclusiva(string caminhoRelativo) => SoDoPortal.Contains(Normalizar(caminhoRelativo));

    private static string Normalizar(string caminhoRelativo) =>
        Uri.UnescapeDataString(caminhoRelativo.Split('?', '#')[0]).Trim('/');
}
