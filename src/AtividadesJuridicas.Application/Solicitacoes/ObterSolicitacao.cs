using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.Application.Solicitacoes;

/// <summary>A ficha de uma solicitação de qualquer cliente, para a Diretoria.</summary>
public sealed record ObterSolicitacao(Guid Id) : IRequisicao<Resultado<SolicitacaoDetalhe>>, IConsultaAuditavel
{
    public string TipoRecurso => "Solicitacao";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => Id;
}

/// <summary>
/// O que a Diretoria lê de uma solicitação. Os nomes do cliente e do solicitante valem mesmo que eles tenham sido
/// excluídos. <paramref name="AtividadeId"/> é o vínculo como está gravado; <paramref name="AtividadeTitulo"/> só
/// existe enquanto a atividade estiver viva. <paramref name="Versao"/> é a que a Fase B devolve ao agir.
/// </summary>
public sealed record SolicitacaoDetalhe(
    Guid Id, DateTime AbertaEm, Guid ClienteId, string ClienteNome, Guid SolicitanteId, string SolicitanteNome,
    UrgenciaDaSolicitacao Urgencia, string Descricao, SituacaoDaSolicitacao Situacao, DateTime? CanceladaEm,
    IReadOnlyList<DocumentoDoPortal> Documentos, Guid? AtividadeId, string? AtividadeTitulo, uint Versao);

public sealed class ObterSolicitacaoManipulador(
    IContextoDeAcesso contexto, ISolicitacaoRepositorio solicitacoes, IDocumentoRepositorio documentos)
    : IManipulador<ObterSolicitacao, Resultado<SolicitacaoDetalhe>>
{
    public async Task<Resultado<SolicitacaoDetalhe>> ManipularAsync(ObterSolicitacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<SolicitacaoDetalhe>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var solicitacao = await solicitacoes.ObterPorIdAsync(requisicao.Id, ct);
        if (solicitacao is null)
            return Resultado<SolicitacaoDetalhe>.Falha(TipoErro.NaoEncontrado, SolicitacoesOpcoes.NaoEncontrada);

        // A atividade só dá título se estiver viva: o repositório não devolve a excluída.
        var atividade = solicitacao.AtividadeId is { } atividadeId
            ? await solicitacoes.ObterAtividadeAsync(atividadeId, ct)
            : null;

        return Resultado<SolicitacaoDetalhe>.Ok(new SolicitacaoDetalhe(
            solicitacao.Id, solicitacao.CriadoEm,
            solicitacao.ClienteId, await solicitacoes.NomeDoClienteAsync(solicitacao.ClienteId, ct) ?? string.Empty,
            solicitacao.SolicitanteId, await solicitacoes.NomeDoUsuarioAsync(solicitacao.SolicitanteId, ct) ?? string.Empty,
            solicitacao.Urgencia, solicitacao.Descricao, solicitacao.Situacao, solicitacao.CanceladaEm,
            await documentos.ListarDaSolicitacaoAsync(solicitacao.Id, ct),
            solicitacao.AtividadeId, atividade?.Titulo, solicitacao.Versao));
    }
}
