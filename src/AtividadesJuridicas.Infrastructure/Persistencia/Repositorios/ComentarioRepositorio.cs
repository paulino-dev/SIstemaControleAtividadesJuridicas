using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Domain.Atividades;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class ComentarioRepositorio(AppDbContext db) : IComentarioRepositorio
{
    public void Adicionar(AtividadeComentario comentario) => db.Set<AtividadeComentario>().Add(comentario);

    public async Task<IReadOnlyList<ComentarioDetalhe>> ListarDaAtividadeAsync(Guid atividadeId, CancellationToken ct)
    {
        var comentarios = await db.Set<AtividadeComentario>().AsNoTracking()
            .Where(c => c.AtividadeId == atividadeId)
            .OrderBy(c => c.DataHora).ThenBy(c => c.Id)
            .ToListAsync(ct);

        // O autor excluído logicamente continua identificado pelo nome.
        var ids = comentarios.Select(c => c.UsuarioId).Distinct().ToList();
        var nomes = await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);

        return [.. comentarios.Select(c => new ComentarioDetalhe(
            c.Id, c.UsuarioId, nomes.TryGetValue(c.UsuarioId, out var nome) ? nome : null, c.Texto, c.DataHora))];
    }
}
