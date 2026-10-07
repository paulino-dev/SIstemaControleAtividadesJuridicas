using System.Security.Claims;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Web.Infra;

public static class IdentidadeHttp
{
    public static Guid? UsuarioId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Para endpoints fora do Blazor: fixa a identidade no escopo da própria requisição HTTP.</summary>
    public static void Definir(HttpContext contexto) =>
        contexto.RequestServices.GetRequiredService<IdentidadeAtual>()
            .Definir(UsuarioId(contexto.User), contexto.Connection.RemoteIpAddress?.ToString());
}
