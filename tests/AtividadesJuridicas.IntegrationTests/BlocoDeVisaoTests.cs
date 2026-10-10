namespace AtividadesJuridicas.IntegrationTests;

public class BlocoDeVisaoTests
{
    [Fact]
    public void Recorta_o_bloco_com_os_divs_de_dentro()
    {
        const string html = "<main><div class=\"aj-so-desktop\"><div class=\"aj-tabela-caixa\"><table></table></div><p>fim</p></div><div class=\"aj-tablet-mobile\"><span>cartão</span></div></main>";

        BlocoDeVisao.De(html, "aj-so-desktop").Should().Be("<div class=\"aj-tabela-caixa\"><table></table></div><p>fim</p>");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should().Be("<span>cartão</span>");
    }

    [Fact]
    public void Junta_os_blocos_da_mesma_visao_e_aceita_span()
    {
        const string html = "<span class=\"aj-so-mobile\">um</span><p>meio</p><div class=\"aj-so-mobile\">dois</div>";

        BlocoDeVisao.De(html, "aj-so-mobile").Should().Be("umdois");
    }

    [Fact]
    public void Sem_bloco_devolve_vazio_e_nao_confunde_classe_parecida()
    {
        const string html = "<div class=\"aj-so-desktop-extra\">x</div><div class=\"aj-painel aj-so-desktop\">y</div>";

        BlocoDeVisao.De(html, "aj-so-desktop").Should().BeEmpty();
    }
}
