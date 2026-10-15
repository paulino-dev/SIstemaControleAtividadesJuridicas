using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class EventoFichaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _beatriz, _audiencia, _daProducao, _reservado;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
        var producao = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
                .FirstAsync(p => p.Tipo == TipoPerfil.Producao && p.Ativo)).Id);
        var beatriz = await _app.ExecutarAsync(_admin, new CriarUsuario("Beatriz Lins", "beatriz@x.com", "senha-da-beatriz", producao, null));
        beatriz.Sucesso.Should().BeTrue(beatriz.Mensagem);
        _beatriz = beatriz.Valor;

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

        var audiencia = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Audiência de instrução — Litoral", "Audiência de instrução.\nLevar a proposta de acordo em seis parcelas.",
            "Fórum Trabalhista, sala 4", TipoEvento.Audiencia, new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc), 90,
            litoral.Valor, processo.Valor, null, [_beatriz, _pedro]));
        audiencia.Sucesso.Should().BeTrue(audiencia.Mensagem);
        _audiencia = audiencia.Valor!.Id;

        var daProducao = await _app.ExecutarAsync(_pedro, new SalvarEvento(
            null, "Reunião de alinhamento", null, null, TipoEvento.Reuniao, new DateTime(2026, 10, 9, 17, 0, 0, DateTimeKind.Utc), 60,
            null, null, null, []));
        daProducao.Sucesso.Should().BeTrue(daProducao.Mensagem);
        _daProducao = daProducao.Valor!.Id;

        var reservado = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Reunião reservada da diretoria", null, null, TipoEvento.Reuniao, new DateTime(2026, 10, 8, 13, 0, 0, DateTimeKind.Utc), 60,
            null, null, null, []));
        reservado.Sucesso.Should().BeTrue(reservado.Mensagem);
        _reservado = reservado.Valor!.Id;
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

    private const string NomesDosParticipantes = "<span class=\"aj-participante__nome\">([^<]+)</span>";

    [Fact]
    public async Task Diretoria_ve_o_cabecalho_os_paineis_e_as_acoes_em_cada_visao()
    {
        var html = await PaginaAsync($"/agenda/{_audiencia}");

        // O voltar: link no desktop e no tablet, barra de voltar com "Evento" no mobile.
        html.Should().Contain("<h1>Evento</h1>").And.Contain("aria-label=\"Voltar para Agenda\"")
            .And.MatchRegex("<a href=\"/agenda\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Agenda</a>")
            .And.NotContain("Seu acesso foi encerrado");

        // Cabeçalho do desktop e do tablet: o título e a linha de contexto com o local.
        html.Should().MatchRegex(
            "(?s)<header class=\"aj-painel aj-painel--principal aj-painel--cabecalho\">\\s*" +
            "<div class=\"aj-cabecalho__texto aj-cabecalho__texto--folgado\">\\s*<h1 class=\"aj-titulo\">Audiência de instrução — Litoral</h1>\\s*" +
            "<p class=\"aj-subtitulo\">Audiência · 07/10/2026, 09:00 – 10:30 · Fórum Trabalhista, sala 4</p>");
        // Mobile: o resumo sem o local, acima do título em h2 (o h1 é o da barra).
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex(
            "(?s)<div class=\"aj-titulo-da-ficha\">\\s*<span class=\"aj-ajuda\">Audiência · 07/10/2026, 09:00 – 10:30</span>\\s*" +
            "<h2 class=\"aj-titulo\">Audiência de instrução — Litoral</h2>");

        // Desktop: Excluir (perigo) e Editar (secundário) no cabeçalho; o painel Detalhes com as nove linhas do quadro.
        BlocoDeVisao.De(html, "aj-so-desktop").Should()
            .MatchRegex(
                "(?s)<div class=\"aj-painel__botoes\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--perigo\"[^>]*>Excluir</button>\\s*" +
                $"<a href=\"/agenda/{_audiencia}/editar\" class=\"aj-botao aj-botao--secundario\">Editar</a>\\s*</div>")
            .And.MatchRegex(
                "(?s)<section class=\"aj-painel aj-lado-a-lado__painel\">\\s*<h2>Detalhes</h2>\\s*<dl class=\"aj-detalhes\">\\s*" +
                "<div><dt>Tipo</dt><dd>Audiência</dd></div>\\s*" +
                "<div><dt>Início</dt><dd>07/10/2026 09:00</dd></div>\\s*" +
                "<div><dt>Fim</dt><dd>07/10/2026 10:30</dd></div>\\s*" +
                "<div><dt>Duração</dt><dd>90 minutos</dd></div>\\s*" +
                "<div><dt>Local</dt><dd>Fórum Trabalhista, sala 4</dd></div>\\s*" +
                "<div><dt>Cliente</dt><dd>Transportes Litoral Ltda.</dd></div>\\s*" +
                "<div><dt>Processo</dt><dd>Litoral — reclamação trabalhista</dd></div>\\s*" +
                "<div><dt>Atividade</dt><dd>—</dd></div>\\s*" +
                "<div><dt>Criado por</dt><dd>Diana Diretora</dd></div>\\s*</dl>");

        // Tablet e mobile: o mesmo painel Detalhes e a barra fixa com Excluir (secundário vermelho) e Editar (primário).
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should()
            .MatchRegex(
                "(?s)<section class=\"aj-painel\">\\s*<h2>Detalhes</h2>\\s*<dl class=\"aj-detalhes\">\\s*<div><dt>Tipo</dt><dd>Audiência</dd></div>" +
                ".*?<div><dt>Criado por</dt><dd>Diana Diretora</dd></div>\\s*</dl>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--destrutivo\"[^>]*>Excluir</button>\\s*" +
                $"<a href=\"/agenda/{_audiencia}/editar\" class=\"aj-botao aj-botao--primario\">Editar</a>\\s*</div>");

        // A ordem: no desktop, a coluna (Descrição, Participantes) e depois o painel Detalhes; no tablet e no mobile o
        // arranjo some e fica Descrição, Detalhes, Participantes. A descrição guarda a quebra de linha digitada.
        html.Should().MatchRegex(
            "(?s)<div class=\"aj-lado-a-lado aj-lado-a-lado--ficha\">\\s*<div class=\"aj-lado-a-lado__lista aj-pilha\">\\s*" +
            "<section class=\"aj-painel\">\\s*<h2>Descrição</h2>\\s*<p class=\"aj-texto-livre\">Audiência de instrução.\nLevar a proposta de acordo em seis parcelas.</p>\\s*</section>\\s*" +
            "<div class=\"aj-tablet-mobile\">\\s*<section class=\"aj-painel\">\\s*<h2>Detalhes</h2>.*?</section>\\s*</div>\\s*" +
            "<section class=\"aj-painel\">\\s*<h2>Participantes</h2>.*?</section>\\s*</div>\\s*" +
            "<div class=\"aj-so-desktop\">\\s*<section class=\"aj-painel aj-lado-a-lado__painel\">\\s*<h2>Detalhes</h2>");
        Regex.Matches(html, "<h2>Descrição</h2>").Count.Should().Be(1);
        Regex.Matches(html, "<h2>Detalhes</h2>").Count.Should().Be(2);
        Regex.Matches(html, "<h2>Participantes</h2>").Count.Should().Be(1);

        // Participantes: o próprio usuário primeiro, com "(você)" e o avatar dourado (sem modificador de cor); os outros
        // na ordem do servidor, com a cor da regra dos avatares.
        Grupos(html, NomesDosParticipantes).Should().Equal("Diana Diretora (você)", "Beatriz Lins", "Pedro Produção");
        html.Should()
            .MatchRegex("<ul class=\"aj-participantes\">\\s*<li class=\"aj-participante\"><span class=\"aj-avatar\" aria-hidden=\"true\">DD</span>" +
                "<span class=\"aj-participante__nome\">Diana Diretora \\(você\\)</span></li>")
            .And.Contain($"<li class=\"aj-participante\"><span class=\"aj-avatar {AvatarDePessoa.Classe("Beatriz Lins")}\" aria-hidden=\"true\">BL</span>" +
                "<span class=\"aj-participante__nome\">Beatriz Lins</span></li>")
            .And.Contain($"<span class=\"aj-avatar {AvatarDePessoa.Classe("Pedro Produção")}\" aria-hidden=\"true\">PP</span>");
    }

    [Fact]
    public async Task Producao_que_participa_e_nao_criou_ve_a_ficha_sem_acoes_e_sem_barra()
    {
        var html = await PaginaAsync($"/agenda/{_audiencia}", comoPedro: true);

        html.Should().Contain("<h1 class=\"aj-titulo\">Audiência de instrução — Litoral</h1>").And.Contain("<h2>Detalhes</h2>");
        // O "(você)" e o dourado acompanham quem vê: aqui o Pedro vem primeiro, e a Diana ganha a cor da regra.
        Grupos(html, NomesDosParticipantes).Should().Equal("Pedro Produção (você)", "Beatriz Lins", "Diana Diretora");
        html.Should().Contain("<span class=\"aj-avatar\" aria-hidden=\"true\">PP</span>")
            .And.Contain($"<span class=\"aj-avatar {AvatarDePessoa.Classe("Diana Diretora")}\" aria-hidden=\"true\">DD</span>");
        html.Should().NotContain("aj-painel__botoes").And.NotContain("aj-acoes--fixa").And.NotContain(">Excluir</button>")
            .And.NotContain("/editar").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Producao_que_criou_ve_excluir_e_editar_no_desktop_e_na_barra()
    {
        var html = await PaginaAsync($"/agenda/{_daProducao}", comoPedro: true);

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<div class=\"aj-painel__botoes\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--perigo\"[^>]*>Excluir</button>\\s*" +
            $"<a href=\"/agenda/{_daProducao}/editar\" class=\"aj-botao aj-botao--secundario\">Editar</a>");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--destrutivo\"[^>]*>Excluir</button>\\s*" +
            $"<a href=\"/agenda/{_daProducao}/editar\" class=\"aj-botao aj-botao--primario\">Editar</a>");
        Grupos(html, NomesDosParticipantes).Should().Equal("Pedro Produção (você)");
    }

    [Fact]
    public async Task Evento_sem_descricao_sem_local_e_sem_vinculos_some_com_a_descricao_e_mostra_os_vazios()
    {
        var html = await PaginaAsync($"/agenda/{_daProducao}");

        // Sem local, a linha de contexto e o resumo do mobile acabam no horário.
        html.Should().Contain("<p class=\"aj-subtitulo\">Reunião · 09/10/2026, 14:00 – 15:00</p>")
            .And.Contain("<span class=\"aj-ajuda\">Reunião · 09/10/2026, 14:00 – 15:00</span>");
        BlocoDeVisao.De(html, "aj-so-desktop").Should()
            .Contain("<div><dt>Duração</dt><dd>60 minutos</dd></div>")
            .And.Contain("<div><dt>Local</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Cliente</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Processo</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Atividade</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Criado por</dt><dd>Pedro Produção</dd></div>");
        // Sem descrição, o painel some: a coluna começa pelos Detalhes do tablet e do mobile.
        html.Should().MatchRegex(
                "<div class=\"aj-lado-a-lado__lista aj-pilha\">\\s*<div class=\"aj-tablet-mobile\">\\s*<section class=\"aj-painel\">\\s*<h2>Detalhes</h2>")
            .And.NotContain("<h2>Descrição</h2>").And.NotContain("aj-texto-livre");
        // A Diretoria não participa: ninguém leva "(você)", e o avatar do Pedro tem a cor da regra.
        html.Should().Contain($"<span class=\"aj-avatar {AvatarDePessoa.Classe("Pedro Produção")}\" aria-hidden=\"true\">PP</span>" +
                "<span class=\"aj-participante__nome\">Pedro Produção</span>")
            .And.NotContain("(você)");
    }

    [Fact]
    public async Task Evento_que_nao_existe_e_o_que_a_producao_nao_ve_mostram_o_mesmo_nao_encontrado()
    {
        var antes = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Count;

        var inexistente = await PaginaAsync($"/agenda/{Guid.NewGuid()}");
        var escondido = await PaginaAsync($"/agenda/{_reservado}", comoPedro: true);

        foreach (var html in new[] { inexistente, escondido })
        {
            html.Should().Contain("Evento não encontrado.").And.Contain("<h1>Evento</h1>")
                .And.Contain("aria-label=\"Voltar para Agenda\"").And.MatchRegex("<a href=\"/agenda\" class=\"aj-voltar\">");
            html.Should().NotContain("<h2>Detalhes</h2>").And.NotContain("<h2>Participantes</h2>").And.NotContain("aj-acoes--fixa")
                .And.NotContain("Reunião reservada da diretoria");
        }

        // O servidor recusou a leitura do Pedro e registrou a tentativa (o id inexistente não registra nada).
        var negados = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Skip(antes)
            .Select(e => (string?)JsonNode.Parse(e.Dados!)!["requisicao"]).ToList();
        negados.Should().NotBeEmpty().And.OnlyContain(r => r == nameof(ObterEvento));
    }

    [Fact]
    public async Task Vinda_da_agenda_leva_o_periodo_ao_voltar_e_ao_editar_e_descarta_o_que_a_agenda_nao_aceita()
    {
        var cliente = await _app.ClienteAutenticadoAsync();
        async Task<string> Pagina(string rota) => WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));

        var doDia = await Pagina($"/agenda/{_audiencia}?visao=dia&data=2026-10-07");
        var estranho = await Pagina($"/agenda/{_audiencia}?visao=xyz&data=2026-13-40");
        var naoEncontrado = await Pagina($"/agenda/{Guid.NewGuid()}?visao=mes&data=2026-10-07");

        doDia.Should()
            .MatchRegex("<a href=\"/agenda\\?visao=dia&data=2026-10-07\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Agenda</a>")
            .And.Contain("<a href=\"/agenda?visao=dia&data=2026-10-07\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para Agenda\">")
            .And.Contain($"<a href=\"/agenda/{_audiencia}/editar?visao=dia&data=2026-10-07\" class=\"aj-botao aj-botao--secundario\">Editar</a>")
            .And.Contain($"<a href=\"/agenda/{_audiencia}/editar?visao=dia&data=2026-10-07\" class=\"aj-botao aj-botao--primario\">Editar</a>");

        // Visão desconhecida e data inválida não seguem adiante: o voltar e o editar vão sem consulta.
        estranho.Should().MatchRegex("<a href=\"/agenda\" class=\"aj-voltar\">")
            .And.Contain($"<a href=\"/agenda/{_audiencia}/editar\" class=\"aj-botao aj-botao--primario\">Editar</a>");
        estranho.Should().NotContain("href=\"/agenda?visao=xyz").And.NotContain("editar?");

        // O não encontrado também volta ao período de onde se veio.
        naoEncontrado.Should().Contain("Evento não encontrado.")
            .And.MatchRegex("<a href=\"/agenda\\?visao=mes&data=2026-10-07\" class=\"aj-voltar\">");
    }
}
