using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>Exclusão lógica da atividade, dos responsáveis e do histórico.</summary>
public sealed record ExcluirAtividade(Guid Id, uint? VersaoEsperada = null) : IRequisicao<Resultado>;

public sealed class ExcluirAtividadeManipulador(
    IContextoDeAcesso contexto, IAtividadeRepositorio atividades, IUnidadeDeTrabalho unidade)
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
