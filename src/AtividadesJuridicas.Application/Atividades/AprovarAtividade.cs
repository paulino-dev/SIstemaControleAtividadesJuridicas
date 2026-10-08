using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>Só o revisor (que é da Diretoria) aprova; a recusa de quem não é o revisor vem do domínio.</summary>
public sealed record AprovarAtividade(Guid Id) : IRequisicao<Resultado>;

public sealed class AprovarAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IRelogio relogio,
    IUnidadeDeTrabalho unidade) : IManipulador<AprovarAtividade, Resultado>
{
    public async Task<Resultado> ManipularAsync(AprovarAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.Id, ct);
        if (atividade is null) return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);

        try
        {
            atividade.Aprovar(AcessoAAtividade.Pessoa(ator), relogio.AgoraUtc);
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
