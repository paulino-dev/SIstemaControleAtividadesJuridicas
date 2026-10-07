using AtividadesJuridicas.Application;
using AtividadesJuridicas.Application.Comum;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.UnitTests.Application;

public class RegistroDeManipuladoresTests
{
    [Fact]
    public void Toda_requisicao_tem_exatamente_um_manipulador_registrado()
    {
        var servicos = new ServiceCollection().AddApplication();

        var requisicoes = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequisicao<>))
                .Select(i => typeof(IManipulador<,>).MakeGenericType(t, i.GetGenericArguments()[0])))
            .ToList();

        requisicoes.Should().NotBeEmpty();
        foreach (var contrato in requisicoes)
            servicos.Count(s => s.ServiceType == contrato).Should().Be(1, $"{contrato} deve ter um manipulador");
    }
}
