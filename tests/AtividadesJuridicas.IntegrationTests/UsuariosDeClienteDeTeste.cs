using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Cria o usuário de cliente pelo caso de uso, com o perfil Cliente que o semeador criou. O escopo de <c>sp</c> precisa ser o da Diretoria.</summary>
public static class UsuariosDeClienteDeTeste
{
    public const string EmailDaCarla = "carla@x.com";
    public const string SenhaDaCarla = "senha-da-carla";

    public static async Task<Guid> GravarAsync(
        IServiceProvider sp, Guid clienteId, string nome = "Carla Cliente", string email = EmailDaCarla)
    {
        var perfil = await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
            .SingleAsync(p => p.Tipo == TipoPerfil.Cliente);

        var criado = await sp.GetRequiredService<ExecutorDeRequisicoes>().ExecutarAsync(
            new CriarUsuario(nome, email, SenhaDaCarla, perfil.Id, null, clienteId), default);
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }
}
