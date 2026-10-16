using AngleSharp.Dom;
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// As abas do mobile das fichas, no padrão tablist: só a escolhida entra no Tab; ←/→ (com volta nas pontas), Home e End
/// escolhem a aba e pedem o foco para ela; cada aba aponta (aria-controls) para os invólucros de painel dela. O bUnit
/// prova o foco pedido; o foco que chega, a fileira que rola até a aba e a página que não rola com as teclas ficam para
/// a conferência no navegador.
/// </summary>
public class AbasInterativasTests : TelaInterativa
{
    private const string Modulo = "./Components/Layout/Abas.razor.js";

    private static readonly List<Aba> TresAbas =
    [
        new("dados", "Dados", Painel: "dados-do-processo observacoes-do-processo"),
        new("clientes", "Clientes", 1, "clientes-do-processo"),
        new("situacao", "Situação"),
    ];

    private string? _escolhida;

    private IRenderedComponent<Abas> AsAbas(string ativa) => Desenhar<Abas>(p => p
        .Add(a => a.Rotulo, "Assuntos do processo")
        .Add(a => a.Ativa, ativa)
        .Add(a => a.Itens, TresAbas)
        .Add(a => a.AoEscolher, (string chave) => _escolhida = chave));

    // Sempre procurados de novo: o elemento achado antes de um evento não serve para disparar outro depois dele.
    private static IReadOnlyList<IElement> Botoes(IRenderedComponent<Abas> abas) => abas.FindAll("button[role=tab]");

    private static IElement Escolhida(IRenderedComponent<Abas> abas) => abas.Find("button[aria-selected=true]");

    [Fact]
    public void Tocar_numa_aba_avisa_a_chave_dela()
    {
        string? escolhida = null;
        var abas = Desenhar<Abas>(p => p
            .Add(a => a.Rotulo, "Assuntos do processo")
            .Add(a => a.Ativa, "dados")
            .Add(a => a.Itens, new List<Aba> { new("dados", "Dados"), new("clientes", "Clientes", 1), new("situacao", "Situação") })
            .Add(a => a.AoEscolher, (string chave) => escolhida = chave));

        abas.FindAll("button[role=tab]")[2].Click();

        escolhida.Should().Be("situacao");
    }

    [Fact]
    public void So_a_aba_escolhida_entra_no_tab()
    {
        var abas = AsAbas("clientes");

        Botoes(abas).Select(b => b.GetAttribute("tabindex")).Should().Equal("-1", "0", "-1");
        Escolhida(abas).TextContent.Should().Be("Clientes1");
    }

    [Fact]
    public void A_aba_aponta_para_os_paineis_dela_e_sem_painel_nao_aponta()
    {
        var botoes = Botoes(AsAbas("dados"));

        botoes[0].GetAttribute("aria-controls").Should().Be("dados-do-processo observacoes-do-processo");
        botoes[1].GetAttribute("aria-controls").Should().Be("clientes-do-processo");
        botoes[2].HasAttribute("aria-controls").Should().BeFalse();
    }

    [Theory]
    [InlineData("clientes", "ArrowRight", 2)]
    [InlineData("situacao", "ArrowRight", 0)]   // da última, volta para a primeira
    [InlineData("clientes", "ArrowLeft", 0)]
    [InlineData("dados", "ArrowLeft", 2)]       // da primeira, volta para a última
    [InlineData("clientes", "Home", 0)]
    [InlineData("clientes", "End", 2)]
    public void Tecla_escolhe_a_aba_e_pede_o_foco_para_ela(string ativa, string tecla, int destino)
    {
        var abas = AsAbas(ativa);
        // Os botões sobrevivem ao desenho seguinte (têm @key): o bUnit os atualiza no lugar e o blazor:elementReference
        // deles fica vazio; por isso a cópia rasa de logo depois do primeiro desenho é a que a comparação usa.
        var antes = Botoes(abas).Select(b => b.Clone(deep: false)).Cast<IElement>().ToList();

        Escolhida(abas).KeyDown(new KeyboardEventArgs { Key = tecla });

        _escolhida.Should().Be(TresAbas[destino].Chave);
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(antes[destino]);
    }

    [Fact]
    public void Outras_teclas_e_as_setas_com_alt_ficam_com_o_navegador()
    {
        var abas = AsAbas("clientes");
        var antes = Botoes(abas).Select(b => b.Clone(deep: false)).Cast<IElement>().ToList();

        Escolhida(abas).KeyDown(new KeyboardEventArgs { Key = "Tab" });
        Escolhida(abas).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", AltKey = true });   // Alt+← volta a página
        _escolhida.Should().BeNull();
        FocosDeElementoPedidos.Should().Be(0);

        Escolhida(abas).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        _escolhida.Should().Be("dados");
        // Um pedido de foco só: o da seta sem Alt.
        FocosDeElementoPedidos.Should().Be(1);
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(antes[0]);
    }

    [Fact]
    public void As_teclas_das_abas_nao_rolam_a_pagina()
    {
        var modulo = JSInterop.SetupModule(Modulo);
        modulo.SetupVoid("prenderTeclas", _ => true).SetVoidResult();

        var abas = AsAbas("dados");

        modulo.VerifyInvoke("prenderTeclas").Arguments[0].ShouldBeElementReferenceTo(abas.Find("[role=tablist]"));
    }

    /// <summary>
    /// Cada aba da ficha aponta (aria-controls) para os invólucros de painel dela, na ordem das abas, e cada id existe uma
    /// vez só na marcação das três visões, num invólucro aj-aba-painel. Usado pelos testes das três fichas.
    /// </summary>
    internal static void CadaAbaApontaParaOsPaineisDela<T>(IRenderedComponent<T> ficha, params string[] paineisPorAba)
        where T : IComponent
    {
        ficha.FindAll("[role=tablist] button[role=tab]").Select(a => a.GetAttribute("aria-controls"))
            .Should().Equal(paineisPorAba);
        foreach (var id in paineisPorAba.SelectMany(p => p.Split(' ')))
            ficha.FindAll($"#{id}").Should().ContainSingle($"o painel {id} existe uma vez só")
                .Which.ClassList.Should().Contain("aj-aba-painel");
    }
}
