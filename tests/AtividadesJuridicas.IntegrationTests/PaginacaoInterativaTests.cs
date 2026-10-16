using AngleSharp.Dom;
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O clique na paginação: o botão sem ação (na ponta ou durante a carga) continua focável, com aria-disabled, e o clique
/// nele não muda a página; o botão com ação pede a página vizinha. Nas duas formas (com e sem setas).
/// </summary>
public class PaginacaoInterativaTests : TelaInterativa
{
    private readonly List<int> _pedidas = [];

    private IRenderedComponent<Paginacao> Paginacao(int numero, bool ocupado = false, bool comSetas = false) =>
        Desenhar<Paginacao>(p => p
            .Add(c => c.Numero, numero)
            .Add(c => c.TotalDePaginas, 3)
            .Add(c => c.Total, 45)
            .Add(c => c.Singular, "atividade")
            .Add(c => c.Plural, "atividades")
            .Add(c => c.Ocupado, ocupado)
            .Add(c => c.ComSetas, comSetas)
            .Add(c => c.IrPara, (int pagina) => _pedidas.Add(pagina)));

    private static IElement Botao(IRenderedComponent<Paginacao> paginacao, string texto) =>
        paginacao.FindAll("button").Single(b => b.TextContent.Trim() == texto);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Botao_da_ponta_tem_aria_disabled_e_o_clique_nao_muda_a_pagina(bool comSetas)
    {
        var primeira = Paginacao(1, comSetas: comSetas);

        Botao(primeira, "Anterior").GetAttribute("aria-disabled").Should().Be("true");
        Botao(primeira, "Anterior").HasAttribute("disabled").Should().BeFalse();
        Botao(primeira, "Anterior").Click();
        _pedidas.Should().BeEmpty();

        Botao(primeira, "Próxima").HasAttribute("aria-disabled").Should().BeFalse();
        Botao(primeira, "Próxima").Click();
        _pedidas.Should().Equal(2);

        var ultima = Paginacao(3, comSetas: comSetas);

        Botao(ultima, "Próxima").GetAttribute("aria-disabled").Should().Be("true");
        Botao(ultima, "Próxima").Click();
        Botao(ultima, "Anterior").Click();
        _pedidas.Should().Equal(2, 2);
    }

    [Fact]
    public void Durante_a_carga_os_dois_botoes_tem_aria_disabled_e_o_clique_nao_muda_a_pagina()
    {
        var paginacao = Paginacao(2, ocupado: true);

        paginacao.FindAll("button").Should().HaveCount(2)
            .And.OnlyContain(b => b.GetAttribute("aria-disabled") == "true" && !b.HasAttribute("disabled"));
        Botao(paginacao, "Anterior").Click();
        Botao(paginacao, "Próxima").Click();
        _pedidas.Should().BeEmpty();

        paginacao.Render(p => p.Add(c => c.Ocupado, false));

        paginacao.FindAll("button").Should().HaveCount(2).And.OnlyContain(b => !b.HasAttribute("aria-disabled"));
        Botao(paginacao, "Próxima").Click();
        _pedidas.Should().Equal(3);
    }
}
