using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record ListarUsuariosDoCliente(Guid ClienteId)
    : IRequisicao<Resultado<IReadOnlyList<UsuarioDoCliente>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Usuario";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { ClienteId });
}

public sealed class ListarUsuariosDoClienteManipulador(
    IContextoDeAcesso contexto, IUsuarioRepositorio usuarios, IClientesDaIdentidade clientes)
    : IManipulador<ListarUsuariosDoCliente, Resultado<IReadOnlyList<UsuarioDoCliente>>>
{
    public async Task<Resultado<IReadOnlyList<UsuarioDoCliente>>> ManipularAsync(
        ListarUsuariosDoCliente requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<IReadOnlyList<UsuarioDoCliente>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (await clientes.ObterAsync(requisicao.ClienteId, ct) is null)
            return Resultado<IReadOnlyList<UsuarioDoCliente>>.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.");

        return Resultado<IReadOnlyList<UsuarioDoCliente>>.Ok(await usuarios.ListarDoClienteAsync(requisicao.ClienteId, ct));
    }
}
