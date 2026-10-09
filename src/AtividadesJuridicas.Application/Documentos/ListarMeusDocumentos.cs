using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>Os documentos do cliente do ator. Não há Id de cliente aqui: é sempre o do usuário que pede.</summary>
public sealed record ListarMeusDocumentos(int Pagina = 1)
    : IRequisicao<Resultado<Pagina<DocumentoDoPortal>>>, IConsultaAuditavel, IRequisicaoAbertaAoCliente
{
    public string TipoRecurso => "Documento";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Pagina });
}

public sealed class ListarMeusDocumentosManipulador(IContextoDeAcesso contexto, IDocumentoRepositorio documentos)
    : IManipulador<ListarMeusDocumentos, Resultado<Pagina<DocumentoDoPortal>>>
{
    public async Task<Resultado<Pagina<DocumentoDoPortal>>> ManipularAsync(ListarMeusDocumentos requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhCliente: true, ClienteId: { } clienteId })
            return Resultado<Pagina<DocumentoDoPortal>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<Pagina<DocumentoDoPortal>>.Ok(await documentos.ListarDoPortalAsync(
            clienteId, Paginacao.Normalizar(requisicao.Pagina, DocumentosOpcoes.TamanhoDaPagina),
            DocumentosOpcoes.TamanhoDaPagina, ct));
    }
}
