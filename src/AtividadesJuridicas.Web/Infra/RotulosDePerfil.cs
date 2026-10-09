using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>O rótulo de cada tipo de perfil, em um só lugar.</summary>
public static class RotulosDePerfil
{
    public static string DoTipo(TipoPerfil tipo) => tipo switch
    {
        TipoPerfil.Diretoria => "Diretoria",
        TipoPerfil.Producao => "Produção",
        TipoPerfil.Cliente => "Cliente",
        _ => tipo.ToString(),
    };
}
