using AtividadesJuridicas.Web.Components.Layout;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O aviso que se revela: ao aparecer, recebe o foco (o leitor de tela o anuncia e a página rola até ele). Sem
/// <c>Revelar</c>, o aviso de hoje, que não mexe no foco; a confirmação (com <c>Rotulo</c> e ações) continua se focando.
/// </summary>
public class AvisoInterativoTests : TelaInterativa
{
    private const string TentarDeNovo = "<button type=\"button\" class=\"aj-botao aj-botao--secundario\">Tentar de novo</button>";

    private IRenderedComponent<Aviso> DesenharAviso(bool revelar, string texto = "Não foi possível salvar.") =>
        Desenhar<Aviso>(p => p
            .Add(a => a.Tipo, TipoDeAviso.Erro)
            .Add(a => a.Revelar, revelar)
            .AddChildContent(texto));

    [Fact]
    public void Aviso_que_se_revela_pede_o_foco_a_si_mesmo_ao_aparecer()
    {
        var tela = DesenharAviso(revelar: true);

        var aviso = tela.Find("p.aj-aviso");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        aviso.GetAttribute("role").Should().Be("alert");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Aviso_sem_revelar_nao_pede_foco_nem_ganha_tabindex()
    {
        var tela = DesenharAviso(revelar: false);

        var aviso = tela.Find("p.aj-aviso");
        aviso.GetAttribute("role").Should().Be("alert");
        aviso.TextContent.Should().Be("Não foi possível salvar.");
        aviso.HasAttribute("tabindex").Should().BeFalse();
        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void Aviso_que_continua_a_vista_nao_toma_o_foco_de_novo_a_cada_desenho()
    {
        var tela = DesenharAviso(revelar: true);

        // A tela de cima se desenha de novo (o usuário digitou num campo): o aviso não rouba o foco.
        tela.Render(p => p.Add(a => a.Revelar, true));

        tela.Find("p.aj-aviso").TextContent.Should().Be("Não foi possível salvar.");
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 1);
    }

    [Fact]
    public void Aviso_que_passa_a_se_revelar_pede_o_foco()
    {
        var tela = DesenharAviso(revelar: false);
        JSInterop.Invocations.Should().BeEmpty();

        tela.Render(p => p.Add(a => a.Revelar, true));

        var aviso = tela.Find("p.aj-aviso");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Erro_com_acao_que_se_revela_pede_o_foco_a_caixa()
    {
        var tela = Desenhar<Aviso>(p => p
            .Add(a => a.Tipo, TipoDeAviso.Erro)
            .Add(a => a.Revelar, true)
            .AddChildContent("Não foi possível consultar.")
            .Add(a => a.Acoes, TentarDeNovo));

        var caixa = tela.Find("div.aj-aviso--com-acoes");
        caixa.GetAttribute("role").Should().Be("alert");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(caixa);
    }

    [Fact]
    public void Erro_com_acao_sem_revelar_nao_pede_foco_e_a_confirmacao_continua_se_focando()
    {
        var erro = Desenhar<Aviso>(p => p
            .Add(a => a.Tipo, TipoDeAviso.Erro)
            .AddChildContent("Não foi possível consultar.")
            .Add(a => a.Acoes, TentarDeNovo));
        erro.Find("div.aj-aviso--com-acoes").GetAttribute("role").Should().Be("alert");
        JSInterop.Invocations.Should().BeEmpty();

        var confirmacao = Desenhar<Aviso>(p => p
            .Add(a => a.Tipo, TipoDeAviso.Atencao)
            .Add(a => a.Rotulo, "Confirmar exclusão")
            .AddChildContent("O arquivo sai da ficha.")
            .Add(a => a.Acoes, TentarDeNovo));

        JSInterop.VerifyFocusAsyncInvoke().Arguments[0]
            .ShouldBeElementReferenceTo(confirmacao.Find("div[role=alertdialog]"));
    }
}
