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
}
