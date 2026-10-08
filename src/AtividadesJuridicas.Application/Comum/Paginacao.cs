namespace AtividadesJuridicas.Application.Comum;

public static class Paginacao
{
    /// <summary>
    /// Mantém o número da página entre 1 e o maior valor cujo deslocamento (página - 1) * tamanho cabe em
    /// um inteiro; uma página absurda devolve lista vazia em vez de estourar e virar um OFFSET negativo.
    /// </summary>
    public static int Normalizar(int pagina, int tamanho) => Math.Clamp(pagina, 1, int.MaxValue / tamanho);
}
