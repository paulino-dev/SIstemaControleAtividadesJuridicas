using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Cadastros;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class ProcessoRepositorio(AppDbContext db) : IProcessoRepositorio
{
    public Task<Processo?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Processos.Include(p => p.Clientes).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExisteNumeroCnjAsync(NumeroCnj numero, Guid? excetoId, CancellationToken ct) =>
        db.Processos.AnyAsync(p => p.NumeroCnj == numero && p.Id != excetoId, ct);

    public async Task<Pagina<ProcessoResumo>> ListarAsync(
        FiltroDeProcessos filtro, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.Processos.AsNoTracking();

        if (filtro.Status is { } status) consulta = consulta.Where(p => p.Status == status);
        if (filtro.ClienteId is { } clienteId) consulta = consulta.Where(p => p.Clientes.Any(v => v.ClienteId == clienteId));

        if (NumeroCnj.TentarCriar(filtro.Busca, out var numero))
        {
            consulta = consulta.Where(p => p.NumeroCnj == numero);
        }
        else if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            // % e _ digitados pelo usuário são texto, não curingas.
            var padrao = "%" + filtro.Busca.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            consulta = consulta.Where(p =>
                EF.Functions.ILike(EF.Functions.Unaccent(p.Titulo), EF.Functions.Unaccent(padrao), "\\") ||
                (p.ParteContraria != null &&
                 EF.Functions.ILike(EF.Functions.Unaccent(p.ParteContraria), EF.Functions.Unaccent(padrao), "\\")));
        }

        var total = await consulta.CountAsync(ct);
        var processos = await consulta
            .OrderBy(p => p.Titulo).ThenBy(p => p.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Include(p => p.Clientes)
            .ToListAsync(ct);

        var areas = await NomesDasAreasAsync(processos.Select(p => p.AreaAtuacaoId), ct);
        var clientes = await ClientesAsync(processos.SelectMany(p => p.Clientes).Select(v => v.ClienteId), ct);

        var itens = processos.Select(p => new ProcessoResumo(
            p.Id, p.NumeroCnj?.Formatado, p.Titulo, areas.GetValueOrDefault(p.AreaAtuacaoId, string.Empty), p.Status,
            [.. p.Clientes.Select(v => clientes[v.ClienteId].Nome).Order(StringComparer.CurrentCultureIgnoreCase)],
            p.ParteContraria)).ToList();

        return new Pagina<ProcessoResumo>(itens, total, pagina, tamanho);
    }

    public async Task<ProcessoDetalhe?> ObterDetalheAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Processos.AsNoTracking().Include(x => x.Clientes).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;

        var areas = await NomesDasAreasAsync([p.AreaAtuacaoId], ct);
        var clientes = await ClientesAsync(p.Clientes.Select(v => v.ClienteId), ct);

        return new ProcessoDetalhe(
            p.Id, p.NumeroCnj?.Formatado, p.Titulo, p.AreaAtuacaoId, areas.GetValueOrDefault(p.AreaAtuacaoId, string.Empty),
            p.Status, p.Orgao, p.Comarca, p.ParteContraria, p.DataDistribuicao, p.ValorCausa, p.Observacoes,
            [.. p.Clientes
                .Select(v => (Vinculo: v, Cliente: clientes[v.ClienteId]))
                .Select(x => new ClienteDoProcessoDetalhe(
                    x.Cliente.Id, x.Cliente.Nome, Documento(x.Cliente), x.Vinculo.Polo, x.Cliente.Ativo))
                .OrderBy(c => c.Nome, StringComparer.CurrentCultureIgnoreCase)]);
    }

    public void Adicionar(Processo processo) => db.Processos.Add(processo);

    // Áreas e clientes excluídos logicamente continuam identificados pelo nome nos processos que os citam.
    private async Task<Dictionary<Guid, string>> NomesDasAreasAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var lista = ids.Distinct().ToList();
        return await db.AreasAtuacao.IgnoreQueryFilters().AsNoTracking()
            .Where(a => lista.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Nome, ct);
    }

    private async Task<Dictionary<Guid, Cliente>> ClientesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var lista = ids.Distinct().ToList();
        return await db.Clientes.IgnoreQueryFilters().AsNoTracking()
            .Where(c => lista.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
    }

    private static string Documento(Cliente cliente) => cliente switch
    {
        ClientePessoaFisica pf => pf.Cpf.Formatado,
        ClientePessoaJuridica pj => pj.Cnpj.Formatado,
        _ => string.Empty,
    };
}
