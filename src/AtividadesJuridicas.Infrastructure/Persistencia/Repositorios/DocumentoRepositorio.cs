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
            var perfilId = acesso.PerfilId;
            var usuarioId = acesso.UsuarioId;
            consulta = consulta.Where(d =>
                d.Categoria != CategoriaDocumento.ContratoHonorarios &&
                (d.PerfisComAcesso.Any(a => a.PerfilId == perfilId) ||
                 d.UsuariosComAcesso.Any(a => a.UsuarioId == usuarioId)));
        }

        var total = await consulta.CountAsync(ct);
        var documentos = await consulta
            .OrderByDescending(d => d.CriadoEm).ThenByDescending(d => d.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .ToListAsync(ct);

        // Quem enviou continua identificado pelo nome mesmo depois de excluído.
        var autores = documentos.Where(d => d.CriadoPor.HasValue).Select(d => d.CriadoPor!.Value).Distinct().ToList();
        var nomes = await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Where(u => autores.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);

        var itens = documentos.Select(d => new DocumentoResumo(
            d.Id, d.Nome, d.NomeArquivoOriginal, d.TamanhoBytes, d.Categoria, d.CriadoEm,
            d.CriadoPor is { } autor ? nomes.GetValueOrDefault(autor) : null)).ToList();

        return new Pagina<DocumentoResumo>(itens, total, pagina, tamanho);
    }

    public void Adicionar(Documento documento) => db.Documentos.Add(documento);

    public void Remover(Documento documento) => db.Documentos.Remove(documento);
}
