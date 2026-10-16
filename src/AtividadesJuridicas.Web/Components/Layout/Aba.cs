namespace AtividadesJuridicas.Web.Components.Layout;

/// <summary>
/// Uma aba da ficha: a chave que a tela guarda, o rótulo, quando houver a contagem ao lado dele e o id do invólucro do
/// painel que ela mostra (vai no aria-controls; mais de um id, separados por espaço, quando a aba mostra dois invólucros).
/// </summary>
public sealed record Aba(string Chave, string Rotulo, int? Contador = null, string? Painel = null);
