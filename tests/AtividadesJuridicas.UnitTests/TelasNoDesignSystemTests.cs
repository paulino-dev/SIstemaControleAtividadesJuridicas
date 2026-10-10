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
        "Components/Layout/CabecalhoDePagina.razor",
        "Components/Layout/MainLayout.razor",
        "Components/Layout/RotuloDeCampo.razor",
    ];

    [GeneratedRegex("\\sstyle\\s*=\\s*[\"']")]
    private static partial Regex EstiloEmbutido();

    // Só o valor fixo: classe montada por expressão Razor (com @) é conferida na revisão.
    [GeneratedRegex("\\sclass\\s*=\\s*(?:\"([^\"@]*)\"|'([^'@]*)')")]
    private static partial Regex ClasseFixa();

    /// <summary>O que a marcação tem de fora do design system: "style" para estilo embutido, ou o nome da classe.</summary>
    private static List<string> Violacoes(string marcacao)
    {
        var violacoes = new List<string>();
        if (EstiloEmbutido().IsMatch(marcacao)) violacoes.Add("style");

        violacoes.AddRange(ClasseFixa().Matches(marcacao)
            .SelectMany(m => (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(classe => !classe.StartsWith("aj-", StringComparison.Ordinal)));
        return violacoes;
    }

    public static TheoryData<string> Arquivos => new(Migradas);

    [Theory]
    [MemberData(nameof(Arquivos))]
    public void Tela_migrada_nao_tem_estilo_embutido_nem_classe_fora_do_design_system(string arquivo)
    {
        var caminho = Path.Combine(
            [GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web", .. arquivo.Split('/')]);

        Violacoes(File.ReadAllText(caminho)).Should().BeEmpty(
            "tela migrada não tem estilo embutido nem classe fora do design system: o padrão vira classe aj- lá");
    }

    // A própria trava: o que ela tem de reprovar e o que tem de deixar passar.
    [Theory]
    [InlineData("<p style=\"margin:0\">x</p>", "style")]
    [InlineData("<p style='margin:0'>x</p>", "style")]
    [InlineData("<p class=\"erro\">x</p>", "erro")]
    [InlineData("<p class='aj-aviso erro'>x</p>", "erro")]
    [InlineData("<div class=\"aj-painel formulario\"></div>", "formulario")]
    public void A_trava_reprova_estilo_embutido_e_classe_de_fora(string marcacao, string violacao)
    {
        Violacoes(marcacao).Should().Equal(violacao);
    }

    [Theory]
    [InlineData("<p class=\"aj-aviso aj-aviso--erro\">x</p>")]
    [InlineData("<label class=\"@(erro ? \"aj-rotulo aj-rotulo--erro\" : \"aj-rotulo\")\">x</label>")]
    [InlineData("<Icone Nome=\"voltar\" Classe=\"aj-cartao__seta\" />")]
    public void A_trava_aceita_marcacao_do_design_system(string marcacao)
    {
        Violacoes(marcacao).Should().BeEmpty();
    }
}
