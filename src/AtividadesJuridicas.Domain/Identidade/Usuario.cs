using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Identidade;

public sealed class Usuario : Entidade
{
    public const int TamanhoMaximoNome = 200;
    public const int TamanhoMaximoOab = 20;
    public const int MaximoDeFalhasDeLogin = 5;
    public static readonly TimeSpan DuracaoDoBloqueio = TimeSpan.FromMinutes(15);

    private Usuario() { }

    public string Nome { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    [NaoAuditar]
    public string SenhaHash { get; private set; } = null!;
    public Guid PerfilId { get; private set; }
    public string? NumeroOab { get; private set; }
    public bool Ativo { get; private set; }
    [NaoAuditar]
    public DateTime? UltimoAcessoEm { get; private set; }

    [NaoAuditar]
    public int FalhasDeLogin { get; private set; }

    [NaoAuditar]
    public DateTime? BloqueadoAte { get; private set; }

    /// <summary>Geração das sessões do usuário. Uma sessão aberta com outra versão é recusada.</summary>
    [NaoAuditar]
    public Guid VersaoDeSessao { get; private set; }

    public const string SoDeCliente = "Só um usuário de perfil Cliente é vinculado a um cliente.";
    public const string TrocaEntreTipos =
        "Um usuário de cliente não pode receber um perfil interno, nem um usuário interno o perfil de cliente.";

    /// <summary>Preenchido se e somente se o perfil do usuário é de tipo Cliente.</summary>
    public Guid? ClienteId { get; private set; }

    /// <summary>Usuário do portal. O vínculo com o cliente é o que diz de que lado o usuário está.</summary>
    public bool EhDeCliente => ClienteId is not null;

    public static Usuario Criar(
        string nome, Email email, string senhaHash, Perfil perfil, string? numeroOab = null, Guid? clienteId = null)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        if (clienteId == Guid.Empty)
            throw new ExcecaoDeDominio("Cliente inválido.");
        if (perfil.Tipo == TipoPerfil.Cliente && clienteId is null)
            throw new ExcecaoDeDominio("Informe o cliente do usuário.");
        if (perfil.Tipo != TipoPerfil.Cliente && clienteId is not null)
            throw new ExcecaoDeDominio(SoDeCliente);

        var usuario = new Usuario { Ativo = true, ClienteId = clienteId };
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
        NumeroOab = EhDeCliente ? null : oab;
    }

    public void AlterarPerfil(Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        if (!perfil.Ativo)
            throw new ExcecaoDeDominio("Não é possível atribuir um perfil inativo.");

        // Quem é de cliente não vira equipe, e vice-versa.
        if ((perfil.Tipo == TipoPerfil.Cliente) != EhDeCliente)
            throw new ExcecaoDeDominio(TrocaEntreTipos);

        PerfilId = perfil.Id;
    }

    /// <summary>
    /// Troca o cliente de um usuário de cliente. Devolve falso quando já era esse; ao trocar, as sessões
    /// abertas deixam de valer.
    /// </summary>
    public bool VincularAoCliente(Guid clienteId)
    {
        if (!EhDeCliente)
            throw new ExcecaoDeDominio(SoDeCliente);
        if (clienteId == Guid.Empty)
            throw new ExcecaoDeDominio("Cliente inválido.");
        if (ClienteId == clienteId) return false;

        ClienteId = clienteId;
        RenovarSessao();
        return true;
    }

    public void DefinirSenha(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("O hash da senha é obrigatório.", nameof(senhaHash));
        SenhaHash = senhaHash;
        RenovarSessao();
    }

    public void Inativar()
    {
        Ativo = false;
        RenovarSessao();
    }

    public void Ativar() => Ativo = true;

    public bool EstaBloqueado(DateTime agoraUtc) => BloqueadoAte is { } ate && ate > agoraUtc;

    /// <summary>Conta uma senha errada. Devolve verdadeiro quando esta falha causou o bloqueio.</summary>
    public bool RegistrarFalhaDeLogin(DateTime agoraUtc)
    {
        FalhasDeLogin++;
        if (FalhasDeLogin < MaximoDeFalhasDeLogin) return false;

        FalhasDeLogin = 0;
        BloqueadoAte = agoraUtc + DuracaoDoBloqueio;
        return true;
    }

    public void RegistrarAcesso(DateTime agoraUtc)
    {
        UltimoAcessoEm = agoraUtc;
        Desbloquear();
    }

    public void Desbloquear()
    {
        FalhasDeLogin = 0;
        BloqueadoAte = null;
    }

    private void RenovarSessao() => VersaoDeSessao = Guid.CreateVersion7();
}
