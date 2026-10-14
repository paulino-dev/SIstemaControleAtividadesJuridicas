using AtividadesJuridicas.Web.Components.Layout;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

public class AbasInterativasTests : TelaInterativa
{
    [Fact]
    public void Tocar_numa_aba_avisa_a_chave_dela()
    {
        string? escolhida = null;
        var abas = Desenhar<Abas>(p => p
            .Add(a => a.Rotulo, "Assuntos do processo")
            .Add(a => a.Ativa, "dados")
            .Add(a => a.Itens, new List<Aba> { new("dados", "Dados"), new("clientes", "Clientes", 1), new("situacao", "Situação") })
            .Add(a => a.AoEscolher, (string chave) => escolhida = chave));

        abas.FindAll("button[role=tab]")[2].Click();

        escolhida.Should().Be("situacao");
    }
}
