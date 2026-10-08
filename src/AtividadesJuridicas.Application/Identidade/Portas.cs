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

/// <summary><paramref name="UsuariosAtivos"/>: quantos usuários ativos têm o perfil.</summary>
public sealed record PerfilDetalhe(Guid Id, string Nome, TipoPerfil Tipo, bool Ativo, int UsuariosAtivos);

public enum TentativaDeLogin
{
    /// <summary>A tentativa foi contada e pode verificar a senha.</summary>
    Admitida,

    /// <summary>A tentativa foi contada, pode verificar a senha, e foi ela que acionou o bloqueio.</summary>
    AdmitidaEBloqueou,

    /// <summary>O usuário já estava bloqueado: a senha não deve ser verificada.</summary>
    Recusada,
}

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct);
    Task<bool> ExisteEmailAsync(Email email, Guid? excetoUsuarioId, CancellationToken ct);

    /// <summary>Usuários ativos cujo perfil é de tipo Diretoria, desconsiderando <paramref name="excetoUsuarioId"/>.</summary>
    Task<int> ContarDiretoresAtivosAsync(Guid? excetoUsuarioId, CancellationToken ct);

    Task<int> ContarAtivosComPerfilAsync(Guid perfilId, CancellationToken ct);

    Task<IReadOnlyList<UsuarioResumo>> ListarAsync(CancellationToken ct);

    /// <summary>Os usuários com os Ids informados; os inexistentes simplesmente não vêm.</summary>
    Task<IReadOnlyList<Usuario>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);

    /// <summary>Id e nome dos usuários ativos, por nome.</summary>
    Task<IReadOnlyList<(Guid Id, string Nome)>> ListarNomesDosAtivosAsync(CancellationToken ct);

    void Adicionar(Usuario usuario);

    /// <summary>
    /// Conta uma tentativa de login de forma atômica, antes de a senha ser verificada, e aciona o
    /// bloqueio quando ela é a de número <see cref="Usuario.MaximoDeFalhasDeLogin"/>. Fora da unidade
    /// de trabalho: não muda carimbos nem gera registro de alteração.
    /// </summary>
    Task<TentativaDeLogin> ReservarTentativaDeLoginAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct);

    /// <summary>Zera falhas e bloqueio e grava o último acesso. Fora da unidade de trabalho.</summary>
    Task RegistrarLoginBemSucedidoAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct);
}

public interface IPerfilRepositorio
{
    Task<Perfil?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PerfilResumo>> ListarAtivosAsync(CancellationToken ct);
    Task<IReadOnlyList<Perfil>> ObterAtivosAsync(CancellationToken ct);

    /// <summary>Os perfis com os Ids informados; os inexistentes simplesmente não vêm.</summary>
    Task<IReadOnlyList<Perfil>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);

    /// <summary>Compara sem diferenciar maiúsculas, desconsiderando <paramref name="excetoId"/>.</summary>
    Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct);

    /// <summary>Todos os perfis, ativos e inativos, por nome.</summary>
    Task<IReadOnlyList<PerfilDetalhe>> ListarAsync(CancellationToken ct);

    void Adicionar(Perfil perfil);
}

/// <summary>O que um perfil recém-criado recebe: acesso a todos os Documentos Gerais existentes.</summary>
public interface IAcessoInicialDePerfil
{
    /// <summary>
    /// Inclui o perfil na lista de acesso de cada Documento Geral que ainda não o tem e devolve quantos
    /// foram. Grava por fora dos interceptores: quem chama registra o evento de auditoria.
    /// </summary>
    Task<int> IncluirNosDocumentosGeraisAsync(Guid perfilId, CancellationToken ct);
}

public interface IHasherDeSenha
{
    string Gerar(string senha);
    bool Verificar(string senha, string hash);

    /// <summary>Hash válido de uma senha que ninguém conhece, para gastar o mesmo tempo quando o usuário não existe.</summary>
    string HashDeDescarte { get; }
}
