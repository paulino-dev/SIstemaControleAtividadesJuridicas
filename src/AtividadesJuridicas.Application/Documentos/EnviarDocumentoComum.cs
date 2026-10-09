using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>
/// Envia um documento comum, visível a todos os clientes. <paramref name="ConfirmouVisibilidade"/> precisa ser
/// verdadeiro: sem ele nada é validado nem gravado. <paramref name="NomeArquivo"/> é tratado como no envio de cliente.
/// </summary>
public sealed record EnviarDocumentoComum(
    string? Nome, string NomeArquivo, string? TipoConteudo, Stream Conteudo, bool ConfirmouVisibilidade)
    : IRequisicao<Resultado<Guid>>;

public sealed class EnviarDocumentoComumManipulador(
    IContextoDeAcesso contexto,
    IDocumentoRepositorio documentos,
    IArmazenamentoDeArquivos armazenamento,
    IUnidadeDeTrabalho unidade) : IManipulador<EnviarDocumentoComum, Resultado<Guid>>
{
    public async Task<Resultado<Guid>> ManipularAsync(EnviarDocumentoComum requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (!requisicao.ConfirmouVisibilidade)
            return Falha(TipoErro.Validacao, DocumentosOpcoes.ConfirmacaoExigida);

        if (ArquivoEnviado.Validar(requisicao.NomeArquivo, requisicao.Conteudo, requisicao.Nome, out var nomeDoArquivo) is { } recusa)
            return Falha(TipoErro.Validacao, recusa);

        return await ArquivoEnviado.GravarAsync(
            armazenamento, documentos, unidade, nomeDoArquivo, requisicao.TipoConteudo, requisicao.Conteudo,
            arquivo => Task.FromResult(Documento.CriarComum(requisicao.Nome, arquivo)), ct);
    }

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
