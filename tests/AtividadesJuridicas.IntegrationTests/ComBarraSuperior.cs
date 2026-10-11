using AtividadesJuridicas.Web.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Sections;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Desenha um componente junto do encaixe da barra superior, como o layout faz. Sozinho, o componente não mostra a
/// barra do mobile: o <c>CabecalhoDePagina</c> a entrega ao encaixe, e sem encaixe ela não chega ao HTML. Os
/// parâmetros informados seguem para o componente.
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
    }
}
