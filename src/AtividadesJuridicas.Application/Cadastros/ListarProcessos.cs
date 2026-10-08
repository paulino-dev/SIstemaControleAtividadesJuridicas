using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ListarProcessos(
    string? Busca = null, Guid? ClienteId = null, StatusProcesso? Status = null, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<ProcessoResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Processo";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Busca, ClienteId, Status, Pagina });
}

public sealed class ListarProcessosManipulador(IContextoDeAcesso contexto, IProcessoRepositorio processos)
    : IManipulador<ListarProcessos, Resultado<Pagina<ProcessoResumo>>>
{
    public async Task<Resultado<Pagina<ProcessoResumo>>> ManipularAsync(ListarProcessos requisicao, CancellationToken ct)
    {
        // Diretoria e Produção consultam processos; só a Diretoria altera.
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<Pagina<ProcessoResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<Pagina<ProcessoResumo>>.Ok(await processos.ListarAsync(
            new FiltroDeProcessos(requisicao.Busca, requisicao.ClienteId, requisicao.Status),
            Paginacao.Normalizar(requisicao.Pagina, ProcessosOpcoes.TamanhoDaPagina), ProcessosOpcoes.TamanhoDaPagina, ct));
    }
}
