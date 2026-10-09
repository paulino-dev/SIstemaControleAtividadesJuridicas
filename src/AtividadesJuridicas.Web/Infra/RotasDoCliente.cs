namespace AtividadesJuridicas.Web.Infra;

/// <summary>As páginas que um usuário de perfil Cliente pode abrir. Fechado por padrão: página nova é interna.</summary>
public static class RotasDoCliente
{
    // A fase 8B acrescenta andamento, meus-documentos e documentos-comuns.
    private static readonly HashSet<string> Abertas =
        new(StringComparer.OrdinalIgnoreCase) { "", "minha-senha", "nao-encontrada" };

    /// <summary><paramref name="caminhoRelativo"/>: o endereço sem a base, como em <c>NavigationManager.ToBaseRelativePath</c>.</summary>
    public static bool Permite(string caminhoRelativo) =>
        Abertas.Contains(caminhoRelativo.Split('?', '#')[0].Trim('/'));
}
