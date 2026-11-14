using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Solicitacoes;

/// <summary>
/// Só quem abriu a solicitação tira um documento dela, e só enquanto ela aguarda. A exclusão é lógica e o arquivo
/// permanece. Não há Id de cliente aqui: o documento de outra categoria ou de solicitação de outro cliente não existe
/// para quem pede.
/// </summary>
public sealed record RemoverDocumentoDaSolicitacao(Guid DocumentoId, uint VersaoEsperada)
    : IRequisicao<Resultado>, IRequisicaoAbertaAoCliente;

public sealed class RemoverDocumentoDaSolicitacaoManipulador(
    IContextoDeAcesso contexto, ISolicitacaoRepositorio solicitacoes, IDocumentoRepositorio documentos, IUnidadeDeTrabalho unidade)
    : IManipulador<RemoverDocumentoDaSolicitacao, Resultado>
{
    public async Task<Resultado> ManipularAsync(RemoverDocumentoDaSolicitacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhCliente: true, ClienteId: { } clienteId } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, MinhaSolicitacao.AcessoNegado);

        // Só o documento da solicitação (nenhuma outra categoria), e só se a solicitação for do cliente do ator.
        var documento = await documentos.ObterPorIdAsync(requisicao.DocumentoId, ct);
        if (documento is not { Categoria: CategoriaDocumento.DocumentoDaSolicitacao, SolicitacaoId: { } solicitacaoId })
            return MinhaSolicitacao.NaoEncontrada();

        var solicitacao = await MinhaSolicitacao.ObterDoClienteAsync(solicitacoes, solicitacaoId, clienteId, ct);
        if (solicitacao is null) return MinhaSolicitacao.NaoEncontrada();

        if (MinhaSolicitacao.Conferir(ator, solicitacao, requisicao.VersaoEsperada) is { } recusa) return recusa;

        try
        {
            // O domínio recusa antes de o documento sair: a mudança nos documentos é gravada com a remoção.
            solicitacao.RegistrarMudancaNosDocumentos();
            documentos.Remover(documento);
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (Exception ex) when (ex is ExcecaoDeConcorrencia or ExcecaoDeConflito)
        {
            return MinhaSolicitacao.Alterada();
        }
    }
}
