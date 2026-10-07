using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record ListarUsuarios : IRequisicao<Resultado<IReadOnlyList<UsuarioResumo>>>;

public sealed class ListarUsuariosManipulador(IContextoDeAcesso contexto, IUsuarioRepositorio usuarios)
    : IManipulador<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>
{
    public async Task<Resultado<IReadOnlyList<UsuarioResumo>>> ManipularAsync(ListarUsuarios requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<IReadOnlyList<UsuarioResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<IReadOnlyList<UsuarioResumo>>.Ok(await usuarios.ListarAsync(ct));
    }
}
