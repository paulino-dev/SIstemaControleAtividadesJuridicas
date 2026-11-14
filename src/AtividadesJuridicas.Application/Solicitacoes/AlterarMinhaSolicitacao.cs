using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.Application.Solicitacoes;

/// <summary>
/// Só quem abriu a solicitação a altera, e só enquanto ela aguarda. <paramref name="VersaoEsperada"/> é a versão com que
/// a tela foi aberta. Não há Id de cliente aqui: a de outro cliente não existe para quem pede.
/// </summary>
public sealed record AlterarMinhaSolicitacao(Guid Id, string Descricao, UrgenciaDaSolicitacao Urgencia, uint VersaoEsperada)
    : IRequisicao<Resultado>, IRequisicaoAbertaAoCliente;

public sealed class AlterarMinhaSolicitacaoManipulador(
    IContextoDeAcesso contexto, ISolicitacaoRepositorio solicitacoes, IUnidadeDeTrabalho unidade)
    : IManipulador<AlterarMinhaSolicitacao, Resultado>
{
    public async Task<Resultado> ManipularAsync(AlterarMinhaSolicitacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhCliente: true, ClienteId: { } clienteId } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, MinhaSolicitacao.AcessoNegado);

        var (solicitacao, recusa) = await MinhaSolicitacao.CarregarParaAlterarAsync(
            solicitacoes, ator, clienteId, requisicao.Id, requisicao.VersaoEsperada, ct);
        if (solicitacao is null) return recusa!;

        try
        {
            solicitacao.Alterar(requisicao.Descricao, requisicao.Urgencia);
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
