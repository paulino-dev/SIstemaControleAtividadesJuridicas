using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Components.Agenda;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O Mês da agenda com os eventos do quadro: a grade de semanas inteiras do desktop e do tablet (4, 5 ou 6 linhas,
/// dias de fora apagados e sem eventos, até três eventos por dia e o "+N", evento de vários dias em cada dia) e a
/// lista do dia da data no mobile.
/// </summary>
public class MesDaAgendaTests : TelaInterativa
{
    private DateOnly? _aberto;

    private IRenderedComponent<MesDaAgenda> Mes(DateOnly data, IReadOnlyList<EventoResumo> eventos) =>
        Desenhar<MesDaAgenda>(p => p
            .Add(c => c.Periodo, new PeriodoDaAgenda(VisaoDaAgenda.Mes, data, true))
            .Add(c => c.Eventos, eventos)
            .Add(c => c.Hoje, QuadroDaAgenda.Hoje)
            .Add(c => c.AoAbrirDia, (DateOnly dia) => _aberto = dia));

    private static IReadOnlyList<IElement> Celulas(IRenderedComponent<MesDaAgenda> mes) =>
        mes.FindAll("div.aj-desktop-tablet > div.aj-mes > div.aj-mes__rolagem > div.aj-mes__grade > div");

    // A grade de outubro de 2026 começa na segunda-feira, 28/09: o dia d de outubro é a célula d + 2.
    private static IElement DeOutubro(IRenderedComponent<MesDaAgenda> mes, int dia) => Celulas(mes)[dia + 2];

    private static string Numero(IElement celula) => celula.QuerySelector("span.aj-mes__numero")!.TextContent;

    private static string[] Itens(IElement celula) =>
        celula.QuerySelectorAll("a.aj-evento .aj-evento__titulo").Select(t => t.TextContent).ToArray();

    private static IElement Item(IElement celula, string titulo) =>
        celula.QuerySelectorAll("a.aj-evento").Single(a => a.QuerySelector(".aj-evento__titulo")!.TextContent == titulo);

    private static string[] Linhas(IElement elemento) =>
        elemento.Children.Select(linha => $"{linha.ClassName}: {linha.TextContent}").ToArray();

    [Fact]
    public void Outubro_do_quadro_tem_cinco_semanas_os_dias_de_fora_apagados_e_sem_eventos_hoje_marcado_e_os_itens()
    {
        // Um evento em 30/09 (dia de fora na grade de outubro) não aparece.
        var setembro = QuadroDaAgenda.Evento(new DateTime(2026, 9, 30, 10, 0, 0), 60, TipoEvento.Reuniao, "Fechamento de setembro");

        var mes = Mes(QuadroDaAgenda.Hoje, [setembro, .. QuadroDaAgenda.Semana]);

        var cabeca = mes.Find("div.aj-desktop-tablet > div.aj-mes > div.aj-mes__cabeca");
        cabeca.GetAttribute("aria-hidden").Should().Be("true");
        cabeca.Children.Select(s => s.TextContent).Should().Equal("Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom");

        var celulas = Celulas(mes);
        celulas.Should().HaveCount(35);
        Numero(celulas[0]).Should().Be("28");
        Numero(celulas[34]).Should().Be("01");
        var fora = celulas.Where(c => c.ClassList.Contains("aj-mes__celula--fora")).ToList();
        fora.Select(Numero).Should().Equal("28", "29", "30", "01");
        fora.Should().OnlyContain(c => c.ClassName == "aj-mes__celula aj-mes__celula--fora" && c.QuerySelectorAll("a").Length == 0);
        celulas.Count(c => c.ClassName == "aj-mes__celula").Should().Be(31);

        // Hoje: o número no círculo e "hoje" para o leitor de tela; só no dia 08.
        var hoje = DeOutubro(mes, 8);
        hoje.QuerySelector("span.aj-mes__numero")!.ClassName.Should().Be("aj-mes__numero aj-mes__numero--hoje");
        hoje.Children.Where(c => c.ClassName == "aj-so-leitor").Select(c => c.TextContent).Should().Equal("hoje");
        mes.FindAll("span.aj-mes__numero--hoje").Should().ContainSingle();
        DeOutubro(mes, 7).QuerySelector("span.aj-mes__numero")!.ClassName.Should().Be("aj-mes__numero");

        var cinco = DeOutubro(mes, 5).QuerySelectorAll("a.aj-evento");
        cinco.Select(a => a.ClassName).Should().Equal(
            "aj-evento aj-evento--mes aj-evento--reuniao", "aj-evento aj-evento--mes aj-evento--audiencia");
        Linhas(cinco[0]).Should().Equal("aj-so-leitor: Reunião, ", "aj-evento__horario: 09:00", "aj-evento__titulo: Pauta da semana");
        Linhas(cinco[1]).Should().Equal("aj-so-leitor: Audiência, ", "aj-evento__horario: 14:00", "aj-evento__titulo: Audiência de instrução");
        cinco[0].GetAttribute("href").Should().Be($"/agenda/{QuadroDaAgenda.PautaDaSemana.Id}?visao=mes&data=2026-10-08");
        Itens(DeOutubro(mes, 6)).Should().Equal("Contestação — cobrança de ISS");
        Itens(DeOutubro(mes, 7)).Should().Equal("Alinhamento com o cliente", "Audiência de conciliação");
        Itens(DeOutubro(mes, 8)).Should().Equal("Protocolo de documentos", "Revisão de minutas");
        Itens(DeOutubro(mes, 9)).Should().Equal("Recurso ordinário");
        Itens(DeOutubro(mes, 10)).Should().BeEmpty();
        Numero(DeOutubro(mes, 10)).Should().Be("10");
        mes.FindAll("button.aj-mes__mais").Should().BeEmpty();
        mes.FindAll(".aj-evento__titulo").Select(t => t.TextContent).Should()
            .Contain("Pauta da semana").And.NotContain("Fechamento de setembro");
    }

    [Fact]
    public void Agosto_de_2026_tem_seis_semanas_e_fevereiro_de_2027_tem_quatro()
    {
        var agosto = Celulas(Mes(new DateOnly(2026, 8, 20), []));
        agosto.Should().HaveCount(42);
        Numero(agosto[0]).Should().Be("27");
        Numero(agosto[41]).Should().Be("06");
        agosto.Count(c => c.ClassList.Contains("aj-mes__celula--fora")).Should().Be(11);
        agosto.Count(c => c.ClassName == "aj-mes__celula").Should().Be(31);
        agosto.Should().OnlyContain(c => c.QuerySelector("span.aj-mes__numero--hoje") == null);

        var fevereiro = Celulas(Mes(new DateOnly(2027, 2, 10), []));
        fevereiro.Should().HaveCount(28);
        Numero(fevereiro[0]).Should().Be("01");
        Numero(fevereiro[27]).Should().Be("28");
        fevereiro.Should().OnlyContain(c => c.ClassName == "aj-mes__celula");
    }

    [Fact]
    public void Dia_com_mais_de_tres_eventos_mostra_os_tres_primeiros_e_o_mais_que_abre_o_dia()
    {
        var sete = Enumerable.Range(0, 3).Select(i =>
            QuadroDaAgenda.Evento(new DateTime(2026, 10, 7, 8 + i, 0, 0), 30, TipoEvento.Audiencia, $"Audiência {i + 1}"));
        var oito = Enumerable.Range(0, 5).Select(i =>
            QuadroDaAgenda.Evento(new DateTime(2026, 10, 8, 8 + i, 0, 0), 30, TipoEvento.Reuniao, $"Reunião {i + 1}"));
        var nove = Enumerable.Range(0, 4).Select(i =>
            QuadroDaAgenda.Evento(new DateTime(2026, 10, 9, 8 + i, 0, 0), 30, TipoEvento.Prazo, $"Prazo {i + 1}"));

        var mes = Mes(QuadroDaAgenda.Hoje, [.. sete, .. oito, .. nove]);

        // Exatamente três: todos, sem o "+".
        Itens(DeOutubro(mes, 7)).Should().Equal("Audiência 1", "Audiência 2", "Audiência 3");
        DeOutubro(mes, 7).QuerySelector("button.aj-mes__mais").Should().BeNull();

        Itens(DeOutubro(mes, 8)).Should().Equal("Reunião 1", "Reunião 2", "Reunião 3");
        var mais = DeOutubro(mes, 8).QuerySelector("button.aj-mes__mais")!;
        mais.GetAttribute("type").Should().Be("button");
        mais.TextContent.Should().Be("+2");
        mais.GetAttribute("aria-label").Should().Be("Mais 2 eventos em 08/10");

        Itens(DeOutubro(mes, 9)).Should().Equal("Prazo 1", "Prazo 2", "Prazo 3");
        var maisUm = DeOutubro(mes, 9).QuerySelector("button.aj-mes__mais")!;
        maisUm.TextContent.Should().Be("+1");
        maisUm.GetAttribute("aria-label").Should().Be("Mais 1 evento em 09/10");

        mais.Click();

        _aberto.Should().Be(new DateOnly(2026, 10, 8));
    }

    [Fact]
    public void Evento_de_varios_dias_aparece_em_cada_dia_com_a_hora_so_no_primeiro()
    {
        // 06/10 às 20:00 até 08/10 às 10:00.
        var plantao = QuadroDaAgenda.Evento(new DateTime(2026, 10, 6, 20, 0, 0), 38 * 60, TipoEvento.Prazo, "Plantão de protocolo");

        var mes = Mes(QuadroDaAgenda.Hoje, [.. QuadroDaAgenda.Semana, plantao]);

        Itens(DeOutubro(mes, 6)).Should().Equal("Contestação — cobrança de ISS", "Plantão de protocolo");
        Linhas(Item(DeOutubro(mes, 6), "Plantão de protocolo")).Should().Equal(
            "aj-so-leitor: Prazo, ", "aj-evento__horario: 20:00", "aj-evento__titulo: Plantão de protocolo");
        Itens(DeOutubro(mes, 7)).Should().Equal("Plantão de protocolo", "Alinhamento com o cliente", "Audiência de conciliação");
        Linhas(Item(DeOutubro(mes, 7), "Plantão de protocolo")).Should().Equal(
            "aj-so-leitor: Prazo, ", "aj-evento__titulo: Plantão de protocolo");
        Itens(DeOutubro(mes, 8)).Should().Equal("Plantão de protocolo", "Protocolo de documentos", "Revisão de minutas");
        Linhas(Item(DeOutubro(mes, 8), "Plantão de protocolo")).Should().Equal(
            "aj-so-leitor: Prazo, ", "aj-evento__titulo: Plantão de protocolo");
        Itens(DeOutubro(mes, 9)).Should().Equal("Recurso ordinário");
        mes.FindAll("button.aj-mes__mais").Should().BeEmpty();
    }

    [Fact]
    public void No_mobile_a_lista_e_a_do_dia_da_data_ou_o_vazio_com_dica()
    {
        var mobile = Mes(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana).Find("div.aj-so-mobile");

        mobile.QuerySelector("div.aj-dia-escolhido > h2")!.TextContent.Should().Be("Quinta, 08/10 · hoje");
        mobile.QuerySelector("div.aj-dia-escolhido > span.aj-dia-escolhido__total")!.TextContent.Should().Be("2 eventos");
        mobile.QuerySelectorAll("a.aj-evento").Select(a => a.ClassName).Should().Equal(
            "aj-evento aj-evento--folgado aj-evento--outro", "aj-evento aj-evento--folgado aj-evento--reuniao");
        mobile.QuerySelectorAll("a.aj-evento .aj-evento__titulo").Select(t => t.TextContent).Should().Equal(
            "Protocolo de documentos", "Revisão de minutas");
        mobile.QuerySelector(".aj-estado-lista").Should().BeNull();

        var sabado = Mes(new DateOnly(2026, 10, 10), QuadroDaAgenda.Semana).Find("div.aj-so-mobile");

        sabado.QuerySelector("div.aj-dia-escolhido > h2")!.TextContent.Should().Be("Sábado, 10/10");
        sabado.QuerySelector("span.aj-dia-escolhido__total")!.TextContent.Should().Be("0 eventos");
        var vazio = sabado.QuerySelector("div.aj-estado-lista")!;
        vazio.QuerySelector("svg.aj-estado-lista__icone").Should().NotBeNull();
        vazio.QuerySelector("strong")!.TextContent.Should().Be("Nenhum evento neste dia");
        vazio.QuerySelector("span.aj-estado-lista__dica")!.TextContent.Should().Be("Toque em + para agendar.");
        sabado.QuerySelectorAll("a.aj-evento").Should().BeEmpty();
    }

    [Fact]
    public void No_mobile_o_cartao_da_lista_leva_a_ficha_com_a_origem_do_mes()
    {
        var mobile = Mes(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana).Find("div.aj-so-mobile");

        mobile.QuerySelectorAll("a.aj-evento").Select(a => a.GetAttribute("href")).Should().Equal(
            $"/agenda/{QuadroDaAgenda.ProtocoloDeDocumentos.Id}?visao=mes&data=2026-10-08",
            $"/agenda/{QuadroDaAgenda.RevisaoDeMinutas.Id}?visao=mes&data=2026-10-08");
    }
}

/// <summary>
/// A grade compacta do mês na barra do mobile: a linha das siglas, um botão por dia das semanas do mês (sem sigla),
/// os dias de fora apagados e sem pontos, o escolhido, o hoje e o toque que avisa a página.
/// </summary>
public class GradeCompactaDoMesTests : TelaInterativa
{
    private DateOnly? _escolhido;

    private IRenderedComponent<GradeCompactaDoMes> Grade(DateOnly data, IReadOnlyList<EventoResumo> eventos) =>
        Desenhar<GradeCompactaDoMes>(p => p
            .Add(c => c.Periodo, new PeriodoDaAgenda(VisaoDaAgenda.Mes, data, true))
            .Add(c => c.Eventos, eventos)
            .Add(c => c.Hoje, QuadroDaAgenda.Hoje)
            .Add(c => c.AoEscolher, (DateOnly dia) => _escolhido = dia));

    // A grade de outubro de 2026 começa na segunda-feira, 28/09: o dia d de outubro é o botão d + 2.
    private static IReadOnlyList<IElement> Dias(IRenderedComponent<GradeCompactaDoMes> grade) =>
        grade.FindAll("div.aj-faixa-dias.aj-faixa-dias--mes > button");

    private static string[] Pontos(IElement dia) =>
        dia.QuerySelectorAll("span.aj-pontos-evento > span").Select(p => p.ClassName!).ToArray();

    [Fact]
    public void Grade_de_outubro_tem_as_siglas_os_dias_de_fora_apagados_o_escolhido_e_o_hoje()
    {
        var grade = Grade(new DateOnly(2026, 10, 7), QuadroDaAgenda.Semana);

        grade.Find("div.aj-faixa-dias").ClassName.Should().Be("aj-faixa-dias aj-faixa-dias--mes");
        grade.Find("div.aj-faixa-dias--mes").FirstElementChild!.ClassName.Should().Be("aj-faixa-dias__cabeca");
        var cabeca = grade.Find("div.aj-faixa-dias--mes > div.aj-faixa-dias__cabeca");
        cabeca.GetAttribute("aria-hidden").Should().Be("true");
        cabeca.Children.Select(s => s.TextContent).Should().Equal("Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom");

        var dias = Dias(grade);
        dias.Should().HaveCount(35);
        dias.Should().OnlyContain(d => d.GetAttribute("type") == "button" && d.QuerySelector("span.aj-faixa-dias__sigla") == null);
        dias.Select(d => d.QuerySelector("span.aj-faixa-dias__numero")!.TextContent).Should().StartWith("28").And.EndWith("01");
        dias[0].GetAttribute("aria-label").Should().Be("Segunda, 28/09");
        dias[10].GetAttribute("aria-label").Should().Be("Quinta, 08/10, hoje");
        dias.Where(d => d.ClassList.Contains("aj-faixa-dias__dia--fora"))
            .Select(d => d.QuerySelector("span.aj-faixa-dias__numero")!.TextContent).Should().Equal("28", "29", "30", "01");
        dias[0].ClassName.Should().Be("aj-faixa-dias__dia aj-faixa-dias__dia--fora");
        dias[9].ClassName.Should().Be("aj-faixa-dias__dia");
        dias[10].ClassName.Should().Be("aj-faixa-dias__dia aj-faixa-dias__dia--hoje");
        dias.Select(d => d.GetAttribute("aria-pressed")).Count(p => p == "true").Should().Be(1);
        dias[9].GetAttribute("aria-pressed").Should().Be("true");
        dias[10].GetAttribute("aria-pressed").Should().Be("false");
    }

    [Fact]
    public void Um_ponto_por_evento_do_dia_e_nenhum_no_dia_de_fora()
    {
        var setembro = QuadroDaAgenda.Evento(new DateTime(2026, 9, 30, 10, 0, 0), 60, TipoEvento.Reuniao, "Fechamento de setembro");

        var dias = Dias(Grade(QuadroDaAgenda.Hoje, [setembro, .. QuadroDaAgenda.Semana]));

        Pontos(dias[7]).Should().Equal("aj-ponto-evento aj-ponto-evento--reuniao", "aj-ponto-evento aj-ponto-evento--audiencia");
        Pontos(dias[10]).Should().Equal("aj-ponto-evento aj-ponto-evento--outro", "aj-ponto-evento aj-ponto-evento--reuniao");
        Pontos(dias[12]).Should().BeEmpty();
        // 30/09 tem evento, mas é de fora: sem pontos, com a fileira (a mesma altura dos outros dias).
        Pontos(dias[2]).Should().BeEmpty();
        dias[2].QuerySelector("span.aj-pontos-evento")!.GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void Tocar_num_dia_avisa_a_pagina_com_a_data_inclusive_num_dia_de_fora()
    {
        var grade = Grade(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana);

        Dias(grade)[11].Click();
        _escolhido.Should().Be(new DateOnly(2026, 10, 9));

        Dias(grade)[34].Click();
        _escolhido.Should().Be(new DateOnly(2026, 11, 1));
    }

    [Fact]
    public void Dia_com_sete_eventos_mostra_seis_pontos()
    {
        var sete = Enumerable.Range(0, 7).Select(i =>
            QuadroDaAgenda.Evento(new DateTime(2026, 10, 14, 8 + i, 0, 0), 30, TipoEvento.Reuniao, $"Reunião {i + 1}"));
        var seis = Enumerable.Range(0, 6).Select(i =>
            QuadroDaAgenda.Evento(new DateTime(2026, 10, 15, 8 + i, 0, 0), 30, TipoEvento.Prazo, $"Prazo {i + 1}"));

        var dias = Dias(Grade(QuadroDaAgenda.Hoje, [.. sete, .. seis]));

        Pontos(dias[16]).Should().HaveCount(6).And.OnlyContain(p => p == "aj-ponto-evento aj-ponto-evento--reuniao");
        Pontos(dias[17]).Should().HaveCount(6).And.OnlyContain(p => p == "aj-ponto-evento aj-ponto-evento--prazo");
    }

    [Fact]
    public void Hoje_num_dia_de_fora_nao_leva_a_marca_e_um_dia_de_dentro_continua_sem_ela()
    {
        // Hoje é 01/11/2026, de fora na grade de outubro: sem a marca; 15/10 (de dentro) segue sem ela também.
        var grade = Desenhar<GradeCompactaDoMes>(p => p
            .Add(c => c.Periodo, new PeriodoDaAgenda(VisaoDaAgenda.Mes, new DateOnly(2026, 10, 7), true))
            .Add(c => c.Eventos, QuadroDaAgenda.Semana)
            .Add(c => c.Hoje, new DateOnly(2026, 11, 1)));

        var dias = Dias(grade);
        dias[34].ClassName.Should().Be("aj-faixa-dias__dia aj-faixa-dias__dia--fora");
        dias[34].GetAttribute("aria-label").Should().Be("Domingo, 01/11");
        dias[17].ClassName.Should().Be("aj-faixa-dias__dia");
        dias.Should().NotContain(d => d.ClassList.Contains("aj-faixa-dias__dia--hoje"));
    }
}
