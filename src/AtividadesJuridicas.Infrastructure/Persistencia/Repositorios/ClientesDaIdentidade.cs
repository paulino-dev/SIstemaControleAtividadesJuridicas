using AtividadesJuridicas.Application.Identidade;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

/// <summary>O que a identidade pergunta ao cadastro de clientes, sempre contra o banco.</summary>
public sealed class ClientesDaIdentidade(AppDbContext db) : IClientesDaIdentidade
{
    // Pela entidade mapeada: o filtro de exclusão lógica vale, e cliente excluído é cliente que não existe.
    public Task<ClienteDoUsuario?> ObterAsync(Guid clienteId, CancellationToken ct) =>
        db.Clientes.AsNoTracking()
            .Where(c => c.Id == clienteId)
            .Select(c => new ClienteDoUsuario(c.Id, c.Nome, c.Ativo))
            .FirstOrDefaultAsync(ct);
}
