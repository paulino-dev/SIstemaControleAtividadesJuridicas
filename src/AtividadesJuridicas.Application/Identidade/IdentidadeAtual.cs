namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Quem está por trás da requisição em curso e de onde ela veio.</summary>
public interface IIdentidadeAtual
{
    /// <summary>Nulo quando anônimo ou sistema.</summary>
    Guid? UsuarioId { get; }

    string? Ip { get; }

    /// <summary>Versão que a sessão trouxe. Nulo quando o chamador é interno e confiável (testes, rotinas).</summary>
    Guid? VersaoDeSessao { get; }

    /// <summary>Um por requisição; liga os registros de auditoria gerados por ela.</summary>
    Guid IdCorrelacao { get; }
}

/// <summary>Preenchida no início de cada escopo de requisição por quem o abre.</summary>
public sealed class IdentidadeAtual : IIdentidadeAtual
{
    public Guid? UsuarioId { get; private set; }
    public string? Ip { get; private set; }
    public Guid? VersaoDeSessao { get; private set; }
    public Guid IdCorrelacao { get; } = Guid.CreateVersion7();

    /// <summary>
    /// Para quem representa uma sessão de verdade (a Web). Com usuário, a versão da sessão é
    /// obrigatória: sem ela o acesso não seria conferido contra o banco.
    /// </summary>
    public void Definir(Guid? usuarioId, string? ip, Guid? versaoDeSessao)
    {
        if (usuarioId is not null && versaoDeSessao is null)
            throw new ArgumentException("Um usuário identificado exige a versão da sessão.", nameof(versaoDeSessao));

        UsuarioId = usuarioId;
        Ip = ip;
        VersaoDeSessao = versaoDeSessao;
    }

    /// <summary>Para chamadores internos e confiáveis (testes, semeador, rotinas): sem versão de sessão.</summary>
    public void DefinirInterno(Guid? usuarioId, string? ip = null)
    {
        UsuarioId = usuarioId;
        Ip = ip;
        VersaoDeSessao = null;
    }
}
