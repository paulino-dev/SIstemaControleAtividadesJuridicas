using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Atividades;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class AtividadeRepositorio(AppDbContext db) : IAtividadeRepositorio
{
    public Task<Atividade?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Atividades.Include(a => a.Responsaveis).Include(a => a.Historico).FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<ClienteAtualDaAtividade?> ObterClienteAtualAsync(Guid atividadeId, CancellationToken ct) =>
        db.Atividades.AsNoTracking().Where(a => a.Id == atividadeId)
            .Select(a => new ClienteAtualDaAtividade(a.ClienteId)).FirstOrDefaultAsync(ct);

    public async Task<Pagina<AtividadeResumo>> ListarAsync(
        FiltroDeAtividades filtro, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.Atividades.AsNoTracking();

        if (filtro.Status is { } status) consulta = consulta.Where(a => a.Status == status);
        if (filtro.ResponsavelId is { } responsavelId)
            consulta = consulta.Where(a => a.Responsaveis.Any(r => r.UsuarioId == responsavelId));
        if (filtro.RevisorId is { } revisorId) consulta = consulta.Where(a => a.RevisorId == revisorId);
        if (filtro.ClienteId is { } clienteId) consulta = consulta.Where(a => a.ClienteId == clienteId);
        if (filtro.ProcessoId is { } processoId) consulta = consulta.Where(a => a.ProcessoId == processoId);
        if (filtro.SomenteAtrasadas)
        {
            var hoje = filtro.Hoje;
            consulta = consulta.Where(a => a.Status != StatusAtividade.Concluida && a.DataEntrega < hoje);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            // % e _ digitados pelo usuário são texto, não curingas.
            var padrao = "%" + filtro.Busca.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            consulta = consulta.Where(a => EF.Functions.ILike(EF.Functions.Unaccent(a.Titulo), EF.Functions.Unaccent(padrao), "\\"));
        }

        var total = await consulta.CountAsync(ct);
        var atividades = await consulta
            .OrderBy(a => a.DataEntrega == null).ThenBy(a => a.DataEntrega)
            .ThenByDescending(a => a.CriadoEm).ThenByDescending(a => a.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Include(a => a.Responsaveis)
            .ToListAsync(ct);

        // Registros excluídos logicamente continuam identificados pelo nome nas atividades que os citam.
        var idsDeClientes = atividades.Where(a => a.ClienteId != null).Select(a => a.ClienteId!.Value).Distinct().ToList();
        var idsDeProcessos = atividades.Where(a => a.ProcessoId != null).Select(a => a.ProcessoId!.Value).Distinct().ToList();
        var idsDeAreas = atividades.Where(a => a.AreaAtuacaoId != null).Select(a => a.AreaAtuacaoId!.Value).Distinct().ToList();
        var idsDeUsuarios = atividades
            .SelectMany(a => a.Responsaveis.Select(r => r.UsuarioId).Concat(a.RevisorId is { } r ? [r] : []))
            .Distinct().ToList();

        var clientes = await db.Clientes.IgnoreQueryFilters().AsNoTracking()
            .Where(c => idsDeClientes.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nome, ct);
        var processos = await db.Processos.IgnoreQueryFilters().AsNoTracking()
            .Where(p => idsDeProcessos.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Titulo, ct);
        var areas = await db.AreasAtuacao.IgnoreQueryFilters().AsNoTracking()
            .Where(x => idsDeAreas.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Nome, ct);
        var usuarios = await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Where(u => idsDeUsuarios.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);

        static string? Nome(Dictionary<Guid, string> nomes, Guid? id) =>
            id is { } x && nomes.TryGetValue(x, out var nome) ? nome : null;

        var itens = atividades.Select(a => new AtividadeResumo(
            a.Id, a.Titulo, a.Status, a.DataEntrega, a.EstaAtrasada(filtro.Hoje),
            Nome(clientes, a.ClienteId), Nome(processos, a.ProcessoId), Nome(areas, a.AreaAtuacaoId),
            [.. a.Responsaveis.Select(r => Nome(usuarios, r.UsuarioId) ?? string.Empty)
                .Order(StringComparer.CurrentCultureIgnoreCase)],
            Nome(usuarios, a.RevisorId))).ToList();

        return new Pagina<AtividadeResumo>(itens, total, pagina, tamanho);
    }

    public void Adicionar(Atividade atividade) => db.Atividades.Add(atividade);

    public void Remover(Atividade atividade) => db.Atividades.Remove(atividade);
}
