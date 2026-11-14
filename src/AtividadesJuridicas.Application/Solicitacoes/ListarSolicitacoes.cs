using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.Application.Solicitacoes;

/// <summary>
/// Todas as solicitações dos clientes, da mais antiga para a mais nova, para a Diretoria. Ao contrário do portal,
/// aqui o cliente é um filtro opcional e a situação é a do domínio.
/// </summary>
public sealed record ListarSolicitacoes(SituacaoDaSolicitacao? Situacao = null, Guid? ClienteId = null, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<SolicitacaoResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Solicitacao";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Situacao = Situacao?.ToString(), ClienteId, Pagina });
}

public sealed class ListarSolicitacoesManipulador(IContextoDeAcesso contexto, ISolicitacaoRepositorio solicitacoes)
    : IManipulador<ListarSolicitacoes, Resultado<Pagina<SolicitacaoResumo>>>
{
    public async Task<Resultado<Pagina<SolicitacaoResumo>>> ManipularAsync(
        ListarSolicitacoes requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Pagina<SolicitacaoResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (requisicao.Situacao is { } situacao && !Enum.IsDefined(situacao))
            return Resultado<Pagina<SolicitacaoResumo>>.Falha(TipoErro.Validacao, "Situação inválida.");

        return Resultado<Pagina<SolicitacaoResumo>>.Ok(await solicitacoes.ListarAsync(
            requisicao.Situacao, requisicao.ClienteId,
            Paginacao.Normalizar(requisicao.Pagina, SolicitacoesOpcoes.TamanhoDaPagina), SolicitacoesOpcoes.TamanhoDaPagina, ct));
    }
}
