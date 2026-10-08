using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;

namespace AtividadesJuridicas.Infrastructure.Armazenamento;

/// <summary>
/// Guarda cada arquivo em <c>pasta/aaaa/mm/guid</c>. A chave nunca vem do usuário e só é aceita
/// no formato que este armazenamento gera, o que impede sair da pasta.
/// </summary>
public sealed partial class ArmazenamentoEmDisco(string pasta, IRelogio relogio) : IArmazenamentoDeArquivos
{
    private const int TamanhoDoBuffer = 81920;

    private readonly string _raiz = Path.GetFullPath(pasta);

    [GeneratedRegex(@"^\d{4}/\d{2}/[0-9a-f]{32}$")]
    private static partial Regex FormatoDaChave();

    public async Task<ArquivoArmazenado> GravarAsync(Stream conteudo, long limiteBytes, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conteudo);

        var agora = relogio.AgoraUtc;
        var chave = $"{agora:yyyy}/{agora:MM}/{Guid.NewGuid():N}";
        var destino = Caminho(chave);
        var temporario = destino + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);

        try
        {
            long total = 0;
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            await using (var arquivo = new FileStream(
                temporario, FileMode.CreateNew, FileAccess.Write, FileShare.None, TamanhoDoBuffer, useAsync: true))
            {
                var buffer = new byte[TamanhoDoBuffer];
                int lidos;
                while ((lidos = await conteudo.ReadAsync(buffer, ct)) > 0)
                {
                    total += lidos;
                    if (total > limiteBytes) throw new ExcecaoDeArquivoGrandeDemais(limiteBytes);

                    hash.AppendData(buffer, 0, lidos);
                    await arquivo.WriteAsync(buffer.AsMemory(0, lidos), ct);
                }
            }

            // Só aparece com o nome definitivo depois de completo.
            File.Move(temporario, destino);
            return new ArquivoArmazenado(chave, total, Convert.ToHexStringLower(hash.GetHashAndReset()));
        }
        catch
        {
            File.Delete(temporario);
            throw;
        }
    }

    public Task<Stream?> AbrirAsync(string chave, CancellationToken ct)
    {
        var caminho = Caminho(chave);
        return Task.FromResult<Stream?>(File.Exists(caminho)
            ? new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read, TamanhoDoBuffer, useAsync: true)
            : null);
    }

    public Task RemoverAsync(string chave, CancellationToken ct)
    {
        try
        {
            File.Delete(Caminho(chave));
        }
        catch (DirectoryNotFoundException)
        {
            // A pasta do mês nunca foi criada: o arquivo não existe, e remover o que não existe não é erro.
        }

        return Task.CompletedTask;
    }

    private string Caminho(string chave)
    {
        if (chave is null || !FormatoDaChave().IsMatch(chave))
            throw new ArgumentException("Chave de armazenamento fora do formato.", nameof(chave));

        return Path.Combine(_raiz, chave.Replace('/', Path.DirectorySeparatorChar));
    }
}
