using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class UsuarioRepositorio(AppDbContext db) : IUsuarioRepositorio
{
    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> ExisteEmailAsync(Email email, Guid? excetoUsuarioId, CancellationToken ct) =>
        db.Usuarios.AnyAsync(u => u.Email == email && u.Id != excetoUsuarioId, ct);

    public Task<int> ContarDiretoresAtivosAsync(Guid? excetoUsuarioId, CancellationToken ct) =>
        db.Usuarios
            .Where(u => u.Ativo && u.Id != excetoUsuarioId)
            .Join(db.Perfis.Where(p => p.Tipo == TipoPerfil.Diretoria), u => u.PerfilId, p => p.Id, (u, p) => u.Id)
            .CountAsync(ct);

    public async Task<IReadOnlyList<UsuarioResumo>> ListarAsync(CancellationToken ct)
    {
        // O e-mail usa conversor de valor; a leitura de .Valor acontece depois de materializar.
        var linhas = await db.Usuarios.AsNoTracking()
            .Join(db.Perfis, u => u.PerfilId, p => p.Id, (u, p) => new
            {
                u.Id, u.Nome, u.Email, PerfilId = p.Id, PerfilNome = p.Nome, p.Tipo,
                u.NumeroOab, u.Ativo, u.UltimoAcessoEm,
            })
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

        return [.. linhas.Select(x => new UsuarioResumo(
            x.Id, x.Nome, x.Email.Valor, x.PerfilId, x.PerfilNome, x.Tipo, x.NumeroOab, x.Ativo, x.UltimoAcessoEm))];
    }

    public void Adicionar(Usuario usuario) => db.Usuarios.Add(usuario);

    public async Task GravarEstadoDeLoginAsync(Usuario usuario, CancellationToken ct)
    {
        await db.Usuarios.Where(u => u.Id == usuario.Id).ExecuteUpdateAsync(c => c
            .SetProperty(u => u.UltimoAcessoEm, usuario.UltimoAcessoEm)
            .SetProperty(u => u.FalhasDeLogin, usuario.FalhasDeLogin)
            .SetProperty(u => u.BloqueadoAte, usuario.BloqueadoAte), ct);

        // Os valores já estão no banco: a entidade rastreada não pode ficar como alterada,
        // ou uma gravação posterior no mesmo escopo a carimbaria e a auditaria.
        db.Entry(usuario).State = EntityState.Unchanged;
    }
}
