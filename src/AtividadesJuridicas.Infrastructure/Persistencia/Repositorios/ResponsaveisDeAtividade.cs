using AtividadesJuridicas.Application.Documentos;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

/// <summary>O que o controle de acesso a documentos pergunta às atividades, sempre contra o banco.</summary>
public sealed class ResponsaveisDeAtividade(AppDbContext db) : IResponsaveisDeAtividade
{
    // Pelas entidades mapeadas: o filtro de exclusão lógica vale para a atividade e para a entrada do responsável.
    public Task<bool> EhResponsavelAsync(Guid atividadeId, Guid usuarioId, CancellationToken ct) =>
        db.Atividades.AsNoTracking()
            .AnyAsync(a => a.Id == atividadeId && a.Responsaveis.Any(r => r.UsuarioId == usuarioId), ct);
}
