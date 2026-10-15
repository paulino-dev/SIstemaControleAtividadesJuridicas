using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Components.Agenda;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A Semana da agenda com os eventos do quadro (05/10 a 11/10/2026, hoje quinta-feira 08/10): as sete colunas do
/// desktop e as sete seções do tablet e do mobile, com os mesmos eventos, cada um em todo dia que toca, o hoje
/// marcado e os cartões levando à ficha com o período de origem.
/// </summary>
public class SemanaDaAgendaTests : TelaInterativa
{
    private static readonly PeriodoDaAgenda SemanaDoQuadro = new(VisaoDaAgenda.Semana, QuadroDaAgenda.Hoje, true);

    private static readonly string[] RotulosDaSemana =
    [
        "segunda-feira, 05/10/2026", "terça-feira, 06/10/2026", "quarta-feira, 07/10/2026",
        "quinta-feira, 08/10/2026, hoje", "sexta-feira, 09/10/2026", "sábado, 10/10/2026", "domingo, 11/10/2026",
    ];

    private IRenderedComponent<SemanaDaAgenda> Semana(
        IReadOnlyList<EventoResumo> eventos, PeriodoDaAgenda? periodo = null, DateOnly? hoje = null) =>
        Desenhar<SemanaDaAgenda>(p => p
            .Add(c => c.Periodo, periodo ?? SemanaDoQuadro)
            .Add(c => c.Eventos, eventos)
            .Add(c => c.Hoje, hoje ?? QuadroDaAgenda.Hoje));

    private static IReadOnlyList<IElement> Colunas(IRenderedComponent<SemanaDaAgenda> semana) =>
        semana.FindAll("div.aj-so-desktop > div.aj-semana > section");

    private static IReadOnlyList<IElement> Secoes(IRenderedComponent<SemanaDaAgenda> semana) =>
        semana.FindAll("div.aj-tablet-mobile > section");

    private static string[] Titulos(IElement dia) =>
        dia.QuerySelectorAll("a.aj-evento .aj-evento__titulo").Select(t => t.TextContent).ToArray();

    private static IElement Cartao(IElement dia, string titulo) =>
        dia.QuerySelectorAll("a.aj-evento").Single(a => a.QuerySelector(".aj-evento__titulo")!.TextContent == titulo);

    private static string[] Linhas(IElement elemento) =>
        elemento.Children.Select(linha => $"{linha.ClassName}: {linha.TextContent}").ToArray();

    [Fact]
    public void Desktop_tem_uma_coluna_por_dia_com_os_eventos_do_quadro_hoje_marcado_e_o_fim_de_semana_apagado_e_vazio()
    {
        var colunas = Colunas(Semana(QuadroDaAgenda.Semana));

        colunas.Should().HaveCount(7);
        colunas.Select(c => c.ClassName).Should().Equal(
            "aj-semana__dia", "aj-semana__dia", "aj-semana__dia", "aj-semana__dia aj-semana__dia--hoje",
            "aj-semana__dia", "aj-semana__dia aj-semana__dia--folga", "aj-semana__dia aj-semana__dia--folga");
        colunas.Select(c => c.QuerySelector("h2.aj-semana__cabeca")!.GetAttribute("aria-label")).Should().Equal(RotulosDaSemana);
        colunas.Select(c => c.QuerySelector("h2 > span.aj-semana__sigla")!.TextContent).Should().Equal(
            "Seg", "Ter", "Qua", "Qui · hoje", "Sex", "Sáb", "Dom");
        colunas.Select(c => c.QuerySelector("h2 > span.aj-semana__numero")!.TextContent).Should().Equal(
            "05", "06", "07", "08", "09", "10", "11");

        Titulos(colunas[0]).Should().Equal("Pauta da semana", "Audiência de instrução");
        Titulos(colunas[1]).Should().Equal("Contestação — cobrança de ISS");
        Titulos(colunas[2]).Should().Equal("Alinhamento com o cliente", "Audiência de conciliação");
        Titulos(colunas[3]).Should().Equal("Protocolo de documentos", "Revisão de minutas");
        Titulos(colunas[4]).Should().Equal("Recurso ordinário");
        colunas[0].QuerySelector("p.aj-semana__vazio").Should().BeNull();
        foreach (var folga in colunas.Skip(5))
        {
            Titulos(folga).Should().BeEmpty();
            folga.QuerySelector("p.aj-semana__vazio")!.TextContent.Should().Be("Sem eventos");
        }
    }

    [Fact]
    public void Tablet_e_mobile_tem_uma_secao_por_dia_com_os_mesmos_eventos_e_a_sigla_hoje()
    {
        var secoes = Secoes(Semana(QuadroDaAgenda.Semana));

        secoes.Should().HaveCount(7);
        secoes.Select(s => s.ClassName).Should().Equal(
            "aj-secao-dia", "aj-secao-dia", "aj-secao-dia", "aj-secao-dia aj-secao-dia--hoje",
            "aj-secao-dia", "aj-secao-dia", "aj-secao-dia");
        secoes.Select(s => s.QuerySelector("h2.aj-secao-dia__cabeca")!.GetAttribute("aria-label")).Should().Equal(RotulosDaSemana);
        secoes.Select(s => s.QuerySelector("h2 > span.aj-secao-dia__sigla")!.TextContent).Should().Equal(
            "Seg", "Ter", "Qua", "Hoje", "Sex", "Sáb", "Dom");
        secoes.Select(s => s.QuerySelector("h2 > span.aj-secao-dia__numero")!.TextContent).Should().Equal(
            "05", "06", "07", "08", "09", "10", "11");

        secoes.Select(s => s.QuerySelectorAll("div.aj-secao-dia__eventos > a.aj-evento").Length).Should().Equal(2, 1, 2, 2, 1, 0, 0);
        Titulos(secoes[2]).Should().Equal("Alinhamento com o cliente", "Audiência de conciliação");
        Titulos(secoes[3]).Should().Equal("Protocolo de documentos", "Revisão de minutas");
        secoes[3].QuerySelector("p.aj-secao-dia__vazio").Should().BeNull();
        foreach (var folga in secoes.Skip(5))
            folga.QuerySelector("div.aj-secao-dia__eventos > p.aj-secao-dia__vazio")!.TextContent.Should().Be("Sem eventos");
    }

    [Fact]
    public void Cartao_da_coluna_tem_tipo_horario_titulo_e_as_linhas_de_local_cliente_e_participantes_so_quando_ha()
    {
        var colunas = Colunas(Semana(QuadroDaAgenda.Semana));

        var instrucao = Cartao(colunas[0], "Audiência de instrução");
        instrucao.ClassName.Should().Be("aj-evento aj-evento--coluna aj-evento--audiencia");
        Linhas(instrucao).Should().Equal(
            "aj-evento__tipo: Audiência", "aj-evento__horario: 14:00 – 15:30", "aj-evento__titulo: Audiência de instrução",
            "aj-evento__local: 2ª Vara do Trabalho", "aj-evento__meta: Transportes Litoral Ltda.", "aj-evento__meta: Beatriz Lins");

        // Sem cliente: local e participantes.
        Linhas(Cartao(colunas[0], "Pauta da semana")).Should().Equal(
            "aj-evento__tipo: Reunião", "aj-evento__horario: 09:00 – 10:00", "aj-evento__titulo: Pauta da semana",
            "aj-evento__local: Sala de reuniões", "aj-evento__meta: Marina Couto, Rafael Teles, Beatriz Lins");

        // Sem local: cliente e participantes.
        var contestacao = Cartao(colunas[1], "Contestação — cobrança de ISS");
        contestacao.ClassName.Should().Be("aj-evento aj-evento--coluna aj-evento--prazo");
        Linhas(contestacao).Should().Equal(
            "aj-evento__tipo: Prazo", "aj-evento__horario: 17:00 – 18:00", "aj-evento__titulo: Contestação — cobrança de ISS",
            "aj-evento__meta: Construtora Serra Azul Ltda.", "aj-evento__meta: Rafael Teles");

        Cartao(colunas[3], "Protocolo de documentos").ClassName.Should().Be("aj-evento aj-evento--coluna aj-evento--outro");
    }

    [Fact]
    public void Cartao_da_secao_tem_a_hora_em_coluna_e_uma_linha_com_o_local_ou_o_cliente()
    {
        var secoes = Secoes(Semana(QuadroDaAgenda.Semana));

        var instrucao = Cartao(secoes[0], "Audiência de instrução");
        instrucao.ClassName.Should().Be("aj-evento aj-evento--audiencia");
        instrucao.QuerySelector("span.aj-evento__hora > b")!.TextContent.Should().Be("14:00");
        instrucao.QuerySelector("span.aj-evento__hora > span")!.TextContent.Should().Be("15:30");
        Linhas(instrucao.QuerySelector("span.aj-evento__corpo")!).Should().Equal(
            "aj-evento__tipo: Audiência", "aj-evento__titulo: Audiência de instrução", "aj-evento__local: 2ª Vara do Trabalho");
        // O cartão do tablet não mostra o cliente quando há local, nem os participantes.
        instrucao.TextContent.Should().Contain("2ª Vara do Trabalho").And.NotContain("Transportes Litoral Ltda.").And.NotContain("Beatriz Lins");

        // Sem local, a linha é o cliente.
        Linhas(Cartao(secoes[1], "Contestação — cobrança de ISS").QuerySelector("span.aj-evento__corpo")!).Should().Equal(
            "aj-evento__tipo: Prazo", "aj-evento__titulo: Contestação — cobrança de ISS", "aj-evento__local: Construtora Serra Azul Ltda.");
    }

    [Fact]
    public void Evento_sem_local_sem_cliente_e_sem_participantes_so_tem_tipo_horario_e_titulo()
    {
        var despacho = QuadroDaAgenda.Evento(new DateTime(2026, 10, 6, 8, 0, 0), 60, TipoEvento.Outro, "Despacho interno");

        var semana = Semana([despacho]);

        Linhas(Cartao(Colunas(semana)[1], "Despacho interno")).Should().Equal(
            "aj-evento__tipo: Outro", "aj-evento__horario: 08:00 – 09:00", "aj-evento__titulo: Despacho interno");
        Linhas(Cartao(Secoes(semana)[1], "Despacho interno").QuerySelector("span.aj-evento__corpo")!).Should().Equal(
            "aj-evento__tipo: Outro", "aj-evento__titulo: Despacho interno");
    }

    [Fact]
    public void Evento_de_tres_dias_aparece_nas_tres_colunas_e_secoes_com_o_lado_de_outro_dia_marcado()
    {
        // 06/10 às 20:00 até 08/10 às 10:00, mandado por último: a ordem do dia é a do início, não a da lista.
        var plantao = QuadroDaAgenda.Evento(new DateTime(2026, 10, 6, 20, 0, 0), 38 * 60, TipoEvento.Prazo, "Plantão de protocolo");

        var semana = Semana([.. QuadroDaAgenda.Semana, plantao]);

        var colunas = Colunas(semana);
        Titulos(colunas[0]).Should().NotContain("Plantão de protocolo").And.Contain("Pauta da semana");
        Titulos(colunas[1]).Should().Equal("Contestação — cobrança de ISS", "Plantão de protocolo");
        Titulos(colunas[2]).Should().Equal("Plantão de protocolo", "Alinhamento com o cliente", "Audiência de conciliação");
        Titulos(colunas[3]).Should().Equal("Plantão de protocolo", "Protocolo de documentos", "Revisão de minutas");
        Titulos(colunas[4]).Should().Equal("Recurso ordinário");
        Cartao(colunas[1], "Plantão de protocolo").QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("20:00 – 08/10 10:00");
        Cartao(colunas[2], "Plantão de protocolo").QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("06/10 20:00 – 08/10 10:00");
        Cartao(colunas[3], "Plantão de protocolo").QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("06/10 20:00 – 10:00");

        var secoes = Secoes(semana);
        (string Inicio, string Fim) Hora(int indice)
        {
            var cartao = Cartao(secoes[indice], "Plantão de protocolo");
            return (cartao.QuerySelector("span.aj-evento__hora > b")!.TextContent,
                cartao.QuerySelector("span.aj-evento__hora > span")!.TextContent);
        }
        Hora(1).Should().Be(("20:00", "…"));
        Hora(2).Should().Be(("…", "…"));
        Hora(3).Should().Be(("…", "10:00"));
        Titulos(secoes[4]).Should().Equal("Recurso ordinário");
    }

    [Fact]
    public void Evento_que_termina_a_meia_noite_nao_aparece_no_dia_seguinte()
    {
        var jantar = QuadroDaAgenda.Evento(new DateTime(2026, 10, 9, 22, 0, 0), 120, TipoEvento.Outro, "Jantar de encerramento");

        var semana = Semana([.. QuadroDaAgenda.Semana, jantar]);

        var colunas = Colunas(semana);
        Titulos(colunas[4]).Should().Equal("Recurso ordinário", "Jantar de encerramento");
        Cartao(colunas[4], "Jantar de encerramento").QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("22:00 – 00:00");
        Titulos(colunas[5]).Should().BeEmpty();
        colunas[5].QuerySelector("p.aj-semana__vazio")!.TextContent.Should().Be("Sem eventos");

        var secoes = Secoes(semana);
        Titulos(secoes[4]).Should().Equal("Recurso ordinário", "Jantar de encerramento");
        Titulos(secoes[5]).Should().BeEmpty();
        secoes[5].QuerySelector("p.aj-secao-dia__vazio")!.TextContent.Should().Be("Sem eventos");
    }

    [Fact]
    public void Os_cartoes_levam_a_ficha_com_o_periodo_de_origem()
    {
        var semana = Semana(QuadroDaAgenda.Semana);

        var esperados = QuadroDaAgenda.Semana.Select(e => $"/agenda/{e.Id}?visao=semana&data=2026-10-08").ToArray();
        semana.FindAll("div.aj-so-desktop a.aj-evento").Select(a => a.GetAttribute("href")).Should().Equal(esperados);
        semana.FindAll("div.aj-tablet-mobile a.aj-evento").Select(a => a.GetAttribute("href")).Should().Equal(esperados);

        // Sem nada no endereço (a agenda aberta pelo menu), a ficha não leva origem.
        var semOrigem = Semana(QuadroDaAgenda.Semana, PeriodoDaAgenda.Ler(null, null, QuadroDaAgenda.Hoje));
        Cartao(Colunas(semOrigem)[0], "Pauta da semana").GetAttribute("href")
            .Should().Be($"/agenda/{QuadroDaAgenda.PautaDaSemana.Id}");
        Cartao(Secoes(semOrigem)[0], "Pauta da semana").GetAttribute("href")
            .Should().Be($"/agenda/{QuadroDaAgenda.PautaDaSemana.Id}");
    }

    [Fact]
    public void Hoje_fora_da_semana_nao_marca_nenhum_dia()
    {
        var semana = Semana(QuadroDaAgenda.Semana, hoje: new DateOnly(2026, 10, 15));

        var colunas = Colunas(semana);
        colunas.Select(c => c.ClassName).Should().Equal(
            "aj-semana__dia", "aj-semana__dia", "aj-semana__dia", "aj-semana__dia",
            "aj-semana__dia", "aj-semana__dia aj-semana__dia--folga", "aj-semana__dia aj-semana__dia--folga");
        colunas[3].QuerySelector("span.aj-semana__sigla")!.TextContent.Should().Be("Qui");
        colunas[3].QuerySelector("h2")!.GetAttribute("aria-label").Should().Be("quinta-feira, 08/10/2026");

        var secoes = Secoes(semana);
        secoes.Should().HaveCount(7).And.OnlyContain(s => s.ClassName == "aj-secao-dia");
        secoes[3].QuerySelector("span.aj-secao-dia__sigla")!.TextContent.Should().Be("Qui");
    }
}

/// <summary>
/// O cartão de evento desenhado sozinho, nas cinco formas: a classe da forma e do tipo, o link para a ficha e as
/// linhas que cada forma mostra (os quadros).
/// </summary>
public class CartaoDeEventoTests : TelaInterativa
{
    private const string Destino = "/agenda/0199a3f2-7c1e-7000-8000-0000000a0001?visao=dia&data=2026-10-08";

    private IRenderedComponent<CartaoDeEvento> Cartao(
        EventoResumo evento, DateOnly dia, FormaDoCartao forma, string? classesDaRegua = null) =>
        Desenhar<CartaoDeEvento>(p => p
            .Add(c => c.Evento, evento)
            .Add(c => c.Dia, dia)
            .Add(c => c.Forma, forma)
            .Add(c => c.Href, Destino)
            .Add(c => c.ClassesDaRegua, classesDaRegua));

    private static string[] Linhas(IElement elemento) =>
        elemento.Children.Select(linha => $"{linha.ClassName}: {linha.TextContent}").ToArray();

    [Theory]
    [InlineData(FormaDoCartao.Coluna, "aj-evento aj-evento--coluna aj-evento--audiencia")]
    [InlineData(FormaDoCartao.Secao, "aj-evento aj-evento--audiencia")]
    [InlineData(FormaDoCartao.Folgado, "aj-evento aj-evento--folgado aj-evento--audiencia")]
    [InlineData(FormaDoCartao.Linha, "aj-evento aj-evento--linha aj-evento--audiencia")]
    [InlineData(FormaDoCartao.Mes, "aj-evento aj-evento--mes aj-evento--audiencia")]
    public void Cada_forma_e_um_link_para_a_ficha_com_a_classe_da_forma_e_do_tipo(FormaDoCartao forma, string classe)
    {
        var cartao = Cartao(QuadroDaAgenda.AudienciaDeInstrucao, new DateOnly(2026, 10, 5), forma);

        var link = cartao.Find("a");
        link.ClassName.Should().Be(classe);
        link.GetAttribute("href").Should().Be(Destino);
        link.QuerySelector(".aj-evento__titulo")!.TextContent.Should().Be("Audiência de instrução");
    }

    [Theory]
    [InlineData(TipoEvento.Audiencia, "aj-evento aj-evento--audiencia", "Audiência")]
    [InlineData(TipoEvento.Reuniao, "aj-evento aj-evento--reuniao", "Reunião")]
    [InlineData(TipoEvento.Prazo, "aj-evento aj-evento--prazo", "Prazo")]
    [InlineData(TipoEvento.Outro, "aj-evento aj-evento--outro", "Outro")]
    public void Cada_tipo_tem_a_sua_classe_e_o_seu_rotulo(TipoEvento tipo, string classe, string rotulo)
    {
        var evento = QuadroDaAgenda.Evento(new DateTime(2026, 10, 8, 9, 0, 0), 60, tipo, "Compromisso");

        var link = Cartao(evento, QuadroDaAgenda.Hoje, FormaDoCartao.Secao).Find("a");

        link.ClassName.Should().Be(classe);
        link.QuerySelector(".aj-evento__tipo")!.TextContent.Should().Be(rotulo);
    }

    [Fact]
    public void Folgado_tem_a_hora_em_coluna_local_e_cliente_numa_linha_e_os_participantes_na_outra()
    {
        var link = Cartao(QuadroDaAgenda.AudienciaDeInstrucao, new DateOnly(2026, 10, 5), FormaDoCartao.Folgado).Find("a");

        link.QuerySelector("span.aj-evento__hora > b")!.TextContent.Should().Be("14:00");
        link.QuerySelector("span.aj-evento__hora > span")!.TextContent.Should().Be("15:30");
        Linhas(link.QuerySelector("span.aj-evento__corpo")!).Should().Equal(
            "aj-evento__tipo: Audiência", "aj-evento__titulo: Audiência de instrução",
            "aj-evento__meta: 2ª Vara do Trabalho · Transportes Litoral Ltda.", "aj-evento__meta: Beatriz Lins");

        // Sem local, sem cliente e sem participantes: só o tipo e o título no corpo.
        var despacho = QuadroDaAgenda.Evento(new DateTime(2026, 10, 8, 8, 0, 0), 60, TipoEvento.Outro, "Despacho interno");
        Linhas(Cartao(despacho, QuadroDaAgenda.Hoje, FormaDoCartao.Folgado).Find("span.aj-evento__corpo")).Should().Equal(
            "aj-evento__tipo: Outro", "aj-evento__titulo: Despacho interno");
    }

    [Fact]
    public void Linha_da_regua_tem_tipo_horario_titulo_e_local_com_participantes_e_leva_as_classes_de_posicao()
    {
        var link = Cartao(QuadroDaAgenda.ProtocoloDeDocumentos, QuadroDaAgenda.Hoje, FormaDoCartao.Linha,
            "aj-regua__evento aj-regua__evento--inicio-6 aj-regua__evento--quartos-4").Find("a");

        link.ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--outro aj-regua__evento aj-regua__evento--inicio-6 aj-regua__evento--quartos-4");
        Linhas(link).Should().Equal(
            "aj-evento__tipo: Outro", "aj-evento__horario: 09:30 – 10:30", "aj-evento__titulo: Protocolo de documentos",
            "aj-evento__meta: Cartório do 1º Ofício · Otávio Brandão");
    }

    [Fact]
    public void Item_do_mes_tem_o_tipo_so_para_leitor_a_hora_de_inicio_no_primeiro_dia_e_o_titulo()
    {
        var link = Cartao(QuadroDaAgenda.PautaDaSemana, new DateOnly(2026, 10, 5), FormaDoCartao.Mes).Find("a");

        Linhas(link).Should().Equal("aj-so-leitor: Reunião, ", "aj-evento__horario: 09:00", "aj-evento__titulo: Pauta da semana");

        // Evento de vários dias: nos dias seguintes ao do início, sem hora.
        var plantao = QuadroDaAgenda.Evento(new DateTime(2026, 10, 6, 20, 0, 0), 38 * 60, TipoEvento.Prazo, "Plantão de protocolo");
        Linhas(Cartao(plantao, new DateOnly(2026, 10, 7), FormaDoCartao.Mes).Find("a")).Should().Equal(
            "aj-so-leitor: Prazo, ", "aj-evento__titulo: Plantão de protocolo");
    }

    [Fact]
    public void Participante_sem_nome_aparece_como_usuario_nao_encontrado()
    {
        var evento = QuadroDaAgenda.Evento(new DateTime(2026, 10, 8, 9, 0, 0), 60, TipoEvento.Reuniao, "Reunião de equipe",
            "Sala de reuniões", null, "Marina Couto", "");

        var link = Cartao(evento, QuadroDaAgenda.Hoje, FormaDoCartao.Coluna).Find("a");

        link.QuerySelectorAll(".aj-evento__meta").Select(m => m.TextContent).Should().Equal("Marina Couto, Usuário não encontrado");
    }
}
