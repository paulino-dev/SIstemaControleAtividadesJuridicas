using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection servicos)
    {
        servicos.AddScoped<IdentidadeAtual>();
        servicos.AddScoped<IIdentidadeAtual>(sp => sp.GetRequiredService<IdentidadeAtual>());
        servicos.AddScoped<IContextoDeAcesso, ContextoDeAcesso>();
        servicos.AddScoped<ExecutorDeRequisicoes>();

        var tipos = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var tipo in tipos)
        {
            var contratos = tipo.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IManipulador<,>));

            foreach (var contrato in contratos)
                servicos.AddScoped(contrato, tipo);
        }

        return servicos;
    }
}
