using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class ProcessoFichaInterativaTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1");

    private static ProcessoDetalhe Processo(StatusProcesso status = StatusProcesso.Ativo) => new(
        Id, "0000000-00.2026.5.19.0002", "Litoral — reclamação trabalhista", Guid.NewGuid(), "Trabalhista", status,
        null, "Maceió", "Paulo Mendes", null, 84000m, null,
        [new ClienteDoProcessoDetalhe(Guid.NewGuid(), "Transportes Litoral Ltda.", "11.222.333/0001-81", PoloProcessual.Passivo, true)]);

    private IRenderedComponent<ProcessoFicha> Ficha()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterProcesso, Resultado<ProcessoDetalhe>>(Resultado<ProcessoDetalhe>.Ok(Processo()))
            .Responde<AlterarStatusProcesso, Resultado>(Resultado.Ok());
        return Desenhar<ProcessoFicha>(p => p.Add(f => f.Id, Id));
    }

    [Fact]
    public void Alterar_a_situacao_muda_a_ficha_e_avisa()
    {
        var ficha = Ficha();
        // O desktop vem antes na marcação: o primeiro formulário de situação é o dele.
        var formulario = ficha.FindAll("form.aj-campo-com-botao")[0];

        formulario.QuerySelector("select")!.Change(nameof(StatusProcesso.Encerrado));
        formulario.Submit();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Situação alterada para Encerrado."));
        Despachante.Recebidas.OfType<AlterarStatusProcesso>().Should().ContainSingle()
            .Which.Should().Be(new AlterarStatusProcesso(Id, StatusProcesso.Encerrado));
        ficha.Markup.Should().Contain("Situação atual: <strong>Encerrado</strong>").And.Contain("· Trabalhista · Encerrado</p>");
        ficha.FindAll("form.aj-campo-com-botao")[0].QuerySelectorAll("option").Select(o => o.TextContent).Should()
            .Equal("Ativo", "Suspenso", "Arquivado");
    }

    // O "Alterar" se desabilita durante a gravação e o resultado aparece no topo: o foco vai ao aviso, que o leitor de
    // tela anuncia e a página rola até ele.
    [Fact]
    public void Alterar_a_situacao_leva_o_foco_ao_aviso_de_resultado()
    {
        var ficha = Ficha();
        var formulario = ficha.FindAll("form.aj-campo-com-botao")[0];
        formulario.QuerySelector("select")!.Change(nameof(StatusProcesso.Encerrado));

        formulario.Submit();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Situação alterada para Encerrado."));
        var aviso = ficha.Find("p.aj-aviso[tabindex='-1']");
        aviso.TextContent.Should().Contain("Situação alterada para Encerrado.");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Recado_de_outra_tela_aparece_sem_levar_o_foco()
    {
        // Trava: o recado chega com a navegação, e quem leva o foco ao título é o FocoNoTitulo, não o aviso.
        Services.GetRequiredService<Recados>().Deixar("Processo salvo.");

        var ficha = Ficha();

        ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Contain("Processo salvo.");
        ficha.Find("p.aj-aviso--sucesso").HasAttribute("tabindex").Should().BeFalse();
        FocosDeElementoPedidos.Should().Be(0);
    }

    [Fact]
    public void Alterar_situacao_na_barra_do_mobile_abre_a_aba_situacao()
    {
        var ficha = Ficha();

        ficha.FindAll(".aj-acoes--fixa button").Single(b => b.TextContent == "Alterar situação").Click();

        ficha.Find(".aj-abas button[aria-selected=true]").TextContent.Should().Be("Situação");
        ficha.Markup.Should().MatchRegex("<div class=\"aj-aba-painel\" id=\"situacao-do-processo\">\\s*<section class=\"aj-painel\">\\s*<h2>Situação</h2>");
    }

    [Fact]
    public void Cada_aba_aponta_para_os_paineis_dela()
    {
        var ficha = Ficha();

        // A aba Dados mostra dois invólucros: Dados e Observações (que no tablet vem depois de Clientes).
        AbasInterativasTests.CadaAbaApontaParaOsPaineisDela(ficha,
            "dados-do-processo observacoes-do-processo", "clientes-do-processo", "situacao-do-processo");
    }

    [Fact]
    public void Alterar_a_situacao_some_com_o_recado_que_veio_de_outra_tela()
    {
        Services.GetRequiredService<Recados>().Deixar("Processo salvo.");
        var ficha = Ficha();
        ficha.Markup.Should().Contain("Processo salvo.");
        var formulario = ficha.FindAll("form.aj-campo-com-botao")[0];

        formulario.QuerySelector("select")!.Change(nameof(StatusProcesso.Encerrado));
        formulario.Submit();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Situação alterada para Encerrado."));
        ficha.Markup.Should().NotContain("Processo salvo.");
    }
}
