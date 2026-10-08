using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>Só o revisor (que é da Diretoria) devolve; só as partes dos responsáveis informados voltam a ficar pendentes.</summary>
public sealed record DevolverAtividade(Guid Id, string Motivo, IReadOnlyList<Guid> Responsaveis) : IRequisicao<Resultado>;

public sealed class DevolverAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IRelogio relogio,
    IUnidadeDeTrabalho unidade) : IManipulador<DevolverAtividade, Resultado>
{
    public async Task<Resultado> ManipularAsync(DevolverAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.Id, ct);
        if (atividade is null) return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);

        try
        {
            atividade.Devolver(AcessoAAtividade.Pessoa(ator), requisicao.Motivo, requisicao.Responsaveis ?? [], relogio.AgoraUtc);
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConcorrencia)
        {
            return Resultado.Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
        }
    }
}
