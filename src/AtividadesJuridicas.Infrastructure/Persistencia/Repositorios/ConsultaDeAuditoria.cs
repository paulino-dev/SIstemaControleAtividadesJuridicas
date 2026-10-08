using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class ConsultaDeAuditoria(AppDbContext db) : IConsultaDeAuditoria
{
    public async Task<Pagina<RegistroAuditoriaResumo>> ConsultarAsync(
        FiltroDeAuditoria filtro, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.RegistrosAuditoria.AsNoTracking();

        if (filtro.DeUtc is { } de) consulta = consulta.Where(r => r.DataHora >= de);
        if (filtro.AteUtc is { } ate) consulta = consulta.Where(r => r.DataHora < ate);
        if (filtro.UsuarioId is { } usuarioId) consulta = consulta.Where(r => r.UsuarioId == usuarioId);
        if (!string.IsNullOrWhiteSpace(filtro.Entidade)) consulta = consulta.Where(r => r.Entidade == filtro.Entidade);
        if (filtro.Operacao is { } operacao) consulta = consulta.Where(r => r.Operacao == operacao);
        if (filtro.EntidadeId is { } entidadeId) consulta = consulta.Where(r => r.EntidadeId == entidadeId);
        if (filtro.IdCorrelacao is { } idCorrelacao) consulta = consulta.Where(r => r.IdCorrelacao == idCorrelacao);

        var total = await consulta.CountAsync(ct);

        // Usuários excluídos logicamente continuam identificados pelo nome.
        var usuarios = db.Usuarios.IgnoreQueryFilters();
        var itens = await consulta
            .OrderByDescending(r => r.DataHora).ThenByDescending(r => r.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(r => new RegistroAuditoriaResumo(
                r.Id, r.DataHora, r.UsuarioId,
                usuarios.Where(u => u.Id == r.UsuarioId).Select(u => u.Nome).FirstOrDefault(),
                r.Operacao, r.Entidade, r.EntidadeId, r.Evento, r.Dados, r.Ip, r.IdCorrelacao))
            .ToListAsync(ct);

        return new Pagina<RegistroAuditoriaResumo>(itens, total, pagina, tamanho);
    }

    public async Task<Pagina<RegistroAcessoResumo>> ConsultarAcessosAsync(
        FiltroDeAcessos filtro, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.RegistrosAcesso.AsNoTracking();

        if (filtro.DeUtc is { } de) consulta = consulta.Where(r => r.DataHora >= de);
        if (filtro.AteUtc is { } ate) consulta = consulta.Where(r => r.DataHora < ate);
        if (filtro.UsuarioId is { } usuarioId) consulta = consulta.Where(r => r.UsuarioId == usuarioId);
        if (!string.IsNullOrWhiteSpace(filtro.TipoRecurso)) consulta = consulta.Where(r => r.TipoRecurso == filtro.TipoRecurso);
        if (filtro.Acao is { } acao) consulta = consulta.Where(r => r.Acao == acao);

        var total = await consulta.CountAsync(ct);

        var usuarios = db.Usuarios.IgnoreQueryFilters();
        var itens = await consulta
            .OrderByDescending(r => r.DataHora).ThenByDescending(r => r.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(r => new RegistroAcessoResumo(
                r.Id, r.DataHora, r.UsuarioId,
                usuarios.Where(u => u.Id == r.UsuarioId).Select(u => u.Nome).FirstOrDefault(),
                r.TipoRecurso, r.RecursoId, r.Acao, r.Detalhe, r.Ip))
            .ToListAsync(ct);

        return new Pagina<RegistroAcessoResumo>(itens, total, pagina, tamanho);
    }
}
