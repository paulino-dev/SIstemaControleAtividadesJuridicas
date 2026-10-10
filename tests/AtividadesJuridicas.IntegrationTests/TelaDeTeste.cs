using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Web.Infra;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Desenha um componente fora da página, até ele terminar de carregar, e devolve o HTML. Serve aos painéis que só
/// abrem por clique e por isso não aparecem no HTML pré-renderizado. Os casos de uso rodam de verdade, contra o banco
/// do teste, como o usuário informado. Cliques ficam fora do alcance: só o primeiro desenho é verificado.
/// </summary>
public static class TelaDeTeste
{
    public static async Task<string> RenderizarAsync<TComponente>(
        AplicacaoDeTeste app, Guid usuarioId, Dictionary<string, object?> parametros)
        where TComponente : IComponent
    {
        // Só o que os componentes da aplicação injetam: o despachante e o fuso do escritório.
        await using var servicos = new ServiceCollection()
            .AddSingleton<IDespachante>(new DespachanteDeTeste(app, usuarioId))
            .AddSingleton(new FusoDoEscritorio("America/Sao_Paulo"))
            .BuildServiceProvider();
        await using var renderizador = new HtmlRenderer(servicos, NullLoggerFactory.Instance);

        return await renderizador.Dispatcher.InvokeAsync(async () =>
            (await renderizador.RenderComponentAsync<TComponente>(ParameterView.FromDictionary(parametros))).ToHtmlString());
    }

    /// <summary>Desenha um componente que não consulta nada: sem aplicação, sem banco.</summary>
    public static async Task<string> RenderizarSemDadosAsync<TComponente>(Dictionary<string, object?> parametros)
        where TComponente : IComponent
    {
        await using var servicos = new ServiceCollection().BuildServiceProvider();
        await using var renderizador = new HtmlRenderer(servicos, NullLoggerFactory.Instance);

        return await renderizador.Dispatcher.InvokeAsync(async () =>
            (await renderizador.RenderComponentAsync<TComponente>(ParameterView.FromDictionary(parametros))).ToHtmlString());
    }

    private sealed class DespachanteDeTeste(AplicacaoDeTeste app, Guid usuarioId) : IDespachante
    {
        public Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default) =>
            app.ExecutarAsync(usuarioId, requisicao);
    }
}
