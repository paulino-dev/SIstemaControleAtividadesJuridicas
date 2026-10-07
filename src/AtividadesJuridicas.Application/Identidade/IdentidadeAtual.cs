namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Quem está por trás da requisição em curso e de onde ela veio.</summary>
public interface IIdentidadeAtual
{
    /// <summary>Nulo quando anônimo ou sistema.</summary>
    Guid? UsuarioId { get; }

    string? Ip { get; }

    /// <summary>Um por requisição; liga os registros de auditoria gerados por ela.</summary>
    Guid IdCorrelacao { get; }
}

/// <summary>Preenchida no início de cada escopo de requisição por quem o abre.</summary>
public sealed class IdentidadeAtual : IIdentidadeAtual
{
    public Guid? UsuarioId { get; private set; }
    public string? Ip { get; private set; }
    public Guid IdCorrelacao { get; } = Guid.CreateVersion7();

    public void Definir(Guid? usuarioId, string? ip = null)
    {
        UsuarioId = usuarioId;
        Ip = ip;
    }
}
