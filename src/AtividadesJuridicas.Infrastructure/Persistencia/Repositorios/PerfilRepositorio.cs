using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class PerfilRepositorio(AppDbContext db) : IPerfilRepositorio
{
    public Task<Perfil?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Perfis.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<PerfilResumo>> ListarAtivosAsync(CancellationToken ct) =>
        await db.Perfis.AsNoTracking()
            .Where(p => p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new PerfilResumo(p.Id, p.Nome, p.Tipo))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Perfil>> ObterAtivosAsync(CancellationToken ct) =>
        await db.Perfis.Where(p => p.Ativo).OrderBy(p => p.Nome).ToListAsync(ct);

    public async Task<IReadOnlyList<Perfil>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        await db.Perfis.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
}
