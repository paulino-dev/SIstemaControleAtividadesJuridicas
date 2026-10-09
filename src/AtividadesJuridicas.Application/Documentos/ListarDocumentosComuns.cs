using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>Os documentos comuns a todos os clientes: da Diretoria (com o autor) e do cliente (sem ele).</summary>
public sealed record ListarDocumentosComuns(int Pagina = 1)
    : IRequisicao<Resultado<Pagina<DocumentoDoPortal>>>, IConsultaAuditavel, IRequisicaoAbertaAoCliente
{
    public string TipoRecurso => "Documento";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Pagina });
}

public sealed class ListarDocumentosComunsManipulador(IContextoDeAcesso contexto, IDocumentoRepositorio documentos)
    : IManipulador<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>
{
    public async Task<Resultado<Pagina<DocumentoDoPortal>>> ManipularAsync(ListarDocumentosComuns requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator || !(ator.EhDiretoria || ator.EhCliente))
            return Resultado<Pagina<DocumentoDoPortal>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<Pagina<DocumentoDoPortal>>.Ok(await documentos.ListarComunsAsync(
            comAutor: ator.EhDiretoria, Paginacao.Normalizar(requisicao.Pagina, DocumentosOpcoes.TamanhoDaPagina),
            DocumentosOpcoes.TamanhoDaPagina, ct));
    }
}
