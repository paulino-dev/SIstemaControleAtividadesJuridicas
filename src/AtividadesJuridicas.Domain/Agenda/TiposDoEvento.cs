using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Agenda;

public enum TipoEvento
{
    Audiencia = 1,
    Reuniao = 2,
    Prazo = 3,
    Outro = 4,
}

public sealed record DadosDoEvento(
    string Titulo, string? Descricao, string? Local, TipoEvento Tipo, DateTime InicioEm, int DuracaoEstimadaMinutos);

/// <summary>Um usuário como o evento precisa conhecê-lo agora: quem é e se está ativo.</summary>
public sealed record PessoaDoEvento(Guid UsuarioId, bool Ativo);

/// <summary>Cliente, processo e atividade de um evento, já coerentes entre si.</summary>
public sealed record VinculoDoEvento
{
    private VinculoDoEvento(Guid? clienteId, Guid? processoId, Guid? atividadeId)
    {
        ClienteId = clienteId;
        ProcessoId = processoId;
        AtividadeId = atividadeId;
    }

    public Guid? ClienteId { get; }
    public Guid? ProcessoId { get; }
    public Guid? AtividadeId { get; }

    /// <summary>Sem cliente, sem processo e sem atividade.</summary>
    public static VinculoDoEvento Nenhum { get; } = new(null, null, null);

    /// <summary>
    /// Sem processo, cliente e atividade são livres. Com processo, o cliente é opcional, mas, se informado,
    /// tem de ser um dos clientes do processo.
    /// </summary>
    public static VinculoDoEvento Criar(Guid? clienteId, Processo? processo, Guid? atividadeId)
    {
        if (clienteId == Guid.Empty)
            throw new ExcecaoDeDominio("Cliente inválido.");
        if (atividadeId == Guid.Empty)
            throw new ExcecaoDeDominio("Atividade inválida.");

        if (processo is null) return new VinculoDoEvento(clienteId, null, atividadeId);

        if (clienteId is not null && processo.Clientes.All(c => c.ClienteId != clienteId))
            throw new ExcecaoDeDominio("O cliente do evento deve ser um dos clientes do processo.");

        return new VinculoDoEvento(clienteId, processo.Id, atividadeId);
    }
}

/// <summary>O que mudou nos participantes de um evento, pelos Ids dos usuários.</summary>
public sealed record AlteracaoDeParticipantes(IReadOnlyList<Guid> Adicionados, IReadOnlyList<Guid> Removidos)
{
    public bool Houve => Adicionados.Count > 0 || Removidos.Count > 0;
}
