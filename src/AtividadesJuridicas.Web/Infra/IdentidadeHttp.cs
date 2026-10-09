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

    /// <summary>
    /// Marca de que a sessão é de um usuário de perfil Cliente. Não autoriza nada: o acesso é decidido no banco, pelo
    /// portão do executor. Serve só para o layout saber, sem esperar consulta, que não deve montar páginas internas.
    /// Um usuário nunca troca entre o perfil Cliente e os internos, então a marca não envelhece.
    /// </summary>
    public const string ClaimDoPortal = "atividades:portal";

    public static bool EhDoPortal(ClaimsPrincipal principal) => principal.HasClaim(ClaimDoPortal, "1");

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
