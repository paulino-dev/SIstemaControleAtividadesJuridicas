using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Documentos;

public sealed record ObterAcessoDoDocumento(Guid Id) : IRequisicao<Resultado<AcessoDoDocumento>>, IConsultaAuditavel
{
    public string TipoRecurso => "Documento";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => Id;
}

public sealed class ObterAcessoDoDocumentoManipulador(IContextoDeAcesso contexto, IDocumentoRepositorio documentos)
    : IManipulador<ObterAcessoDoDocumento, Resultado<AcessoDoDocumento>>
{
    public async Task<Resultado<AcessoDoDocumento>> ManipularAsync(ObterAcessoDoDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<AcessoDoDocumento>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var documento = await documentos.ObterPorIdAsync(requisicao.Id, ct);
        return documento is null
            ? Resultado<AcessoDoDocumento>.Falha(TipoErro.NaoEncontrado, "Documento não encontrado.")
            : Resultado<AcessoDoDocumento>.Ok(new AcessoDoDocumento(
                documento.Id, documento.Nome, documento.Categoria,
                [.. documento.PerfisComAcesso.Select(a => a.PerfilId)],
                [.. documento.UsuariosComAcesso.Select(a => a.UsuarioId)]));
    }
}
