using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class AreaAtuacaoRepositorio(AppDbContext db) : IAreaAtuacaoRepositorio
{
    public Task<AreaAtuacao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.AreasAtuacao.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct)
    {
        var procurado = nome.Trim().ToLowerInvariant();
        return db.AreasAtuacao.AnyAsync(a => a.Id != excetoId && a.Nome.ToLower() == procurado, ct);
    }

    public async Task<IReadOnlyList<AreaAtuacaoResumo>> ListarAsync(bool somenteAtivas, CancellationToken ct) =>
        await db.AreasAtuacao.AsNoTracking()
            .Where(a => !somenteAtivas || a.Ativo)
            .OrderBy(a => a.Nome)
            .Select(a => new AreaAtuacaoResumo(a.Id, a.Nome, a.Ativo))
            .ToListAsync(ct);

    public void Adicionar(AreaAtuacao area) => db.AreasAtuacao.Add(area);
}
