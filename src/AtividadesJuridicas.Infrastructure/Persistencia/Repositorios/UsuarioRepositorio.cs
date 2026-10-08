using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class UsuarioRepositorio(AppDbContext db) : IUsuarioRepositorio
{
    public Task<int> ContarAtivosComPerfilAsync(Guid perfilId, CancellationToken ct) =>
        db.Usuarios.CountAsync(u => u.Ativo && u.PerfilId == perfilId, ct);

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

    public async Task<IReadOnlyList<Usuario>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        await db.Usuarios.Where(u => ids.Contains(u.Id)).ToListAsync(ct);

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

    public async Task<TentativaDeLogin> ReservarTentativaDeLoginAsync(
        Guid usuarioId, DateTime agoraUtc, CancellationToken ct)
    {
        var maximo = Usuario.MaximoDeFalhasDeLogin;
        var bloqueadoAte = agoraUtc + Usuario.DuracaoDoBloqueio;

        // Um único UPDATE condicional: execuções simultâneas se serializam no PostgreSQL e cada uma
        // reavalia a condição sobre a linha já atualizada. Colunas em snake_case, valores parametrizados.
        // O filtro de exclusão lógica não se aplica a SQL cru, por isso excluido_em é testado aqui.
        var linhas = await db.Database.SqlQuery<bool>($"""
            UPDATE usuarios SET
                falhas_de_login = CASE WHEN falhas_de_login + 1 >= {maximo} THEN 0 ELSE falhas_de_login + 1 END,
                bloqueado_ate   = CASE WHEN falhas_de_login + 1 >= {maximo} THEN {bloqueadoAte} ELSE NULL END
            WHERE id = {usuarioId}
              AND excluido_em IS NULL
              AND (bloqueado_ate IS NULL OR bloqueado_ate <= {agoraUtc})
            RETURNING (bloqueado_ate IS NOT NULL) AS "Value"
            """).ToListAsync(ct);

        if (linhas.Count == 0) return TentativaDeLogin.Recusada;
        return linhas[0] ? TentativaDeLogin.AdmitidaEBloqueou : TentativaDeLogin.Admitida;
    }

    public async Task RegistrarLoginBemSucedidoAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct) =>
        await db.Usuarios.Where(u => u.Id == usuarioId).ExecuteUpdateAsync(c => c
            .SetProperty(u => u.UltimoAcessoEm, (DateTime?)agoraUtc)
            .SetProperty(u => u.FalhasDeLogin, 0)
            .SetProperty(u => u.BloqueadoAte, (DateTime?)null), ct);
}
