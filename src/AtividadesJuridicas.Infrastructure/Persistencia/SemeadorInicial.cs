using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

public sealed record AdminInicialOpcoes(string Nome, string Email, string Senha);

/// <summary>Aplica as migrations e, em banco vazio, cria os perfis padrão e o primeiro diretor.</summary>
public sealed class SemeadorInicial(AppDbContext db, IHasherDeSenha hasher)
{
    public async Task ExecutarAsync(AdminInicialOpcoes? admin, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (await db.Perfis.IgnoreQueryFilters().AnyAsync(ct)) return;

        if (admin is null)
            throw new InvalidOperationException(
                "Banco vazio: configure AdminInicial:Nome, AdminInicial:Email e AdminInicial:Senha para criar o primeiro diretor.");
        if (PoliticaDeSenha.Validar(admin.Senha) is { } erro)
            throw new InvalidOperationException("AdminInicial:Senha inválida. " + erro);

        var diretor = Perfil.Criar("Diretor", TipoPerfil.Diretoria);
        var producao = Perfil.Criar("Produção", TipoPerfil.Producao);
        var usuario = Usuario.Criar(admin.Nome, Email.Criar(admin.Email), hasher.Gerar(admin.Senha), diretor);

        db.AddRange(diretor, producao, usuario);
        await db.SaveChangesAsync(ct);
    }
}
