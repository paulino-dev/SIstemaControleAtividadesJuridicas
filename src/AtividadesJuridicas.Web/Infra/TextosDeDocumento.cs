namespace AtividadesJuridicas.Web.Infra;

/// <summary>O endereço de download e a linha de dados de um documento, iguais em todas as listas de documentos.</summary>
public static class TextosDeDocumento
{
    // Link fora da navegação do Blazor: é um download, não uma página. Quem o usa põe "download" e
    // data-enhance-nav="false" no link: é o "download" que impede o roteador de interceptar o clique.
    public static string Endereco(Guid id) => $"/documentos/{id}/baixar";

    public static string Detalhe(long tamanhoBytes, DateTime enviadoEm, string? enviadoPor, FusoDoEscritorio fuso) =>
        string.Join(" · ", new[] { TamanhoDeArquivo.Formatar(tamanhoBytes), fuso.Formatar(enviadoEm), enviadoPor }
            .Where(parte => !string.IsNullOrWhiteSpace(parte)));
}