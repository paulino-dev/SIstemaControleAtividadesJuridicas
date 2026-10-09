using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record UsuarioAutenticado(Guid Id, string Nome, TipoPerfil TipoPerfil, Guid PerfilId, Guid? ClienteId = null)
{
    public bool EhDiretoria => TipoPerfil == TipoPerfil.Diretoria;
    public bool EhCliente => TipoPerfil == TipoPerfil.Cliente;
}

public interface IContextoDeAcesso
{
    /// <summary>
    /// Usuário da requisição, com o perfil lido do banco agora. Nulo quando não há
    /// usuário autenticado, quando ele não existe mais ou quando está inativo.
    /// </summary>
    Task<UsuarioAutenticado?> ObterAsync(CancellationToken ct);
}

public sealed class ContextoDeAcesso(
    IIdentidadeAtual identidade,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IClientesDaIdentidade clientes) : IContextoDeAcesso
{
    public async Task<UsuarioAutenticado?> ObterAsync(CancellationToken ct)
    {
        if (identidade.UsuarioId is not { } id) return null;

        var usuario = await usuarios.ObterPorIdAsync(id, ct);
        if (usuario is null || !usuario.Ativo) return null;

        // Sessão aberta antes de uma troca de senha ou de uma inativação.
        // Versão vazia nunca vale, mesmo que seja a armazenada (linhas anteriores à migração).
        if (identidade.VersaoDeSessao is { } versao &&
            (versao == Guid.Empty || versao != usuario.VersaoDeSessao)) return null;

        var perfil = await perfis.ObterPorIdAsync(usuario.PerfilId, ct);
        if (perfil is null) return null;

        if (!AcessoPeloCliente.ParCoerente(usuario, perfil)) return null;

        // Usuário de cliente só tem acesso enquanto o cliente dele existe e está ativo.
        if (!await AcessoPeloCliente.ClienteAtivoAsync(usuario, clientes, ct)) return null;

        return new UsuarioAutenticado(usuario.Id, usuario.Nome, perfil.Tipo, perfil.Id, usuario.ClienteId);
    }
}
