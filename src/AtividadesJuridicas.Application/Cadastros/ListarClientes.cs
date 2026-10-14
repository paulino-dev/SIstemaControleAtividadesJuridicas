using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ListarClientes(string? Busca = null, bool IncluirInativos = false, int Pagina = 1)
    : IRequisicao<Resultado<PaginaDeClientes>>, IConsultaAuditavel
{
    public string TipoRecurso => "Cliente";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Busca, IncluirInativos, Pagina });
}

public sealed class ListarClientesManipulador(IContextoDeAcesso contexto, IClienteRepositorio clientes)
    : IManipulador<ListarClientes, Resultado<PaginaDeClientes>>
{
    public async Task<Resultado<PaginaDeClientes>> ManipularAsync(ListarClientes requisicao, CancellationToken ct)
    {
        // Diretoria e Produção consultam clientes; só a Diretoria altera.
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<PaginaDeClientes>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var pagina = await clientes.ListarAsync(
            requisicao.Busca, requisicao.IncluirInativos, Paginacao.Normalizar(requisicao.Pagina, ClientesOpcoes.TamanhoDaPagina), ClientesOpcoes.TamanhoDaPagina, ct);

        var contagem = await clientes.ContarAsync(ct);

        return Resultado<PaginaDeClientes>.Ok(new PaginaDeClientes(
            [.. pagina.Itens.Select(ClienteMapeamento.ParaResumo)], pagina.Total, pagina.Numero, pagina.Tamanho,
            contagem.Total, contagem.Inativos));
    }
}
