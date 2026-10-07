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
}
