using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Solicitacoes;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Solicitacoes;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class SolicitacaoRepositorio(AppDbContext db) : ISolicitacaoRepositorio
{
    public Task<SolicitacaoDoCliente?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.SolicitacoesDoCliente.FirstOrDefaultAsync(s => s.Id == id, ct);

    public void Adicionar(SolicitacaoDoCliente solicitacao) => db.SolicitacoesDoCliente.Add(solicitacao);

    public async Task<Pagina<SolicitacaoDoPortal>> ListarDoClienteAsync(
        Guid clienteId, SituacaoParaOCliente? situacao, int pagina, int tamanho, CancellationToken ct)
    {
        // A situação vista pelo cliente é calculada no banco, numa consulta só, e o filtro usa a mesma expressão. A
        // atividade vinculada só conta se estiver viva: o filtro de exclusão lógica de Atividades vale na subconsulta.
        var comSituacao = db.SolicitacoesDoCliente.AsNoTracking()
            .Where(s => s.ClienteId == clienteId)
            .Select(s => new
            {
                s.Id,
                s.CriadoEm,
                s.Descricao,
                s.Urgencia,
                s.SolicitanteId,
                Situacao =
                    db.Atividades.Any(a => a.Id == s.AtividadeId && a.Status == StatusAtividade.Concluida)
                        ? SituacaoParaOCliente.Concluida
                    : db.Atividades.Any(a => a.Id == s.AtividadeId)
                        ? SituacaoParaOCliente.EmAndamento
                    : s.CanceladaEm != null
                        ? SituacaoParaOCliente.Cancelada
                        : SituacaoParaOCliente.AguardandoAnalise,
            });

        if (situacao is { } filtro) comSituacao = comSituacao.Where(s => s.Situacao == filtro);

        var total = await comSituacao.CountAsync(ct);
        var linhas = await comSituacao
            .OrderByDescending(s => s.CriadoEm).ThenByDescending(s => s.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(s => new
            {
                s.Id,
                s.CriadoEm,
                s.Descricao,
                s.Urgencia,
                s.SolicitanteId,
                s.Situacao,
                Documentos = db.Documentos.Count(d => d.SolicitacaoId == s.Id),
            })
            .ToListAsync(ct);

        var solicitantes = await NomesDeUsuariosAsync(linhas.Select(l => l.SolicitanteId), ct);

        var itens = linhas.Select(l => new SolicitacaoDoPortal(
            l.Id, l.CriadoEm, SolicitacoesOpcoes.Trecho(l.Descricao), l.Urgencia, l.Situacao,
            solicitantes.GetValueOrDefault(l.SolicitanteId) ?? string.Empty, l.Documentos)).ToList();

        return new Pagina<SolicitacaoDoPortal>(itens, total, pagina, tamanho);
    }

    public async Task<Pagina<SolicitacaoResumo>> ListarAsync(
        SituacaoDaSolicitacao? situacao, Guid? clienteId, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.SolicitacoesDoCliente.AsNoTracking();

        if (clienteId is { } cliente) consulta = consulta.Where(s => s.ClienteId == cliente);

        // A mesma regra de SolicitacaoDoCliente.Situacao, aplicada no banco.
        consulta = situacao switch
        {
            null => consulta,
            SituacaoDaSolicitacao.EmAtividade => consulta.Where(s => s.AtividadeId != null),
            SituacaoDaSolicitacao.Cancelada => consulta.Where(s => s.AtividadeId == null && s.CanceladaEm != null),
            SituacaoDaSolicitacao.Aguardando => consulta.Where(s => s.AtividadeId == null && s.CanceladaEm == null),
            _ => throw new ArgumentOutOfRangeException(nameof(situacao), situacao, "Situação desconhecida."),
        };

        var total = await consulta.CountAsync(ct);
        var linhas = await consulta
            .OrderBy(s => s.CriadoEm).ThenBy(s => s.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(s => new
            {
                s.Id,
                s.CriadoEm,
                s.ClienteId,
                s.SolicitanteId,
                s.Descricao,
                s.Urgencia,
                s.AtividadeId,
                s.CanceladaEm,
                Documentos = db.Documentos.Count(d => d.SolicitacaoId == s.Id),
            })
            .ToListAsync(ct);

        // Cliente e usuário excluídos logicamente continuam identificados pelo nome.
        var idsDeClientes = linhas.Select(l => l.ClienteId).Distinct().ToList();
        var clientes = await db.Clientes.IgnoreQueryFilters().AsNoTracking()
            .Where(c => idsDeClientes.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nome, ct);
        var solicitantes = await NomesDeUsuariosAsync(linhas.Select(l => l.SolicitanteId), ct);

        var itens = linhas.Select(l => new SolicitacaoResumo(
            l.Id, l.CriadoEm, l.ClienteId, clientes.GetValueOrDefault(l.ClienteId) ?? string.Empty,
            solicitantes.GetValueOrDefault(l.SolicitanteId) ?? string.Empty, l.Urgencia,
            SolicitacoesOpcoes.Trecho(l.Descricao), l.Documentos,
            l.AtividadeId is not null ? SituacaoDaSolicitacao.EmAtividade
            : l.CanceladaEm is not null ? SituacaoDaSolicitacao.Cancelada
            : SituacaoDaSolicitacao.Aguardando)).ToList();

        return new Pagina<SolicitacaoResumo>(itens, total, pagina, tamanho);
    }

    // O filtro de exclusão lógica de Atividades já deixa de fora a atividade excluída.
    public Task<AtividadeDaSolicitacao?> ObterAtividadeAsync(Guid atividadeId, CancellationToken ct) =>
        db.Atividades.AsNoTracking().Where(a => a.Id == atividadeId)
            .Select(a => new AtividadeDaSolicitacao(a.Id, a.Titulo, a.Status, a.ConcluidaEm))
            .FirstOrDefaultAsync(ct);

    public Task<string?> NomeDoUsuarioAsync(Guid usuarioId, CancellationToken ct) =>
        db.Usuarios.IgnoreQueryFilters().AsNoTracking().Where(u => u.Id == usuarioId)
            .Select(u => (string?)u.Nome).FirstOrDefaultAsync(ct);

    public Task<string?> NomeDoClienteAsync(Guid clienteId, CancellationToken ct) =>
        db.Clientes.IgnoreQueryFilters().AsNoTracking().Where(c => c.Id == clienteId)
            .Select(c => (string?)c.Nome).FirstOrDefaultAsync(ct);

    private Task<Dictionary<Guid, string>> NomesDeUsuariosAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var distintos = ids.Distinct().ToList();
        return db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Where(u => distintos.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);
    }
}
