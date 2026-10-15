using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ListarProcessos(
    string? Busca = null, Guid? ClienteId = null, StatusProcesso? Status = null, int Pagina = 1)
    : IRequisicao<Resultado<PaginaDeProcessos>>, IConsultaAuditavel
{
    public string TipoRecurso => "Processo";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Busca, ClienteId, Status, Pagina });
}

public sealed class ListarProcessosManipulador(IContextoDeAcesso contexto, IProcessoRepositorio processos)
    : IManipulador<ListarProcessos, Resultado<PaginaDeProcessos>>
{
    public async Task<Resultado<PaginaDeProcessos>> ManipularAsync(ListarProcessos requisicao, CancellationToken ct)
    {
        // Diretoria e Produção consultam processos; só a Diretoria altera.
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<PaginaDeProcessos>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var filtro = new FiltroDeProcessos(requisicao.Busca, requisicao.ClienteId, requisicao.Status);
        var pagina = await processos.ListarAsync(
            filtro, Paginacao.Normalizar(requisicao.Pagina, ProcessosOpcoes.TamanhoDaPagina), ProcessosOpcoes.TamanhoDaPagina, ct);
        var todas = await processos.ContarAsync(filtro with { Status = null }, ct);

        return Resultado<PaginaDeProcessos>.Ok(new PaginaDeProcessos(
            pagina.Itens, pagina.Total, pagina.Numero, pagina.Tamanho, todas));
    }
}
