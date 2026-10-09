using AtividadesJuridicas.Application.Identidade;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AtividadesJuridicas.Web.Infra;

public static class ValidacaoDeSessao
{
    /// <summary>A cada requisição HTTP, derruba o cookie de quem foi inativado, não existe mais ou teve as sessões renovadas.</summary>
    public static async Task ValidarAsync(CookieValidatePrincipalContext contexto)
    {
        var servicos = contexto.HttpContext.RequestServices;
        var usuarios = servicos.GetRequiredService<IUsuarioRepositorio>();
        var ct = contexto.HttpContext.RequestAborted;

        var principal = contexto.Principal;
        var valido =
            principal is not null &&
            IdentidadeHttp.VersaoDeSessao(principal) != Guid.Empty &&
            IdentidadeHttp.UsuarioId(principal) is { } id &&
            await usuarios.ObterPorIdAsync(id, ct) is { Ativo: true } usuario &&
            usuario.VersaoDeSessao == IdentidadeHttp.VersaoDeSessao(principal) &&
            // Usuário de cliente só mantém a sessão enquanto o cliente dele está ativo.
            await AcessoPeloCliente.ClienteAtivoAsync(usuario, servicos.GetRequiredService<IClientesDaIdentidade>(), ct);

        if (valido) return;

        contexto.RejectPrincipal();
        await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
