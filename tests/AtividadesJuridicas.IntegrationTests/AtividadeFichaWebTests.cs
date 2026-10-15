using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Atividades;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadeFichaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _minuta, _interna;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();

        var area = await _app.ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        area.Sucesso.Should().BeTrue(area.Mensagem);
        var litoral = await _app.ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null, new DadosDoClienteEntrada("Transportes Litoral Ltda.", null, null, new EnderecoEntrada(), null),
            "11.222.333/0001-81", null, null, null, null));
        litoral.Sucesso.Should().BeTrue(litoral.Mensagem);
        var processo = await _app.ExecutarAsync(_admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(null, "Litoral — reclamação trabalhista", area.Valor, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(litoral.Valor, PoloProcessual.Passivo)]));
        processo.Sucesso.Should().BeTrue(processo.Mensagem);

        var minuta = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Minuta de acordo trabalhista", "Redigir a minuta com as condições da audiência.", new DateOnly(2090, 10, 9),
            litoral.Valor, processo.Valor, null, 2));
        minuta.Sucesso.Should().BeTrue(minuta.Mensagem);
        _minuta = minuta.Valor;
        (await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            _minuta, [new ResponsavelEntrada(_pedro, "Conferir os cálculos das verbas rescisórias.")], _admin))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(_pedro, new ConcluirMinhaParte(_minuta))).Sucesso.Should().BeTrue();

        var interna = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Recurso interno", "Sem responsáveis.", new DateOnly(2020, 1, 15), null, null, null, 3));
        interna.Sucesso.Should().BeTrue(interna.Mensagem);
        _interna = interna.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota, bool comoPedro = false)
    {
        var cliente = comoPedro
            ? await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro)
            : await _app.ClienteAutenticadoAsync();
        return WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));
    }

    private static List<string> Grupos(string html, string padrao) =>
        [.. Regex.Matches(html, padrao).Select(m => m.Groups[1].Value)];

    [Fact]
    public async Task Diretoria_ve_o_cabecalho_os_detalhes_os_responsaveis_e_o_historico_em_cada_visao()
    {
        var html = await PaginaAsync($"/atividades/{_minuta}");

        // O voltar: link no desktop e no tablet, barra de voltar com "Atividade" no mobile.
        html.Should().Contain("<h1>Atividade</h1>").And.Contain("aria-label=\"Voltar para Atividades\"")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Atividades</a>");

        BlocoDeVisao.De(html, "aj-desktop-tablet").Should().MatchRegex(
            "(?s)<header class=\"aj-painel aj-painel--principal aj-painel--cabecalho-com-faixa\">\\s*" +
            "<div class=\"aj-titulo-da-ficha aj-titulo-da-ficha--com-situacao\">\\s*" +
            "<div class=\"aj-titulo-da-ficha__situacao\">\\s*<span class=\"aj-etiqueta aj-etiqueta--feita[^\"]*\">Feita</span>\\s*" +
            "<span class=\"aj-titulo-da-ficha__entrega\">Entrega em 09/10/2090 · Trabalhista</span>\\s*</div>\\s*" +
            "<h1 class=\"aj-titulo\">Minuta de acordo trabalhista</h1>");

        // Desktop: o painel Detalhes vem primeiro no arranjo (o CSS o leva para a direita).
        html.Should().MatchRegex(
            "<div class=\"aj-lado-a-lado aj-lado-a-lado--painel-primeiro aj-lado-a-lado--ficha aj-lado-a-lado--painel-estreito\">\\s*" +
            "<div class=\"aj-so-desktop\">\\s*<aside class=\"aj-painel aj-painel--ficha aj-painel--detalhes aj-lado-a-lado__painel\" aria-label=\"Dados da atividade\">\\s*<h2>Detalhes</h2>");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex(
                "(?s)<h2>Detalhes</h2>\\s*<dl class=\"aj-detalhes\">\\s*" +
                "<div><dt>Entrega</dt><dd>09/10/2090</dd></div>\\s*" +
                "<div><dt>Cliente</dt><dd>Transportes Litoral Ltda.</dd></div>\\s*" +
                "<div><dt>Processo</dt><dd>Litoral — reclamação trabalhista</dd></div>\\s*" +
                "<div><dt>Área</dt><dd>Trabalhista</dd></div>\\s*" +
                "<div><dt>Complexidade</dt><dd>2</dd></div>\\s*" +
                "<div><dt>Autor</dt><dd>Diana Diretora</dd></div>\\s*" +
                "<div><dt>Revisor</dt><dd>Diana Diretora</dd></div>\\s*</dl>")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--ficha aj-painel--descricao\">\\s*<h2>Descrição</h2>\\s*<p>Redigir a minuta com as condições da audiência.</p>")
            .And.Contain($"<li class=\"aj-responsavel\"><span class=\"aj-avatar {AvatarDePessoa.Classe("Pedro Produção")}\" aria-hidden=\"true\">PP</span>" +
                "<div class=\"aj-responsavel__texto\"><div class=\"aj-responsavel__cabeca\"><strong>Pedro Produção</strong><span class=\"aj-responsavel__estado\">")
            .And.MatchRegex("<span class=\"aj-so-desktop\">Concluída em \\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}</span>")
            .And.Contain("<span class=\"aj-responsavel__direcionamento\">Conferir os cálculos das verbas rescisórias.</span>");
        // Histórico em linha do tempo, na ordem do servidor: o primeiro registro só com a situação nova; o último cheio e sem traço.
        Grupos(desktop, "<span class=\"aj-historico__mudanca\">([^<]+)</span>")
            .Should().Equal("Criada", "Criada → Direcionada", "Direcionada → Feita");
        Grupos(desktop, "<span class=\"aj-historico__autor\">([^<]+) · \\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}</span>")
            .Should().Equal("Diana Diretora", "Diana Diretora", "Pedro Produção");
        desktop.Should().Contain("<span class=\"aj-historico__ponto\"></span><span class=\"aj-historico__linha\"></span>")
            .And.Contain("<span class=\"aj-historico__ponto aj-historico__ponto--direcionada\"></span><span class=\"aj-historico__linha\"></span>")
            .And.Contain("<span class=\"aj-historico__ponto aj-historico__ponto--feita aj-historico__ponto--atual\"></span></div>");

        // Tablet e mobile: a grade Descrição | Dados (sem Entrega e sem Área), e as datas curtas dos responsáveis.
        var tabletEMobile = BlocoDeVisao.De(html, "aj-tablet-mobile");
        tabletEMobile.Should().MatchRegex(
                "(?s)<div class=\"aj-aba-painel\">\\s*<div class=\"aj-grade\">\\s*<section class=\"aj-painel aj-painel--ficha aj-painel--descricao\">\\s*<h2>Descrição</h2>.*?</section>\\s*" +
                "<section class=\"aj-painel aj-painel--ficha aj-painel--detalhes\">\\s*<h2>Dados</h2>\\s*<dl class=\"aj-detalhes\">\\s*" +
                "<div><dt>Cliente</dt><dd>Transportes Litoral Ltda.</dd></div>\\s*" +
                "<div><dt>Processo</dt><dd>Litoral — reclamação trabalhista</dd></div>\\s*" +
                "<div><dt>Autor</dt><dd>Diana Diretora</dd></div>\\s*" +
                "<div><dt>Revisor</dt><dd>Diana Diretora</dd></div>\\s*" +
                "<div><dt>Complexidade</dt><dd>2</dd></div>\\s*</dl>")
            .And.MatchRegex("<span class=\"aj-tablet-mobile\">Concluída em \\d{2}/\\d{2} \\d{2}:\\d{2}</span>")
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\">\\s*<section class=\"aj-painel aj-painel--ficha aj-painel--historico\">\\s*<h2>Histórico</h2>");

        // Mobile: a situação com a entrega curta e o título em h2 (o h1 é o da barra); as abas começam em Detalhes.
        BlocoDeVisao.De(html, "aj-so-mobile").Should()
            .MatchRegex(
                "(?s)<div class=\"aj-titulo-da-ficha aj-titulo-da-ficha--com-situacao\">\\s*" +
                "<div class=\"aj-titulo-da-ficha__situacao\">\\s*<span class=\"aj-etiqueta aj-etiqueta--feita[^\"]*\">Feita</span>\\s*" +
                "<span class=\"aj-titulo-da-ficha__entrega\">Entrega 09/10/2090 · Trabalhista</span>\\s*</div>\\s*" +
                "<h2 class=\"aj-titulo\">Minuta de acordo trabalhista</h2>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-abas\" role=\"tablist\" aria-label=\"Seções da atividade\">.*?aria-selected=\"true\"[^>]*>Detalhes</button>" +
                ".*?>Comentários(<span class=\"aj-abas__contador\">\\d+</span>)?</button>" +
                ".*?>Documentos(<span class=\"aj-abas__contador\">\\d+</span>)?</button>.*?>Histórico</button>");

        // Comentários e documentos: um painel de cada, uma vez só (os componentes têm estado próprio).
        Regex.Matches(html, "<h2>Comentários</h2>").Count.Should().Be(1);
        Regex.Matches(html, "<h2>Documentos</h2>").Count.Should().Be(1);
    }

    [Fact]
    public async Task Producao_responsavel_ve_a_ficha_com_o_proprio_direcionamento()
    {
        var html = await PaginaAsync($"/atividades/{_minuta}", comoPedro: true);

        html.Should().Contain("<h1 class=\"aj-titulo\">Minuta de acordo trabalhista</h1>").And.Contain("<h2>Detalhes</h2>")
            .And.Contain("<span class=\"aj-responsavel__direcionamento\">Conferir os cálculos das verbas rescisórias.</span>")
            .And.Contain("<h2>Comentários</h2>").And.Contain("<h2>Documentos</h2>");
        html.Should().NotContain("Atividade não encontrada.").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Producao_que_nao_e_responsavel_ve_nao_encontrada_sem_paineis_e_sem_consultar_comentarios_nem_documentos()
    {
        var antes = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Count;

        var html = await PaginaAsync($"/atividades/{_interna}", comoPedro: true);

        html.Should().Contain("Atividade não encontrada.").And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">");
        html.Should().NotContain("Recurso interno").And.NotContain("<h2>Detalhes</h2>").And.NotContain("<h2>Comentários</h2>")
            .And.NotContain("<h2>Documentos</h2>").And.NotContain("aj-abas");
        // Só a leitura da atividade foi negada: os comentários e os documentos nem foram pedidos (cada pedido negado deixaria um registro).
        var negados = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Skip(antes)
            .Select(e => (string?)JsonNode.Parse(e.Dados!)!["requisicao"]).ToList();
        negados.Should().NotBeEmpty().And.OnlyContain(r => r == nameof(ObterAtividade));
    }

    [Fact]
    public async Task Atividade_que_nao_existe_mostra_nao_encontrada_com_o_voltar_e_nada_mais()
    {
        var html = await PaginaAsync($"/atividades/{Guid.NewGuid()}");

        html.Should().Contain("Atividade não encontrada.").And.Contain("aria-label=\"Voltar para Atividades\"")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">");
        html.Should().NotContain("<h2>Detalhes</h2>").And.NotContain("<h2>Comentários</h2>").And.NotContain("<h2>Documentos</h2>")
            .And.NotContain("aj-abas");
    }

    [Fact]
    public async Task Vinda_de_produtores_volta_ao_painel_do_produtor()
    {
        var cliente = await _app.ClienteAutenticadoAsync();
        async Task<string> Pagina(string rota) => WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));

        var comProdutor = await Pagina($"/atividades/{_minuta}?de=produtores&produtor={_pedro}");
        var semProdutor = await Pagina($"/atividades/{_minuta}?de=produtores");
        var produtorQueNaoVale = await Pagina($"/atividades/{_minuta}?de=produtores&produtor=lixo");
        var naoEncontrada = await Pagina($"/atividades/{Guid.NewGuid()}?de=produtores&produtor={_pedro}");

        comProdutor.Should().MatchRegex($"<a href=\"/produtores\\?produtor={_pedro}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Produtores</a>")
            .And.Contain("aria-label=\"Voltar para Produtores\"")
            .And.Contain("<h1 class=\"aj-titulo\">Minuta de acordo trabalhista</h1>");
        comProdutor.Should().NotContain("<a href=\"/atividades\" class=\"aj-voltar\">");
        semProdutor.Should().MatchRegex("<a href=\"/produtores\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Produtores</a>");
        produtorQueNaoVale.Should().MatchRegex("<a href=\"/produtores\" class=\"aj-voltar\">");
        naoEncontrada.Should().Contain("Atividade não encontrada.")
            .And.MatchRegex($"<a href=\"/produtores\\?produtor={_pedro}\" class=\"aj-voltar\">");
    }

    [Fact]
    public async Task Atividade_sem_vinculos_nem_responsaveis_mostra_a_atrasada_e_os_vazios()
    {
        var html = await PaginaAsync($"/atividades/{_interna}");

        // A atrasada vai junto da situação, com ponto e sem ícone; sem área, a entrega fica sem a parte da área.
        BlocoDeVisao.De(html, "aj-desktop-tablet").Should().MatchRegex(
            "<div class=\"aj-titulo-da-ficha__situacao\">\\s*<span class=\"aj-etiqueta aj-etiqueta--criada[^\"]*\">Criada</span>\\s*" +
            "<span class=\"aj-etiqueta aj-etiqueta--atrasada[^\"]*\">Atrasada</span>\\s*" +
            "<span class=\"aj-titulo-da-ficha__entrega\">Entrega em 15/01/2020</span>\\s*</div>");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().Contain("<div><dt>Cliente</dt><dd>—</dd></div>").And.Contain("<div><dt>Processo</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Área</dt><dd>—</dd></div>").And.Contain("<div><dt>Revisor</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Complexidade</dt><dd>3</dd></div>")
            .And.MatchRegex("<h2>Responsáveis</h2>\\s*<div class=\"aj-estado-lista\"><strong>Nenhum responsável.</strong></div>");
        Grupos(desktop, "<span class=\"aj-historico__mudanca\">([^<]+)</span>").Should().Equal("Criada");
        // Um registro só: é o atual (cheio) e não tem traço para o seguinte.
        desktop.Should().Contain("<span class=\"aj-historico__ponto aj-historico__ponto--atual\"></span></div>")
            .And.NotContain("aj-historico__linha");
    }
}
