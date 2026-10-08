using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Atividades;

public sealed record ListarAtividades(
    StatusAtividade? Status = null, Guid? ResponsavelId = null, Guid? ClienteId = null, Guid? ProcessoId = null,
    bool SomenteAtrasadas = false, string? Busca = null, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<AtividadeResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Atividade";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(
        new { Status, ResponsavelId, ClienteId, ProcessoId, SomenteAtrasadas, Busca, Pagina });
}

public sealed class ListarAtividadesManipulador(
    IContextoDeAcesso contexto, IAtividadeRepositorio atividades, ICalendarioDoEscritorio calendario)
    : IManipulador<ListarAtividades, Resultado<Pagina<AtividadeResumo>>>
{
    public async Task<Resultado<Pagina<AtividadeResumo>>> ManipularAsync(ListarAtividades requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado<Pagina<AtividadeResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        // Produção só lista as próprias atividades, qualquer que seja o filtro pedido.
        var responsavelId = ator.EhDiretoria ? requisicao.ResponsavelId : ator.Id;
        var filtro = new FiltroDeAtividades(
            requisicao.Status, responsavelId, requisicao.ClienteId, requisicao.ProcessoId,
            requisicao.SomenteAtrasadas, requisicao.Busca, calendario.Hoje);

        return Resultado<Pagina<AtividadeResumo>>.Ok(await atividades.ListarAsync(
            filtro, Paginacao.Normalizar(requisicao.Pagina, AtividadesOpcoes.TamanhoDaPagina),
            AtividadesOpcoes.TamanhoDaPagina, ct));
    }
}
