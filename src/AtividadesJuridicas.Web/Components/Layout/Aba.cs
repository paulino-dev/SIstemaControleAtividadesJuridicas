namespace AtividadesJuridicas.Web.Components.Layout;

/// <summary>Uma aba da ficha: a chave que a tela guarda, o rótulo e, quando houver, a contagem ao lado dele.</summary>
public sealed record Aba(string Chave, string Rotulo, int? Contador = null);
