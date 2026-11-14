using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>O documento da solicitação não é renomeado por aqui: só o cliente que abriu a solicitação mexe nele.</summary>
public sealed record RenomearDocumento(Guid Id, string Nome) : IRequisicao<Resultado>;

public sealed class RenomearDocumentoManipulador(
    IContextoDeAcesso contexto, IDocumentoRepositorio documentos, IUnidadeDeTrabalho unidade)
    : IManipulador<RenomearDocumento, Resultado>
{
    public async Task<Resultado> ManipularAsync(RenomearDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var documento = await documentos.ObterPorIdAsync(requisicao.Id, ct);
        if (documento is null) return Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado.");
        if (documento.Categoria == CategoriaDocumento.DocumentoDaSolicitacao)
            return Resultado.Falha(TipoErro.Validacao, DocumentosOpcoes.DocumentoDaSolicitacaoSoPeloCliente);

        try
        {
            documento.Renomear(requisicao.Nome);
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
    }
}
