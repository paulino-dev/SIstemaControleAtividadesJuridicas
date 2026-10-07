using AtividadesJuridicas.Application.Identidade;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AtividadesJuridicas.Web.Infra;

public static class ValidacaoDeSessao
{
    /// <summary>A cada requisição HTTP, derruba o cookie de quem foi inativado, não existe mais ou teve as sessões renovadas.</summary>
    public static async Task ValidarAsync(CookieValidatePrincipalContext contexto)
    {
        var usuarios = contexto.HttpContext.RequestServices.GetRequiredService<IUsuarioRepositorio>();

        var principal = contexto.Principal;
        var valido =
            principal is not null &&
            IdentidadeHttp.UsuarioId(principal) is { } id &&
            await usuarios.ObterPorIdAsync(id, contexto.HttpContext.RequestAborted) is { Ativo: true } usuario &&
            usuario.VersaoDeSessao == IdentidadeHttp.VersaoDeSessao(principal);

        if (valido) return;

        contexto.RejectPrincipal();
        await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
