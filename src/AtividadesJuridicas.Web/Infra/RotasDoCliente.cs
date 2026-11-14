using System.Text.RegularExpressions;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>As páginas que um usuário de perfil Cliente pode abrir. Fechado por padrão: página nova é interna.</summary>
public static partial class RotasDoCliente
{
    private static readonly HashSet<string> Abertas = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "minha-senha", "nao-encontrada", "andamento", "meus-documentos", "documentos-comuns",
        "minhas-solicitacoes", "minhas-solicitacoes/nova",
    };

    // Só do portal: não são montadas para quem não é cliente. Documentos comuns fica de fora, porque a Diretoria usa.
    private static readonly HashSet<string> SoDoPortal = new(StringComparer.OrdinalIgnoreCase)
    {
        "andamento", "meus-documentos", "minhas-solicitacoes", "minhas-solicitacoes/nova",
    };

    /// <summary><paramref name="caminhoRelativo"/>: o endereço sem a base, como em <c>NavigationManager.ToBaseRelativePath</c>.</summary>
    public static bool Permite(string caminhoRelativo)
    {
        var caminho = Normalizar(caminhoRelativo);
        return Abertas.Contains(caminho) || FichaOuEdicaoDeSolicitacao(caminho);
    }

    /// <summary>Verdadeiro para a página que só o cliente abre.</summary>
    public static bool Exclusiva(string caminhoRelativo)
    {
        var caminho = Normalizar(caminhoRelativo);
        return SoDoPortal.Contains(caminho) || FichaOuEdicaoDeSolicitacao(caminho);
    }

    // A ficha e a edição de uma solicitação do portal: /minhas-solicitacoes/{id} e /minhas-solicitacoes/{id}/editar.
    // O trecho do Id vale quando é um Guid para o Guid.TryParse, o mesmo critério da restrição {Id:guid} do roteador:
    // todo endereço que monta essas páginas é exclusivo do portal, e nenhum outro abre ao cliente.
    private static bool FichaOuEdicaoDeSolicitacao(string caminho) =>
        FichaOuEdicao().Match(caminho) is { Success: true } trecho && Guid.TryParse(trecho.Groups["id"].Value, out _);

    [GeneratedRegex("^minhas-solicitacoes/(?<id>[^/]+)(?:/editar)?$", RegexOptions.IgnoreCase)]
    private static partial Regex FichaOuEdicao();

    private static string Normalizar(string caminhoRelativo) =>
        Uri.UnescapeDataString(caminhoRelativo.Split('?', '#')[0]).Trim('/');
}
