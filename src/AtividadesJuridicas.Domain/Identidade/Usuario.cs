using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Identidade;

public sealed class Usuario : Entidade
{
    public const int TamanhoMaximoNome = 200;
    public const int TamanhoMaximoOab = 20;

    private Usuario() { }

    public string Nome { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    [NaoAuditar]
    public string SenhaHash { get; private set; } = null!;
    public Guid PerfilId { get; private set; }
    public string? NumeroOab { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime? UltimoAcessoEm { get; private set; }

    public static Usuario Criar(string nome, Email email, string senhaHash, Perfil perfil, string? numeroOab = null)
    {
        var usuario = new Usuario { Ativo = true };
        usuario.AlterarDados(nome, email, numeroOab);
        usuario.DefinirSenha(senhaHash);
        usuario.AlterarPerfil(perfil);
        return usuario;
    }

    public void AlterarDados(string nome, Email email, string? numeroOab)
    {
        ArgumentNullException.ThrowIfNull(email);
        if (string.IsNullOrWhiteSpace(nome))
            throw new ExcecaoDeDominio("O nome é obrigatório.");
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome deve ter até {TamanhoMaximoNome} caracteres.");

        var oab = string.IsNullOrWhiteSpace(numeroOab) ? null : numeroOab.Trim();
        if (oab is { Length: > TamanhoMaximoOab })
            throw new ExcecaoDeDominio($"O número da OAB deve ter até {TamanhoMaximoOab} caracteres.");

        Nome = nome.Trim();
        Email = email;
        NumeroOab = oab;
    }

    public void AlterarPerfil(Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        if (!perfil.Ativo)
            throw new ExcecaoDeDominio("Não é possível atribuir um perfil inativo.");
        PerfilId = perfil.Id;
    }

    public void DefinirSenha(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("O hash da senha é obrigatório.", nameof(senhaHash));
        SenhaHash = senhaHash;
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;

    public void RegistrarAcesso(DateTime agoraUtc) => UltimoAcessoEm = agoraUtc;
}
