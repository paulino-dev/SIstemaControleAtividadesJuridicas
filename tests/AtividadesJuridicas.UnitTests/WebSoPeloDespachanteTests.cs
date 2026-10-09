using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// O portão do cliente fica no <c>ExecutorDeRequisicoes</c>: só ele barra o ator que não é interno. A camada Web
/// só chega aos dados pelo despachante, que passa pelo executor.
/// </summary>
public partial class WebSoPeloDespachanteTests
{
    // Arquivo → motivo pelo qual chegar aos dados sem passar pelo executor é aceito.
    private static readonly Dictionary<string, string> Permitidos = new()
    {
        ["ValidacaoDeSessao.cs"] = "valida o cookie a cada requisição HTTP: lê só o usuário e o cliente dele, não executa caso de uso",
    };

    [GeneratedRegex(@"\b(I\w+Repositorio|AppDbContext|IUnidadeDeTrabalho|IClientesDaIdentidade|IManipulador\s*<)")]
    private static partial Regex AcessoPorFora();

    [Fact]
    public void A_web_so_chega_aos_dados_pelo_despachante()
    {
        var web = Path.Combine(GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web");
        var separadores = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        var encontrados = new[] { "*.cs", "*.razor" }
            .SelectMany(padrao => Directory.EnumerateFiles(web, padrao, SearchOption.AllDirectories))
            .Where(caminho =>
            {
                var partes = caminho.Split(separadores);
                return !partes.Contains("obj") && !partes.Contains("bin");
            })
            .Where(caminho => AcessoPorFora().IsMatch(File.ReadAllText(caminho)))
            .Select(Path.GetFileName)
            .Order()
            .ToList();

        encontrados.Should().BeEquivalentTo(Permitidos.Keys,
            "o portão do cliente só protege o que passa pelo ExecutorDeRequisicoes (o IDespachante). " +
            "Uma tela, um componente ou um endpoint que lê por repositório, contexto do banco ou manipulador " +
            "fica fora do portão: um usuário de perfil Cliente alcançaria dados que não são dele. " +
            "Envie uma requisição pelo despachante; se o acesso direto for intencional, acrescente o arquivo " +
            "e o motivo a Permitidos");
    }
}
