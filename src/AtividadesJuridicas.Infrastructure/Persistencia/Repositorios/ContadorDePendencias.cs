using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Atividades;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// O que o cadastro de usuários pede às atividades e à agenda. Somente leitura; os filtros de exclusão lógica
/// do contexto deixam de fora atividades, responsáveis, eventos e participantes excluídos.
/// </summary>
public sealed class ContadorDePendencias(AppDbContext db) : IPendenciasDoUsuario
{
    private static readonly StatusAtividade[] StatusQueAguardamPartes =
        [StatusAtividade.Direcionada, StatusAtividade.ParaCorrecao, StatusAtividade.Reaberta];

    public async Task<PendenciasDoUsuario> ContarAsync(
        Guid usuarioId, DateTime agoraUtc, CancellationToken ct)
    {
        var partes = await db.Set<AtividadeResponsavel>().AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId && r.ConcluidoEm == null)
            .CountAsync(r => db.Atividades.Any(a => a.Id == r.AtividadeId && StatusQueAguardamPartes.Contains(a.Status)), ct);

        var revisoes = await db.Atividades.AsNoTracking()
            .CountAsync(a => a.RevisorId == usuarioId && a.Status != StatusAtividade.Concluida, ct);

        // O fim do evento (início + duração) não é coluna. A duração vai até 7 dias: só entram na conta os
        // que começaram depois de "agora" menos esse máximo, e o fim exato é conferido em memória.
        var inicioMinimo = agoraUtc.AddMinutes(-Evento.DuracaoMaximaMinutos);
        var candidatos = await db.Eventos.AsNoTracking()
            .Where(e => e.InicioEm > inicioMinimo && e.Participantes.Any(p => p.UsuarioId == usuarioId))
            .Select(e => new { e.InicioEm, e.DuracaoEstimadaMinutos })
            .ToListAsync(ct);
        var eventos = candidatos.Count(e => e.InicioEm.AddMinutes(e.DuracaoEstimadaMinutos) > agoraUtc);

        return new PendenciasDoUsuario(partes, revisoes, eventos);
    }
}
