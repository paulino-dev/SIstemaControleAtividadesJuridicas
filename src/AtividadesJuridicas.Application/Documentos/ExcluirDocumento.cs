using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>
/// Exclusão lógica. O arquivo permanece no armazenamento. O documento da solicitação não sai por aqui: só o cliente
/// que abriu a solicitação o remove.
/// </summary>
public sealed record ExcluirDocumento(Guid Id) : IRequisicao<Resultado>;

public sealed class ExcluirDocumentoManipulador(
    IContextoDeAcesso contexto, IDocumentoRepositorio documentos, IUnidadeDeTrabalho unidade)
    : IManipulador<ExcluirDocumento, Resultado>
{
    public async Task<Resultado> ManipularAsync(ExcluirDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var documento = await documentos.ObterPorIdAsync(requisicao.Id, ct);
        if (documento is null) return Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado.");
        if (documento.Categoria == CategoriaDocumento.DocumentoDaSolicitacao)
            return Resultado.Falha(TipoErro.Validacao, DocumentosOpcoes.DocumentoDaSolicitacaoSoPeloCliente);

        documentos.Remover(documento);
        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
