namespace AtividadesJuridicas.Web.Infra;

/// <summary>Regra da pasta onde ficam os arquivos dos documentos.</summary>
public static class PastaDeArquivos
{
    /// <summary>
    /// No desenvolvimento, um caminho relativo vale a partir da raiz do conteúdo. Fora dele o valor é
    /// obrigatório e precisa ser um caminho absoluto, para que uma nova implantação não aponte para
    /// uma pasta vazia dentro da própria aplicação.
    /// </summary>
    public static string Resolver(string? configurado, bool desenvolvimento, string raizDoConteudo)
    {
        if (string.IsNullOrWhiteSpace(configurado))
            throw new InvalidOperationException("Armazenamento:Pasta não configurada.");

        var pasta = configurado.Trim();
        if (desenvolvimento) return Path.GetFullPath(pasta, raizDoConteudo);

        if (!Path.IsPathFullyQualified(pasta))
            throw new InvalidOperationException(
                "Armazenamento:Pasta deve ser um caminho absoluto fora do ambiente de desenvolvimento.");

        return Path.GetFullPath(pasta);
    }
}
