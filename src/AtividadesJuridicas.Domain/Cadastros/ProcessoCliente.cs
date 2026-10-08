using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

public enum PoloProcessual
{
    Ativo = 1,
    Passivo = 2,
    Terceiro = 3,
}

/// <summary>
/// Vínculo de um cliente com um processo. É uma entidade, com Id próprio, para que a entrada e a
/// saída do cliente tenham carimbos, exclusão lógica e registro de auditoria.
/// </summary>
public sealed class ProcessoCliente : Entidade
{
    private ProcessoCliente() { }

    internal ProcessoCliente(Guid processoId, Guid clienteId, PoloProcessual polo)
    {
        ProcessoId = processoId;
        ClienteId = clienteId;
        Polo = polo;
    }

    public Guid ProcessoId { get; private set; }
    public Guid ClienteId { get; private set; }
    public PoloProcessual Polo { get; private set; }

    internal void AlterarPolo(PoloProcessual polo) => Polo = polo;
}
