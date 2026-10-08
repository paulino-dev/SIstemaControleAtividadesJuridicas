using AtividadesJuridicas.Application.Cadastros;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// O que o cadastro de processos pede às atividades. Não grava nada: as atividades alteradas seguem no
/// contexto da requisição e são gravadas pelo SalvarAsync do caso de uso, junto com o processo.
/// </summary>
public sealed class AtividadesDoProcesso(AppDbContext db) : IAtividadesDoProcesso
{
    // O filtro de exclusão lógica do contexto deixa de fora as atividades excluídas.
    public async Task<IReadOnlyList<Guid>> ClientesComAtividadeAsync(
        Guid processoId, IReadOnlyCollection<Guid> clienteIds, CancellationToken ct) =>
        await db.Atividades.AsNoTracking()
            .Where(a => a.ProcessoId == processoId && a.ClienteId != null && clienteIds.Contains(a.ClienteId.Value))
            .Select(a => a.ClienteId!.Value).Distinct().ToListAsync(ct);

    public async Task<int> AcompanharAreaAsync(Guid processoId, Guid areaAtuacaoId, CancellationToken ct)
    {
        // Inteiras, como em AtividadeRepositorio.ObterPorIdAsync: responsáveis e histórico vêm junto.
        var atividades = await db.Atividades
            .Include(a => a.Responsaveis).Include(a => a.Historico)
            .Where(a => a.ProcessoId == processoId && a.AreaAtuacaoId != areaAtuacaoId)
            .ToListAsync(ct);

        foreach (var atividade in atividades) atividade.AcompanharAreaDoProcesso(areaAtuacaoId);
        return atividades.Count;
    }
}
