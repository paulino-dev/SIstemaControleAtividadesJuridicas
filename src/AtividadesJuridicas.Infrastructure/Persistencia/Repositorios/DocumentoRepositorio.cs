using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Documentos;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class DocumentoRepositorio(AppDbContext db) : IDocumentoRepositorio
{
    public Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Documentos
            .Include(d => d.PerfisComAcesso)
            .Include(d => d.UsuariosComAcesso)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<Pagina<DocumentoResumo>> ListarDoClienteAsync(
        Guid clienteId, CategoriaDocumento categoria, FiltroDeAcesso? acesso, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.Documentos.AsNoTracking().Where(d => d.ClienteId == clienteId && d.Categoria == categoria);

        if (acesso is not null)
        {
            // A mesma regra de Documento.PodeSerAcessadoPor para quem não é da Diretoria, aplicada no banco.
            // O documento de atividade não olha as listas: é do responsável de hoje de uma atividade viva. As
            // consultas passam pelas entidades mapeadas para que a exclusão lógica da atividade e da entrada
            // de responsável valha aqui também.
            var perfilId = acesso.PerfilId;
            var usuarioId = acesso.UsuarioId;
            consulta = consulta.Where(d =>
                d.Categoria != CategoriaDocumento.ContratoHonorarios &&
                d.Categoria != CategoriaDocumento.DocumentoDoCliente &&
                d.Categoria != CategoriaDocumento.DocumentoComum &&
                ((d.Categoria != CategoriaDocumento.DocumentoAtividade &&
                  (d.PerfisComAcesso.Any(a => a.PerfilId == perfilId) ||
                   d.UsuariosComAcesso.Any(a => a.UsuarioId == usuarioId))) ||
                 (d.Categoria == CategoriaDocumento.DocumentoAtividade &&
                  db.Atividades.Any(at => at.Id == d.AtividadeId && at.Responsaveis.Any(r => r.UsuarioId == usuarioId)))));
        }

        return await PaginaAsync(consulta, pagina, tamanho, ct);
    }

    // As consultas do portal não usam FiltroDeAcesso: a categoria e o cliente já dizem o que o portal mostra.
    public Task<Pagina<DocumentoDoPortal>> ListarDoPortalAsync(Guid clienteId, int pagina, int tamanho, CancellationToken ct) =>
        DoPortalAsync(
            db.Documentos.AsNoTracking()
                .Where(d => d.Categoria == CategoriaDocumento.DocumentoDoCliente && d.ClienteId == clienteId),
            comAutor: false, pagina, tamanho, ct);

    public Task<Pagina<DocumentoDoPortal>> ListarComunsAsync(bool comAutor, int pagina, int tamanho, CancellationToken ct) =>
        DoPortalAsync(
            db.Documentos.AsNoTracking().Where(d => d.Categoria == CategoriaDocumento.DocumentoComum),
            comAutor, pagina, tamanho, ct);

    private async Task<Pagina<DocumentoDoPortal>> DoPortalAsync(
        IQueryable<Documento> consulta, bool comAutor, int pagina, int tamanho, CancellationToken ct)
    {
        var total = await consulta.CountAsync(ct);
        var documentos = await consulta
            .OrderByDescending(d => d.CriadoEm).ThenByDescending(d => d.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .ToListAsync(ct);

        // O autor só é buscado para quem pode vê-lo; continua identificado pelo nome mesmo depois de excluído.
        var nomes = new Dictionary<Guid, string>();
        if (comAutor)
        {
            var autores = documentos.Where(d => d.CriadoPor.HasValue).Select(d => d.CriadoPor!.Value).Distinct().ToList();
            nomes = await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
                .Where(u => autores.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);
        }

        var itens = documentos.Select(d => new DocumentoDoPortal(
            d.Id, d.Nome, d.TamanhoBytes, d.CriadoEm,
            comAutor && d.CriadoPor is { } autor ? nomes.GetValueOrDefault(autor) : null)).ToList();

        return new Pagina<DocumentoDoPortal>(itens, total, pagina, tamanho);
    }

    public Task<Pagina<DocumentoResumo>> ListarDaAtividadeAsync(Guid atividadeId, int pagina, int tamanho, CancellationToken ct) =>
        PaginaAsync(db.Documentos.AsNoTracking().Where(d => d.AtividadeId == atividadeId), pagina, tamanho, ct);

    public async Task<IReadOnlyList<Documento>> ObterDaAtividadeAsync(Guid atividadeId, CancellationToken ct) =>
        await db.Documentos.Where(d => d.AtividadeId == atividadeId).ToListAsync(ct);

    private async Task<Pagina<DocumentoResumo>> PaginaAsync(
        IQueryable<Documento> consulta, int pagina, int tamanho, CancellationToken ct)
    {
        var total = await consulta.CountAsync(ct);
        var documentos = await consulta
            .OrderByDescending(d => d.CriadoEm).ThenByDescending(d => d.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .ToListAsync(ct);

        // Quem enviou continua identificado pelo nome mesmo depois de excluído.
        var autores = documentos.Where(d => d.CriadoPor.HasValue).Select(d => d.CriadoPor!.Value).Distinct().ToList();
        var nomes = await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Where(u => autores.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);

        // O título da atividade aparece mesmo que ela tenha sido excluída (a Diretoria ainda vê o documento).
        var atividades = documentos.Where(d => d.AtividadeId.HasValue).Select(d => d.AtividadeId!.Value).Distinct().ToList();
        var titulos = await db.Atividades.IgnoreQueryFilters().AsNoTracking()
            .Where(a => atividades.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Titulo, ct);

        var itens = documentos.Select(d => new DocumentoResumo(
            d.Id, d.Nome, d.NomeArquivoOriginal, d.TamanhoBytes, d.Categoria, d.CriadoEm,
            d.CriadoPor is { } autor ? nomes.GetValueOrDefault(autor) : null,
            d.AtividadeId, d.AtividadeId is { } atividade ? titulos.GetValueOrDefault(atividade) : null)).ToList();

        return new Pagina<DocumentoResumo>(itens, total, pagina, tamanho);
    }

    public void Adicionar(Documento documento) => db.Documentos.Add(documento);

    public void Remover(Documento documento) => db.Documentos.Remove(documento);
}
