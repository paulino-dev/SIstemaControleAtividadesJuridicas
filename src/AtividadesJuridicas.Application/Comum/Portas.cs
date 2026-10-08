namespace AtividadesJuridicas.Application.Comum;

public interface IUnidadeDeTrabalho
{
    /// <summary>Grava as alterações pendentes. Lança <see cref="ExcecaoDeConflito"/> em violação de unicidade.</summary>
    Task SalvarAsync(CancellationToken ct);

    /// <summary>
    /// Roda <paramref name="acao"/> numa transação: tudo o que ela gravar (inclusive por mais de um
    /// <see cref="SalvarAsync"/>) é confirmado junto, ou nada é, se ela lançar.
    /// </summary>
    Task EmTransacaoAsync(Func<Task> acao, CancellationToken ct);
}

public interface IRelogio
{
    DateTime AgoraUtc { get; }
}

/// <summary>Outra operação gravou um valor que deveria ser único.</summary>
public sealed class ExcecaoDeConflito(string mensagem, Exception? interna = null, string? restricao = null)
    : Exception(mensagem, interna)
{
    /// <summary>Nome da restrição de unicidade violada, quando o banco o informa.</summary>
    public string? Restricao { get; } = restricao;
}

/// <summary>O registro foi alterado por outra pessoa desde que foi lido.</summary>
public sealed class ExcecaoDeConcorrencia(string mensagem, Exception? interna = null) : Exception(mensagem, interna);
