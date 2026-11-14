using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.Application.Solicitacoes;

/// <summary>
/// A ficha de uma solicitação do cliente do ator. Não há Id de cliente aqui: a de outro cliente não existe para quem pede.
/// </summary>
public sealed record ObterMinhaSolicitacao(Guid Id)
    : IRequisicao<Resultado<MinhaSolicitacaoDetalhe>>, IConsultaAuditavel, IRequisicaoAbertaAoCliente
{
    public string TipoRecurso => "Solicitacao";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => Id;
}

/// <summary>
/// O que o cliente lê de uma solicitação sua. <paramref name="PodeAlterar"/>: o ator é quem abriu e ela ainda aguarda.
/// <paramref name="AtividadeTitulo"/> e <paramref name="AtividadeConcluidaEm"/>: só da atividade viva vinculada, e a
/// data só quando ela está concluída. <paramref name="Versao"/> é a que a tela devolve ao alterar.
/// </summary>
public sealed record MinhaSolicitacaoDetalhe(
    Guid Id, DateTime AbertaEm, string Descricao, UrgenciaDaSolicitacao Urgencia, SituacaoParaOCliente Situacao,
    string SolicitanteNome, bool PodeAlterar, IReadOnlyList<DocumentoDoPortal> Documentos, string? AtividadeTitulo,
    DateTime? AtividadeConcluidaEm, uint Versao);

public sealed class ObterMinhaSolicitacaoManipulador(
    IContextoDeAcesso contexto, ISolicitacaoRepositorio solicitacoes, IDocumentoRepositorio documentos)
    : IManipulador<ObterMinhaSolicitacao, Resultado<MinhaSolicitacaoDetalhe>>
{
    public async Task<Resultado<MinhaSolicitacaoDetalhe>> ManipularAsync(ObterMinhaSolicitacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhCliente: true, ClienteId: { } clienteId } ator)
            return Resultado<MinhaSolicitacaoDetalhe>.Falha(TipoErro.AcessoNegado, MinhaSolicitacao.AcessoNegado);

        var solicitacao = await MinhaSolicitacao.ObterDoClienteAsync(solicitacoes, requisicao.Id, clienteId, ct);
        if (solicitacao is null)
            return Resultado<MinhaSolicitacaoDetalhe>.Falha(TipoErro.NaoEncontrado, SolicitacoesOpcoes.NaoEncontrada);

        // A atividade só conta se estiver viva: o repositório não devolve a excluída.
        var atividade = solicitacao.AtividadeId is { } atividadeId
            ? await solicitacoes.ObterAtividadeAsync(atividadeId, ct)
            : null;
        var concluida = atividade is not null && Andamento.SituacaoDe(atividade.Status) == SituacaoDoAndamento.Concluida;

        // A mesma precedência da lista: a atividade viva decide; sem ela, vale a situação da própria solicitação.
        var situacao = concluida ? SituacaoParaOCliente.Concluida
            : atividade is not null ? SituacaoParaOCliente.EmAndamento
            : solicitacao.CanceladaEm is not null ? SituacaoParaOCliente.Cancelada
            : SituacaoParaOCliente.AguardandoAnalise;

        return Resultado<MinhaSolicitacaoDetalhe>.Ok(new MinhaSolicitacaoDetalhe(
            solicitacao.Id, solicitacao.CriadoEm, solicitacao.Descricao, solicitacao.Urgencia, situacao,
            await solicitacoes.NomeDoUsuarioAsync(solicitacao.SolicitanteId, ct) ?? string.Empty,
            solicitacao.EhDoSolicitante(ator.Id) && solicitacao.Situacao == SituacaoDaSolicitacao.Aguardando,
            await documentos.ListarDaSolicitacaoAsync(solicitacao.Id, ct),
            atividade?.Titulo, concluida ? atividade!.ConcluidaEm : null, solicitacao.Versao));
    }
}
