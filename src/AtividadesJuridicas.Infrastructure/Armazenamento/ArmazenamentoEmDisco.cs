using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;

namespace AtividadesJuridicas.Infrastructure.Armazenamento;

/// <summary>
/// Guarda cada arquivo em <c>pasta/aaaa/mm/guid</c>. A chave nunca vem do usuário e só é aceita
/// no formato que este armazenamento gera, o que impede sair da pasta. Com <paramref name="criarPastaRaiz"/>
/// falso a pasta raiz precisa já existir: uma raiz ausente fora do desenvolvimento é erro de implantação,
/// e recriá-la esconderia a perda de todos os arquivos.
/// </summary>
public sealed partial class ArmazenamentoEmDisco(string pasta, IRelogio relogio, bool criarPastaRaiz) : IArmazenamentoDeArquivos
{
    private const int TamanhoDoBuffer = 81920;

    private readonly string _raiz = PrepararRaiz(pasta, criarPastaRaiz);

    private static string PrepararRaiz(string pasta, bool criar)
    {
        var raiz = Path.GetFullPath(pasta);
        if (criar) Directory.CreateDirectory(raiz);
        else ExigirRaiz(raiz, null);
        return raiz;
    }

    /// <summary>Confirma que dá para gravar na raiz: cria e apaga um arquivo temporário dentro dela.</summary>
    public void VerificarGravacao()
    {
        ExigirRaiz(_raiz, null);
        var sonda = Path.Combine(_raiz, $".sonda-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(sonda, [0]);
            File.Delete(sonda);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"Não é possível gravar na pasta de armazenamento '{_raiz}'.", ex);
        }
    }

    [GeneratedRegex(@"^[0-9]{4}/[0-9]{2}/[0-9a-f]{32}\z")]
    private static partial Regex FormatoDaChave();

    public async Task<ArquivoArmazenado> GravarAsync(Stream conteudo, long limiteBytes, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conteudo);

        var agora = relogio.AgoraUtc;
        var chave = $"{agora.ToString("yyyy", CultureInfo.InvariantCulture)}/{agora.ToString("MM", CultureInfo.InvariantCulture)}/{Guid.NewGuid():N}";
        var destino = Caminho(chave);
        var temporario = destino + ".tmp";
        ExigirRaiz(_raiz, null);
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

            var hashHex = Convert.ToHexStringLower(hash.GetHashAndReset());

            // Só aparece com o nome definitivo depois de completo e quando nada mais pode falhar.
            File.Move(temporario, destino);
            return new ArquivoArmazenado(chave, total, hashHex);
        }
        catch
        {
            try
            {
                File.Delete(temporario);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A exceção original é a que importa.
            }

            throw;
        }
    }

    public async Task<Stream?> AbrirAsync(string chave, CancellationToken ct)
    {
        var caminho = Caminho(chave);
        try
        {
            return new FileStream(
                caminho, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, TamanhoDoBuffer, useAsync: true);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            ExigirRaiz(_raiz, ex);
            return null;
        }
    }

    public async Task RemoverAsync(string chave, CancellationToken ct)
    {
        var caminho = Caminho(chave);
        try
        {
            File.Delete(caminho);
        }
        catch (DirectoryNotFoundException ex)
        {
            // Sem a pasta do mês o arquivo não existe, e remover o que não existe não é erro.
            ExigirRaiz(_raiz, ex);
        }
    }

    /// <summary>Arquivo ou pasta do mês ausentes são "não existe"; a raiz ausente é um problema de configuração.</summary>
    private static void ExigirRaiz(string raiz, Exception? causa)
    {
        if (!Directory.Exists(raiz))
            throw new IOException($"A pasta de armazenamento '{raiz}' não está acessível.", causa);
    }

    private string Caminho(string chave)
    {
        if (chave is null || !FormatoDaChave().IsMatch(chave))
            throw new ArgumentException("Chave de armazenamento fora do formato.", nameof(chave));

        return Path.Combine(_raiz, chave.Replace('/', Path.DirectorySeparatorChar));
    }
}
