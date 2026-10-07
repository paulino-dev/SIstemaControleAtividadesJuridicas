using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record UsuarioResumo(
    Guid Id,
    string Nome,
    string Email,
    Guid PerfilId,
    string PerfilNome,
    TipoPerfil TipoPerfil,
    string? NumeroOab,
    bool Ativo,
    DateTime? UltimoAcessoEm);

public sealed record PerfilResumo(Guid Id, string Nome, TipoPerfil Tipo);

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct);
    Task<bool> ExisteEmailAsync(Email email, Guid? excetoUsuarioId, CancellationToken ct);

    /// <summary>Usuários ativos cujo perfil é de tipo Diretoria, desconsiderando <paramref name="excetoUsuarioId"/>.</summary>
    Task<int> ContarDiretoresAtivosAsync(Guid? excetoUsuarioId, CancellationToken ct);

    Task<IReadOnlyList<UsuarioResumo>> ListarAsync(CancellationToken ct);
    void Adicionar(Usuario usuario);

    /// <summary>
    /// Grava somente último acesso, falhas e bloqueio de login, fora da unidade de trabalho: não são
    /// alteração de negócio e não devem mudar os carimbos nem gerar registro de alteração.
    /// </summary>
    Task GravarEstadoDeLoginAsync(Usuario usuario, CancellationToken ct);
}

public interface IPerfilRepositorio
{
    Task<Perfil?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PerfilResumo>> ListarAtivosAsync(CancellationToken ct);
}

public interface IHasherDeSenha
{
    string Gerar(string senha);
    bool Verificar(string senha, string hash);

    /// <summary>Hash válido de uma senha que ninguém conhece, para gastar o mesmo tempo quando o usuário não existe.</summary>
    string HashDeDescarte { get; }
}
