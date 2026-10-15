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
/// escritório e os recados entre telas. O JavaScript fica solto (as chamadas de foco são registradas, não executadas)
/// e o teste confere o foco pelo registro.
/// </summary>
public abstract class TelaInterativa : BunitContext
{
    /// <summary>Os atores que <c>ObterMeuAcesso</c> devolve nos testes: uma diretora e um usuário de Produção.</summary>
    protected static readonly UsuarioAutenticado AtorDaDiretoria = new(
        Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d001"), "Diana Diretora", TipoPerfil.Diretoria, Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e001"));
    protected static readonly UsuarioAutenticado AtorDaProducao = new(
        Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d002"), "Pedro Produção", TipoPerfil.Producao, Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e002"));

    protected DespachanteCombinado Despachante { get; } = new();

    protected TelaInterativa()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IDespachante>(Despachante);
        Services.AddSingleton(new FusoDoEscritorio("America/Sao_Paulo"));
        Services.AddSingleton(new Recados());
    }

    protected IRenderedComponent<T> Desenhar<T>(Action<ComponentParameterCollectionBuilder<T>>? parametros = null)
        where T : IComponent =>
        Render(parametros);
}
