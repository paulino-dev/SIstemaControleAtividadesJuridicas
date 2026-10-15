using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Atividades;

public sealed record ListarAtividades(
    StatusAtividade? Status = null, Guid? ResponsavelId = null, Guid? RevisorId = null, Guid? ClienteId = null, Guid? ProcessoId = null,
    bool SomenteAtrasadas = false, string? Busca = null, int Pagina = 1)
    : IRequisicao<Resultado<PaginaDeAtividades>>, IConsultaAuditavel
{
    public string TipoRecurso => "Atividade";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(
        new { Status, ResponsavelId, RevisorId, ClienteId, ProcessoId, SomenteAtrasadas, Busca, Pagina });
}

public sealed class ListarAtividadesManipulador(
    IContextoDeAcesso contexto, IAtividadeRepositorio atividades, ICalendarioDoEscritorio calendario)
    : IManipulador<ListarAtividades, Resultado<PaginaDeAtividades>>
{
    public async Task<Resultado<PaginaDeAtividades>> ManipularAsync(ListarAtividades requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado<PaginaDeAtividades>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        // Um "hoje" só para a página e para a contagem: as duas decidem "atrasada" com a mesma data.
        var hoje = calendario.Hoje;

        // Produção só lista as próprias atividades, qualquer que seja o filtro pedido.
        var responsavelId = ator.EhDiretoria ? requisicao.ResponsavelId : ator.Id;
        var filtro = new FiltroDeAtividades(
            requisicao.Status, responsavelId, requisicao.RevisorId, requisicao.ClienteId, requisicao.ProcessoId,
            requisicao.SomenteAtrasadas, requisicao.Busca, hoje);

        var pagina = await atividades.ListarAsync(
            filtro, Paginacao.Normalizar(requisicao.Pagina, AtividadesOpcoes.TamanhoDaPagina),
            AtividadesOpcoes.TamanhoDaPagina, ct);

        // Os contadores da tela são do escopo de acesso, não do filtro: a Diretoria conta o escritório inteiro (o
        // responsável que ela pediu também fica de fora); a Produção, as atividades em que é responsável.
        var contagem = await atividades.ContarAsync(ator.EhDiretoria ? null : ator.Id, hoje, ct);

        return Resultado<PaginaDeAtividades>.Ok(new PaginaDeAtividades(
            pagina.Itens, pagina.Total, pagina.Numero, pagina.Tamanho, contagem.Total, contagem.Atrasadas));
    }
}
