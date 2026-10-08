namespace AtividadesJuridicas.Application.Documentos;

/// <summary><paramref name="HashSha256"/> em hexadecimal minúsculo.</summary>
public sealed record ArquivoArmazenado(string Chave, long TamanhoBytes, string HashSha256);

/// <summary>Onde os arquivos ficam, fora do banco. A chave é sempre gerada pelo armazenamento.</summary>
public interface IArmazenamentoDeArquivos
{
    /// <summary>
    /// Grava o conteúdo e devolve onde ficou, o tamanho e o hash. Lança
    /// <see cref="ExcecaoDeArquivoGrandeDemais"/> quando o conteúdo passa de <paramref name="limiteBytes"/>;
    /// em qualquer falha, nada fica gravado.
    /// </summary>
    Task<ArquivoArmazenado> GravarAsync(Stream conteudo, long limiteBytes, CancellationToken ct);

    /// <summary>Nulo quando o arquivo não existe. Quem chama fecha o fluxo.</summary>
    Task<Stream?> AbrirAsync(string chave, CancellationToken ct);

    /// <summary>Remover o que não existe não é erro.</summary>
    Task RemoverAsync(string chave, CancellationToken ct);
}

public sealed class ExcecaoDeArquivoGrandeDemais(long limiteBytes)
    : Exception($"O arquivo passa do limite de {limiteBytes} bytes.")
{
    public long LimiteBytes { get; } = limiteBytes;
}
