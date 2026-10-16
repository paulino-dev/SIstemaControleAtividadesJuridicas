using AngleSharp.Dom;
using System.Reflection;
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O menu de ações pelo teclado e pelo toque. Fecham o menu: o Esc (com o foco no botão, na lista ou num item) e o
/// toque no fundo, que devolvem o foco ao botão; o foco que sai do menu (o focusout do módulo, que chama FocoSaiu) e a
/// escolha de um item, que não mexem no foco. Abrir um menu fecha o que estava aberto.
/// O bUnit não dispara o focusout do navegador: o teste confere a instalação do módulo e chama o FocoSaiu como o módulo
/// chamaria. O Tab real, o Shift+Tab no botão e o clique fora ficam para a conferência no navegador.
/// </summary>
public class MenuDeAcoesInterativoTests : TelaInterativa
{
    private const string Modulo = "./Components/Layout/MenuDeAcoes.razor.js";

    private const string Itens =
        "<button type=\"button\" class=\"aj-menu-acoes__item\">Renomear</button>" +
        "<button type=\"button\" class=\"aj-menu-acoes__item aj-menu-acoes__item--perigo\">Excluir</button>";

    private IRenderedComponent<MenuDeAcoes> Menu(string rotulo = "Ações de modelo.pdf") =>
        Desenhar<MenuDeAcoes>(p => p.Add(m => m.Rotulo, rotulo).AddChildContent(Itens));

    // Sempre procurados de novo: o elemento achado antes de um evento não serve para disparar outro depois dele.
    private static IElement Botao(IRenderedComponent<MenuDeAcoes> menu) => menu.Find(".aj-menu-acoes > button[aria-expanded]");

    private static IElement Lista(IRenderedComponent<MenuDeAcoes> menu) => menu.Find(".aj-menu-acoes__lista");

    private static void DeveEstarAberto(IRenderedComponent<MenuDeAcoes> menu)
    {
        Botao(menu).GetAttribute("aria-expanded").Should().Be("true");
        Lista(menu).HasAttribute("hidden").Should().BeFalse();
        menu.FindAll(".aj-menu-acoes__fundo").Should().ContainSingle();
    }

    private static void DeveEstarFechado(IRenderedComponent<MenuDeAcoes> menu)
    {
        Botao(menu).GetAttribute("aria-expanded").Should().Be("false");
        Lista(menu).HasAttribute("hidden").Should().BeTrue();
        menu.FindAll(".aj-menu-acoes__fundo").Should().BeEmpty();
    }

    // Aqui o atributo blazor:elementReference do HTML fica vazio (o id da referência só nasce no primeiro pedido de foco,
    // depois de o elemento já estar desenhado), então ShouldBeElementReferenceTo não acha o elemento. O teste compara o id do
    // pedido com o da referência que o componente guarda (o campo privado), a mesma que o JavaScript recebe.
    private static void DeveSerOElemento(object? argumento, IRenderedComponent<MenuDeAcoes> menu, string campo)
    {
        var referencia = (ElementReference)typeof(MenuDeAcoes)
            .GetField(campo, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(menu.Instance)!;

        argumento.Should().BeOfType<ElementReference>().Which.Id.Should().Be(referencia.Id).And.NotBeNullOrEmpty();
    }
    /// <summary>Abre pelo clique: o menu aberto e o foco pedido para a lista, o primeiro pedido de foco do teste.</summary>
    private IRenderedComponent<MenuDeAcoes> Aberto(string rotulo = "Ações de modelo.pdf")
    {
        var menu = Menu(rotulo);
        Botao(menu).Click();

        DeveEstarAberto(menu);
        DeveSerOElemento(JSInterop.VerifyFocusAsyncInvoke().Arguments[0], menu, "_lista");
        return menu;
    }

    /// <summary>Dois pedidos de foco no teste: a lista, ao abrir, e depois o botão do menu.</summary>
    private void FocoDevolvidoAoBotao(IRenderedComponent<MenuDeAcoes> menu)
    {
        var focos = JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2);
        DeveSerOElemento(focos[0].Arguments[0], menu, "_lista");
        DeveSerOElemento(focos[1].Arguments[0], menu, "_gatilho");
    }

    [Theory]
    [InlineData(".aj-menu-acoes > button[aria-expanded]")]   // o botão
    [InlineData(".aj-menu-acoes__lista")]                    // a lista, que recebe o foco ao abrir
    [InlineData(".aj-menu-acoes__item--perigo")]             // um item da lista
    public void Esc_fecha_com_o_foco_no_botao_ou_na_lista_e_devolve_o_foco_ao_botao(string ondeEstaOFoco)
    {
        var menu = Aberto();

        // A tecla sobe do elemento focado até o invólucro do menu, como no navegador.
        menu.Find(ondeEstaOFoco).KeyDown(new KeyboardEventArgs { Key = "Escape" });

        DeveEstarFechado(menu);
        FocoDevolvidoAoBotao(menu);
    }

    [Fact]
    public async Task O_foco_que_sai_do_menu_fecha_sem_pedir_foco()
    {
        var menu = Aberto();

        // O que o focusout do módulo faz quando o foco vai para fora do invólucro (Tab no último item, Shift+Tab no botão).
        await menu.Instance.FocoSaiuAsync();

        DeveEstarFechado(menu);
        // Só o pedido de foco da abertura: o Tab segue o caminho natural.
        DeveSerOElemento(JSInterop.VerifyFocusAsyncInvoke().Arguments[0], menu, "_lista");
    }

    [Fact]
    public void Abrir_instala_a_vigia_do_foco_uma_vez_com_o_involucro_e_o_menu()
    {
        var modulo = JSInterop.SetupModule(Modulo);
        modulo.SetupVoid("vigiarSaida", _ => true).SetVoidResult();
        var menu = Menu();

        Botao(menu).Click();
        Botao(menu).KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Botao(menu).Click();

        // Uma instalação só, na primeira abertura, com o invólucro inteiro (botão, fundo e lista) e a referência do menu.
        var instalacao = modulo.VerifyInvoke("vigiarSaida");
        DeveSerOElemento(instalacao.Arguments[0], menu, "_involucro");
        instalacao.Arguments[1].Should().BeOfType<DotNetObjectReference<MenuDeAcoes>>()
            .Which.Value.Should().BeSameAs(menu.Instance);
        DeveEstarAberto(menu);
    }

    [Fact]
    public void Tocar_fora_fecha_e_devolve_o_foco_ao_botao()
    {
        var menu = Aberto();

        menu.Find(".aj-menu-acoes__fundo").Click();

        DeveEstarFechado(menu);
        FocoDevolvidoAoBotao(menu);
    }

    [Fact]
    public void Escolher_um_item_fecha_sem_pedir_o_foco()
    {
        var menu = Aberto();

        menu.Find(".aj-menu-acoes__item--perigo").Click();

        DeveEstarFechado(menu);
        // Só o pedido de foco da abertura: para onde o foco vai, decide o que a ação abre.
        DeveSerOElemento(JSInterop.VerifyFocusAsyncInvoke().Arguments[0], menu, "_lista");
    }

    [Fact]
    public void Abrir_um_segundo_menu_fecha_o_primeiro_sem_mexer_no_foco_dele()
    {
        var primeiro = Aberto("Ações de modelo.pdf");
        var segundo = Menu("Ações de contrato.pdf");

        Botao(segundo).Click();

        DeveEstarFechado(primeiro);
        DeveEstarAberto(segundo);
        // O primeiro fecha sem pedir foco: o segundo pedido vai para a lista do segundo menu.
        var focos = JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2);
        DeveSerOElemento(focos[0].Arguments[0], primeiro, "_lista");
        DeveSerOElemento(focos[1].Arguments[0], segundo, "_lista");
    }
}
