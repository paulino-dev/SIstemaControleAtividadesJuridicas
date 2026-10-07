using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Infrastructure.Comum;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Infrastructure.Persistencia.Interceptores;
using AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;
using AtividadesJuridicas.Infrastructure.Seguranca;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection servicos, Func<IServiceProvider, string> obterStringDeConexao)
    {
        servicos.AddScoped<CarimbosInterceptor>();

        servicos.AddDbContext<AppDbContext>((sp, opcoes) => opcoes
            .UseNpgsql(obterStringDeConexao(sp))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<CarimbosInterceptor>()));

        servicos.AddSingleton<IRelogio, RelogioDoSistema>();
        servicos.AddSingleton<IHasherDeSenha, HasherDeSenhaPbkdf2>();
        servicos.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        servicos.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        servicos.AddScoped<IPerfilRepositorio, PerfilRepositorio>();
        servicos.AddScoped<SemeadorInicial>();

        return servicos;
    }
}
