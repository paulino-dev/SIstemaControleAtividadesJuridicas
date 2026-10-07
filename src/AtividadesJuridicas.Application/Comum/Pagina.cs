namespace AtividadesJuridicas.Application.Comum;

public sealed record Pagina<T>(IReadOnlyList<T> Itens, int Total, int Numero, int Tamanho)
{
    public int TotalDePaginas => Total == 0 ? 1 : (int)Math.Ceiling(Total / (double)Tamanho);
}
