using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

public sealed record AdminInicialOpcoes(string Nome, string Email, string Senha);

/// <summary>
/// Aplica as migrations, cria os perfis padrão e o primeiro diretor em banco vazio e garante o perfil
/// Cliente em qualquer banco.
/// </summary>
public sealed class SemeadorInicial(AppDbContext db, IHasherDeSenha hasher)
{
    public async Task ExecutarAsync(AdminInicialOpcoes? admin, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Perfis.IgnoreQueryFilters().AnyAsync(ct))
        {
            if (admin is null)
                throw new InvalidOperationException(
                    "Banco vazio: configure AdminInicial:Nome, AdminInicial:Email e AdminInicial:Senha para criar o primeiro diretor.");
            if (PoliticaDeSenha.Validar(admin.Senha) is { } erro)
                throw new InvalidOperationException("AdminInicial:Senha inválida. " + erro);

            var diretor = Perfil.Criar("Diretor", TipoPerfil.Diretoria);
            var producao = Perfil.Criar("Produção", TipoPerfil.Producao);
            var usuario = Usuario.Criar(admin.Nome, Email.Criar(admin.Email), hasher.Gerar(admin.Senha), diretor);

            db.AddRange(diretor, producao, usuario);
        }

        // Banco anterior ao portal do cliente não tem o perfil; em banco novo ele nasce junto dos outros dois.
        // Fora de IAcessoInicialDePerfil: o perfil Cliente não entra em lista de acesso de documento.
        if (!await db.Perfis.IgnoreQueryFilters().AnyAsync(p => p.Tipo == TipoPerfil.Cliente, ct))
        {
            var nomeEmUso = await db.Perfis.AnyAsync(p => p.Nome.ToLower() == "cliente", ct);
            db.Add(Perfil.Criar(nomeEmUso ? "Cliente (portal)" : "Cliente", TipoPerfil.Cliente));
        }

        await db.SaveChangesAsync(ct);
    }
}
