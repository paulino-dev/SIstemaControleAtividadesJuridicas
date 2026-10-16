using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using Bunit;
using TelaAreas = AtividadesJuridicas.Web.Components.Pages.Areas;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// As áreas de atuação, clicando: no tablet e no mobile, "Inativar" é um item do menu de ações, que fecha ao escolher e
/// esconde o item focado; o aviso de resultado recebe o foco (o leitor de tela o anuncia e a página rola até ele).
/// </summary>
public class AreasInterativasTests : TelaInterativa
{
    private static readonly Guid Trabalhista = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1");

    // O que o servidor tem: a lista lê daqui, e a inativação muda aqui.
    private bool _ativa = true;

    private IRenderedComponent<TelaAreas> Tela()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(_ =>
                Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Ok([new AreaAtuacaoResumo(Trabalhista, "Trabalhista", _ativa)]))
            .Responde<DefinirAtivoAreaAtuacao, Resultado>(pedido =>
            {
                _ativa = pedido.Ativo;
                return Resultado.Ok();
            });
        return Desenhar<TelaAreas>();
    }

    [Fact]
    public void Inativar_pelo_menu_de_acoes_leva_o_foco_ao_aviso_de_resultado()
    {
        var tela = Tela();
        tela.Find("button[aria-label='Ações de Trabalhista']").Click();

        tela.FindAll(".aj-menu-acoes__lista button").Single(b => b.TextContent == "Inativar").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Área inativada."));
        Despachante.Recebidas.OfType<DefinirAtivoAreaAtuacao>().Should().ContainSingle()
            .Which.Should().Be(new DefinirAtivoAreaAtuacao(Trabalhista, false));
        var aviso = tela.Find("p.aj-aviso[tabindex='-1']");
        aviso.TextContent.Should().Contain("Área inativada.");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Ativar_de_novo_com_o_aviso_a_vista_revela_a_mensagem_nova()
    {
        var tela = Tela();
        tela.FindAll("div.aj-so-desktop tbody button").Single(b => b.TextContent == "Inativar").Click();
        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Área inativada."));
        var antes = FocosDeElementoPedidos;

        tela.FindAll("div.aj-so-desktop tbody button").Single(b => b.TextContent == "Ativar").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Área ativada."));
        FocosDeElementoPedidos.Should().Be(antes + 1);
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso[tabindex='-1']"));
    }
}
