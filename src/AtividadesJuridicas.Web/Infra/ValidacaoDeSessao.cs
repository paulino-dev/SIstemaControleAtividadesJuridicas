using System.Security.Claims;
using AtividadesJuridicas.Application.Identidade;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AtividadesJuridicas.Web.Infra;

public static class ValidacaoDeSessao
{
    /// <summary>A cada requisição HTTP, derruba o cookie de quem foi inativado ou não existe mais.</summary>
    public static async Task ValidarAsync(CookieValidatePrincipalContext contexto)
    {
        var usuarios = contexto.HttpContext.RequestServices.GetRequiredService<IUsuarioRepositorio>();

        var valido =
            Guid.TryParse(contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) &&
            await usuarios.ObterPorIdAsync(id, contexto.HttpContext.RequestAborted) is { Ativo: true };

        if (valido) return;

        contexto.RejectPrincipal();
        await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
