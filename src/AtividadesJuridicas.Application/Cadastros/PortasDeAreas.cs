using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record AreaAtuacaoResumo(Guid Id, string Nome, bool Ativo);

public interface IAreaAtuacaoRepositorio
{
    Task<AreaAtuacao?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Compara sem diferenciar maiúsculas de minúsculas.</summary>
    Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct);

    Task<IReadOnlyList<AreaAtuacaoResumo>> ListarAsync(bool somenteAtivas, CancellationToken ct);
    void Adicionar(AreaAtuacao area);
}
