using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Web.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

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

        // O invólucro é um div: a lista de dentro é um div, que não cabe num span.
        html.Should().Contain("<div class=\"aj-menu-acoes\">")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\"[^>]*aria-label=\"Ações de modelo.pdf\"")
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

    private const string Frase = "Visíveis para todos os clientes do escritório.";

    private static string BarraDaTela(string html) =>
        Regex.Match(html, "<header class=\"aj-barra-superior\">.*?</header>", RegexOptions.Singleline).Value;

    // Sem subtítulo próprio, a barra do mobile repete o do desktop; com um, mostra o dela.
    [Theory]
    [InlineData(null, Frase)]
    [InlineData("3 documentos", "3 documentos")]
    public async Task Cabecalho_traz_o_subtitulo_na_barra_superior_do_mobile(string? subtituloMobile, string naBarra)
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Documentos comuns", ["Subtitulo"] = Frase, ["SubtituloMobile"] = subtituloMobile }));

        html.Should().Contain($"<p class=\"aj-subtitulo\">{Frase}</p>");
        BarraDaTela(html).Should().Contain("<h1 class=\"aj-titulo\">Documentos comuns</h1>")
            .And.Contain($"<span class=\"aj-subtitulo\">{naBarra}</span>");
    }

    [Fact]
    public async Task Cabecalho_com_subtitulo_do_mobile_vazio_fica_sem_subtitulo_na_barra_superior()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Documentos comuns", ["Subtitulo"] = Frase, ["SubtituloMobile"] = string.Empty }));

        html.Should().Contain($"<p class=\"aj-subtitulo\">{Frase}</p>").And.NotContain("<span class=\"aj-subtitulo\">");
        // A barra está no HTML, com o título: é só o subtítulo que ela não tem.
        BarraDaTela(html).Should().Contain("<h1 class=\"aj-titulo\">Documentos comuns</h1>").And.NotContain("aj-subtitulo");
    }

    [Fact]
    public async Task Cabecalho_leva_o_complemento_ao_lado_do_titulo_e_a_linha_do_mobile_na_barra()
    {
        RenderFragment complemento = c => c.AddMarkupContent(0, "<div class=\"aj-segmentado\">segmentado do cabeçalho</div>");
        RenderFragment linha = c => c.AddMarkupContent(0, "<div class=\"aj-segmentado\">segmentado da barra</div>");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(new()
        {
            ["Titulo"] = "Auditoria", ["Complemento"] = complemento, ["LinhaDoMobile"] = linha,
            ["Acao"] = "Novo registro", ["AcaoHref"] = "/novo",
        }));

        var desktop = BlocoDeVisao.De(html, "aj-desktop-tablet");
        desktop.Should().MatchRegex("(?s)<header class=\"aj-cabecalho\">.*segmentado do cabeçalho.*</header>");
        desktop.Should().NotContain("segmentado da barra");
        // A ordem do cabeçalho: o início (título), o complemento e, por último, a ação.
        var inicio = desktop.IndexOf("aj-cabecalho__inicio", StringComparison.Ordinal);
        var posicaoDoComplemento = desktop.IndexOf("segmentado do cabeçalho", StringComparison.Ordinal);
        var posicaoDaAcao = desktop.IndexOf("Novo registro", StringComparison.Ordinal);
        inicio.Should().BeGreaterThanOrEqualTo(0);
        posicaoDoComplemento.Should().BeGreaterThan(inicio);
        posicaoDaAcao.Should().BeGreaterThan(posicaoDoComplemento);
        html.Should().MatchRegex(
            "(?s)<header class=\"aj-barra-superior\">\\s*<div class=\"aj-barra-superior__linha\">.*?</div>\\s*<div class=\"aj-segmentado\">segmentado da barra</div>\\s*</header>");
    }

    [Fact]
    public async Task Cabecalho_sem_complemento_nem_linha_continua_como_era()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Áreas de atuação" }));

        html.Should().MatchRegex("(?s)<header class=\"aj-barra-superior\">\\s*<div class=\"aj-barra-superior__linha\">.*?</div>\\s*</header>")
            .And.Contain("<h1 class=\"aj-titulo\">Áreas de atuação</h1>");
        html.Should().NotContain("aj-segmentado");
    }

    [Fact]
    public async Task Cabecalho_de_detalhe_traz_o_voltar_no_desktop_e_a_barra_de_voltar_no_mobile()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDeDetalhe>>(new()
        {
            ["Titulo"] = "Novo usuário", ["Subtitulo"] = "Escolha o perfil.", ["VoltarPara"] = "/usuarios", ["Origem"] = "Usuários",
        }));

        var desktop = BlocoDeVisao.De(html, "aj-desktop-tablet");
        desktop.Should().MatchRegex("(?s)<div>\\s*<a href=\"/usuarios\" class=\"aj-voltar\">\\s*<svg[^>]*>.*?</svg>\\s*Usuários</a>\\s*</div>")
            .And.MatchRegex("<div class=\"aj-cabecalho__texto aj-cabecalho__texto--folgado\">\\s*<h1 class=\"aj-titulo\">Novo usuário</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Escolha o perfil.</p>");
        html.Should().MatchRegex(
            "<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"/usuarios\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para Usuários\">");
        // O voltar do desktop não vai para a barra do mobile, e a barra não repete o subtítulo.
        Regex.Matches(html, "class=\"aj-voltar\"").Should().ContainSingle();
    }

    [Fact]
    public async Task Cabecalho_de_ficha_traz_o_painel_com_as_acoes_no_desktop_e_o_bloco_de_titulo_no_mobile()
    {
        RenderFragment acoes = c => c.AddMarkupContent(0, "<a href=\"/clientes/1/editar\" class=\"aj-botao aj-botao--secundario\">Editar</a>");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDeFicha>>(new()
        {
            ["Titulo"] = "Transportes Litoral Ltda.", ["Subtitulo"] = "Pessoa jurídica · CNPJ 00.000.000/0002-00 · Ativo",
            ["Resumo"] = "Pessoa jurídica · Ativo", ["TituloDaBarra"] = "Cliente", ["VoltarPara"] = "/clientes",
            ["Origem"] = "Clientes", ["Acoes"] = acoes,
        }));

        var desktopETablet = BlocoDeVisao.De(html, "aj-desktop-tablet");
        desktopETablet.Should().MatchRegex("(?s)<div>\\s*<a href=\"/clientes\" class=\"aj-voltar\">\\s*<svg[^>]*>.*?</svg>\\s*Clientes</a>\\s*</div>")
            .And.MatchRegex("(?s)<header class=\"aj-painel aj-painel--principal aj-painel--cabecalho\">\\s*<div class=\"aj-cabecalho__texto aj-cabecalho__texto--folgado\">\\s*<h1 class=\"aj-titulo\">Transportes Litoral Ltda.</h1>\\s*<p class=\"aj-subtitulo\">Pessoa jurídica · CNPJ 00.000.000/0002-00 · Ativo</p>\\s*</div>")
            .And.MatchRegex("(?s)<div class=\"aj-so-desktop\">\\s*<div class=\"aj-painel__botoes\">\\s*<a href=\"/clientes/1/editar\"");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex(
            "(?s)<div class=\"aj-titulo-da-ficha\">\\s*<span class=\"aj-ajuda\">Pessoa jurídica · Ativo</span>\\s*<h2 class=\"aj-titulo\">Transportes Litoral Ltda.</h2>");
        html.Should().MatchRegex(
            "(?s)<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"/clientes\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para Clientes\">.*?<h1>Cliente</h1>");
    }

    [Fact]
    public async Task Cabecalho_de_ficha_sem_acoes_nao_emite_o_grupo_de_botoes()
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDeFicha>>(new()
        {
            ["Titulo"] = "Litoral — reclamação trabalhista", ["TituloDaBarra"] = "Processo", ["VoltarPara"] = "/processos", ["Origem"] = "Processos",
        });

        html.Should().Contain("aj-painel--cabecalho").And.NotContain("aj-painel__botoes").And.NotContain("aj-subtitulo");
    }

    [Fact]
    public async Task Abas_marcam_a_escolhida_e_trazem_o_contador()
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<Abas>(new()
        {
            ["Rotulo"] = "Assuntos do cliente", ["Ativa"] = "dados",
            ["Itens"] = new List<Aba> { new("dados", "Dados"), new("processos", "Processos", 2) },
        });

        html.Should().MatchRegex(
            "(?s)<div class=\"aj-abas\" role=\"tablist\" aria-label=\"Assuntos do cliente\">\\s*<button type=\"button\" role=\"tab\" class=\"aj-abas__aba\" aria-selected=\"true\"[^>]*>Dados</button>\\s*<button type=\"button\" role=\"tab\" class=\"aj-abas__aba\" aria-selected=\"false\"[^>]*>Processos<span class=\"aj-abas__contador\">2</span></button>");
    }

    [Fact]
    public async Task Rotulo_de_campo_leva_a_classe_de_posicao()
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<RotuloDeCampo<string>>(new()
        {
            ["Texto"] = "Logradouro", ["Para"] = (System.Linq.Expressions.Expression<Func<string>>)(() => "x"),
            ["Classe"] = "aj-grade-endereco__logradouro",
        });

        html.Should().Contain("<label class=\"aj-rotulo aj-grade-endereco__logradouro\">");
    }

    [Fact]
    public async Task Desenho_estatico_aceita_formulario_com_nome()
    {
        var html = await TelaDeTeste.RenderizarComAsync<ComFormularioNomeado>(new DespachanteCombinado(), new());

        html.Should().Contain("<form");
    }

    private sealed class ComFormularioNomeado : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder construtor)
        {
            construtor.OpenComponent<EditForm>(0);
            construtor.AddComponentParameter(1, nameof(EditForm.Model), new object());
            construtor.AddComponentParameter(2, nameof(EditForm.FormName), "teste");
            construtor.CloseComponent();
        }
    }
}
