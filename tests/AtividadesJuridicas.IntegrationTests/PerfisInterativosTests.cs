using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Bunit;
using TelaPerfis = AtividadesJuridicas.Web.Components.Pages.Perfis;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Os perfis, clicando: no tablet e no mobile, "Inativar" é um item do menu de ações, que fecha ao escolher e esconde o
/// item focado; o aviso de resultado recebe o foco (o leitor de tela o anuncia e a página rola até ele).
/// </summary>
public class PerfisInterativosTests : TelaInterativa
{
    private static readonly Guid Estagio = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c2");

    // O que o servidor tem: a lista lê daqui, e a inativação muda aqui.
    private bool _ativo = true;

    private IRenderedComponent<TelaPerfis> Tela()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(_ =>
                Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([new PerfilDetalhe(Estagio, "Estágio", TipoPerfil.Producao, _ativo, 0)]))
            .Responde<DefinirAtivoPerfil, Resultado>(pedido =>
            {
                _ativo = pedido.Ativo;
                return Resultado.Ok();
            });
        return Desenhar<TelaPerfis>();
    }

    [Fact]
    public void Inativar_pelo_menu_de_acoes_leva_o_foco_ao_aviso_de_resultado()
    {
        var tela = Tela();
        tela.Find("button[aria-label='Ações de Estágio']").Click();

        tela.FindAll(".aj-menu-acoes__lista button").Single(b => b.TextContent == "Inativar").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Perfil inativado."));
        Despachante.Recebidas.OfType<DefinirAtivoPerfil>().Should().ContainSingle()
            .Which.Should().Be(new DefinirAtivoPerfil(Estagio, false));
        var aviso = tela.Find("p.aj-aviso[tabindex='-1']");
        aviso.TextContent.Should().Contain("Perfil inativado.");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Ativar_de_novo_com_o_aviso_a_vista_revela_a_mensagem_nova()
    {
        var tela = Tela();
        tela.FindAll("div.aj-so-desktop tbody button").Single(b => b.TextContent == "Inativar").Click();
        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Perfil inativado."));
        var antes = FocosDeElementoPedidos;

        tela.FindAll("div.aj-so-desktop tbody button").Single(b => b.TextContent == "Ativar").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Perfil ativado."));
        FocosDeElementoPedidos.Should().Be(antes + 1);
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso[tabindex='-1']"));
    }
}
