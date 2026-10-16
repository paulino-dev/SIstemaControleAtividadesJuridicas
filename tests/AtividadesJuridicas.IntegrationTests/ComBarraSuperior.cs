using AtividadesJuridicas.Web.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Sections;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Desenha um componente entre os encaixes do layout, como o layout faz: a barra superior antes dele e o fim do
/// conteúdo depois. Sozinho, o componente não mostra a barra do mobile nem o botão flutuante: o
/// <c>CabecalhoDePagina</c> os entrega aos encaixes, e sem encaixe eles não chegam ao HTML. Os parâmetros
/// informados seguem para o componente.
/// </summary>
public sealed class ComBarraSuperior<TComponente> : ComponentBase where TComponente : IComponent
{
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? Parametros { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<SectionOutlet>(0);
        builder.AddComponentParameter(1, nameof(SectionOutlet.SectionName), Encaixes.BarraSuperior);
        builder.CloseComponent();
        builder.OpenComponent<TComponente>(2);
        builder.AddMultipleAttributes(3, Parametros);
        builder.CloseComponent();
        builder.OpenComponent<SectionOutlet>(4);
        builder.AddComponentParameter(5, nameof(SectionOutlet.SectionName), Encaixes.RodapeDoConteudo);
        builder.CloseComponent();
    }
}
