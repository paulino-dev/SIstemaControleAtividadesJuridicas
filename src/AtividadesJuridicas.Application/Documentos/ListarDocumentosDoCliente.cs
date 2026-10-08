using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

public sealed record ListarDocumentosDoCliente(Guid ClienteId, CategoriaDocumento Categoria, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<DocumentoResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Documento";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { ClienteId, Categoria, Pagina });
}

public sealed class ListarDocumentosDoClienteManipulador(IContextoDeAcesso contexto, IDocumentoRepositorio documentos)
    : IManipulador<ListarDocumentosDoCliente, Resultado<Pagina<DocumentoResumo>>>
{
    public async Task<Resultado<Pagina<DocumentoResumo>>> ManipularAsync(ListarDocumentosDoCliente requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Falha("Acesso negado.");

        // Contrato de honorários: Produção não tem acesso, nem à lista.
        if (requisicao.Categoria == CategoriaDocumento.ContratoHonorarios && !ator.EhDiretoria)
            return Falha("Acesso negado.");

        var acesso = ator.EhDiretoria ? null : new FiltroDeAcesso(ator.Id, ator.PerfilId);

        return Resultado<Pagina<DocumentoResumo>>.Ok(await documentos.ListarDoClienteAsync(
            requisicao.ClienteId, requisicao.Categoria, acesso,
            Paginacao.Normalizar(requisicao.Pagina, DocumentosOpcoes.TamanhoDaPagina), DocumentosOpcoes.TamanhoDaPagina, ct));
    }

    private static Resultado<Pagina<DocumentoResumo>> Falha(string mensagem) =>
        Resultado<Pagina<DocumentoResumo>>.Falha(TipoErro.AcessoNegado, mensagem);
}
