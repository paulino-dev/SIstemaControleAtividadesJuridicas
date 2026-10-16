using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Components.Agenda;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AgendaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");
    private Guid _admin;
    private Guid _pedro;
    private Guid _doPedro;
    private Guid _restrito;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    /// <summary>Hoje, no fuso do escritório: a agenda sem endereço abre nele.</summary>
    private DateOnly Hoje() => DateOnly.FromDateTime(_fuso.ParaLocal(DateTime.UtcNow));

    /// <summary>Hoje, no fuso do escritório, às <paramref name="hora"/>: sempre dentro da semana corrente.</summary>
    private DateTime HojeAs(int hora, int minuto = 0) => _fuso.ParaUtc(Hoje().ToDateTime(new TimeOnly(hora, minuto)));

    private async Task CriarEventosAsync()
    {
        // Participa o Pedro; o diretor é o criador e também participa.
        var doPedro = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Audiência de instrução do Pedro", null, "Fórum Central", TipoEvento.Audiencia, HojeAs(10), 60,
            null, null, null, [_pedro]));
        doPedro.Sucesso.Should().BeTrue(doPedro.Mensagem);
        _doPedro = doPedro.Valor!.Id;

        // Só a Diretoria participa: o Pedro não pode ver.
        var restrito = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Reunião reservada da diretoria", null, null, TipoEvento.Reuniao, HojeAs(14, 30), 90,
            null, null, null, []));
        restrito.Sucesso.Should().BeTrue(restrito.Mensagem);
        _restrito = restrito.Valor!.Id;
    }

    // A barra superior que a agenda declara (a do layout é "aj-barra-superior aj-barra-superior--padrao").
    private static string BarraDoMobile(string html)
    {
        var barra = Regex.Match(html, "<header class=\"aj-barra-superior\">.*?</header>", RegexOptions.Singleline);
        barra.Success.Should().BeTrue("a agenda declara a própria barra superior do mobile");
        return barra.Value;
    }

    // O cartão da Semana do desktop (forma Coluna) que leva à ficha do evento, com o período de origem.
    private static string CartaoDaSemana(string desktop, Guid id, string consulta = "")
    {
        var cartao = Regex.Match(desktop,
            $"<a class=\"(?=[^\"]*aj-evento--coluna)[^\"]*\" href=\"/agenda/{id}{Regex.Escape(consulta)}\">.*?</a>",
            RegexOptions.Singleline);
        cartao.Success.Should().BeTrue($"a Semana do desktop traz o cartão do evento {id} como link para a ficha");
        return cartao.Value;
    }

    // As consultas da agenda que o usuário fez (o registro de consultas guarda o período pedido no detalhe).
    private Task<List<RegistroAcesso>> ConsultasDaAgendaAsync(Guid usuario) =>
        _app.EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(a => a.UsuarioId == usuario && a.TipoRecurso == "Evento" && a.Acao == AcaoDeAcesso.Listou)
            .OrderBy(a => a.DataHora)
            .ToListAsync());

    private static (DateTime De, DateTime Ate) PeriodoConsultado(RegistroAcesso registro)
    {
        using var detalhe = JsonDocument.Parse(registro.Detalhe!);
        return (detalhe.RootElement.GetProperty("DeUtc").GetDateTime(), detalhe.RootElement.GetProperty("AteUtc").GetDateTime());
    }

    [Fact]
    public async Task Diretor_ve_os_eventos_da_semana()
    {
        await CriarEventosAsync();
        var cliente = await _app.ClienteAutenticadoAsync();
        var hoje = Hoje();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/agenda"));

        html.Should().Contain("Audiência de instrução do Pedro").And.Contain("Reunião reservada da diretoria")
            .And.NotContain("Seu acesso foi encerrado");
        // Cabeçalho: o título, o período da Semana (padrão do desktop e do tablet) e a ação de cadastro nos dois lugares.
        html.Should().Contain("<h1 class=\"aj-titulo\">Agenda</h1>")
            .And.Contain($"<p class=\"aj-subtitulo\">{RotulosDeAgenda.SubtituloDaSemana(RotulosDeAgenda.SegundaDe(hoje))}</p>")
            .And.MatchRegex("<a href=\"/agenda/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\">\\s*<svg[^>]*>.*?</svg>\\s*Novo evento</a>")
            .And.Contain("<a href=\"/agenda/novo\" class=\"aj-fab\" aria-label=\"Novo evento\">");

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex(
            "(?s)<div class=\"aj-barra-filtros aj-barra-filtros--dividida\">\\s*<div class=\"aj-barra-filtros__grupo\">\\s*" +
            "<div class=\"aj-segmentado aj-segmentado--periodo\">\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__seta\" aria-label=\"Período anterior\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__hoje\"[^>]*>Hoje</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__seta\" aria-label=\"Próximo período\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*</button>\\s*</div>\\s*" +
            "<div class=\"aj-segmentado aj-segmentado--periodo\" role=\"group\" aria-label=\"Visualização da agenda\">\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Dia</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"true\"[^>]*>Semana</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Mês</button>\\s*</div>\\s*</div>\\s*" +
            "<form class=\"aj-barra-filtros__grupo\" role=\"search\"[^>]*>\\s*" +
            "<label class=\"aj-campo-filtro aj-campo-filtro--tipo\">\\s*<span>Tipo</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>\\s*" +
            "<option value(?:=\"\")?>Todos</option>\\s*<option value=\"Audiencia\"[^>]*>Audiência</option>\\s*<option value=\"Reuniao\"[^>]*>Reunião</option>\\s*" +
            "<option value=\"Prazo\"[^>]*>Prazo</option>\\s*<option value=\"Outro\"[^>]*>Outro</option>\\s*</select>\\s*</label>\\s*" +
            "<label class=\"aj-campo-filtro aj-campo-filtro--pessoa\">\\s*<span>Participante</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>.*?</select>\\s*</label>\\s*" +
            "<button type=\"submit\" class=\"aj-botao aj-botao--secundario aj-botao--com-icone-pequeno\">\\s*<svg[^>]*>.*?</svg>\\s*Filtrar</button>\\s*</form>\\s*</div>");
        // Cada evento no seu cartão da Semana, que leva à ficha: tipo, horário no fuso do escritório, título, local e
        // participantes, nessa ordem e dentro do mesmo cartão.
        CartaoDaSemana(desktop, _doPedro).Should().Contain("aj-evento--audiencia").And.MatchRegex(
            "(?s)>Audiência<.*>10:00 – 11:00<.*>Audiência de instrução do Pedro<.*>Fórum Central<.*>Diana Diretora, Pedro Produção<");
        CartaoDaSemana(desktop, _restrito).Should().Contain("aj-evento--reuniao").And.MatchRegex(
            "(?s)>Reunião<.*>14:30 – 16:00<.*>Reunião reservada da diretoria<.*>Diana Diretora<");
        // A semana tem sete dias; dia sem eventos é dito; hoje é marcado no cabeçalho do dia.
        Regex.Matches(desktop, "<section class=\"aj-semana__dia[\" ]").Count.Should().Be(7);
        desktop.Should().Contain(">Sem eventos<").And.Contain($"aria-label=\"{RotulosDeAgenda.RotuloLongo(hoje, true)}\"");

        var tablet = BlocoDeVisao.De(html, "aj-so-tablet");
        tablet.Should().MatchRegex(
            "(?s)<div class=\"aj-barra-filtros aj-barra-filtros--plana\">\\s*<div class=\"aj-segmentado aj-segmentado--periodo\">\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__seta\" aria-label=\"Semana anterior\"[^>]*>.*?</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__hoje\"[^>]*>Hoje</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__seta\" aria-label=\"Próxima semana\"[^>]*>.*?</button>\\s*</div>\\s*" +
            "<div class=\"aj-segmentado aj-segmentado--periodo\" role=\"group\" aria-label=\"Visualização\">\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Dia</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao aj-segmentado__opcao--larga\" aria-pressed=\"true\"[^>]*>Semana</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Mês</button>\\s*</div>\\s*" +
            "<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--com-icone aj-botao--ao-fim\" aria-expanded=\"false\" aria-controls=\"filtros-da-agenda\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Filtros</button>\\s*</div>");
        Regex.Matches(BlocoDeVisao.De(html, "aj-tablet-mobile"), "<section class=\"aj-secao-dia[\" ]").Count.Should().Be(7);

        // Mobile: sem visão no endereço, o Dia de hoje, com os cartões que levam à ficha.
        var mobile = BlocoDeVisao.De(html, "aj-so-mobile");
        mobile.Should().Contain("aj-dia-escolhido").And.Contain($"href=\"/agenda/{_doPedro}\"").And.Contain($"href=\"/agenda/{_restrito}\"");

        var barra = BarraDoMobile(html);
        barra.Should().Contain("<h1 class=\"aj-titulo\">Agenda</h1>")
            .And.MatchRegex("(?s)<h1 class=\"aj-titulo\">Agenda</h1>.*?aria-label=\"Filtrar agenda\".*?<a href=\"/minha-senha\" class=\"aj-avatar\"")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone aj-barra-superior__acao\" aria-label=\"Filtrar agenda\" aria-expanded=\"false\" aria-controls=\"filtros-da-agenda\"[^>]*>\\s*<svg")
            .And.MatchRegex(
                "(?s)<div class=\"aj-segmentado aj-segmentado--recuado aj-segmentado--claro\" role=\"group\" aria-label=\"Visualização\">\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"true\"[^>]*>Dia</button>\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Semana</button>\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Mês</button>\\s*</div>")
            .And.MatchRegex(
                $"(?s)<div class=\"aj-barra-superior__periodo\">\\s*<strong>{RotulosDeAgenda.MesDoMobile(hoje)}</strong>\\s*<div class=\"aj-barra-superior__setas\">\\s*" +
                "<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Semana anterior\"[^>]*>.*?</button>\\s*" +
                "<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Próxima semana\"[^>]*>.*?</button>\\s*</div>\\s*</div>")
            .And.Contain("aj-faixa-dias");
        // O mobile não tem subtítulo nem "Hoje" (os quadros).
        barra.Should().NotContain("aj-subtitulo").And.NotContain(">Hoje<");
    }

    [Fact]
    public async Task Producao_ve_so_os_seus()
    {
        await CriarEventosAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/agenda"));

        html.Should().Contain("Audiência de instrução do Pedro")
            .And.Contain("<a href=\"/agenda/novo\" class=\"aj-fab\" aria-label=\"Novo evento\">");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        CartaoDaSemana(desktop, _doPedro).Should().MatchRegex(">10:00 – 11:00<");
        desktop.Should().Contain("<span>Tipo</span>");
        html.Should().NotContain("Reunião reservada da diretoria").And.NotContain("14:30").And.NotContain(_restrito.ToString())
            .And.NotContain("Seu acesso foi encerrado");
        // O filtro por participante é só da Diretoria (a positiva do mesmo bloco é o Tipo, acima).
        desktop.Should().NotContain("<span>Participante</span>");
    }

    [Fact]
    public async Task Menu_mostra_agenda_para_todos()
    {
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/agenda\"");
        (await doPedro.GetStringAsync("/")).Should().Contain("href=\"/agenda\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/agenda");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }

    [Fact]
    public async Task Listagem_fica_no_registro_de_consultas()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await cliente.GetAsync("/agenda")).EnsureSuccessStatusCode();

        var acessos = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().ToListAsync());
        acessos.Should().Contain(a =>
            a.TipoRecurso == "Evento" && a.Acao == AcaoDeAcesso.Listou && a.UsuarioId == _pedro);
    }

    // Review Focus 1: endereço sem período ou com período estranho abre hoje, no padrão de cada largura, sem erro e
    // com uma consulta só (a semana de hoje, que serve à Semana do desktop e do tablet e ao Dia do mobile).
    [Theory]
    [InlineData("/agenda")]
    [InlineData("/agenda?visao=xyz")]
    [InlineData("/agenda?data=2026-13-40")]
    [InlineData("/agenda?data=1999-01-01")]
    [InlineData("/agenda?visao=xyz&data=ontem")]
    public async Task Endereco_sem_periodo_ou_estranho_abre_hoje_no_padrao_com_uma_consulta_so(string rota)
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);
        var segunda = RotulosDeAgenda.SegundaDe(Hoje());

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));

        html.Should().Contain($"<p class=\"aj-subtitulo\">{RotulosDeAgenda.SubtituloDaSemana(segunda)}</p>")
            .And.NotContain("Seu acesso foi encerrado").And.NotContain("Não foi possível consultar a agenda.");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex("aria-pressed=\"true\"[^>]*>Semana</button>")
            .And.NotMatchRegex("aria-pressed=\"true\"[^>]*>(Dia|Mês)</button>");
        BarraDoMobile(html).Should().MatchRegex("aria-pressed=\"true\"[^>]*>Dia</button>")
            .And.NotMatchRegex("aria-pressed=\"true\"[^>]*>(Semana|Mês)</button>");
        var consulta = (await ConsultasDaAgendaAsync(_pedro)).Should().ContainSingle().Which;
        PeriodoConsultado(consulta).Should().Be((_fuso.InicioDoDiaEmUtc(segunda), _fuso.InicioDoDiaEmUtc(segunda.AddDays(7))));
    }

    // O Mês consulta as semanas inteiras que cobrem o mês (outubro de 2026: de 28/09 a 01/11), numa consulta só.
    [Fact]
    public async Task Mes_consulta_as_semanas_inteiras_do_mes()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/agenda?visao=mes&data=2026-10-15"));

        html.Should().Contain("<p class=\"aj-subtitulo\">Outubro de 2026</p>").And.Contain("aj-mes__grade")
            .And.Contain("<a href=\"/agenda/novo?visao=mes&data=2026-10-15\" class=\"aj-fab\" aria-label=\"Novo evento\">");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex("aria-pressed=\"true\"[^>]*>Mês</button>");
        BarraDoMobile(html).Should().Contain("<strong>Outubro 2026</strong>")
            .And.Contain("aria-label=\"Mês anterior\"").And.Contain("aria-label=\"Próximo mês\"")
            .And.NotContain("aj-faixa-dias");
        // A grade compacta fica no conteúdo do mobile, antes da lista do dia, com um botão por dia das semanas do mês.
        var grade = Regex.Match(BlocoDeVisao.De(html, "aj-so-mobile"),
            "(?s)<div class=\"aj-faixa-dias aj-faixa-dias--mes\">(.*?)<div class=\"aj-dia-escolhido\">");
        grade.Success.Should().BeTrue("a grade compacta do Mês vem antes da lista do dia, no bloco do mobile");
        Regex.Matches(grade.Groups[1].Value, "<button type=\"button\" class=\"aj-faixa-dias__dia[\" ]").Count.Should().Be(35);
        var consulta = (await ConsultasDaAgendaAsync(_pedro)).Should().ContainSingle().Which;
        PeriodoConsultado(consulta).Should().Be(
            (_fuso.InicioDoDiaEmUtc(new DateOnly(2026, 9, 28)), _fuso.InicioDoDiaEmUtc(new DateOnly(2026, 11, 2))));
    }

    // O período de onde se veio segue para a ficha (cartão) e para o novo evento.
    [Fact]
    public async Task Cartao_e_novo_evento_levam_o_periodo_de_origem()
    {
        await CriarEventosAsync();
        var cliente = await _app.ClienteAutenticadoAsync();
        var consulta = $"?visao=semana&data={Hoje().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync($"/agenda{consulta}"));

        html.Should().Contain($"<a href=\"/agenda/novo{consulta}\" class=\"aj-fab\" aria-label=\"Novo evento\">");
        CartaoDaSemana(BlocoDeVisao.De(html, "aj-so-desktop"), _doPedro, consulta).Should().Contain("Audiência de instrução do Pedro");
        // Nenhum cartão perde a origem.
        html.Should().NotContain($"href=\"/agenda/{_doPedro}\"");
    }

    [Fact]
    public async Task Diretor_encontra_no_filtro_o_usuario_inativo_marcado_e_em_ordem_de_nome_e_producao_nao_tem_o_filtro()
    {
        var producao = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
                .FirstAsync(p => p.Tipo == TipoPerfil.Producao && p.Ativo)).Id);
        var iara = await _app.ExecutarAsync(_admin, new CriarUsuario("Iara Inativa", "iara@x.com", "senha-da-iara", producao, null));
        iara.Sucesso.Should().BeTrue(iara.Mensagem);
        (await _app.ExecutarAsync(_admin, new DefinirAtivoUsuario(iara.Valor, false))).Sucesso.Should().BeTrue();

        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var htmlDoDiretor = WebUtility.HtmlDecode(await doDiretor.GetStringAsync("/agenda"));
        var htmlDoPedro = WebUtility.HtmlDecode(await doPedro.GetStringAsync("/agenda"));

        var select = Regex.Match(BlocoDeVisao.De(htmlDoDiretor, "aj-so-desktop"),
            "(?s)<span>Participante</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>(.*?)</select>");
        select.Success.Should().BeTrue();
        select.Groups[1].Value.Should().MatchRegex(@">\s*Iara Inativa \(inativo\)\s*<");
        var nomes = Regex.Matches(select.Groups[1].Value, @"<option[^>]*>\s*([^<]*?)\s*</option>")
            .Select(m => m.Groups[1].Value).ToList();
        nomes.Should().Equal("Todos", "Diana Diretora", "Iara Inativa (inativo)", "Pedro Produção");

        var desktopDoPedro = BlocoDeVisao.De(htmlDoPedro, "aj-so-desktop");
        desktopDoPedro.Should().Contain("<span>Tipo</span>");
        desktopDoPedro.Should().NotContain("<span>Participante</span>");
        htmlDoPedro.Should().NotContain("Iara");
    }
}

/// <summary>Em que dia(s) da semana, no fuso do escritório, um evento aparece.</summary>
public class AgendaPorDiaTests
{
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    private EventoResumo Evento(DateTime inicioLocal, int minutos)
    {
        var inicio = _fuso.ParaUtc(inicioLocal);
        return new EventoResumo(Guid.NewGuid(), "Evento", TipoEvento.Outro, inicio, inicio.AddMinutes(minutos), null, null, null, []);
    }

    [Fact]
    public void Evento_que_atravessa_a_meia_noite_aparece_nos_dois_dias()
    {
        var evento = Evento(new DateTime(2026, 10, 7, 23, 30, 0), 60);   // 23:30 a 00:30

        RotulosDeAgenda.TocaODia(_fuso, evento, new DateOnly(2026, 10, 6)).Should().BeFalse();
        RotulosDeAgenda.TocaODia(_fuso, evento, new DateOnly(2026, 10, 7)).Should().BeTrue();
        RotulosDeAgenda.TocaODia(_fuso, evento, new DateOnly(2026, 10, 8)).Should().BeTrue();
        RotulosDeAgenda.TocaODia(_fuso, evento, new DateOnly(2026, 10, 9)).Should().BeFalse();
    }

    [Fact]
    public void Evento_que_termina_exatamente_a_meia_noite_nao_aparece_no_dia_seguinte()
    {
        var evento = Evento(new DateTime(2026, 10, 7, 22, 0, 0), 120);   // 22:00 a 00:00

        RotulosDeAgenda.TocaODia(_fuso, evento, new DateOnly(2026, 10, 7)).Should().BeTrue();
        RotulosDeAgenda.TocaODia(_fuso, evento, new DateOnly(2026, 10, 8)).Should().BeFalse();
    }

    [Fact]
    public void Agrupa_pela_data_local_e_nao_pela_data_em_utc()
    {
        // 22:00 em São Paulo (UTC-3) já é o dia seguinte em UTC.
        var evento = Evento(new DateTime(2026, 10, 7, 22, 0, 0), 30);

        evento.InicioEm.Date.Should().Be(new DateTime(2026, 10, 8));
        RotulosDeAgenda.TocaODia(_fuso, evento, new DateOnly(2026, 10, 7)).Should().BeTrue();
        RotulosDeAgenda.TocaODia(_fuso, evento, new DateOnly(2026, 10, 8)).Should().BeFalse();
    }

    [Fact]
    public void Intervalo_mostra_a_data_do_lado_que_esta_em_outro_dia()
    {
        var evento = Evento(new DateTime(2026, 10, 7, 23, 30, 0), 60);

        RotulosDeAgenda.Intervalo(_fuso, evento, new DateOnly(2026, 10, 7)).Should().Be("23:30 – 08/10 00:30");
        RotulosDeAgenda.Intervalo(_fuso, evento, new DateOnly(2026, 10, 8)).Should().Be("07/10 23:30 – 00:30");
        RotulosDeAgenda.Intervalo(_fuso, Evento(new DateTime(2026, 10, 7, 10, 0, 0), 60), new DateOnly(2026, 10, 7))
            .Should().Be("10:00 – 11:00");
    }

    [Fact]
    public async Task Aviso_de_sobreposicao_diz_que_o_evento_foi_salvo_e_lista_cada_pessoa_no_horario_do_escritorio()
    {
        var sobreposicoes = new[]
        {
            new SobreposicaoDeAgenda(Guid.NewGuid(), "Pedro", _fuso.ParaUtc(new DateTime(2026, 10, 7, 10, 0, 0)),
                _fuso.ParaUtc(new DateTime(2026, 10, 7, 11, 30, 0))),
        };

        // O aviso que a agenda mostra depois de salvar: com sobreposição, a frase do desktop diz que o evento foi salvo e
        // lista a pessoa no horário do escritório; sem sobreposição, só o recado.
        var comConflito = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<AvisoDeEventoSalvo>(
            new DespachanteCombinado(), new() { ["Recado"] = new Recado("Evento salvo.", TipoDeAviso.Sucesso, sobreposicoes) }));
        var semConflito = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<AvisoDeEventoSalvo>(
            new DespachanteCombinado(), new() { ["Recado"] = new Recado("Evento salvo.", TipoDeAviso.Sucesso, []) }));

        RotulosDeAgenda.ConflitoNoDesktop(_fuso, sobreposicoes).Should().Be("Atenção: Pedro já tem evento das 10:00 às 11:30 (07/10).");
        BlocoDeVisao.De(comConflito, "aj-so-desktop").Should()
            .Contain("<strong>Evento salvo.</strong> Atenção: Pedro já tem evento das 10:00 às 11:30 (07/10).");
        semConflito.Should().Contain("<span>Evento salvo.</span>").And.NotContain("Atenção");
    }

    // ---- Onda final ----

    [Theory]
    [InlineData(2026, 10, 11, 2026, 10, 5)]    // domingo volta para a segunda anterior
    [InlineData(2026, 10, 5, 2026, 10, 5)]     // segunda é ela mesma
    [InlineData(2026, 10, 8, 2026, 10, 5)]     // quinta
    [InlineData(2026, 10, 12, 2026, 10, 12)]   // a segunda seguinte abre outra semana
    public void Segunda_da_semana_a_partir_de_qualquer_dia(int ano, int mes, int dia, int anoEsperado, int mesEsperado, int diaEsperado)
    {
        RotulosDeAgenda.SegundaDe(new DateOnly(ano, mes, dia)).Should().Be(new DateOnly(anoEsperado, mesEsperado, diaEsperado));
    }

    [Fact]
    public void Evento_que_comecou_antes_da_semana_aparece_nos_primeiros_dias_dela()
    {
        var evento = Evento(new DateTime(2026, 10, 3, 20, 0, 0), 3 * 24 * 60);   // sábado 20:00 a terça 20:00

        RotulosDeAgenda.TocaODia(_fuso, evento, new DateOnly(2026, 10, 5)).Should().BeTrue();
        RotulosDeAgenda.TocaODia(_fuso, evento, new DateOnly(2026, 10, 6)).Should().BeTrue();
        RotulosDeAgenda.TocaODia(_fuso, evento, new DateOnly(2026, 10, 7)).Should().BeFalse();
        RotulosDeAgenda.Intervalo(_fuso, evento, new DateOnly(2026, 10, 5)).Should().Be("03/10 20:00 – 06/10 20:00");
    }

    [Fact]
    public void Evento_de_tres_dias_aparece_nos_tres()
    {
        var evento = Evento(new DateTime(2026, 10, 7, 22, 0, 0), 28 * 60);   // quarta 22:00 a sexta 02:00

        var dias = Enumerable.Range(5, 7).Select(d => new DateOnly(2026, 10, d))
            .Where(d => RotulosDeAgenda.TocaODia(_fuso, evento, d)).ToList();

        dias.Should().Equal(new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 9));
    }

    [Fact]
    public void Dia_com_mudanca_de_horario_de_verao_em_Nova_York_agrupa_pela_data_local()
    {
        var novaYork = new FusoDoEscritorio("America/New_York");
        EventoResumo Em(DateTime local, int minutos)
        {
            var inicio = novaYork.ParaUtc(local);
            return new EventoResumo(Guid.NewGuid(), "E", TipoEvento.Outro, inicio, inicio.AddMinutes(minutos), null, null, null, []);
        }

        // 08/03/2026: o relógio salta de 02:00 para 03:00. Sábado 22:00 a domingo 03:00 (local) dura 4 horas.
        var primavera = Em(new DateTime(2026, 3, 7, 22, 0, 0), 4 * 60);
        RotulosDeAgenda.TocaODia(novaYork, primavera, new DateOnly(2026, 3, 7)).Should().BeTrue();
        RotulosDeAgenda.TocaODia(novaYork, primavera, new DateOnly(2026, 3, 8)).Should().BeTrue();
        RotulosDeAgenda.TocaODia(novaYork, primavera, new DateOnly(2026, 3, 9)).Should().BeFalse();
        RotulosDeAgenda.Intervalo(novaYork, primavera, new DateOnly(2026, 3, 8)).Should().Be("07/03 22:00 – 03:00");

        // 01/11/2026: o relógio volta e o dia tem 25 horas. Um evento de 25 horas desde a meia-noite termina à meia-noite
        // seguinte e não aparece no dia 2.
        var outono = Em(new DateTime(2026, 11, 1, 0, 0, 0), 25 * 60);
        RotulosDeAgenda.TocaODia(novaYork, outono, new DateOnly(2026, 11, 1)).Should().BeTrue();
        RotulosDeAgenda.TocaODia(novaYork, outono, new DateOnly(2026, 11, 2)).Should().BeFalse();
    }

    [Fact]
    public void Aviso_de_sobreposicao_que_atravessa_a_meia_noite_mostra_a_data_do_fim()
    {
        var atravessa = new SobreposicaoDeAgenda(Guid.NewGuid(), "Pedro", _fuso.ParaUtc(new DateTime(2026, 3, 10, 22, 0, 0)),
            _fuso.ParaUtc(new DateTime(2026, 3, 11, 1, 0, 0)));
        var ateMeiaNoite = new SobreposicaoDeAgenda(Guid.NewGuid(), "Paula", _fuso.ParaUtc(new DateTime(2026, 3, 10, 22, 0, 0)),
            _fuso.ParaUtc(new DateTime(2026, 3, 11, 0, 0, 0)));

        // A frase de cada pessoa como o tablet a mostra (a do desktop é a mesma depois de "Atenção:").
        RotulosDeAgenda.ConflitoNoTablet(_fuso, [atravessa]).Should().Be("Pedro já tem evento de 10/03 22:00 a 11/03 01:00.");
        RotulosDeAgenda.ConflitoNoTablet(_fuso, [ateMeiaNoite]).Should().Be("Paula já tem evento das 22:00 às 00:00 (10/03).");
    }

    [Fact]
    public void Formatos_de_hora_e_data_nao_dependem_da_cultura_da_thread()
    {
        var evento = Evento(new DateTime(2026, 10, 7, 23, 30, 0), 60);
        var antes = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");   // outros algarismos e outro calendário

            RotulosDeAgenda.Intervalo(_fuso, evento, new DateOnly(2026, 10, 7)).Should().Be("23:30 – 08/10 00:30");
            RotulosDeAgenda.ConflitoNoTablet(_fuso, [new SobreposicaoDeAgenda(Guid.NewGuid(), "Pedro", evento.InicioEm,
                evento.InicioEm.AddMinutes(30))]).Should().Be("Pedro já tem evento das 23:30 às 00:00 (07/10).");
        }
        finally
        {
            CultureInfo.CurrentCulture = antes;
        }
    }

    [Fact]
    public void Atividade_vinculada_tem_um_rotulo_para_cada_situacao()
    {
        RotulosDeAgenda.Atividade("Contestação", excluida: false).Should().Be("Contestação");
        RotulosDeAgenda.Atividade(null, excluida: false).Should().Be("Atividade vinculada (sem acesso)");
        RotulosDeAgenda.Atividade(null, excluida: true).Should().Be("Atividade vinculada (excluída)");
    }
}
