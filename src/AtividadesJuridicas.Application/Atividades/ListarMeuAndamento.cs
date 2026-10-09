using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>O andamento das atividades do cliente do ator. Não há Id de cliente aqui: é sempre o do usuário que pede.</summary>
public sealed record ListarMeuAndamento(SituacaoDoAndamento? Situacao = null, int Pagina = 1)
    : IRequisicao<Resultado<AndamentoDoCliente>>, IConsultaAuditavel, IRequisicaoAbertaAoCliente
{
    public string TipoRecurso => "Atividade";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Situacao = Situacao?.ToString(), Pagina });
}

public sealed class ListarMeuAndamentoManipulador(IContextoDeAcesso contexto, IAtividadeRepositorio atividades)
    : IManipulador<ListarMeuAndamento, Resultado<AndamentoDoCliente>>
{
    public async Task<Resultado<AndamentoDoCliente>> ManipularAsync(ListarMeuAndamento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhCliente: true, ClienteId: { } clienteId })
            return Resultado<AndamentoDoCliente>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (requisicao.Situacao is { } situacao && !Enum.IsDefined(situacao))
            return Resultado<AndamentoDoCliente>.Falha(TipoErro.Validacao, "Situação inválida.");

        return Resultado<AndamentoDoCliente>.Ok(await atividades.ListarAndamentoAsync(
            clienteId, requisicao.Situacao,
            Paginacao.Normalizar(requisicao.Pagina, AtividadesOpcoes.TamanhoDaPagina), AtividadesOpcoes.TamanhoDaPagina, ct));
    }
}
