using Microsoft.JSInterop;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// O foco e a rolagem que o teclado e o leitor de tela precisam, num lugar só: chama o módulo
/// <c>Components/Layout/FocoNoTitulo.razor.js</c>, importado uma vez por circuito. O foco é uma ajuda: uma falha do
/// JavaScript (circuito caindo, desenho sem JavaScript) nunca derruba a tela que pediu.
/// </summary>
public sealed class Foco(IJSRuntime js)
{
    /// <summary>O módulo, pelo endereço em que o Blazor o serve.</summary>
    public const string Modulo = "./Components/Layout/FocoNoTitulo.razor.js";

    // Vive enquanto o circuito vive: a referência vai embora com a conexão.
    private IJSObjectReference? _modulo;

    /// <summary>
    /// Foca o primeiro elemento à vista que casa com <paramref name="seletor"/> (o par visível de um botão que tem uma
    /// cópia por visão, o botão que abriu um painel). Nenhum à vista: o foco fica onde está.
    /// </summary>
    public Task FocarPrimeiroVisivelAsync(string seletor) => ChamarAsync("focarPrimeiroVisivel", seletor);

    /// <summary>Foca o título (<c>h1</c>) à vista, sem rolar: é o que o leitor de tela anuncia a cada página.</summary>
    public Task FocarTituloVisivelAsync() => ChamarAsync("focarTituloVisivel");

    /// <summary>Instala a rede de segurança do foco (uma vez por página carregada; chamar de novo não faz nada).</summary>
    public Task VigiarAsync() => ChamarAsync("vigiarFoco");

    /// <summary>Leva a rolagem do conteúdo (<c>main.aj-app__conteudo</c>) ao topo.</summary>
    public Task ZerarRolagemAsync() => ChamarAsync("zerarRolagem");

    private async Task ChamarAsync(string funcao, params object?[] argumentos)
    {
        try
        {
            _modulo ??= await js.InvokeAsync<IJSObjectReference>("import", Modulo);
            await _modulo.InvokeVoidAsync(funcao, argumentos);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException or OperationCanceledException)
        {
            // Sem foco (circuito caindo, JavaScript fora do ar, desenho sem circuito) a tela continua funcional.
        }
    }
}
