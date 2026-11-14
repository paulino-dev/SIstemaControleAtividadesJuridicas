using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Solicitacoes;

/// <summary>
/// Só quem abriu a solicitação a cancela, e só enquanto ela aguarda; depois disso nada mais muda. Não há Id de cliente
/// aqui: a de outro cliente não existe para quem pede.
/// </summary>
public sealed record CancelarMinhaSolicitacao(Guid Id, uint VersaoEsperada)
    : IRequisicao<Resultado>, IRequisicaoAbertaAoCliente;

public sealed class CancelarMinhaSolicitacaoManipulador(
    IContextoDeAcesso contexto, ISolicitacaoRepositorio solicitacoes, IRelogio relogio, IUnidadeDeTrabalho unidade)
    : IManipulador<CancelarMinhaSolicitacao, Resultado>
{
    public async Task<Resultado> ManipularAsync(CancelarMinhaSolicitacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhCliente: true, ClienteId: { } clienteId } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, MinhaSolicitacao.AcessoNegado);

        var (solicitacao, recusa) = await MinhaSolicitacao.CarregarParaAlterarAsync(
            solicitacoes, ator, clienteId, requisicao.Id, requisicao.VersaoEsperada, ct);
        if (solicitacao is null) return recusa!;

        try
        {
            solicitacao.Cancelar(relogio.AgoraUtc);
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
