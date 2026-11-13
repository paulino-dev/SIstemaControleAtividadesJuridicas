namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// A barra de ações fixa é, no desktop como no tablet, uma faixa presa ao pé da coluna de conteúdo (e não um cartão
/// flutuante). Estas regras vêm do design system (<c>css/componentes.css</c>, cópia do <c>bundle.css</c> do artifact).
/// </summary>
public class ComponentesCssTests
{
    // A faixa: de borda a borda da coluna, com linha no topo, sem cantos arredondados nem sombra.
    private const string Faixa =
        "position:absolute;left:0;right:0;bottom:0;z-index:5;max-width:none;gap:var(--espaco-10);" +
        "padding:var(--espaco-14) var(--espaco-24);border:0;border-top:1px solid var(--borda);border-radius:0;box-shadow:none";

    private static string Componentes() =>
        File.ReadAllText(Path.Combine(
            GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web", "wwwroot", "css", "componentes.css"));

    [Fact]
    public void Barra_fixa_do_desktop_e_rodape_da_coluna()
    {
        var css = Componentes();
        css.Should().Contain(".aj-acoes--fixa{" + Faixa + "}")
            .And.Contain("[data-visao=\"desktop\"] .aj-acoes--fixa{display:flex;" + Faixa + "}")
            .And.NotContain("position:sticky");
    }

    [Fact]
    public void Espaco_da_barra_no_desktop_so_vale_com_a_barra_do_desktop()
    {
        var css = Componentes();
        css.Should().Contain("@media (min-width:1200px){.aj-app:has(.aj-so-desktop>.aj-acoes--fixa) .aj-app__conteudo{padding-bottom:104px}}")
            .And.Contain("[data-visao=\"desktop\"] .aj-app:has(.aj-so-desktop>.aj-acoes--fixa) .aj-app__conteudo{padding-bottom:104px}");
    }

    [Fact]
    public void Barra_fixa_media_ocupa_a_coluna()
    {
        var css = Componentes();
        var medio = css.IndexOf(".aj-acoes--medio{max-width:680px}", StringComparison.Ordinal);
        var fixaMedia = css.IndexOf(".aj-acoes--fixa.aj-acoes--medio{max-width:none}", StringComparison.Ordinal);
        medio.Should().BeGreaterThan(-1);
        fixaMedia.Should().BeGreaterThan(medio);
    }
}
