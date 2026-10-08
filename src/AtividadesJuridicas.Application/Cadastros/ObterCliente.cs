using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ObterCliente(Guid Id) : IRequisicao<Resultado<ClienteDetalhe>>, IConsultaAuditavel
{
    public string TipoRecurso => "Cliente";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => Id;
}

public sealed class ObterClienteManipulador(IContextoDeAcesso contexto, IClienteRepositorio clientes)
    : IManipulador<ObterCliente, Resultado<ClienteDetalhe>>
{
    public async Task<Resultado<ClienteDetalhe>> ManipularAsync(ObterCliente requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<ClienteDetalhe>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var cliente = await clientes.ObterPorIdAsync(requisicao.Id, ct);
        return cliente is null
            ? Resultado<ClienteDetalhe>.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.")
            : Resultado<ClienteDetalhe>.Ok(ClienteMapeamento.ParaDetalhe(cliente));
    }
}
