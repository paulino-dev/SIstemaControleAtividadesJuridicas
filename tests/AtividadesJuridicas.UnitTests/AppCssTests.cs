using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// O <c>app.css</c> é só a base da página: a caixa de borda, o corpo, o contorno de foco e o título focado por roteiro sem
/// contorno. Tudo o mais vem das classes <c>aj-</c> do design system (<c>css/componentes.css</c>); uma regra nova aqui
/// voltaria a vestir as telas por fora dele.
/// </summary>
public partial class AppCssTests
{
    // Os seletores de cada regra da base, na ordem do arquivo. Lista fechada: qualquer outro seletor reprova.
    private static readonly string[] Base = ["*, *::before, *::after", "body", ":focus-visible", "h1:focus"];

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comentario();

    // Uma regra simples: o seletor e um bloco sem chave dentro.
    [GeneratedRegex(@"([^{}]+)\{[^{}]*\}")]
    private static partial Regex Regra();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacos();

    /// <summary>
    /// Os seletores de cada regra (com os espaços normalizados) e o que sobrou fora delas: um bloco <c>@media</c>, um
    /// <c>@supports</c>, uma regra aninhada ou uma chave solta sobram e reprovam.
    /// </summary>
    private static (List<string> Seletores, string Sobra) Regras(string css)
    {
        var semComentarios = Comentario().Replace(css, " ");
        var seletores = Regra().Matches(semComentarios)
            .Select(regra => Espacos().Replace(regra.Groups[1].Value, " ").Trim())
            .ToList();
        return (seletores, Regra().Replace(semComentarios, "").Trim());
    }

    private static string Caminho(params string[] partes) =>
        Path.Combine([GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web", .. partes]);

    [Fact]
    public void App_css_so_tem_a_base()
    {
        var (seletores, sobra) = Regras(File.ReadAllText(Caminho("wwwroot", "app.css")));

        seletores.Should().Equal(Base,
            "o app.css é só a base da página (caixa, corpo e foco); o que uma tela precisa vem das classes aj- do design system");
        sobra.Should().BeEmpty("o app.css não tem bloco fora das regras da base (@media, @supports, regra aninhada)");
    }

    // O aviso de reconexão não tem classe aj- e mede a largura com borda e respiro: não pode depender da caixa de borda
    // da base para caber na tela do celular.
    [Fact]
    public void Aviso_de_reconexao_traz_a_propria_caixa_de_borda()
    {
        var css = File.ReadAllText(Caminho("Components", "Layout", "ReconnectModal.razor.css"));

        css.Should().MatchRegex(@"#components-reconnect-modal\s*\{[^}]*box-sizing:\s*border-box;[^}]*width:\s*min\(22rem");
    }

    // A própria trava: o que ela lê de cada regra e o que ela deixa sobrar.
    [Fact]
    public void A_trava_le_os_seletores_sem_os_comentarios()
    {
        var (seletores, sobra) = Regras("/* h2 { margin: 0; } */\n*,\n*::before { box-sizing: border-box; }\nh1:focus { outline: none; }");

        seletores.Should().Equal("*, *::before", "h1:focus");
        sobra.Should().BeEmpty();
    }

    [Fact]
    public void A_trava_reprova_bloco_fora_das_regras()
    {
        var (seletores, sobra) = Regras("body { margin: 0; }\n@media (max-width: 760.98px) { p { margin: 0; } }");

        seletores.Should().Equal("body", "p");
        sobra.Should().StartWith("@media");
    }
}
