using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record AlterarStatusProcesso(Guid Id, StatusProcesso Status) : IRequisicao<Resultado>;

public sealed class AlterarStatusProcessoManipulador(
    IContextoDeAcesso contexto,
    IProcessoRepositorio processos,
    IUnidadeDeTrabalho unidade) : IManipulador<AlterarStatusProcesso, Resultado>
{
    public async Task<Resultado> ManipularAsync(AlterarStatusProcesso requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var processo = await processos.ObterPorIdAsync(requisicao.Id, ct);
        if (processo is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Processo não encontrado.");

        if (processo.Status == requisicao.Status) return Resultado.Ok();

        try
        {
            processo.AlterarStatus(requisicao.Status);
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeConcorrencia)
        {
            return Resultado.Falha(TipoErro.Conflito, "O processo foi alterado por outra pessoa. Recarregue e tente novamente.");
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
    }
}
