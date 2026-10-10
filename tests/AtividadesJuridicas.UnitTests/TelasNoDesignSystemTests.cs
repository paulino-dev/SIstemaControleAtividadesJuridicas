using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// Tela migrada para o design system não volta atrás: nada de estilo embutido nem de classe fora do prefixo
/// <c>aj-</c>. Cada fase da migração acrescenta os seus arquivos a <see cref="Migradas"/>.
/// </summary>
public partial class TelasNoDesignSystemTests
{
    // Caminhos a partir de src/AtividadesJuridicas.Web.
    private static readonly string[] Migradas =
    [
        "Components/Layout/Aviso.razor",
        "Components/Layout/RotuloDeCampo.razor",
    ];

    [GeneratedRegex("\\sstyle\\s*=\\s*\"")]
    private static partial Regex EstiloEmbutido();

    // Só o valor fixo: classe montada por expressão Razor (com @) é conferida na revisão.
    [GeneratedRegex("\\sclass\\s*=\\s*\"([^\"@]*)\"")]
    private static partial Regex ClasseFixa();

    public static TheoryData<string> Arquivos => new(Migradas);

    [Theory]
    [MemberData(nameof(Arquivos))]
    public void Tela_migrada_nao_tem_estilo_embutido_nem_classe_fora_do_design_system(string arquivo)
    {
        var caminho = Path.Combine(
            [GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web", .. arquivo.Split('/')]);
        var texto = File.ReadAllText(caminho);

        EstiloEmbutido().IsMatch(texto).Should().BeFalse(
            "tela migrada não tem estilo embutido: o padrão vira classe aj- no design system");
        ClasseFixa().Matches(texto)
            .SelectMany(m => m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(classe => !classe.StartsWith("aj-", StringComparison.Ordinal))
            .Should().BeEmpty("tela migrada só usa classes do design system");
    }
}
