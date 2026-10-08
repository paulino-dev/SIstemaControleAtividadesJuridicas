using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Atividades;

public sealed record ListarDocumentosDaAtividade(Guid AtividadeId, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<DocumentoResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Documento";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { AtividadeId, Pagina });
}

public sealed class ListarDocumentosDaAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IDocumentoRepositorio documentos,
    IAuditoria auditoria) : IManipulador<ListarDocumentosDaAtividade, Resultado<Pagina<DocumentoResumo>>>
{
    public async Task<Resultado<Pagina<DocumentoResumo>>> ManipularAsync(ListarDocumentosDaAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.AtividadeId, ct);
        if (atividade is null) return Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);

        if (!AcessoAAtividade.PodeVer(ator, atividade))
        {
            await AcessoAAtividade.RegistrarNegadoAsync(auditoria, atividade.Id, nameof(ListarDocumentosDaAtividade));
            return Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);
        }

        return Resultado<Pagina<DocumentoResumo>>.Ok(await documentos.ListarDaAtividadeAsync(
            atividade.Id, Paginacao.Normalizar(requisicao.Pagina, DocumentosOpcoes.TamanhoDaPagina), DocumentosOpcoes.TamanhoDaPagina, ct));
    }

    private static Resultado<Pagina<DocumentoResumo>> Falha(TipoErro erro, string mensagem) =>
        Resultado<Pagina<DocumentoResumo>>.Falha(erro, mensagem);
}
