using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record DefinirAtivoCliente(Guid Id, bool Ativo) : IRequisicao<Resultado>;

public sealed class DefinirAtivoClienteManipulador(
    IContextoDeAcesso contexto,
    IClienteRepositorio clientes,
    IUnidadeDeTrabalho unidade) : IManipulador<DefinirAtivoCliente, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirAtivoCliente requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var cliente = await clientes.ObterPorIdAsync(requisicao.Id, ct);
        if (cliente is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.");

        if (cliente.Ativo == requisicao.Ativo) return Resultado.Ok();

        if (requisicao.Ativo) cliente.Ativar();
        else cliente.Inativar();

        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
