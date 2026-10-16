using AngleSharp.Dom;
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;
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

    // Cópias dos elementos do menu tiradas logo depois de desenhar, antes de qualquer clique. O bUnit escreve o id da
    // referência (blazor:elementReference) a partir dos quadros da última renderização, e num elemento que sobrevive a uma
    // renderização o quadro novo não leva o id: o HTML de depois do clique sai com o atributo vazio, e o próprio elemento
    // achado antes é atualizado no lugar (perde o id). A cópia guarda o atributo da primeira renderização, com o id real, e a
    // referência do componente não muda.
    private sealed record MenuAberto(IRenderedComponent<MenuDeAcoes> Menu, IElement Involucro, IElement Botao, IElement Lista);

    private static IElement Copia(IElement elemento) => (IElement)elemento.Clone(deep: false);

    private MenuAberto Guardado(string rotulo = "Ações de modelo.pdf")
    {
        var menu = Menu(rotulo);
        return new MenuAberto(menu, Copia(menu.Find(".aj-menu-acoes")), Copia(Botao(menu)), Copia(Lista(menu)));
    }

    /// <summary>Abre pelo clique: o menu aberto e o foco pedido para a lista, o primeiro pedido de foco do teste.</summary>
    private MenuAberto Aberto(string rotulo = "Ações de modelo.pdf")
    {
        var guardado = Guardado(rotulo);
        Botao(guardado.Menu).Click();

        DeveEstarAberto(guardado.Menu);
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(guardado.Lista);
        return guardado;
    }

    /// <summary>Dois pedidos de foco no teste: a lista, ao abrir, e depois o botão do menu.</summary>
    private void FocoDevolvidoAoBotao(MenuAberto aberto)
    {
        var focos = JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2);
        focos[0].Arguments[0].ShouldBeElementReferenceTo(aberto.Lista);
        focos[1].Arguments[0].ShouldBeElementReferenceTo(aberto.Botao);
    }

    [Theory]
    [InlineData(".aj-menu-acoes > button[aria-expanded]")]   // o botão
    [InlineData(".aj-menu-acoes__lista")]                    // a lista, que recebe o foco ao abrir
    [InlineData(".aj-menu-acoes__item--perigo")]             // um item da lista
    public void Esc_fecha_com_o_foco_no_botao_ou_na_lista_e_devolve_o_foco_ao_botao(string ondeEstaOFoco)
    {
        var aberto = Aberto();

        // A tecla sobe do elemento focado até o invólucro do menu, como no navegador.
        aberto.Menu.Find(ondeEstaOFoco).KeyDown(new KeyboardEventArgs { Key = "Escape" });

        DeveEstarFechado(aberto.Menu);
        FocoDevolvidoAoBotao(aberto);
    }

    [Fact]
    public async Task O_foco_que_sai_do_menu_fecha_sem_pedir_foco()
    {
        var aberto = Aberto();

        // O que o focusout do módulo faz quando o foco vai para fora do invólucro (Tab no último item, Shift+Tab no botão).
        await aberto.Menu.Instance.FocoSaiuAsync();

        DeveEstarFechado(aberto.Menu);
        // Só o pedido de foco da abertura: o Tab segue o caminho natural.
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(aberto.Lista);
    }

    [Fact]
    public void Abrir_instala_a_vigia_do_foco_uma_vez_com_o_involucro_e_o_menu()
    {
        var modulo = JSInterop.SetupModule(Modulo);
        modulo.SetupVoid("vigiarSaida", _ => true).SetVoidResult();
        var guardado = Guardado();
        var menu = guardado.Menu;

        Botao(menu).Click();
        Botao(menu).KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Botao(menu).Click();

        // Uma instalação só, na primeira abertura, com o invólucro inteiro (botão, fundo e lista) e a referência do menu.
        var instalacao = modulo.VerifyInvoke("vigiarSaida");
        instalacao.Arguments[0].ShouldBeElementReferenceTo(guardado.Involucro);
        instalacao.Arguments[1].Should().BeOfType<DotNetObjectReference<MenuDeAcoes>>()
            .Which.Value.Should().BeSameAs(menu.Instance);
        DeveEstarAberto(menu);
    }

    [Fact]
    public void Tocar_fora_fecha_e_devolve_o_foco_ao_botao()
    {
        var aberto = Aberto();

        aberto.Menu.Find(".aj-menu-acoes__fundo").Click();

        DeveEstarFechado(aberto.Menu);
        FocoDevolvidoAoBotao(aberto);
    }

    [Fact]
    public void Escolher_um_item_fecha_sem_pedir_o_foco()
    {
        var aberto = Aberto();

        aberto.Menu.Find(".aj-menu-acoes__item--perigo").Click();

        DeveEstarFechado(aberto.Menu);
        // Só o pedido de foco da abertura: para onde o foco vai, decide o que a ação abre.
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(aberto.Lista);
    }

    [Fact]
    public void Abrir_um_segundo_menu_fecha_o_primeiro_sem_mexer_no_foco_dele()
    {
        var primeiro = Aberto("Ações de modelo.pdf");
        var segundo = Guardado("Ações de contrato.pdf");
        Botao(segundo.Menu).Click();

        DeveEstarFechado(primeiro.Menu);
        DeveEstarAberto(segundo.Menu);
        // O primeiro fecha sem pedir foco: o segundo pedido vai para a lista do segundo menu.
        var focos = JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2);
        focos[0].Arguments[0].ShouldBeElementReferenceTo(primeiro.Lista);
        focos[1].Arguments[0].ShouldBeElementReferenceTo(segundo.Lista);
    }
}
