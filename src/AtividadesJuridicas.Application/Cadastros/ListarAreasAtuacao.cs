using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ListarAreasAtuacao(bool SomenteAtivas = false)
    : IRequisicao<Resultado<IReadOnlyList<AreaAtuacaoResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "AreaAtuacao";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
}

public sealed class ListarAreasAtuacaoManipulador(IContextoDeAcesso contexto, IAreaAtuacaoRepositorio areas)
    : IManipulador<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>
{
    public async Task<Resultado<IReadOnlyList<AreaAtuacaoResumo>>> ManipularAsync(
        ListarAreasAtuacao requisicao, CancellationToken ct)
    {
        // Qualquer usuário autenticado lê os nomes das áreas; a gestão é da Diretoria.
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Ok(await areas.ListarAsync(requisicao.SomenteAtivas, ct));
    }
}
