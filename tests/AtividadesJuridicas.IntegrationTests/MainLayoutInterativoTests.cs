using System.Reflection;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O layout desenhado no bUnit: o menu aberto por "Mais" (o resto inerte, o foco para dentro e de volta, Esc), o
/// "Exibir/Esconder menu" levando o foco ao par, o botão flutuante no fim do conteúdo, o ator que a
/// pré-renderização entrega ao circuito e o nome inteiro no bloco do usuário. O foco é conferido pelo pedido (a
/// chamada ao módulo de foco com o seletor); que ele chega, a contenção do Tab e a aparência ficam para o navegador.
/// </summary>
public class MainLayoutInterativoTests : TelaInterativa
{
    // O módulo do próprio layout: a vigia que fecha o menu aberto quando a janela chega ao desktop.
    private const string ModuloDoLayout = "./Components/Layout/MainLayout.razor.js";

    // A chave com que o layout grava o ator no estado que a pré-renderização entrega ao circuito.
    private const string ChaveDoAtor = "ator-do-layout";

    // Os seletores que o layout pede ao módulo de foco (o primeiro visível que casa).
    private const string PrimeiroItem = "#menu-principal .aj-menu__item";
    private const string Mais = "button[aria-controls=\"menu-principal\"]";
    private const string ExibirMenu = ".aj-exibir-menu > button, .aj-cabecalho__menu > button";
    private const string EsconderMenu = ".aj-menu__esconder";

    private readonly BunitJSModuleInterop _moduloDoLayout;
    private readonly BunitPersistentComponentState _estado;

    public MainLayoutInterativoTests()
    {
        Services.AddSingleton<AntiforgeryStateProvider>(new SemAntiforgery());
        AddAuthorization().SetAuthorized("Diana Diretora");
        _estado = AddBunitPersistentComponentState();
        _moduloDoLayout = JSInterop.SetupModule(ModuloDoLayout);
        // O ator do circuito, quando o layout consulta. O teste que precisa de outro combina antes de desenhar.
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria);
    }

    private static readonly RenderFragment Conteudo = b =>
        b.AddMarkupContent(0, "<section class=\"aj-painel\"><h1 class=\"aj-titulo\">Clientes</h1></section>");

    // Uma tela com cabeçalho de verdade: o "Exibir menu" da linha do título e o botão flutuante.
    private static readonly RenderFragment TelaComCabecalho = b =>
    {
        b.OpenComponent<CabecalhoDePagina>(0);
        b.AddComponentParameter(1, nameof(CabecalhoDePagina.Titulo), "Clientes");
        b.AddComponentParameter(2, nameof(CabecalhoDePagina.Acao), "Novo cliente");
        b.AddComponentParameter(3, nameof(CabecalhoDePagina.AcaoHref), "/clientes/novo");
        b.CloseComponent();
        b.AddMarkupContent(4, "<section class=\"aj-painel\"><p class=\"aj-ajuda\">A lista de clientes.</p></section>");
    };

    private NavigationManager Navegacao => Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<MainLayout> Layout(string endereco, RenderFragment? corpo = null)
    {
        Navegacao.NavigateTo(endereco);
        return Desenhar<MainLayout>(p => p.Add(l => l.Body, corpo ?? Conteudo));
    }

    private static void AbrirPeloMais(IRenderedComponent<MainLayout> tela, string barra = ".aj-trilho") =>
        tela.Find($"{barra} button[aria-controls='menu-principal']").Click();

    private static bool Inerte(IRenderedComponent<MainLayout> tela, string seletor) => tela.Find(seletor).HasAttribute("inert");

    // ---- Menu aberto por "Mais" (tablet e mobile) ----

    [Fact]
    public void Menu_fechado_nao_deixa_nada_inerte_nem_pede_foco()
    {
        var tela = Layout("/clientes");

        tela.Find("main.aj-app__conteudo h1").TextContent.Should().Be("Clientes");
        Inerte(tela, "aside.aj-menu").Should().BeFalse();
        Inerte(tela, "nav.aj-trilho").Should().BeFalse();
        Inerte(tela, "div.aj-app__coluna").Should().BeFalse();
        Inerte(tela, "nav.aj-nav-inferior").Should().BeFalse();
        SeletoresFocados.Should().BeEmpty();
    }

    [Theory]
    [InlineData(".aj-trilho")]
    [InlineData(".aj-nav-inferior")]
    public void Abrir_pelo_mais_deixa_o_resto_inerte_e_pede_o_foco_ao_primeiro_item_do_menu(string barra)
    {
        var tela = Layout("/clientes");

        AbrirPeloMais(tela, barra);

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app--menu-aberto");
        Inerte(tela, "nav.aj-trilho").Should().BeTrue();
        Inerte(tela, "div.aj-app__coluna").Should().BeTrue();
        Inerte(tela, "nav.aj-nav-inferior").Should().BeTrue();
        Inerte(tela, "aside.aj-menu").Should().BeFalse();
        // Fora das partes inertes sobra um só nav "Principal": o do menu.
        tela.FindAll("nav[aria-label='Principal']").Where(n => n.Closest("[inert]") is null)
            .Should().ContainSingle().Which.ParentElement!.Id.Should().Be("menu-principal");
        SeletoresFocados.Should().Equal(PrimeiroItem);
    }

    [Fact]
    public void Esc_no_menu_aberto_fecha_e_pede_o_foco_ao_mais()
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);

        tela.Find("aside.aj-menu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app").And.NotContain("aj-app--menu-aberto");
        Inerte(tela, "nav.aj-trilho").Should().BeFalse();
        Inerte(tela, "div.aj-app__coluna").Should().BeFalse();
        Inerte(tela, "nav.aj-nav-inferior").Should().BeFalse();
        tela.Find(".aj-trilho button[aria-controls='menu-principal']").GetAttribute("aria-expanded").Should().Be("false");
        SeletoresFocados.Should().Equal(PrimeiroItem, Mais);
    }

    [Fact]
    public void Outra_tecla_no_menu_aberto_nao_fecha()
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);

        tela.Find("aside.aj-menu").KeyDown(new KeyboardEventArgs { Key = "Tab" });

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app--menu-aberto");
        Inerte(tela, "div.aj-app__coluna").Should().BeTrue();
        SeletoresFocados.Should().Equal(PrimeiroItem);
    }

    // No desktop o menu lateral está sempre à vista: Esc nele não fecha nada nem move o foco.
    [Fact]
    public void Esc_no_menu_lateral_fechado_nao_pede_foco()
    {
        var tela = Layout("/clientes");

        tela.Find("aside.aj-menu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app").And.NotContain("aj-app--menu-aberto");
        SeletoresFocados.Should().BeEmpty();
    }

    [Theory]
    [InlineData("button.aj-app__veu")]
    [InlineData("button.aj-menu__fechar")]
    public void Fechar_pelo_veu_ou_pelo_botao_do_menu_pede_o_foco_ao_mais(string fechar)
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);

        tela.Find(fechar).Click();

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app").And.NotContain("aj-app--menu-aberto");
        Inerte(tela, "div.aj-app__coluna").Should().BeFalse();
        SeletoresFocados.Should().Equal(PrimeiroItem, Mais);
    }

    // Escolher no menu a tela em que já se está não troca de página (o FocoNoTitulo não age): o menu some e o foco
    // volta ao "Mais".
    [Fact]
    public void Escolher_no_menu_a_tela_em_que_ja_esta_fecha_e_pede_o_foco_ao_mais()
    {
        var tela = Layout("/clientes?busca=maria");
        AbrirPeloMais(tela);

        Navegacao.NavigateTo("/clientes");

        tela.WaitForAssertion(() => tela.Find("div.aj-app").ClassList.Should().NotContain("aj-app--menu-aberto"));
        SeletoresFocados.Should().Equal(PrimeiroItem, Mais);
    }

    // Outra tela: o foco vai ao título dela pelo FocoNoTitulo; o layout não pede o "Mais".
    [Fact]
    public void Escolher_outra_tela_no_menu_fecha_sem_pedir_o_foco_ao_mais()
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);

        Navegacao.NavigateTo("/processos");

        tela.WaitForAssertion(() => tela.Find("div.aj-app").ClassList.Should().NotContain("aj-app--menu-aberto"));
        Inerte(tela, "div.aj-app__coluna").Should().BeFalse();
        SeletoresFocados.Should().Equal(PrimeiroItem);
    }

    // ---- Menu aberto e a janela que chega ao desktop ----

    // A referência que o layout entregou à vigia do desktop (a mesma que o JavaScript usa para chamá-lo).
    private DotNetObjectReference<MainLayout> ReferenciaDaVigia() =>
        (DotNetObjectReference<MainLayout>)_moduloDoLayout.VerifyInvoke("vigiarDesktop").Arguments[0]!;

    [Fact]
    public void Primeiro_desenho_instala_a_vigia_do_desktop_uma_vez_com_o_proprio_layout()
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);
        tela.Find("button.aj-menu__fechar").Click();

        // Três desenhos (o primeiro, o de abrir e o de fechar), uma instalação só, com a referência ao layout.
        ReferenciaDaVigia().Value.Should().BeSameAs(tela.Instance);
        var metodo = typeof(MainLayout).GetMethod("AoChegarAoDesktop");
        metodo.Should().NotBeNull();
        metodo!.GetCustomAttribute<JSInvokableAttribute>().Should().NotBeNull();
    }

    [Fact]
    public async Task Chegar_ao_desktop_com_o_menu_aberto_fecha_e_tira_o_inert_sem_mover_o_foco()
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);
        Inerte(tela, "div.aj-app__coluna").Should().BeTrue();

        await TelaDeTeste.ChamarAsync(ReferenciaDaVigia().Value, "AoChegarAoDesktop");

        tela.WaitForAssertion(() => tela.Find("div.aj-app").ClassList.Should().NotContain("aj-app--menu-aberto"));
        Inerte(tela, "nav.aj-trilho").Should().BeFalse();
        Inerte(tela, "div.aj-app__coluna").Should().BeFalse();
        Inerte(tela, "nav.aj-nav-inferior").Should().BeFalse();
        tela.Find(".aj-trilho button[aria-controls='menu-principal']").GetAttribute("aria-expanded").Should().Be("false");
        // Só o foco de quando abriu: no desktop o "Mais" não está à vista, e o foco não é movido.
        SeletoresFocados.Should().Equal(PrimeiroItem);
    }

    [Fact]
    public async Task Chegar_ao_desktop_com_o_menu_fechado_nao_muda_nada()
    {
        var tela = Layout("/clientes");
        tela.Find("button.aj-menu__esconder").Click();

        await TelaDeTeste.ChamarAsync(ReferenciaDaVigia().Value, "AoChegarAoDesktop");

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app--menu-escondido").And.NotContain("aj-app--menu-aberto");
        SeletoresFocados.Should().Equal(ExibirMenu);
    }

    [Fact]
    public async Task Descartar_o_layout_libera_a_referencia_da_vigia()
    {
        Layout("/clientes");
        var referencia = ReferenciaDaVigia();
        referencia.Value.Should().BeOfType<MainLayout>();

        await DisposeComponentsAsync();

        referencia.Invoking(r => r.Value).Should().Throw<ObjectDisposedException>();
    }

    // ---- "Mais": aria-current só pela rota; aberto é aria-expanded ----

    [Fact]
    public void Com_o_menu_aberto_o_mais_fica_so_com_o_expanded_e_o_destino_da_rota_continua_marcado()
    {
        var tela = Layout("/atividades");

        AbrirPeloMais(tela);

        foreach (var barra in new[] { ".aj-trilho", ".aj-nav-inferior" })
        {
            var mais = tela.Find($"{barra} button[aria-controls='menu-principal']");
            mais.GetAttribute("aria-expanded").Should().Be("true", barra);
            mais.HasAttribute("aria-current").Should().BeFalse(barra);
            tela.Find($"{barra} a[href='/atividades']").GetAttribute("aria-current").Should().Be("page", barra);
        }
    }

    [Fact]
    public void Com_o_menu_aberto_numa_tela_do_mais_ele_continua_marcado()
    {
        var tela = Layout("/areas");

        AbrirPeloMais(tela);

        foreach (var barra in new[] { ".aj-trilho", ".aj-nav-inferior" })
        {
            var mais = tela.Find($"{barra} button[aria-controls='menu-principal']");
            mais.GetAttribute("aria-expanded").Should().Be("true", barra);
            mais.GetAttribute("aria-current").Should().Be("page", barra);
        }
    }

    // ---- "Exibir menu" e "Esconder menu" (desktop) ----

    [Fact]
    public void Esconder_menu_pede_o_foco_ao_exibir_menu_e_exibir_menu_o_devolve_ao_esconder()
    {
        var tela = Layout("/clientes");

        tela.Find("button.aj-menu__esconder").Click();

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app--menu-escondido");
        SeletoresFocados.Should().Equal(ExibirMenu);

        tela.Find(".aj-exibir-menu > button").Click();

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app").And.NotContain("aj-app--menu-escondido");
        SeletoresFocados.Should().Equal(ExibirMenu, EsconderMenu);
    }

    [Fact]
    public void Exibir_menu_do_cabecalho_da_tela_tambem_devolve_o_foco_ao_esconder_menu()
    {
        var tela = Layout("/clientes", TelaComCabecalho);
        tela.Find("button.aj-menu__esconder").Click();

        tela.Find(".aj-cabecalho__menu > button").Click();

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app").And.NotContain("aj-app--menu-escondido");
        SeletoresFocados.Should().Equal(ExibirMenu, EsconderMenu);
    }

    // ---- Botão flutuante no fim do conteúdo ----

    [Fact]
    public void Botao_flutuante_do_cabecalho_sai_no_fim_do_conteudo()
    {
        var tela = Layout("/clientes", TelaComCabecalho);

        var conteudo = tela.Find("main.aj-app__conteudo");
        conteudo.QuerySelector("section.aj-painel")!.TextContent.Should().Be("A lista de clientes.");
        var ultimo = conteudo.LastElementChild!;
        ultimo.ClassName.Should().Be("aj-fab");
        ultimo.GetAttribute("href").Should().Be("/clientes/novo");
        tela.FindAll(".aj-fab").Should().ContainSingle();
    }

    // ---- Ator da pré-renderização no circuito ----

    [Fact]
    public void Ator_gravado_na_pre_renderizacao_chega_ao_circuito_sem_nova_consulta()
    {
        _estado.Persist(ChaveDoAtor, AtorDaDiretoria);
        // Se o layout consultasse, viria outra pessoa: o nome na tela diz de onde o ator veio.
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao);

        var tela = Layout("/clientes");

        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().BeEmpty();
        tela.Find(".aj-usuario__texto b").TextContent.Should().Be("Diana Diretora");
        tela.FindAll(".aj-menu__grupo > span").Select(s => s.TextContent).Should().Contain("Administração");
    }

    [Fact]
    public void Sem_ator_gravado_o_layout_consulta_e_grava_o_ator_para_o_circuito()
    {
        var tela = Layout("/clientes");

        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().ContainSingle();
        tela.Find(".aj-usuario__texto b").TextContent.Should().Be("Diana Diretora");

        _estado.TriggerOnPersisting();

        _estado.TryTake<UsuarioAutenticado>(ChaveDoAtor, out var gravado).Should().BeTrue();
        gravado.Should().Be(AtorDaDiretoria);
    }

    // Acesso encerrado na pré-renderização: nada vai ao circuito, que consulta de novo (o comportamento de antes).
    [Fact]
    public void Acesso_encerrado_nao_grava_ator_nem_poe_o_title()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>((UsuarioAutenticado?)null);

        var tela = Layout("/clientes");

        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().ContainSingle();
        tela.Find("div.aj-usuario__texto").HasAttribute("title").Should().BeFalse();

        _estado.TriggerOnPersisting();

        _estado.TryTake<UsuarioAutenticado>(ChaveDoAtor, out _).Should().BeFalse();
    }

    // ---- Nome longo ----

    [Fact]
    public void Bloco_do_usuario_leva_o_nome_inteiro_no_title()
    {
        const string nome = "Maria Aparecida dos Santos Albuquerque Cavalcanti";
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao with { Nome = nome });

        var tela = Layout("/clientes");

        tela.Find("div.aj-usuario__texto").GetAttribute("title").Should().Be(nome);
        tela.Find(".aj-usuario__texto b").TextContent.Should().Be(nome);
        // O title fica no texto, não no bloco: o botão "Sair" não ganha o nome como dica.
        tela.Find("div.aj-usuario").HasAttribute("title").Should().BeFalse();
    }

    // O formulário de sair do bloco do usuário tem o AntiforgeryToken; no teste não há token a emitir.
    private sealed class SemAntiforgery : AntiforgeryStateProvider
    {
        public override AntiforgeryRequestToken? GetAntiforgeryToken() => null;
    }
}
