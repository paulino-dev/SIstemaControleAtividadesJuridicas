using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests;

public class ArquiteturaTests
{
    [Fact]
    public void Domain_nao_referencia_nada_alem_da_biblioteca_base()
    {
        var referencias = typeof(Entidade).Assembly.GetReferencedAssemblies().Select(a => a.Name!);

        referencias.Should().OnlyContain(nome => nome.StartsWith("System") || nome == "netstandard");
    }

    [Fact]
    public void Application_nao_referencia_infraestrutura_nem_web()
    {
        var proibidos = new[]
        {
            "Microsoft.EntityFrameworkCore", "Npgsql",
            "AtividadesJuridicas.Infrastructure", "AtividadesJuridicas.Web",
        };
        var referencias = typeof(AtividadesJuridicas.Application.Comum.IDespachante).Assembly
            .GetReferencedAssemblies().Select(a => a.Name!);

        referencias.Should().NotContain(nome => proibidos.Any(p => nome.StartsWith(p)));
    }
}
