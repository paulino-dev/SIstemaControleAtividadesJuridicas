using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Web.Components.Layout;
using Microsoft.AspNetCore.Components;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Os blocos de tela do design system, desenhados sozinhos.</summary>
public class ComponentesDoDesignSystemTests
{
    [Theory]
    [InlineData("adicionar")]
    [InlineData("buscar")]
    [InlineData("filtros")]
    [InlineData("voltar")]
    [InlineData("avancar")]
    [InlineData("confirmar")]
    [InlineData("excluir")]
    [InlineData("devolver")]
    [InlineData("entrar")]
    [InlineData("baixar")]
    [InlineData("atencao")]
    [InlineData("erro")]
    public async Task Icone_do_design_system_tem_tracado(string nome)
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(new() { ["Nome"] = nome });

        html.Should().MatchRegex("<svg[^>]*aria-hidden=\"true\"[^>]*>\\s*<(path|circle|rect)");
    }

    [Fact]
    public async Task Icone_aceita_classe()
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(
            new() { ["Nome"] = "avancar", ["Classe"] = "aj-cartao__seta" });

        html.Should().Contain("class=\"aj-cartao__seta\"");
    }

    [Theory]
    [InlineData(TipoDeAviso.Sucesso, "aj-aviso aj-aviso--sucesso", "status", "M5 12.5l4.5 4.5L19 7.5")]
    [InlineData(TipoDeAviso.Atencao, "aj-aviso aj-aviso--atencao", "status", "M12 4l9 16H3z")]
    [InlineData(TipoDeAviso.Erro, "aj-aviso aj-aviso--erro", "alert", "M12 7.5v5.5")]
    public async Task Aviso_traz_a_classe_o_papel_e_o_icone_do_tipo(
        TipoDeAviso tipo, string classe, string papel, string tracado)
    {
        RenderFragment texto = construtor => construtor.AddContent(0, "Revisor alterado.");

        var html = await TelaDeTeste.RenderizarSemDadosAsync<Aviso>(
            new() { ["Tipo"] = tipo, ["ChildContent"] = texto });

        // O traçado é o do ícone do tipo: confirmar, atenção ou erro.
        html.Should().Contain($"class=\"{classe}\"").And.Contain($"role=\"{papel}\"")
            .And.Contain($"d=\"{tracado}\"").And.Contain("<span>Revisor alterado.</span>");
    }

    [Fact]
    public async Task Icone_de_mais_e_preenchido_por_classe_e_nao_por_estilo()
    {
        var solto = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(new() { ["Nome"] = "mais" });
        var comClasse = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(new() { ["Nome"] = "mais", ["Classe"] = "aj-x" });

        solto.Should().Contain("class=\"aj-icone-cheio\"").And.NotContain("style=");
        comClasse.Should().Contain("class=\"aj-x aj-icone-cheio\"");
    }

    [Fact]
    public async Task Aviso_com_acoes_e_um_dialogo_de_alerta_com_titulo_e_botoes()
    {
        RenderFragment texto = c => c.AddContent(0, "Não envie aqui arquivos de um cliente.");
        RenderFragment acoes = c => c.AddMarkupContent(0, "<button type=\"button\" class=\"aj-botao aj-botao--secundario\">Cancelar</button>");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<Aviso>(new()
        {
            ["Tipo"] = TipoDeAviso.Atencao, ["Rotulo"] = "Confirmar envio", ["Titulo"] = "modelo.pdf · 96 KB",
            ["ChildContent"] = texto, ["Acoes"] = acoes,
        }));

        html.Should().MatchRegex("<div class=\"aj-aviso aj-aviso--atencao aj-aviso--com-acoes\"[^>]*role=\"alertdialog\"")
            .And.Contain("aria-label=\"Confirmar envio\"").And.Contain("tabindex=\"-1\"")
            .And.Contain("<strong>modelo.pdf · 96 KB</strong>").And.Contain("Não envie aqui arquivos de um cliente.")
            .And.MatchRegex("<div class=\"aj-aviso__acoes\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario\">Cancelar</button>");
    }

    [Fact]
    public async Task Aviso_de_erro_com_acao_continua_sendo_alerta()
    {
        RenderFragment texto = c => c.AddContent(0, "Não foi possível consultar.");
        RenderFragment acoes = c => c.AddMarkupContent(0, "<button type=\"button\" class=\"aj-botao aj-botao--secundario\">Tentar de novo</button>");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<Aviso>(
            new() { ["Tipo"] = TipoDeAviso.Erro, ["ChildContent"] = texto, ["Acoes"] = acoes }));

        html.Should().Contain("role=\"alert\"").And.NotContain("alertdialog").And.NotContain("aria-label=").And.NotContain("<strong>");
    }

    [Theory]
    [InlineData(1, 3, 45, false, "Página 1 de 3 · 45 atividades", true, false)]
    [InlineData(3, 3, 45, false, "Página 3 de 3 · 45 atividades", false, true)]
    [InlineData(1, 1, 1, true, "Página 1 de 1 · 1 atividade", true, true)]
    public async Task Paginacao_diz_onde_esta_e_trava_as_pontas(
        int numero, int paginas, int total, bool sempre, string texto, bool anteriorTravado, bool proximaTravada)
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<Paginacao>(new()
        {
            ["Numero"] = numero, ["TotalDePaginas"] = paginas, ["Total"] = total,
            ["Singular"] = "atividade", ["Plural"] = "atividades", ["SempreVisivel"] = sempre,
        }));

        html.Should().Contain("<div class=\"aj-paginacao\">").And.Contain($"<span class=\"aj-subtitulo\">{texto}</span>");
        Regex.IsMatch(html, "<button[^>]*disabled[^>]*>Anterior</button>").Should().Be(anteriorTravado);
        Regex.IsMatch(html, "<button[^>]*disabled[^>]*>Próxima</button>").Should().Be(proximaTravada);
        Regex.Matches(html, "<button type=\"button\" class=\"aj-botao aj-botao--secundario\"").Should().HaveCount(2);
    }

    [Fact]
    public async Task Paginacao_de_uma_pagina_so_nao_aparece()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<Paginacao>(new()
        {
            ["Numero"] = 1, ["TotalDePaginas"] = 1, ["Total"] = 4, ["Singular"] = "documento", ["Plural"] = "documentos",
        }));

        html.Trim().Should().BeEmpty();
    }

    [Theory]
    [InlineData("Falhou.", true, true, "aj-aviso--erro", "Tentar de novo")]
    [InlineData(null, true, true, "aj-subtitulo", "Carregando…")]
    [InlineData(null, false, true, "aj-estado-lista", "Nenhum documento.")]
    [InlineData(null, false, false, "aj-linha", "a lista")]
    public async Task Estado_da_lista_mostra_um_estado_por_vez(
        string? erro, bool carregando, bool vazio, string classe, string texto)
    {
        RenderFragment lista = c => c.AddMarkupContent(0, "<div class=\"aj-linha\">a lista</div>");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<EstadoDaLista>(new()
        {
            ["Erro"] = erro, ["Carregando"] = carregando, ["Vazio"] = vazio,
            ["TextoDeVazio"] = "Nenhum documento.", ["ChildContent"] = lista,
        }));

        html.Should().Contain(classe).And.Contain(texto);
        // Um estado só: os textos dos outros não aparecem.
        foreach (var outro in new[] { "Tentar de novo", "Carregando…", "Nenhum documento.", "a lista" }.Where(t => t != texto))
            html.Should().NotContain(outro);
    }

    [Fact]
    public async Task Menu_de_acoes_nasce_fechado_com_os_itens_no_html()
    {
        RenderFragment itens = c => c.AddMarkupContent(0, "<button type=\"button\" class=\"aj-menu-acoes__item\">Renomear</button>");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<MenuDeAcoes>(
            new() { ["Rotulo"] = "Ações de modelo.pdf", ["ChildContent"] = itens }));

        html.Should().MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\"[^>]*aria-label=\"Ações de modelo.pdf\"")
            .And.Contain("aria-expanded=\"false\"").And.Contain("class=\"aj-icone-cheio\"")
            .And.MatchRegex("<div class=\"aj-menu-acoes__lista\"[^>]*\\shidden[^>]*>\\s*<button type=\"button\" class=\"aj-menu-acoes__item\">Renomear</button>")
            .And.NotContain("aj-menu-acoes__fundo");
    }

    [Fact]
    public async Task Cabecalho_com_acao_traz_o_botao_e_o_botao_flutuante()
    {
        var botao = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<CabecalhoDePagina>(
            new() { ["Titulo"] = "Documentos comuns", ["Acao"] = "Enviar documento" }));
        var link = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<CabecalhoDePagina>(
            new() { ["Titulo"] = "Clientes", ["Acao"] = "Novo cliente", ["AcaoHref"] = "/clientes/novo" }));
        var semAcao = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<CabecalhoDePagina>(new() { ["Titulo"] = "Início" }));

        // O ícone é um componente: pode haver espaço em branco em volta do svg.
        botao.Should().MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Enviar documento</button>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-fab\" aria-label=\"Enviar documento\"");
        link.Should().MatchRegex("<a href=\"/clientes/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Novo cliente</a>")
            .And.MatchRegex("<a href=\"/clientes/novo\" class=\"aj-fab\" aria-label=\"Novo cliente\"");
        semAcao.Should().NotContain("aj-fab").And.NotContain("aj-botao--primario");
    }
}
