using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

public sealed class UnidadeDeTrabalho(AppDbContext db, IIdentidadeAtual identidade, IRelogio relogio) : IUnidadeDeTrabalho
{
    public async Task SalvarAsync(CancellationToken ct)
    {
        var agora = relogio.AgoraUtc;
        var usuarioId = identidade.UsuarioId;

        foreach (var entrada in db.ChangeTracker.Entries<Entidade>().ToList())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    entrada.Entity.CarimbarCriacao(agora, usuarioId);
                    break;
                case EntityState.Modified:
                    entrada.Entity.CarimbarAtualizacao(agora, usuarioId);
                    break;
                case EntityState.Deleted:
                    // Nada é removido fisicamente: a exclusão vira uma atualização.
                    entrada.State = EntityState.Unchanged;
                    entrada.Entity.MarcarExcluido(agora, usuarioId);
                    break;
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ExcecaoDeConflito("Já existe um registro com o mesmo valor único.", ex);
        }
    }
}
