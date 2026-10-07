using System.Security.Claims;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Web.Infra;

public static class IdentidadeHttp
{
    public static Guid? UsuarioId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public const string ClaimDaVersao = "atividades:sessao";

    /// <summary>Cookie sem a claim (ou com ela malformada) devolve Guid.Empty, que é sempre recusado.</summary>
    public static Guid VersaoDeSessao(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimDaVersao), out var versao) ? versao : Guid.Empty;

    /// <summary>Para endpoints fora do Blazor: fixa a identidade no escopo da própria requisição HTTP.</summary>
    public static void Definir(HttpContext contexto)
    {
        var usuarioId = UsuarioId(contexto.User);
        contexto.RequestServices.GetRequiredService<IdentidadeAtual>().Definir(
            usuarioId,
            contexto.Connection.RemoteIpAddress?.ToString(),
            usuarioId is null ? null : VersaoDeSessao(contexto.User));
    }
}
