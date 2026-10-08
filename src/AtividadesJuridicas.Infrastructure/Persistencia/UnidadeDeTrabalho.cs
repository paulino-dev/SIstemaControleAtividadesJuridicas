using AtividadesJuridicas.Application.Comum;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

/// <summary>Grava as alterações pendentes. Carimbos, exclusão lógica e auditoria são dos interceptores do contexto.</summary>
public sealed class UnidadeDeTrabalho(AppDbContext db) : IUnidadeDeTrabalho
{
    public async Task SalvarAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            throw new ExcecaoDeConflito("Já existe um registro com o mesmo valor único.", ex, pg.ConstraintName);
        }
    }
}
