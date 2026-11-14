using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Solicitacoes;

/// <summary>
/// Só quem abriu a solicitação anexa um documento a ela, e só enquanto ela aguarda, até
/// <see cref="SolicitacoesOpcoes.MaximoDeDocumentos"/>. <paramref name="NomeArquivo"/> é tratado como no envio de
/// documento: só o último segmento é guardado. Não há Id de cliente aqui: a de outro cliente não existe para quem pede.
/// </summary>
public sealed record AnexarDocumentoASolicitacao(
    Guid Id, string NomeArquivo, string? TipoConteudo, Stream Conteudo, uint VersaoEsperada)
    : IRequisicao<Resultado<Guid>>, IRequisicaoAbertaAoCliente;

public sealed class AnexarDocumentoASolicitacaoManipulador(
    IContextoDeAcesso contexto,
    ISolicitacaoRepositorio solicitacoes,
    IDocumentoRepositorio documentos,
    IArmazenamentoDeArquivos armazenamento,
    IUnidadeDeTrabalho unidade) : IManipulador<AnexarDocumentoASolicitacao, Resultado<Guid>>
{
    public async Task<Resultado<Guid>> ManipularAsync(AnexarDocumentoASolicitacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhCliente: true, ClienteId: { } clienteId } ator)
            return Resultado<Guid>.Falha(TipoErro.AcessoNegado, MinhaSolicitacao.AcessoNegado);

        var (solicitacao, recusa) = await MinhaSolicitacao.CarregarParaAlterarAsync(
            solicitacoes, ator, clienteId, requisicao.Id, requisicao.VersaoEsperada, ct);
        if (solicitacao is null) return recusa!.Como<Guid>();

        // O domínio recusa antes do limite e de o arquivo ser gravado. A mudança nos documentos só vale com a gravação,
        // que acontece junto com o documento; a instância não é salva por nenhum caminho de recusa.
        try
        {
            solicitacao.RegistrarMudancaNosDocumentos();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado<Guid>.Falha(TipoErro.Validacao, ex.Message);
        }

        if (await documentos.ContarDaSolicitacaoAsync(solicitacao.Id, ct) >= SolicitacoesOpcoes.MaximoDeDocumentos)
            return Resultado<Guid>.Falha(TipoErro.Validacao, SolicitacoesOpcoes.LimiteDeDocumentos);

        if (ArquivoEnviado.Validar(requisicao.NomeArquivo, requisicao.Conteudo, null, out var nomeDoArquivo) is { } recusaDoArquivo)
            return Resultado<Guid>.Falha(TipoErro.Validacao, recusaDoArquivo);

        try
        {
            return await ArquivoEnviado.GravarAsync(
                armazenamento, documentos, unidade, nomeDoArquivo, requisicao.TipoConteudo, requisicao.Conteudo,
                arquivo => Task.FromResult(Documento.CriarDaSolicitacao(solicitacao.Id, clienteId, null, arquivo)), ct);
        }
        catch (Exception ex) when (ex is ExcecaoDeConcorrencia or ExcecaoDeConflito)
        {
            // O arquivo já foi removido por quem gravou.
            return MinhaSolicitacao.Alterada().Como<Guid>();
        }
    }
}
