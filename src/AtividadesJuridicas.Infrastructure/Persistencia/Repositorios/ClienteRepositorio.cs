using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Cadastros;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class ClienteRepositorio(AppDbContext db) : IClienteRepositorio
{
    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExisteCpfAsync(Cpf cpf, Guid? excetoId, CancellationToken ct) =>
        db.Clientes.OfType<ClientePessoaFisica>().AnyAsync(c => c.Cpf == cpf && c.Id != excetoId, ct);

    public Task<bool> ExisteCnpjAsync(Cnpj cnpj, Guid? excetoId, CancellationToken ct) =>
        db.Clientes.OfType<ClientePessoaJuridica>().AnyAsync(c => c.Cnpj == cnpj && c.Id != excetoId, ct);

    public async Task<Pagina<Cliente>> ListarAsync(
        string? busca, bool incluirInativos, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.Clientes.AsNoTracking();
        if (!incluirInativos) consulta = consulta.Where(c => c.Ativo);

        if (Cpf.TentarCriar(busca, out var cpf))
        {
            consulta = consulta.Where(c => c is ClientePessoaFisica && ((ClientePessoaFisica)c).Cpf == cpf);
        }
        else if (Cnpj.TentarCriar(busca, out var cnpj))
        {
            consulta = consulta.Where(c => c is ClientePessoaJuridica && ((ClientePessoaJuridica)c).Cnpj == cnpj);
        }
        else if (!string.IsNullOrWhiteSpace(busca))
        {
            // % e _ digitados pelo usuário são texto, não curingas.
            var padrao = "%" + busca.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            consulta = consulta.Where(c => EF.Functions.ILike(c.Nome, padrao, "\\"));
        }

        var total = await consulta.CountAsync(ct);
        var itens = await consulta
            .OrderBy(c => c.Nome).ThenBy(c => c.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .ToListAsync(ct);

        return new Pagina<Cliente>(itens, total, pagina, tamanho);
    }

    public void Adicionar(Cliente cliente) => db.Clientes.Add(cliente);
}
