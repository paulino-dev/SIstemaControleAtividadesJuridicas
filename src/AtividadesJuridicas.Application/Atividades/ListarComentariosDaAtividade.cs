using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Atividades;

public sealed record ListarComentariosDaAtividade(Guid AtividadeId)
    : IRequisicao<Resultado<IReadOnlyList<ComentarioDetalhe>>>, IConsultaAuditavel
{
    public string TipoRecurso => "AtividadeComentario";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { AtividadeId });
}

public sealed class ListarComentariosDaAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IComentarioRepositorio comentarios,
    IAuditoria auditoria) : IManipulador<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>
{
    public async Task<Resultado<IReadOnlyList<ComentarioDetalhe>>> ManipularAsync(
        ListarComentariosDaAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.AtividadeId, ct);
        if (atividade is null)
            return Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);

        if (!AcessoAAtividade.PodeVer(ator, atividade))
        {
            await AcessoAAtividade.RegistrarNegadoAsync(auditoria, atividade.Id, nameof(ListarComentariosDaAtividade));
            return Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);
        }

        return Resultado<IReadOnlyList<ComentarioDetalhe>>.Ok(
            await comentarios.ListarDaAtividadeAsync(atividade.Id, ct));
    }
}
