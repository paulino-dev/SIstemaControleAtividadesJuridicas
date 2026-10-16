using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Web.Infra;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Desenha um componente fora da página, até ele terminar de carregar, e devolve o HTML. Serve aos painéis que só
/// abrem por clique e por isso não aparecem no HTML pré-renderizado. Os casos de uso rodam de verdade, contra o banco
/// do teste, como o usuário informado. Os recados entre telas (`Recados`) vêm do teste, para ele deixar um antes de
/// desenhar. O primeiro desenho é verificado por <c>RenderizarAsync</c> e <c>RenderizarComAsync</c>; o que um clique faz,
/// por <c>RenderizarEAgirAsync</c>, que deixa o teste chamar um método da tela (<c>ChamarAsync</c>) e devolve o HTML de depois.
/// </summary>
public static class TelaDeTeste
{
    public static Task<string> RenderizarAsync<TComponente>(
        AplicacaoDeTeste app, Guid usuarioId, Dictionary<string, object?> parametros, Recados? recados = null)
        where TComponente : IComponent =>
        RenderizarComAsync<TComponente>(new DespachanteDeTeste(app, usuarioId), parametros, recados);

    /// <summary>Desenha um componente com o despachante informado: sem aplicação, sem banco.</summary>
    public static async Task<string> RenderizarComAsync<TComponente>(
        IDespachante despachante, Dictionary<string, object?> parametros, Recados? recados = null)
        where TComponente : IComponent
    {
        await using var servicos = Servicos(despachante, recados);
        await using var renderizador = new HtmlRenderer(servicos, NullLoggerFactory.Instance);

        return await renderizador.Dispatcher.InvokeAsync(async () =>
            (await renderizador.RenderComponentAsync<TComponente>(ParameterView.FromDictionary(parametros))).ToHtmlString());
    }

    // Só o que os componentes da aplicação injetam: o despachante, o fuso do escritório, os recados entre telas, o
    // NavigationManager (as telas que navegam depois de salvar), o JavaScript (que o InputFile injeta e o desenho
    // estático nunca chama), o Foco (que as telas chamam depois de desenhar, e o desenho estático também nunca chama)
    // e o menu de ações aberto (que o MenuDeAcoes só usa ao abrir). Um ponto só para os desenhos com despachante.
    private static ServiceProvider Servicos(IDespachante despachante, Recados? recados) => new ServiceCollection()
        .AddSingleton(despachante)
        .AddSingleton(new FusoDoEscritorio("America/Sao_Paulo"))
        .AddSingleton(recados ?? new Recados())
        .AddSingleton<NavigationManager>(new NavegacaoDeTeste())
        .AddSingleton<IJSRuntime>(new SemJavaScript())
        .AddSingleton<Foco>()
        .AddSingleton<MenuDeAcoesAberto>()
        .BuildServiceProvider();

    /// <summary>
    /// Desenha o componente com o despachante informado, deixa o teste agir sobre a instância (o que um clique faria,
    /// por exemplo, chamando um método dela com <c>ChamarAsync</c>) e devolve o HTML de depois da ação.
    /// </summary>
    public static async Task<string> RenderizarEAgirAsync<TComponente>(
        IDespachante despachante, Dictionary<string, object?> parametros, Func<TComponente, Task> agir, Recados? recados = null)
        where TComponente : class, IComponent
    {
        await using var servicos = Servicos(despachante, recados);
        await using var renderizador = new HtmlRenderer(servicos, NullLoggerFactory.Instance);

        return await renderizador.Dispatcher.InvokeAsync(async () =>
        {
            TComponente? instancia = null;
            var raiz = await renderizador.RenderComponentAsync<Hospedeiro<TComponente>>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(Hospedeiro<TComponente>.Parametros)] = parametros,
                    [nameof(Hospedeiro<TComponente>.AoCriar)] = new Action<TComponente>(c => instancia = c),
                }));
            // Como o clique: o componente roda o método e se desenha de novo ao terminar.
            await ((IHandleEvent)instancia!).HandleEventAsync(new EventCallbackWorkItem(() => agir(instancia!)), null);
            await raiz.QuiescenceTask;
            return raiz.ToHtmlString();
        });
    }

    /// <summary>
    /// Chama um método (privado ou não) da tela, como um clique faria. Método que a tela não tem é erro do teste, dito com
    /// o nome do método e o da tela.
    /// </summary>
    public static async Task ChamarAsync<TComponente>(TComponente tela, string metodo, params object?[] argumentos)
        where TComponente : class, IComponent
    {
        var alvo = typeof(TComponente).GetMethod(metodo, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"O método {metodo} não existe na tela {typeof(TComponente).Name}.");
        if (alvo.Invoke(tela, argumentos) is Task tarefa) await tarefa;
    }

    // Expõe a instância do componente desenhado, que o RenderComponentAsync não entrega.
    private sealed class Hospedeiro<TComponente> : ComponentBase where TComponente : class, IComponent
    {
        [Parameter] public Dictionary<string, object?> Parametros { get; set; } = [];
        [Parameter] public Action<TComponente> AoCriar { get; set; } = _ => { };

        protected override void BuildRenderTree(RenderTreeBuilder construtor)
        {
            construtor.OpenComponent<TComponente>(0);
            construtor.AddMultipleAttributes(1, Parametros.Select(p => new KeyValuePair<string, object>(p.Key, p.Value!)));
            construtor.AddComponentReferenceCapture(2, instancia => AoCriar((TComponente)instancia));
            construtor.CloseComponent();
        }
    }

    /// <summary>Desenha um componente que não consulta nada: sem aplicação, sem banco.</summary>
    public static async Task<string> RenderizarSemDadosAsync<TComponente>(Dictionary<string, object?> parametros)
        where TComponente : IComponent
    {
        await using var servicos = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new SemJavaScript())
            .AddSingleton<Foco>()
            .AddSingleton<MenuDeAcoesAberto>()
            .BuildServiceProvider();
        await using var renderizador = new HtmlRenderer(servicos, NullLoggerFactory.Instance);

        return await renderizador.Dispatcher.InvokeAsync(async () =>
            (await renderizador.RenderComponentAsync<TComponente>(ParameterView.FromDictionary(parametros))).ToHtmlString());
    }

    private sealed class DespachanteDeTeste(AplicacaoDeTeste app, Guid usuarioId) : IDespachante
    {
        public Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default) =>
            app.ExecutarAsync(usuarioId, requisicao);
    }

    // Telas que navegam depois de salvar injetam o NavigationManager; o desenho estático só precisa que ele exista.
    private sealed class NavegacaoDeTeste : NavigationManager
    {
        public NavegacaoDeTeste() => Initialize("http://localhost/", "http://localhost/");

        protected override void NavigateToCore(string uri, NavigationOptions options) { }
    }

    private sealed class SemJavaScript : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new InvalidOperationException($"O desenho estático não chama JavaScript ({identifier}).");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new InvalidOperationException($"O desenho estático não chama JavaScript ({identifier}).");
    }
}
