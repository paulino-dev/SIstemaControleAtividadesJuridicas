using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record ListarPerfisAtivos : IRequisicao<Resultado<IReadOnlyList<PerfilResumo>>>;

public sealed class ListarPerfisAtivosManipulador(IContextoDeAcesso contexto, IPerfilRepositorio perfis)
    : IManipulador<ListarPerfisAtivos, Resultado<IReadOnlyList<PerfilResumo>>>
{
    public async Task<Resultado<IReadOnlyList<PerfilResumo>>> ManipularAsync(ListarPerfisAtivos requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<IReadOnlyList<PerfilResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<IReadOnlyList<PerfilResumo>>.Ok(await perfis.ListarAtivosAsync(ct));
    }
}
