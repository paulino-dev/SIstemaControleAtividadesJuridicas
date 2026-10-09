namespace AtividadesJuridicas.Web.Infra;

/// <summary>As frases da tela de clientes que dependem de quantos usuários do portal o cliente tem.</summary>
public static class TextosDoCliente
{
    /// <summary>O aviso depois de inativar um cliente; <paramref name="usuariosAtivosDoPortal"/> perdem o acesso com ele.</summary>
    public static string Inativado(int usuariosAtivosDoPortal) => usuariosAtivosDoPortal switch
    {
        <= 0 => "Cliente inativado.",
        1 => "Cliente inativado. 1 usuário do portal ficou sem acesso.",
        _ => $"Cliente inativado. {usuariosAtivosDoPortal} usuários do portal ficaram sem acesso.",
    };
}
