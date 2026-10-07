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
    /// Grava somente a coluna de último acesso, fora da unidade de trabalho: registrar o acesso
    /// não é uma atualização de negócio e não deve alterar os carimbos de atualização.
    /// </summary>
    Task RegistrarAcessoAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct);
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
}
