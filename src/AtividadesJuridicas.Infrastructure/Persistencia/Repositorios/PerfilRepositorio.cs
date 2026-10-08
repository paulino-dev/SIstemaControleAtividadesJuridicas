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

    public Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct)
    {
        var procurado = nome.Trim().ToLowerInvariant();
        return db.Perfis.AnyAsync(p => p.Id != excetoId && p.Nome.ToLower() == procurado, ct);
    }

    public async Task<IReadOnlyList<PerfilDetalhe>> ListarAsync(CancellationToken ct) =>
        await db.Perfis.AsNoTracking()
            .OrderBy(p => p.Nome)
            .Select(p => new PerfilDetalhe(
                p.Id, p.Nome, p.Tipo, p.Ativo, db.Usuarios.Count(u => u.Ativo && u.PerfilId == p.Id)))
            .ToListAsync(ct);

    public void Adicionar(Perfil perfil) => db.Perfis.Add(perfil);
}
