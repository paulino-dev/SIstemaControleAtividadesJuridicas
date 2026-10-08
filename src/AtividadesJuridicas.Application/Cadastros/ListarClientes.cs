using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ListarClientes(string? Busca = null, bool IncluirInativos = false, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<ClienteResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Cliente";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Busca, IncluirInativos, Pagina });
}

public sealed class ListarClientesManipulador(IContextoDeAcesso contexto, IClienteRepositorio clientes)
    : IManipulador<ListarClientes, Resultado<Pagina<ClienteResumo>>>
{
    public async Task<Resultado<Pagina<ClienteResumo>>> ManipularAsync(ListarClientes requisicao, CancellationToken ct)
    {
        // Diretoria e Produção consultam clientes; só a Diretoria altera.
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<Pagina<ClienteResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var pagina = await clientes.ListarAsync(
            requisicao.Busca, requisicao.IncluirInativos, Math.Max(1, requisicao.Pagina), ClientesOpcoes.TamanhoDaPagina, ct);

        return Resultado<Pagina<ClienteResumo>>.Ok(new Pagina<ClienteResumo>(
            [.. pagina.Itens.Select(ClienteMapeamento.ParaResumo)], pagina.Total, pagina.Numero, pagina.Tamanho));
    }
}
