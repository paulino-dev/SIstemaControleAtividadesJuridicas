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

    public void Definir(Guid? usuarioId, string? ip = null, Guid? versaoDeSessao = null)
    {
        UsuarioId = usuarioId;
        Ip = ip;
        VersaoDeSessao = versaoDeSessao;
    }
}
