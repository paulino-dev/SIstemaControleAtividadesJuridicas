using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Application.Solicitacoes;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>
/// Exclusão lógica da atividade, dos responsáveis e do histórico. Se a atividade nasceu de uma solicitação do
/// cliente, a solicitação é desvinculada na mesma gravação.
/// </summary>
public sealed record ExcluirAtividade(Guid Id, uint? VersaoEsperada = null) : IRequisicao<Resultado>;

public sealed class ExcluirAtividadeManipulador(
    IContextoDeAcesso contexto, IAtividadeRepositorio atividades, ISolicitacaoRepositorio solicitacoes,
    IUnidadeDeTrabalho unidade)
    : IManipulador<ExcluirAtividade, Resultado>
{
    public async Task<Resultado> ManipularAsync(ExcluirAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.Id, ct);
        if (atividade is null) return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);
        if (requisicao.VersaoEsperada is { } esperada && esperada != atividade.Versao)
            return Resultado.Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);

        try
        {
            // A solicitação de onde a atividade nasceu volta a aguardar (ou a ficar cancelada) na mesma gravação da
            // exclusão. Só se ainda aponta para esta atividade: se aponta para outra, não é dela desfazer. Ela é lida
            // (rastreada) antes de a atividade sair, para a gravação levar as duas.
            if (atividade.SolicitacaoId is { } solicitacaoId &&
                await solicitacoes.ObterPorIdAsync(solicitacaoId, ct) is { } solicitacao &&
                solicitacao.AtividadeId == atividade.Id)
                solicitacao.DesvincularAtividade();

            atividades.Remover(atividade);
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado.Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
        }
        catch (ExcecaoDeConcorrencia)
        {
            return Resultado.Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
        }
    }
}
