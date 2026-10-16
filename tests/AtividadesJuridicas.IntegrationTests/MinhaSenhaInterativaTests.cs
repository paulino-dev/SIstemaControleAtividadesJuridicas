using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Minha senha, clicando: a senha atual recusada mostra o erro e leva o foco a ele, a cada tentativa.</summary>
public class MinhaSenhaInterativaTests : TelaInterativa
{
    private static void Digitar(IRenderedComponent<MinhaSenha> tela, string rotulo, string valor) =>
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.TrimStart().StartsWith(rotulo)).QuerySelector("input")!.Change(valor);

    [Fact]
    public void Senha_atual_recusada_leva_o_foco_ao_aviso_de_erro_a_cada_tentativa()
    {
        Despachante.Responde<AlterarMinhaSenha, Resultado>(Resultado.Falha(TipoErro.Validacao, "Senha atual incorreta."));
        var tela = Desenhar<MinhaSenha>();
        Digitar(tela, "Senha atual", "senha-errada");
        Digitar(tela, "Nova senha", "nova-senha-123");
        Digitar(tela, "Confirme a nova senha", "nova-senha-123");

        tela.Find("form.aj-painel").Submit();

        var aviso = tela.Find("p.aj-aviso--erro");
        aviso.TextContent.Should().Be("Senha atual incorreta.");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
        var antes = FocosDeElementoPedidos;

        tela.Find("form.aj-painel").Submit();

        Despachante.Recebidas.OfType<AlterarMinhaSenha>().Should().HaveCount(2);
        FocosDeElementoPedidos.Should().Be(antes + 1);
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso--erro"));
    }
}
