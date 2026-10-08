using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>
/// O que todo envio de documento tem em comum: validar o que dá para recusar antes de gravar, gravar o arquivo
/// primeiro e o registro depois, e remover o arquivo em qualquer saída sem registro no banco.
/// </summary>
internal static class ArquivoEnviado
{
    private const long UmMegabyte = 1024 * 1024;

    /// <summary>
    /// Valida o arquivo e o nome do documento. Devolve a mensagem de recusa ou nulo, e o nome do arquivo
    /// reduzido ao último segmento do caminho informado.
    /// </summary>
    public static string? Validar(string? nomeArquivo, Stream? conteudo, string? nome, out string nomeDoArquivo)
    {
        nomeDoArquivo = SoONome(nomeArquivo);
        if (nomeDoArquivo.Length == 0 || conteudo is null)
            return "Informe o arquivo.";

        // O domínio também valida, mas só depois de o arquivo estar gravado: o que dá para recusar, recusa-se antes.
        if (nomeDoArquivo.Length > Documento.TamanhoMaximoNome)
            return $"O nome do arquivo deve ter até {Documento.TamanhoMaximoNome} caracteres.";
        if (nomeDoArquivo.Any(char.IsControl))
            return "O nome do arquivo é inválido.";

        if (nome is not null)
        {
            if (nome.Trim().Length > Documento.TamanhoMaximoNome)
                return $"O nome do documento deve ter até {Documento.TamanhoMaximoNome} caracteres.";
            if (nome.Any(char.IsControl))
                return "O nome do documento é inválido.";
        }

        return null;
    }

    /// <summary>
    /// Grava o arquivo, cria o documento com <paramref name="criar"/>, grava no banco e devolve o Id. Qualquer
    /// saída sem registro no banco remove o arquivo.
    /// </summary>
    public static async Task<Resultado<Guid>> GravarAsync(
        IArmazenamentoDeArquivos armazenamento, IDocumentoRepositorio documentos, IUnidadeDeTrabalho unidade,
        string nomeDoArquivo, string? tipoConteudo, Stream conteudo,
        Func<ArquivoDoDocumento, Task<Documento>> criar, CancellationToken ct)
    {
        ArquivoArmazenado gravado;
        try
        {
            gravado = await armazenamento.GravarAsync(conteudo, DocumentosOpcoes.TamanhoMaximoBytes, ct);
        }
        catch (ExcecaoDeArquivoGrandeDemais)
        {
            return Falha(TipoErro.Validacao,
                $"O arquivo passa do limite de {DocumentosOpcoes.TamanhoMaximoBytes / UmMegabyte} MB.");
        }

        // Daqui em diante o arquivo já está gravado: qualquer saída sem registro no banco o remove.
        try
        {
            var arquivo = new ArquivoDoDocumento(
                nomeDoArquivo, TipoDe(tipoConteudo), gravado.TamanhoBytes, gravado.HashSha256, gravado.Chave);

            var documento = await criar(arquivo);

            documentos.Adicionar(documento);
            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(documento.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            await TentarRemoverAsync(armazenamento, gravado.Chave);
            return Falha(TipoErro.Validacao, ex.Message);
        }
        catch
        {
            await TentarRemoverAsync(armazenamento, gravado.Chave);
            throw;
        }
    }

    /// <summary>A limpeza nunca pode trocar o desfecho original: se ela mesma falhar, o arquivo fica órfão e só isso se perde.</summary>
    private static async Task TentarRemoverAsync(IArmazenamentoDeArquivos armazenamento, string chave)
    {
        try
        {
            await armazenamento.RemoverAsync(chave, CancellationToken.None);
        }
        catch (Exception)
        {
            // Sem canal para avisar daqui; a recusa ou a exceção original é o que importa a quem chamou.
        }
    }

    /// <summary>O último segmento do caminho informado, com qualquer um dos separadores; vazio quando não há nome.</summary>
    private static string SoONome(string? caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho)) return string.Empty;

        var corte = caminho.LastIndexOfAny(['/', '\\']);
        var nome = (corte >= 0 ? caminho[(corte + 1)..] : caminho).Trim();
        return nome is "." or ".." ? string.Empty : nome;
    }

    private static string TipoDe(string? tipo) =>
        string.IsNullOrWhiteSpace(tipo) || tipo.Trim().Length > Documento.TamanhoMaximoTipoConteudo || tipo.Any(char.IsControl)
            ? DocumentosOpcoes.TipoDeConteudoPadrao
            : tipo.Trim();

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
