using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Solicitacoes;

/// <summary>
/// As solicitações do cliente do ator, da mais recente para a mais antiga. Não há Id de cliente aqui: é sempre o do
/// usuário que pede.
/// </summary>
public sealed record ListarMinhasSolicitacoes(SituacaoParaOCliente? Situacao = null, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<SolicitacaoDoPortal>>>, IConsultaAuditavel, IRequisicaoAbertaAoCliente
{
    public string TipoRecurso => "Solicitacao";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Situacao = Situacao?.ToString(), Pagina });
}

public sealed class ListarMinhasSolicitacoesManipulador(IContextoDeAcesso contexto, ISolicitacaoRepositorio solicitacoes)
    : IManipulador<ListarMinhasSolicitacoes, Resultado<Pagina<SolicitacaoDoPortal>>>
{
    public async Task<Resultado<Pagina<SolicitacaoDoPortal>>> ManipularAsync(
        ListarMinhasSolicitacoes requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhCliente: true, ClienteId: { } clienteId })
            return Resultado<Pagina<SolicitacaoDoPortal>>.Falha(TipoErro.AcessoNegado, MinhaSolicitacao.AcessoNegado);

        if (requisicao.Situacao is { } situacao && !Enum.IsDefined(situacao))
            return Resultado<Pagina<SolicitacaoDoPortal>>.Falha(TipoErro.Validacao, "Situação inválida.");

        return Resultado<Pagina<SolicitacaoDoPortal>>.Ok(await solicitacoes.ListarDoClienteAsync(
            clienteId, requisicao.Situacao,
            Paginacao.Normalizar(requisicao.Pagina, SolicitacoesOpcoes.TamanhoDaPagina), SolicitacoesOpcoes.TamanhoDaPagina, ct));
    }
}
