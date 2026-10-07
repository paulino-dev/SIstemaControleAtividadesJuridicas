using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Interceptores;

/// <summary>
/// Aplica os carimbos e a exclusão lógica em toda gravação do contexto, venha ela do
/// <c>UnidadeDeTrabalho</c> ou de um <c>SaveChanges</c> direto. Deve ser registrado antes
/// do interceptor de auditoria, que depende de ver a exclusão já convertida em alteração.
/// </summary>
public sealed class CarimbosInterceptor(IIdentidadeAtual identidade, IRelogio relogio) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Aplicar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Aplicar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Aplicar(DbContext? db)
    {
        if (db is null) return;

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
    }
}
