using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

public class ProcessosInterativosTests : TelaInterativa
{
    [Fact]
    public void Tocar_num_chip_busca_a_situacao_e_marca_o_chip()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(pedido =>
                Resultado<PaginaDeProcessos>.Ok(new([], 0, 1, 20, 5)));
        var tela = Desenhar<Processos>();

        tela.FindAll(".aj-chips button").Single(b => b.TextContent == "Suspensos").Click();

        Despachante.Recebidas.OfType<ListarProcessos>().Last().Status.Should().Be(StatusProcesso.Suspenso);
        tela.Find(".aj-chips button[aria-pressed=true]").TextContent.Should().Be("Suspensos");
        tela.Find(".aj-chips button").TextContent.Should().Be("Todas · 5");
    }
}
