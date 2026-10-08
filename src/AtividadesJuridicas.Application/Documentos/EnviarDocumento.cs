using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>
/// Envia um documento geral ou um contrato de honorários de um cliente. <paramref name="NomeArquivo"/> é o
/// nome que o navegador informou: só o último segmento é guardado, e ele nunca é usado como caminho.
/// </summary>
public sealed record EnviarDocumento(
    CategoriaDocumento Categoria, Guid ClienteId, string? Nome, string NomeArquivo, string? TipoConteudo, Stream Conteudo)
    : IRequisicao<Resultado<Guid>>;

public sealed class EnviarDocumentoManipulador(
    IContextoDeAcesso contexto,
    IDocumentoRepositorio documentos,
    IClienteRepositorio clientes,
    IPerfilRepositorio perfis,
    IArmazenamentoDeArquivos armazenamento,
    IUnidadeDeTrabalho unidade) : IManipulador<EnviarDocumento, Resultado<Guid>>
{
    private const long UmMegabyte = 1024 * 1024;

    public async Task<Resultado<Guid>> ManipularAsync(EnviarDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (requisicao.Categoria is not (CategoriaDocumento.DocumentoGeral or CategoriaDocumento.ContratoHonorarios))
            return Falha(TipoErro.Validacao, "Categoria inválida para envio.");

        var nomeDoArquivo = SoONome(requisicao.NomeArquivo);
        if (nomeDoArquivo.Length == 0 || requisicao.Conteudo is null)
            return Falha(TipoErro.Validacao, "Informe o arquivo.");

        // O domínio também valida, mas só depois de o arquivo estar gravado: o que dá para recusar, recusa-se antes.
        if (nomeDoArquivo.Length > Documento.TamanhoMaximoNome)
            return Falha(TipoErro.Validacao, $"O nome do arquivo deve ter até {Documento.TamanhoMaximoNome} caracteres.");
        if (nomeDoArquivo.Any(char.IsControl))
            return Falha(TipoErro.Validacao, "O nome do arquivo é inválido.");

        if (requisicao.Nome is { } nome)
        {
            if (nome.Trim().Length > Documento.TamanhoMaximoNome)
                return Falha(TipoErro.Validacao, $"O nome do documento deve ter até {Documento.TamanhoMaximoNome} caracteres.");
            if (nome.Any(char.IsControl))
                return Falha(TipoErro.Validacao, "O nome do documento é inválido.");
        }

        if (await clientes.ObterPorIdAsync(requisicao.ClienteId, ct) is null)
            return Falha(TipoErro.Validacao, "Cliente não encontrado.");

        ArquivoArmazenado gravado;
        try
        {
            gravado = await armazenamento.GravarAsync(requisicao.Conteudo, DocumentosOpcoes.TamanhoMaximoBytes, ct);
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
                nomeDoArquivo, TipoDe(requisicao.TipoConteudo), gravado.TamanhoBytes, gravado.HashSha256, gravado.Chave);

            var documento = requisicao.Categoria == CategoriaDocumento.DocumentoGeral
                ? Documento.CriarGeral(requisicao.ClienteId, requisicao.Nome, arquivo, await perfis.ObterAtivosAsync(ct))
                : Documento.CriarContratoDeHonorarios(requisicao.ClienteId, requisicao.Nome, arquivo);

            documentos.Adicionar(documento);
            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(documento.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            await TentarRemoverAsync(gravado.Chave);
            return Falha(TipoErro.Validacao, ex.Message);
        }
        catch
        {
            await TentarRemoverAsync(gravado.Chave);
            throw;
        }
    }

    /// <summary>A limpeza nunca pode trocar o desfecho original: se ela mesma falhar, o arquivo fica órfão e só isso se perde.</summary>
    private async Task TentarRemoverAsync(string chave)
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
