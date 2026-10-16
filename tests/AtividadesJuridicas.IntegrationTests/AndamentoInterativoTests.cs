using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using Bunit;
using TelaAndamento = AtividadesJuridicas.Web.Components.Pages.Andamento;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A paginação e os chips do Andamento (portal do cliente). A página nova chega sem tirar da tela a lista e a
/// paginação: o botão tocado continua no lugar, e o foco com ele. Outra situação é outra lista: "Carregando…" fica no
/// lugar da anterior até a resposta. A falha não deixa a página anterior na tela.
/// </summary>
public class AndamentoInterativoTests : TelaInterativa
{
    // 45 atividades em páginas de 20: três páginas. Os totais do cliente (40 em andamento, 5 concluídas) não dependem
    // do filtro.
    private static Resultado<AndamentoDoCliente> Resposta(int numero, string titulo) =>
        Resultado<AndamentoDoCliente>.Ok(new AndamentoDoCliente(
            new Pagina<AtividadeDoAndamento>(
                [new AtividadeDoAndamento(titulo, "Tributário", null, SituacaoDoAndamento.EmAndamento, null)], 45, numero, 20),
            40, 5));

    private IRenderedComponent<TelaAndamento> Andamento()
    {
        Despachante.Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(Resposta(1, "Contestação da página 1"));
        return Desenhar<TelaAndamento>();
    }

    // A próxima consulta fica esperando até o teste completar a tarefa.
    private TaskCompletionSource<Resultado<AndamentoDoCliente>> ProximaRespostaEspera()
    {
        var resposta = new TaskCompletionSource<Resultado<AndamentoDoCliente>>();
        Despachante.RespondeDepois<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(resposta.Task);
        return resposta;
    }

    private List<ListarMeuAndamento> Consultas => [.. Despachante.Recebidas.OfType<ListarMeuAndamento>()];

    private static string Texto(IElement elemento) => Regex.Replace(elemento.TextContent, "\\s+", " ").Trim();

    // Um botão da paginação de uma visão ("aj-so-desktop" ou "aj-tablet-mobile") pelo texto.
    private static IElement Botao(IRenderedComponent<TelaAndamento> tela, string visao, string texto) =>
        tela.FindAll($"div.{visao} .aj-paginacao button").Single(b => Texto(b) == texto);

    // B9: "Próxima" não tira a lista nem a paginação da tela enquanto a página nova não chega: os quatro botões esperam
    // (aria-disabled, focáveis) e o tocado continua no lugar; um segundo clique não pede outra página. A resposta troca a
    // lista. Um chip, depois, é outra situação: a lista sai na hora e "Carregando…" fica no lugar dela.
    [Fact]
    public void Proxima_pagina_mantem_a_lista_e_a_paginacao_a_vista_ate_a_resposta_e_o_chip_mostra_carregando()
    {
        var tela = Andamento();
        var segunda = ProximaRespostaEspera();

        Botao(tela, "aj-so-desktop", "Próxima").Click();

        Consultas.Should().HaveCount(2);
        Consultas[1].Pagina.Should().Be(2);
        tela.Markup.Should().Contain("Contestação da página 1").And.NotContain("Carregando…");
        tela.FindAll(".aj-paginacao button").Should().HaveCount(4)
            .And.OnlyContain(b => b.GetAttribute("aria-disabled") == "true" && !b.HasAttribute("disabled"));
        Texto(tela.Find("div.aj-so-desktop .aj-paginacao .aj-subtitulo")).Should().Be("Página 1 de 3 · 45 atividades");
        Botao(tela, "aj-so-desktop", "Próxima").Click();
        Consultas.Should().HaveCount(2);

        segunda.SetResult(Resposta(2, "Recurso da página 2"));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Recurso da página 2"));
        tela.Markup.Should().NotContain("Contestação da página 1");
        Texto(tela.Find("div.aj-so-desktop .aj-paginacao .aj-subtitulo")).Should().Be("Página 2 de 3 · 45 atividades");
        tela.FindAll(".aj-paginacao button").Should().HaveCount(4)
            .And.OnlyContain(b => !b.HasAttribute("aria-disabled") && !b.HasAttribute("disabled"));

        var concluidas = ProximaRespostaEspera();
        tela.FindAll(".aj-chips button").Single(b => Texto(b).StartsWith("Concluídas")).Click();

        tela.Markup.Should().Contain("Carregando…").And.NotContain("Recurso da página 2");
        Consultas[2].Situacao.Should().Be(SituacaoDoAndamento.Concluida);
        Consultas[2].Pagina.Should().Be(1);

        concluidas.SetResult(Resposta(1, "Parecer concluído"));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Parecer concluído"));
        tela.Markup.Should().NotContain("Carregando…");
    }

    // B9: a página anterior não volta à tela depois de uma falha: o erro fica no lugar da lista e, no "Tentar de novo",
    // "Carregando…" fica no lugar até a página pedida chegar.
    [Fact]
    public void Falha_da_pagina_nova_tira_a_anterior_e_tentar_de_novo_pede_a_mesma_pagina()
    {
        var tela = Andamento();
        Despachante.Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(
            Resultado<AndamentoDoCliente>.Falha(TipoErro.Validacao, "Falha de teste."));

        Botao(tela, "aj-tablet-mobile", "Próxima").Click();

        tela.Markup.Should().Contain("Falha de teste.").And.NotContain("Contestação da página 1");
        tela.FindAll(".aj-paginacao").Should().BeEmpty();

        var nova = ProximaRespostaEspera();
        tela.FindAll("button").Single(b => Texto(b) == "Tentar de novo").Click();

        tela.Markup.Should().Contain("Carregando…").And.NotContain("Contestação da página 1").And.NotContain("Falha de teste.");

        nova.SetResult(Resposta(2, "Recurso da página 2"));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Recurso da página 2"));
        Consultas.Select(c => c.Pagina).Should().Equal(1, 2, 2);
    }
}
