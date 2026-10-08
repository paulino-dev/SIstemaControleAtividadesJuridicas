using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class EventoRepositorio(AppDbContext db) : IEventoRepositorio
{
    // Um evento dura no máximo Evento.DuracaoMaximaMinutos (7 dias). Por isso quem termina depois de um instante T
    // começou depois de T menos essa duração: filtrar o início por uma janela ampliada nunca perde um evento, e o
    // índice em inicio_em é aproveitado. O refinamento exato (intervalos semiabertos) é feito em memória, com a
    // mesma regra do domínio (Evento.Sobrepoe).
    private static readonly TimeSpan DuracaoMaxima = TimeSpan.FromMinutes(Evento.DuracaoMaximaMinutos);

    public Task<Evento?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Eventos.Include(e => e.Participantes).FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<EventoResumo>> ListarAsync(FiltroDeEventos filtro, CancellationToken ct)
    {
        var inicioDaJanela = filtro.DeUtc - DuracaoMaxima;
        var consulta = db.Eventos.AsNoTracking()
            .Where(e => e.InicioEm < filtro.AteUtc && e.InicioEm >= inicioDaJanela);

        if (filtro.Tipo is { } tipo) consulta = consulta.Where(e => e.Tipo == tipo);
        if (filtro.ParticipanteId is { } participanteId)
            consulta = consulta.Where(e => e.Participantes.Any(p => p.UsuarioId == participanteId));
        if (filtro.VisivelPara is { } visivelPara)
            consulta = consulta.Where(e => e.CriadorId == visivelPara || e.Participantes.Any(p => p.UsuarioId == visivelPara));

        var candidatos = await consulta.Include(e => e.Participantes).ToListAsync(ct);
        var eventos = candidatos
            .Where(e => e.Sobrepoe(filtro.DeUtc, filtro.AteUtc))
            .OrderBy(e => e.InicioEm).ThenBy(e => e.Id)
            .ToList();

        // Registros excluídos logicamente continuam identificados pelo nome nos eventos que os citam.
        var idsDeClientes = eventos.Where(e => e.ClienteId != null).Select(e => e.ClienteId!.Value).Distinct().ToList();
        var idsDeProcessos = eventos.Where(e => e.ProcessoId != null).Select(e => e.ProcessoId!.Value).Distinct().ToList();
        var idsDeUsuarios = eventos.SelectMany(e => e.Participantes.Select(p => p.UsuarioId)).Distinct().ToList();

        var clientes = await db.Clientes.IgnoreQueryFilters().AsNoTracking()
            .Where(c => idsDeClientes.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nome, ct);
        var processos = await db.Processos.IgnoreQueryFilters().AsNoTracking()
            .Where(p => idsDeProcessos.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Titulo, ct);
        var usuarios = await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Where(u => idsDeUsuarios.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);

        static string? Nome(Dictionary<Guid, string> nomes, Guid? id) =>
            id is { } x && nomes.TryGetValue(x, out var nome) ? nome : null;

        return [.. eventos.Select(e => new EventoResumo(
            e.Id, e.Titulo, e.Tipo, e.InicioEm, e.FimEm, e.Local, Nome(clientes, e.ClienteId), Nome(processos, e.ProcessoId),
            [.. e.Participantes.Select(p => Nome(usuarios, p.UsuarioId) ?? string.Empty)
                .Order(StringComparer.CurrentCultureIgnoreCase)]))];
    }

    public async Task<IReadOnlyList<SobreposicaoDeAgenda>> SobreposicoesAsync(
        Guid? excetoEventoId, IReadOnlyCollection<Guid> usuarioIds, DateTime inicioUtc, DateTime fimUtc, CancellationToken ct)
    {
        // Pelas entidades mapeadas, para que os filtros de exclusão lógica valham para o evento e para o participante.
        var inicioDaJanela = inicioUtc - DuracaoMaxima;
        var consulta = db.Eventos.AsNoTracking()
            .Where(e => e.InicioEm < fimUtc && e.InicioEm >= inicioDaJanela)
            .Where(e => e.Participantes.Any(p => usuarioIds.Contains(p.UsuarioId)));
        if (excetoEventoId is { } exceto) consulta = consulta.Where(e => e.Id != exceto);

        var candidatos = (await consulta.Include(e => e.Participantes).ToListAsync(ct))
            .Where(e => e.Sobrepoe(inicioUtc, fimUtc))
            .ToList();

        var idsDeUsuarios = candidatos.SelectMany(e => e.Participantes.Select(p => p.UsuarioId))
            .Where(usuarioIds.Contains).Distinct().ToList();
        var nomes = await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Where(u => idsDeUsuarios.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);

        return [.. candidatos
            .SelectMany(e => e.Participantes.Where(p => usuarioIds.Contains(p.UsuarioId)).Select(p =>
                new SobreposicaoDeAgenda(p.UsuarioId, nomes.GetValueOrDefault(p.UsuarioId), e.InicioEm, e.FimEm)))
            .OrderBy(s => s.UsuarioNome, StringComparer.CurrentCultureIgnoreCase).ThenBy(s => s.InicioEm)];
    }

    public void Adicionar(Evento evento) => db.Eventos.Add(evento);

    public void Remover(Evento evento) => db.Eventos.Remove(evento);
}
