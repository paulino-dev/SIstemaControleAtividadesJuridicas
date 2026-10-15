using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

public class AtividadesInterativosTests : TelaInterativa
{
    private static readonly AtividadeResumo Contestacao = new(
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1"), "Contestação — cobrança de ISS", StatusAtividade.Direcionada,
        new DateOnly(2026, 10, 6), true, "Construtora Serra Azul Ltda.", "Serra Azul × Município — ISS", "Tributário",
        ["Pedro Produção"], "Diana Diretora", 2);

    private static readonly UsuarioResumo Diana = new(
        AtorDaDiretoria.Id, "Diana Diretora", "diana@x.com", AtorDaDiretoria.PerfilId, "Diretoria", TipoPerfil.Diretoria, null, true, null);

    private static readonly UsuarioResumo Pedro = new(
        AtorDaProducao.Id, "Pedro Produção", "pedro@x.com", AtorDaProducao.PerfilId, "Produção", TipoPerfil.Producao, null, true, null);

    // Uma atividade na página; o escopo do usuário tem 8, uma atrasada.
    private static PaginaDeAtividades Pagina() => new([Contestacao], 1, 1, 20, 8, 1);

    private IRenderedComponent<Atividades> Lista(UsuarioAutenticado ator)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(ator)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ListarAtividades, Resultado<PaginaDeAtividades>>(Resultado<PaginaDeAtividades>.Ok(Pagina()));
        return Desenhar<Atividades>();
    }

    private ListarAtividades UltimaConsulta => Despachante.Recebidas.OfType<ListarAtividades>().Last();

    private static IElement Chip(IRenderedComponent<Atividades> tela, string rotulo) =>
        tela.FindAll(".aj-chips button").Single(b => b.TextContent == rotulo);

    // Um campo pelo nome ("Situação", "Responsável", "Revisor"), na barra do desktop ou no painel de filtros.
    private static IElement Campo(IRenderedComponent<Atividades> tela, string formulario, string nome) =>
        tela.Find(formulario).QuerySelectorAll("label.aj-campo-filtro")
            .Single(l => l.QuerySelector("span")!.TextContent == nome).QuerySelector("select")!;

    private static IElement CampoDaBarra(IRenderedComponent<Atividades> tela, string nome) => Campo(tela, "form.aj-barra-filtros", nome);

    private static IElement CampoDoPainel(IRenderedComponent<Atividades> tela, string nome) => Campo(tela, "form#filtros-de-atividades", nome);

    // O valor que o navegador mostraria: a opção marcada ou, sem nenhuma, a primeira.
    private static string Escolhida(IElement select) => ((IHtmlSelectElement)select).Value ?? "";

    private static string Texto(IElement elemento) => Regex.Replace(elemento.TextContent, "\\s+", " ").Trim();

    [Fact]
    public void Chip_de_situacao_busca_na_hora_mantendo_a_busca_e_o_responsavel()
    {
        var tela = Lista(AtorDaDiretoria);
        tela.Find("form.aj-linha-filtros input").Change("contestação");
        CampoDaBarra(tela, "Responsável").Change(Pedro.Id.ToString());

        Chip(tela, "Feitas").Click();

        var consulta = UltimaConsulta;
        consulta.Status.Should().Be(StatusAtividade.Feita);
        consulta.SomenteAtrasadas.Should().BeFalse();
        consulta.Busca.Should().Be("contestação");
        consulta.ResponsavelId.Should().Be(Pedro.Id);
        consulta.Pagina.Should().Be(1);
        Chip(tela, "Feitas").GetAttribute("aria-pressed").Should().Be("true");
        Chip(tela, "Todas · 8").GetAttribute("aria-pressed").Should().Be("false");
        // O campo do desktop mostra o mesmo estado que o chip.
        CampoDaBarra(tela, "Situação").QuerySelector("option[selected]")!.TextContent.Should().Be("Feita");
        CampoDaBarra(tela, "Responsável").QuerySelector("option[selected]")!.TextContent.Should().Be("Pedro Produção");
    }

    [Fact]
    public void Atrasadas_liga_so_atrasadas_e_limpa_a_situacao_e_Todas_limpa_os_dois()
    {
        var tela = Lista(AtorDaDiretoria);
        Chip(tela, "Feitas").Click();

        Chip(tela, "Atrasadas · 1").Click();

        UltimaConsulta.Status.Should().BeNull();
        UltimaConsulta.SomenteAtrasadas.Should().BeTrue();
        Chip(tela, "Atrasadas · 1").GetAttribute("aria-pressed").Should().Be("true");
        Chip(tela, "Feitas").GetAttribute("aria-pressed").Should().Be("false");
        tela.Find("label.aj-opcao--solta input").HasAttribute("checked").Should().BeTrue();

        Chip(tela, "Concluídas").Click();

        UltimaConsulta.Status.Should().Be(StatusAtividade.Concluida);
        UltimaConsulta.SomenteAtrasadas.Should().BeFalse();

        Chip(tela, "Todas · 8").Click();

        UltimaConsulta.Status.Should().BeNull();
        UltimaConsulta.SomenteAtrasadas.Should().BeFalse();
        Chip(tela, "Todas · 8").GetAttribute("aria-pressed").Should().Be("true");
        tela.Find("label.aj-opcao--solta input").HasAttribute("checked").Should().BeFalse();
    }

    [Fact]
    public void Painel_de_filtros_abre_filtrar_aplica_e_fecha_e_limpar_limpa_sem_tirar_a_busca()
    {
        var tela = Lista(AtorDaDiretoria);
        tela.Find("form.aj-linha-filtros input").Change("minuta");
        Texto(tela.Find("button[aria-controls=filtros-de-atividades]")).Should().Be("Responsável, revisor");

        tela.Find("button[aria-controls=filtros-de-atividades]").Click();

        tela.Find("button[aria-controls=filtros-de-atividades]").GetAttribute("aria-expanded").Should().Be("true");
        CampoDoPainel(tela, "Situação").QuerySelectorAll("option").Select(o => o.TextContent).Should().Equal(
            "Todas", "Criada", "Direcionada", "Feita", "Para correção", "Corrigida", "Concluída", "Reaberta");
        CampoDoPainel(tela, "Situação").Change("Criada");
        CampoDoPainel(tela, "Revisor").Change(Diana.Id.ToString());
        tela.Find("form#filtros-de-atividades").Submit();

        UltimaConsulta.Status.Should().Be(StatusAtividade.Criada);
        UltimaConsulta.RevisorId.Should().Be(Diana.Id);
        UltimaConsulta.Busca.Should().Be("minuta");
        tela.FindAll("form#filtros-de-atividades").Should().BeEmpty();
        tela.Find("button[aria-controls=filtros-de-atividades]").GetAttribute("aria-expanded").Should().Be("false");

        tela.Find("button[aria-controls=filtros-de-atividades]").Click();
        Escolhida(CampoDoPainel(tela, "Situação")).Should().Be("Criada");
        Escolhida(CampoDoPainel(tela, "Revisor")).Should().Be(Diana.Id.ToString());
        var consultas = Despachante.Recebidas.OfType<ListarAtividades>().Count();
        tela.Find("form#filtros-de-atividades").QuerySelectorAll("button").Single(b => b.TextContent == "Limpar").Click();

        Despachante.Recebidas.OfType<ListarAtividades>().Should().HaveCount(consultas);   // limpar não consulta
        CampoDoPainel(tela, "Situação").QuerySelector("option[selected]").Should().BeNull();   // nada escolhido: o navegador mostra "Todas"
        Escolhida(CampoDoPainel(tela, "Situação")).Should().BeEmpty();
        Escolhida(CampoDoPainel(tela, "Revisor")).Should().BeEmpty();
        tela.Find("form#filtros-de-atividades").Submit();
        UltimaConsulta.Status.Should().BeNull();
        UltimaConsulta.RevisorId.Should().BeNull();
        UltimaConsulta.ResponsavelId.Should().BeNull();
        UltimaConsulta.SomenteAtrasadas.Should().BeFalse();
        UltimaConsulta.Busca.Should().Be("minuta");
    }

    [Fact]
    public void Producao_tem_o_botao_mais_filtros_e_o_painel_sem_responsavel_nem_revisor()
    {
        var tela = Lista(AtorDaProducao);
        Texto(tela.Find("button[aria-controls=filtros-de-atividades]")).Should().Be("Mais filtros");

        tela.Find("button[aria-controls=filtros-de-atividades]").Click();

        var painel = tela.Find("form#filtros-de-atividades").TextContent;
        painel.Should().Contain("Situação").And.Contain("Só atrasadas").And.Contain("Filtrar");
        painel.Should().NotContain("Responsável").And.NotContain("Revisor");
        Despachante.Recebidas.OfType<ListarUsuarios>().Should().BeEmpty();
    }

    [Fact]
    public void Diretoria_com_responsavel_escolhido_continua_vendo_o_total_do_escritorio_no_subtitulo()
    {
        var tela = Lista(AtorDaDiretoria);
        CampoDaBarra(tela, "Responsável").Change(Pedro.Id.ToString());

        tela.Find("form.aj-barra-filtros").Submit();

        UltimaConsulta.ResponsavelId.Should().Be(Pedro.Id);
        // O subtítulo é o escopo (NoEscopo); a paginação é o que casou com o filtro (Total).
        Texto(tela.Find("header.aj-cabecalho p.aj-subtitulo")).Should().Be("8 atividades · 1 atrasada");
        Texto(tela.Find(".aj-paginacao .aj-subtitulo")).Should().Be("Página 1 de 1 · 1 atividade");
    }

    [Fact]
    public void Falha_da_consulta_mostra_o_erro_sem_subtitulo_nem_numeros_e_tentar_de_novo_recarrega()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ListarAtividades, Resultado<PaginaDeAtividades>>(
                Resultado<PaginaDeAtividades>.Falha(TipoErro.Validacao, "Falha de teste."));
        var tela = Desenhar<Atividades>();

        tela.Markup.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        tela.Markup.Should().NotContain("Nenhuma atividade.").And.NotContain("Página 1");
        tela.FindAll("header.aj-cabecalho p.aj-subtitulo").Should().BeEmpty();
        tela.FindAll(".aj-chips button").Take(2).Select(b => b.TextContent).Should().Equal("Todas", "Atrasadas");

        Despachante.Responde<ListarAtividades, Resultado<PaginaDeAtividades>>(Resultado<PaginaDeAtividades>.Ok(Pagina()));
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => Texto(tela.Find("header.aj-cabecalho p.aj-subtitulo")).Should().Be("8 atividades · 1 atrasada"));
        tela.FindAll(".aj-chips button").Take(2).Select(b => b.TextContent).Should().Equal("Todas · 8", "Atrasadas · 1");
        tela.Markup.Should().Contain("Contestação — cobrança de ISS").And.NotContain("Falha de teste.");
    }

    [Fact]
    public void Clique_num_chip_com_outra_consulta_em_curso_nao_dispara_outra()
    {
        var tela = Lista(AtorDaDiretoria);
        var resposta = new TaskCompletionSource<Resultado<PaginaDeAtividades>>();
        Despachante.RespondeDepois<ListarAtividades, Resultado<PaginaDeAtividades>>(resposta.Task);

        Chip(tela, "Feitas").Click();
        Chip(tela, "Concluídas").Click();

        Despachante.Recebidas.OfType<ListarAtividades>().Should().HaveCount(2);   // a carga inicial e a de "Feitas"
        resposta.SetResult(Resultado<PaginaDeAtividades>.Ok(Pagina()));
        tela.WaitForAssertion(() => Chip(tela, "Feitas").GetAttribute("aria-pressed").Should().Be("true"));
        Chip(tela, "Concluídas").GetAttribute("aria-pressed").Should().Be("false");
        UltimaConsulta.Status.Should().Be(StatusAtividade.Feita);
    }
}
