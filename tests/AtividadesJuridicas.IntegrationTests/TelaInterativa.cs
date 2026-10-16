using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Base dos testes que clicam: desenha um componente com o bUnit, com o despachante combinado do teste, o fuso do
/// escritório, os recados entre telas, o serviço de foco e o menu de ações aberto. O JavaScript fica solto (as chamadas
/// de foco são registradas, não executadas) e o teste confere o foco pelo registro: o pedido a um elemento
/// (<see cref="UltimoFocoPedido"/>) ou ao módulo de foco do layout (<see cref="SeletoresFocados"/>).
/// </summary>
public abstract class TelaInterativa : BunitContext
{
    /// <summary>Os atores que <c>ObterMeuAcesso</c> devolve nos testes: uma diretora e um usuário de Produção.</summary>
    protected static readonly UsuarioAutenticado AtorDaDiretoria = new(
        Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d001"), "Diana Diretora", TipoPerfil.Diretoria, Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e001"));
    protected static readonly UsuarioAutenticado AtorDaProducao = new(
        Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d002"), "Pedro Produção", TipoPerfil.Producao, Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e002"));

    protected DespachanteCombinado Despachante { get; } = new();

    /// <summary>
    /// O módulo de foco do layout (<c>FocoNoTitulo.razor.js</c>), no caminho que o serviço <see cref="Foco"/> importa
    /// (<see cref="Foco.Modulo"/>): as chamadas a ele ficam registradas aqui. Um só por teste: um segundo
    /// <c>SetupModule</c> no mesmo caminho dividiria as chamadas entre os dois.
    /// </summary>
    protected BunitJSModuleInterop ModuloDeFoco { get; }

    /// <summary>Os seletores que a tela pediu ao <c>focarPrimeiroVisivel</c> do módulo de foco, na ordem dos pedidos.</summary>
    protected IReadOnlyList<string> SeletoresFocados =>
        [.. ModuloDeFoco.Invocations["focarPrimeiroVisivel"].Select(chamada => (string)chamada.Arguments[0]!)];

    /// <summary>O seletor do último pedido ao <c>focarPrimeiroVisivel</c>; sem pedido nenhum, o teste falha.</summary>
    protected string UltimoSeletorFocado()
    {
        var seletores = SeletoresFocados;
        seletores.Should().NotBeEmpty("a tela devia ter pedido o foco ao primeiro elemento visível de um seletor");
        return seletores[^1];
    }

    /// <summary>
    /// O último pedido de foco a um elemento (<c>ElementReference.FocusAsync</c>, que o Blazor faz por
    /// <c>Blazor._internal.domWrapper.focus</c>, o mesmo que o <c>VerifyFocusAsyncInvoke</c> confere); sem pedido
    /// nenhum, o teste falha.
    /// </summary>
    protected JSRuntimeInvocation UltimoFocoPedido()
    {
        var pedidos = JSInterop.Invocations[FocoDeElemento];
        pedidos.Should().NotBeEmpty("a tela devia ter pedido o foco a um elemento");
        return pedidos[^1];
    }

    /// <summary>Quantos pedidos de foco a um elemento (<c>ElementReference.FocusAsync</c>) a tela fez até agora.</summary>
    protected int FocosDeElementoPedidos => JSInterop.Invocations[FocoDeElemento].Count;

    /// <summary>O que o <c>PainelDeFiltros</c> pede ao abrir: o primeiro campo do form, em ordem de documento.</summary>
    protected static string CamposDoPainel(string id) => $"#{id} input, #{id} select, #{id} textarea";

    /// <summary>O que o <c>PainelDeFiltros</c> pede ao fechar por Esc ou por "Filtrar": o botão que o abre na visão atual.</summary>
    protected static string BotaoDoPainel(string id) => $"button[aria-controls=\"{id}\"]";

    // O identificador com que o Blazor pede o foco a um elemento; o bUnit o registra assim. O indexador de Invocations
    // devolve a lista vazia para um identificador sem chamada (bUnit 2.11.3), e por isso os auxiliares não precisam de
    // tratamento à parte antes do primeiro pedido.
    private const string FocoDeElemento = "Blazor._internal.domWrapper.focus";

    protected TelaInterativa()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ModuloDeFoco = JSInterop.SetupModule(Foco.Modulo);
        Services.AddSingleton<IDespachante>(Despachante);
        Services.AddSingleton(new FusoDoEscritorio("America/Sao_Paulo"));
        Services.AddSingleton(new Recados());
        Services.AddSingleton<Foco>();
        Services.AddSingleton<MenuDeAcoesAberto>();
    }

    protected IRenderedComponent<T> Desenhar<T>(Action<ComponentParameterCollectionBuilder<T>>? parametros = null)
        where T : IComponent =>
        Render(parametros);
}
