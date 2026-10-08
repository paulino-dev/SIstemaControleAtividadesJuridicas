using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record DefinirAtivoAreaAtuacao(Guid Id, bool Ativo) : IRequisicao<Resultado>;

public sealed class DefinirAtivoAreaAtuacaoManipulador(
    IContextoDeAcesso contexto,
    IAreaAtuacaoRepositorio areas,
    IUnidadeDeTrabalho unidade) : IManipulador<DefinirAtivoAreaAtuacao, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirAtivoAreaAtuacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var area = await areas.ObterPorIdAsync(requisicao.Id, ct);
        if (area is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Área não encontrada.");

        if (area.Ativo == requisicao.Ativo) return Resultado.Ok();

        if (requisicao.Ativo) area.Ativar();
        else area.Inativar();

        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
