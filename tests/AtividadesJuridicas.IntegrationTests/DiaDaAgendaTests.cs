using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Components.Agenda;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O Dia da agenda com os eventos do quadro: a régua do desktop (cartões posicionados por quartos de hora, eventos
/// simultâneos lado a lado, régua estendida, evento recortado à meia-noite), a seção do tablet e a lista do mobile.
/// </summary>
public class DiaDaAgendaTests : TelaInterativa
{
    private IRenderedComponent<DiaDaAgenda> Dia(DateOnly data, IReadOnlyList<EventoResumo> eventos) =>
        Desenhar<DiaDaAgenda>(p => p
            .Add(c => c.Periodo, new PeriodoDaAgenda(VisaoDaAgenda.Dia, data, true))
            .Add(c => c.Eventos, eventos)
            .Add(c => c.Hoje, QuadroDaAgenda.Hoje));

    private static IElement Desktop(IRenderedComponent<DiaDaAgenda> dia) => dia.Find("div.aj-so-desktop > section");
    private static IElement Tablet(IRenderedComponent<DiaDaAgenda> dia) => dia.Find("div.aj-so-tablet > section");
    private static IElement Mobile(IRenderedComponent<DiaDaAgenda> dia) => dia.Find("div.aj-so-mobile");

    private static string[] Horas(IElement desktop) =>
        desktop.QuerySelectorAll("div.aj-regua > div.aj-regua__horas > span.aj-regua__hora").Select(h => h.TextContent).ToArray();

    private static IElement Grade(IElement desktop) => desktop.QuerySelector("div.aj-regua > div.aj-regua__grade")!;

    private static IElement Cartao(IElement bloco, string titulo) =>
        bloco.QuerySelectorAll("a.aj-evento").Single(a => a.QuerySelector(".aj-evento__titulo")!.TextContent == titulo);

    private static string[] Titulos(IElement bloco) =>
        bloco.QuerySelectorAll("a.aj-evento .aj-evento__titulo").Select(t => t.TextContent).ToArray();

    private static string[] Linhas(IElement elemento) =>
        elemento.Children.Select(linha => $"{linha.ClassName}: {linha.TextContent}").ToArray();

    private static readonly string[] HorasPadrao =
        ["08:00", "09:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00"];

    [Fact]
    public void Dia_do_quadro_tem_a_regua_padrao_com_os_dois_cartoes_na_posicao_e_hoje_marcado_nas_tres_visoes()
    {
        var dia = Dia(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana);

        var desktop = Desktop(dia);
        desktop.ClassName.Should().Be("aj-dia aj-dia--hoje");
        desktop.GetAttribute("aria-label").Should().Be("quinta-feira, 08/10/2026");
        Linhas(desktop.QuerySelector("h2.aj-dia__cabeca")!).Should().Equal(
            "aj-dia__numero: 08", ": Quinta-feira · hoje", "aj-dia__total: 2 eventos");
        desktop.QuerySelector("div.aj-regua__horas")!.GetAttribute("aria-hidden").Should().Be("true");
        Horas(desktop).Should().Equal(HorasPadrao);
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-11");
        // As linhas das horas são fundo da grade (a classe de horas): a grade só tem os cartões.
        Grade(desktop).Children.Should().HaveCount(2).And.OnlyContain(c => c.ClassList.Contains("aj-regua__evento"));

        // Só os eventos do dia, nas posições do quadro (09:30–10:30 e 15:00–16:00).
        Titulos(desktop).Should().BeEquivalentTo("Protocolo de documentos", "Revisão de minutas");
        var protocolo = Cartao(desktop, "Protocolo de documentos");
        protocolo.ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--outro aj-regua__evento aj-regua__evento--inicio-6 aj-regua__evento--quartos-4");
        Linhas(protocolo).Should().Equal(
            "aj-evento__tipo: Outro", "aj-evento__horario: 09:30 – 10:30", "aj-evento__titulo: Protocolo de documentos",
            "aj-evento__meta: Cartório do 1º Ofício · Otávio Brandão");
        var revisao = Cartao(desktop, "Revisão de minutas");
        revisao.ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--reuniao aj-regua__evento aj-regua__evento--inicio-28 aj-regua__evento--quartos-4");
        revisao.QuerySelector(".aj-evento__meta")!.TextContent.Should().Be("Sala de reuniões · Marina Couto, Camila Prado");
        protocolo.GetAttribute("href").Should().Be($"/agenda/{QuadroDaAgenda.ProtocoloDeDocumentos.Id}?visao=dia&data=2026-10-08");

        var tablet = Tablet(dia);
        tablet.ClassName.Should().Be("aj-secao-dia aj-secao-dia--hoje");
        tablet.QuerySelector("h2.aj-secao-dia__cabeca")!.GetAttribute("aria-label").Should().Be("quinta-feira, 08/10/2026, hoje");
        tablet.QuerySelector("span.aj-secao-dia__sigla")!.TextContent.Should().Be("Hoje");
        tablet.QuerySelector("span.aj-secao-dia__numero")!.TextContent.Should().Be("08");
        tablet.QuerySelectorAll("div.aj-secao-dia__eventos > a.aj-evento").Select(a => a.ClassName).Should().Equal(
            "aj-evento aj-evento--outro", "aj-evento aj-evento--reuniao");
        Titulos(tablet).Should().Equal("Protocolo de documentos", "Revisão de minutas");
        tablet.QuerySelector("p.aj-secao-dia__vazio").Should().BeNull();

        var mobile = Mobile(dia);
        mobile.QuerySelector("div.aj-dia-escolhido > h2")!.TextContent.Should().Be("Quinta, 08/10 · hoje");
        mobile.QuerySelector("div.aj-dia-escolhido > span.aj-dia-escolhido__total")!.TextContent.Should().Be("2 eventos");
        mobile.QuerySelectorAll("a.aj-evento").Select(a => a.ClassName).Should().Equal(
            "aj-evento aj-evento--folgado aj-evento--outro", "aj-evento aj-evento--folgado aj-evento--reuniao");
        Titulos(mobile).Should().Equal("Protocolo de documentos", "Revisão de minutas");
        mobile.QuerySelector(".aj-estado-lista").Should().BeNull();

        // Os eventos dos outros dias da semana não entram em nenhuma das três (seis cartões: dois por visão).
        var titulos = dia.FindAll("a.aj-evento .aj-evento__titulo").Select(t => t.TextContent).ToList();
        titulos.Should().HaveCount(6).And.Contain("Protocolo de documentos").And.NotContain("Pauta da semana")
            .And.NotContain("Recurso ordinário");
    }

    [Fact]
    public void Eventos_simultaneos_ficam_lado_a_lado_o_seguinte_volta_a_largura_inteira_e_o_curto_tem_dois_quartos()
    {
        // 07/10: Alinhamento 10:00–11:30 e Audiência de conciliação 11:00–12:00 se sobrepõem (o conflito do quadro).
        var despacho = QuadroDaAgenda.Evento(new DateTime(2026, 10, 7, 14, 0, 0), 60, TipoEvento.Outro, "Despacho com o cliente");
        var ligacao = QuadroDaAgenda.Evento(new DateTime(2026, 10, 7, 16, 0, 0), 15, TipoEvento.Reuniao, "Ligação rápida");

        var dia = Dia(new DateOnly(2026, 10, 7), [.. QuadroDaAgenda.Semana, despacho, ligacao]);

        var desktop = Desktop(dia);
        Cartao(desktop, "Alinhamento com o cliente").ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--reuniao aj-regua__evento aj-regua__evento--inicio-8 aj-regua__evento--quartos-6 aj-regua__evento--parte-1-de-2");
        Cartao(desktop, "Audiência de conciliação").ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--audiencia aj-regua__evento aj-regua__evento--inicio-12 aj-regua__evento--quartos-4 aj-regua__evento--parte-2-de-2");
        Cartao(desktop, "Despacho com o cliente").ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--outro aj-regua__evento aj-regua__evento--inicio-24 aj-regua__evento--quartos-4");
        // 15 minutos ganham a altura mínima de dois quartos.
        Cartao(desktop, "Ligação rápida").ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--reuniao aj-regua__evento aj-regua__evento--inicio-32 aj-regua__evento--quartos-2");
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-11");

        // Dia que não é hoje: sem as marcas de hoje.
        desktop.ClassName.Should().Be("aj-dia");
        Linhas(desktop.QuerySelector("h2.aj-dia__cabeca")!).Should().Equal(
            "aj-dia__numero: 07", ": Quarta-feira", "aj-dia__total: 4 eventos");
        var tablet = Tablet(dia);
        tablet.ClassName.Should().Be("aj-secao-dia");
        tablet.QuerySelector("span.aj-secao-dia__sigla")!.TextContent.Should().Be("Qua");
        tablet.QuerySelector("h2")!.GetAttribute("aria-label").Should().Be("quarta-feira, 07/10/2026");
        Titulos(tablet).Should().Equal("Alinhamento com o cliente", "Audiência de conciliação", "Despacho com o cliente", "Ligação rápida");
        Mobile(dia).QuerySelector("div.aj-dia-escolhido > h2")!.TextContent.Should().Be("Quarta, 07/10");
    }

    [Fact]
    public void Regua_se_estende_ate_a_hora_cheia_do_primeiro_inicio_e_a_seguinte_ao_ultimo_fim()
    {
        // 09/10: um evento às 06:30 e outro que termina às 21:15 levam a régua de 06:00 a 22:00 (16 horas).
        var cedo = QuadroDaAgenda.Evento(new DateTime(2026, 10, 9, 6, 30, 0), 60, TipoEvento.Audiencia, "Audiência cedo");
        var noite = QuadroDaAgenda.Evento(new DateTime(2026, 10, 9, 20, 0, 0), 75, TipoEvento.Reuniao, "Reunião à noite");

        var desktop = Desktop(Dia(new DateOnly(2026, 10, 9), [.. QuadroDaAgenda.Semana, cedo, noite]));

        Horas(desktop).Should().Equal(
            "06:00", "07:00", "08:00", "09:00", "10:00", "11:00", "12:00", "13:00",
            "14:00", "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00");
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-16");
        Grade(desktop).Children.Should().HaveCount(3).And.OnlyContain(c => c.ClassList.Contains("aj-regua__evento"));
        Cartao(desktop, "Audiência cedo").ClassName.Should().EndWith("aj-regua__evento--inicio-2 aj-regua__evento--quartos-4");
        Cartao(desktop, "Recurso ordinário").ClassName.Should().EndWith("aj-regua__evento--inicio-40 aj-regua__evento--quartos-4");
        Cartao(desktop, "Reunião à noite").ClassName.Should().EndWith("aj-regua__evento--inicio-56 aj-regua__evento--quartos-5");
    }

    [Fact]
    public void Evento_que_atravessa_a_meia_noite_entra_recortado_em_cada_dia()
    {
        // 07/10 às 23:00 até 08/10 à 01:00.
        var plantao = QuadroDaAgenda.Evento(new DateTime(2026, 10, 7, 23, 0, 0), 120, TipoEvento.Outro, "Plantão noturno");

        var oitoDeOutubro = Dia(QuadroDaAgenda.Hoje, [plantao]);
        var desktop = Desktop(oitoDeOutubro);
        Horas(desktop).Should().HaveCount(19).And.StartWith("00:00").And.EndWith("18:00");
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-19");
        var cartao = Cartao(desktop, "Plantão noturno");
        cartao.ClassName.Should().EndWith("aj-regua__evento--inicio-0 aj-regua__evento--quartos-4");
        cartao.QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("07/10 23:00 – 01:00");
        var noTablet = Cartao(Tablet(oitoDeOutubro), "Plantão noturno");
        noTablet.QuerySelector("span.aj-evento__hora > b")!.TextContent.Should().Be("…");
        noTablet.QuerySelector("span.aj-evento__hora > span")!.TextContent.Should().Be("01:00");
        Mobile(oitoDeOutubro).QuerySelector("span.aj-dia-escolhido__total")!.TextContent.Should().Be("1 evento");

        var seteDeOutubro = Dia(new DateOnly(2026, 10, 7), [plantao]);
        desktop = Desktop(seteDeOutubro);
        Horas(desktop).Should().HaveCount(16).And.StartWith("08:00").And.EndWith("23:00");
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-16");
        cartao = Cartao(desktop, "Plantão noturno");
        cartao.ClassName.Should().EndWith("aj-regua__evento--inicio-60 aj-regua__evento--quartos-4");
        cartao.QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("23:00 – 08/10 01:00");
        noTablet = Cartao(Tablet(seteDeOutubro), "Plantão noturno");
        noTablet.QuerySelector("span.aj-evento__hora > b")!.TextContent.Should().Be("23:00");
        noTablet.QuerySelector("span.aj-evento__hora > span")!.TextContent.Should().Be("…");
    }

    [Fact]
    public void Dia_sem_eventos_tem_a_regua_padrao_no_desktop_sem_eventos_no_tablet_e_o_vazio_com_dica_no_mobile()
    {
        var dia = Dia(new DateOnly(2026, 10, 10), QuadroDaAgenda.Semana);

        var desktop = Desktop(dia);
        Horas(desktop).Should().Equal(HorasPadrao);
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-11");
        desktop.QuerySelectorAll("a.aj-evento").Should().BeEmpty();
        Linhas(desktop.QuerySelector("h2.aj-dia__cabeca")!).Should().Equal(
            "aj-dia__numero: 10", ": Sábado", "aj-dia__total: 0 eventos");

        var tablet = Tablet(dia);
        tablet.QuerySelector("div.aj-secao-dia__eventos > p.aj-secao-dia__vazio")!.TextContent.Should().Be("Sem eventos");
        tablet.QuerySelectorAll("a.aj-evento").Should().BeEmpty();

        var mobile = Mobile(dia);
        mobile.QuerySelector("div.aj-dia-escolhido > h2")!.TextContent.Should().Be("Sábado, 10/10");
        mobile.QuerySelector("span.aj-dia-escolhido__total")!.TextContent.Should().Be("0 eventos");
        var vazio = mobile.QuerySelector("div.aj-estado-lista")!;
        vazio.QuerySelector("svg.aj-estado-lista__icone").Should().NotBeNull();
        vazio.QuerySelector("strong")!.TextContent.Should().Be("Nenhum evento neste dia");
        vazio.QuerySelector("span.aj-estado-lista__dica")!.TextContent.Should().Be("Toque em + para agendar.");
        mobile.QuerySelectorAll("a.aj-evento").Should().BeEmpty();
    }

    // F11: o "+N" do Mês leva o foco ao título do Dia. O título do desktop e o da seção do tablet aceitam o foco por
    // programa (tabindex -1, fora da ordem de tabulação); o da lista do mobile não precisa (o mobile não tem "+N").
    [Fact]
    public void Titulos_do_dia_no_desktop_e_no_tablet_aceitam_o_foco_por_programa()
    {
        var dia = Dia(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana);

        Desktop(dia).QuerySelector("h2.aj-dia__cabeca")!.GetAttribute("tabindex").Should().Be("-1");
        Tablet(dia).QuerySelector("h2.aj-secao-dia__cabeca")!.GetAttribute("tabindex").Should().Be("-1");
        var doMobile = Mobile(dia).QuerySelector("div.aj-dia-escolhido > h2")!;
        doMobile.TextContent.Should().Be("Quinta, 08/10 · hoje");
        doMobile.HasAttribute("tabindex").Should().BeFalse();
    }
}

/// <summary>
/// A faixa de dias da barra do mobile: os sete dias da semana da data, o escolhido, o hoje, um ponto por evento e o
/// toque que avisa a página.
/// </summary>
public class FaixaDeDiasTests : TelaInterativa
{
    private DateOnly? _escolhido;

    private IRenderedComponent<FaixaDeDias> Faixa(DateOnly data, IReadOnlyList<EventoResumo> eventos) =>
        Desenhar<FaixaDeDias>(p => p
            .Add(c => c.Periodo, new PeriodoDaAgenda(VisaoDaAgenda.Dia, data, true))
            .Add(c => c.Eventos, eventos)
            .Add(c => c.Hoje, QuadroDaAgenda.Hoje)
            .Add(c => c.AoEscolher, (DateOnly dia) => _escolhido = dia));

    private static IReadOnlyList<IElement> Dias(IRenderedComponent<FaixaDeDias> faixa) =>
        faixa.FindAll("div.aj-faixa-dias > button");

    private static string[] Pontos(IElement dia) =>
        dia.QuerySelectorAll("span.aj-pontos-evento > span").Select(p => p.ClassName!).ToArray();

    [Fact]
    public void Sete_dias_com_sigla_numero_e_rotulo_o_escolhido_pressionado_e_hoje_marcado()
    {
        var dias = Dias(Faixa(new DateOnly(2026, 10, 7), QuadroDaAgenda.Semana));

        dias.Should().HaveCount(7);
        dias.Select(d => d.GetAttribute("type")).Should().OnlyContain(t => t == "button");
        dias.Select(d => d.GetAttribute("aria-label")).Should().Equal(
            "Segunda, 05/10", "Terça, 06/10", "Quarta, 07/10", "Quinta, 08/10, hoje", "Sexta, 09/10", "Sábado, 10/10", "Domingo, 11/10");
        dias.Select(d => d.QuerySelector("span.aj-faixa-dias__sigla")!.TextContent).Should().Equal(
            "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom");
        dias.Select(d => d.QuerySelector("span.aj-faixa-dias__numero")!.TextContent).Should().Equal(
            "05", "06", "07", "08", "09", "10", "11");
        dias.Select(d => d.GetAttribute("aria-pressed")).Should().Equal("false", "false", "true", "false", "false", "false", "false");
        dias.Select(d => d.ClassName).Should().Equal(
            "aj-faixa-dias__dia", "aj-faixa-dias__dia", "aj-faixa-dias__dia", "aj-faixa-dias__dia aj-faixa-dias__dia--hoje",
            "aj-faixa-dias__dia", "aj-faixa-dias__dia", "aj-faixa-dias__dia");
    }

    [Fact]
    public void Um_ponto_por_evento_do_dia_na_cor_do_tipo_e_a_fileira_mesmo_sem_eventos()
    {
        var dias = Dias(Faixa(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana));

        Pontos(dias[0]).Should().Equal("aj-ponto-evento aj-ponto-evento--reuniao", "aj-ponto-evento aj-ponto-evento--audiencia");
        Pontos(dias[1]).Should().Equal("aj-ponto-evento aj-ponto-evento--prazo");
        Pontos(dias[3]).Should().Equal("aj-ponto-evento aj-ponto-evento--outro", "aj-ponto-evento aj-ponto-evento--reuniao");
        Pontos(dias[5]).Should().BeEmpty();
        // A fileira existe mesmo vazia (os dias ficam da mesma altura) e não é lida.
        dias[5].QuerySelector("span.aj-pontos-evento")!.GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void No_maximo_seis_pontos_por_dia()
    {
        var muitos = Enumerable.Range(0, 8)
            .Select(i => QuadroDaAgenda.Evento(new DateTime(2026, 10, 10, 8 + i, 0, 0), 30, TipoEvento.Outro, $"Tarefa {i + 1}"))
            .ToList();

        var dias = Dias(Faixa(QuadroDaAgenda.Hoje, [.. QuadroDaAgenda.Semana, .. muitos]));

        Pontos(dias[5]).Should().HaveCount(6).And.OnlyContain(p => p == "aj-ponto-evento aj-ponto-evento--outro");
        Pontos(dias[4]).Should().Equal("aj-ponto-evento aj-ponto-evento--prazo");
    }

    [Fact]
    public void Tocar_num_dia_avisa_a_pagina_com_a_data()
    {
        var faixa = Faixa(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana);

        Dias(faixa)[4].Click();

        _escolhido.Should().Be(new DateOnly(2026, 10, 9));
    }
}
