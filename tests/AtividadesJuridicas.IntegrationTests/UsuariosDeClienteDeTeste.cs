using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Até a Tarefa 3 não há caso de uso que crie usuário de cliente: grava direto, reaproveitando o perfil se ele já existir.</summary>
public static class UsuariosDeClienteDeTeste
{
    public const string EmailDaCarla = "carla@x.com";
    public const string SenhaDaCarla = "senha-da-carla";

    public static async Task<Guid> GravarAsync(
        IServiceProvider sp, Guid clienteId, string nome = "Carla Cliente", string email = EmailDaCarla)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var perfil = await db.Perfis.FirstOrDefaultAsync(p => p.Tipo == TipoPerfil.Cliente);
        if (perfil is null) db.Perfis.Add(perfil = Perfil.Criar("Cliente", TipoPerfil.Cliente));

        var usuario = Usuario.Criar(nome, Email.Criar(email),
            sp.GetRequiredService<IHasherDeSenha>().Gerar(SenhaDaCarla), perfil, null, clienteId);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario.Id;
    }
}
