using System.Globalization;
using System.Net;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Components.Agenda;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A lógica de calendário da agenda, sem tela: o período lido do endereço, a régua de horas do Dia e os rótulos das
/// três visões (e o <c>AvisoDeEventoSalvo</c>, desenhado sem circuito, que mostra a redação do conflito do desktop).
/// Hoje é quinta-feira, 08/10/2026 (o dia dos quadros); a semana dos quadros vai de 05/10 a 11/10.
/// </summary>
public class AgendaPuraTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 8);
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    private static DateOnly D(string data) => DateOnly.ParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private EventoResumo Evento(string titulo, DateTime inicioLocal, int minutos)
    {
        var inicio = _fuso.ParaUtc(inicioLocal);
        return new EventoResumo(Guid.NewGuid(), titulo, TipoEvento.Reuniao, inicio, inicio.AddMinutes(minutos), null, null, null, []);
    }

    private static EventoResumo Cartao(string? local, string? cliente, params string[] participantes) =>
        new(Guid.NewGuid(), "Evento", TipoEvento.Reuniao, new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 8, 13, 0, 0, DateTimeKind.Utc), local, cliente, null, participantes);

    private SobreposicaoDeAgenda Sobreposicao(string? nome, DateTime inicioLocal, DateTime fimLocal) =>
        new(Guid.NewGuid(), nome, _fuso.ParaUtc(inicioLocal), _fuso.ParaUtc(fimLocal));

    private EventoDetalhe Detalhe(TipoEvento tipo, DateTime inicioLocal, int minutos, string? local)
    {
        var inicio = _fuso.ParaUtc(inicioLocal);
        return new EventoDetalhe(Guid.NewGuid(), "Audiência de instrução — Litoral", null, local, tipo, inicio, minutos,
            inicio.AddMinutes(minutos), null, null, null, null, null, null, Guid.NewGuid(), "Marina Couto", [],
            new AcoesDoEvento(true, true), 1);
    }

    // ---- Período no endereço ----

    [Theory]
    [InlineData(null, null, null, "2026-10-08", false)]
    [InlineData("semana", null, VisaoDaAgenda.Semana, "2026-10-08", false)]
    [InlineData("dia", "2026-10-06", VisaoDaAgenda.Dia, "2026-10-06", true)]
    [InlineData("mes", "2026-08-15", VisaoDaAgenda.Mes, "2026-08-15", true)]
    [InlineData(null, "2027-02-10", null, "2027-02-10", true)]
    [InlineData(null, "2000-01-01", null, "2000-01-01", true)]
    [InlineData(null, "2100-12-31", null, "2100-12-31", true)]
    public void Ler_aceita_as_visoes_e_as_datas_do_endereco(
        string? visao, string? data, VisaoDaAgenda? visaoLida, string dataLida, bool noEndereco)
    {
        PeriodoDaAgenda.Ler(visao, data, Hoje).Should().Be(new PeriodoDaAgenda(visaoLida, D(dataLida), noEndereco));
    }

    // Visão desconhecida conta como ausente; data inválida ou fora de 2000–2100 vira hoje. Nada disso é erro.
    [Theory]
    [InlineData("xyz", null, null, "2026-10-08", false)]
    [InlineData("Semana", null, null, "2026-10-08", false)]
    [InlineData("xyz", "2026-10-06", null, "2026-10-06", true)]
    [InlineData("dia", "2026-13-40", VisaoDaAgenda.Dia, "2026-10-08", false)]
    [InlineData(null, "1999-01-01", null, "2026-10-08", false)]
    [InlineData(null, "2101-01-01", null, "2026-10-08", false)]
    [InlineData(null, "08/10/2026", null, "2026-10-08", false)]
    [InlineData("", "", null, "2026-10-08", false)]
    public void Ler_ignora_o_que_nao_entende(
        string? visao, string? data, VisaoDaAgenda? visaoLida, string dataLida, bool noEndereco)
    {
        PeriodoDaAgenda.Ler(visao, data, Hoje).Should().Be(new PeriodoDaAgenda(visaoLida, D(dataLida), noEndereco));
    }

    // Review Focus 1: endereço estranho abre o padrão (hoje; Semana no desktop e no tablet, Dia no mobile), com a
    // consulta da semana de hoje e o endereço limpo.
    [Theory]
    [InlineData(null, null)]
    [InlineData("xyz", null)]
    [InlineData(null, "2026-13-40")]
    [InlineData(null, "1999-01-01")]
    public void Endereco_estranho_abre_o_padrao(string? visao, string? data)
    {
        var periodo = PeriodoDaAgenda.Ler(visao, data, Hoje);

        periodo.NoDesktopETablet.Should().Be(VisaoDaAgenda.Semana);
        periodo.NoMobile.Should().Be(VisaoDaAgenda.Dia);
        periodo.Data.Should().Be(Hoje);
        periodo.InicioDaConsulta.Should().Be(new DateOnly(2026, 10, 5));
        periodo.FimDaConsulta.Should().Be(new DateOnly(2026, 10, 12));
        periodo.Consulta.Should().BeEmpty();
        periodo.Endereco.Should().Be("/agenda");
    }

    [Theory]
    [InlineData(null, VisaoDaAgenda.Semana, VisaoDaAgenda.Dia)]
    [InlineData("dia", VisaoDaAgenda.Dia, VisaoDaAgenda.Dia)]
    [InlineData("semana", VisaoDaAgenda.Semana, VisaoDaAgenda.Semana)]
    [InlineData("mes", VisaoDaAgenda.Mes, VisaoDaAgenda.Mes)]
    public void Sem_visao_cada_largura_tem_o_seu_padrao_e_com_visao_todas_mostram_a_mesma(
        string? visao, VisaoDaAgenda noDesktopETablet, VisaoDaAgenda noMobile)
    {
        var periodo = PeriodoDaAgenda.Ler(visao, "2026-10-08", Hoje);

        periodo.NoDesktopETablet.Should().Be(noDesktopETablet);
        periodo.NoMobile.Should().Be(noMobile);
    }

    [Theory]
    [InlineData(null, "2026-10-08", "2026-10-05", "2026-10-12")]
    [InlineData("semana", "2026-10-11", "2026-10-05", "2026-10-12")]
    [InlineData("dia", "2026-10-08", "2026-10-05", "2026-10-12")]
    [InlineData("semana", "2026-12-31", "2026-12-28", "2027-01-04")]
    [InlineData("mes", "2026-10-08", "2026-09-28", "2026-11-02")]
    [InlineData("mes", "2026-08-15", "2026-07-27", "2026-09-07")]
    [InlineData("mes", "2027-02-10", "2027-02-01", "2027-03-01")]
    public void Consulta_cobre_a_semana_da_data_ou_as_semanas_inteiras_do_mes(
        string? visao, string data, string inicio, string fim)
    {
        var periodo = PeriodoDaAgenda.Ler(visao, data, Hoje);

        periodo.InicioDaConsulta.Should().Be(D(inicio));
        periodo.FimDaConsulta.Should().Be(D(fim));
        // Dentro do limite de 92 dias do ListarEventos: de 7 (semana) a 42 (mês de seis semanas).
        (periodo.FimDaConsulta.DayNumber - periodo.InicioDaConsulta.DayNumber).Should().BeOneOf(7, 28, 35, 42);
    }

    [Fact]
    public void Semana_vai_de_segunda_a_domingo_da_data()
    {
        var esperada = Enumerable.Range(5, 7).Select(dia => new DateOnly(2026, 10, dia)).ToList();

        PeriodoDaAgenda.Ler(null, "2026-10-08", Hoje).Semana.Should().Equal(esperada);
        PeriodoDaAgenda.Ler("mes", "2026-10-11", Hoje).Semana.Should().Equal(esperada);
        PeriodoDaAgenda.Ler(null, "2026-12-31", Hoje).Semana.Should().Equal(
            Enumerable.Range(0, 7).Select(i => new DateOnly(2026, 12, 28).AddDays(i)));
    }

    [Theory]
    [InlineData("2026-10-08", 35, "2026-09-28", "2026-11-01")]
    [InlineData("2026-08-15", 42, "2026-07-27", "2026-09-06")]
    [InlineData("2027-02-10", 28, "2027-02-01", "2027-02-28")]
    public void Grade_do_mes_tem_semanas_inteiras_de_segunda_a_domingo(string data, int dias, string primeiro, string ultimo)
    {
        var grade = PeriodoDaAgenda.Ler("mes", data, Hoje).SemanasDoMes;

        grade.Should().HaveCount(dias);
        grade[0].Should().Be(D(primeiro));
        grade[^1].Should().Be(D(ultimo));
        grade[0].DayOfWeek.Should().Be(DayOfWeek.Monday);
        grade.Zip(grade.Skip(1)).Should().OnlyContain(par => par.Second == par.First.AddDays(1));
        // A grade é a do mês da data, qualquer que seja a visão.
        PeriodoDaAgenda.Ler(null, data, Hoje).SemanasDoMes.Should().Equal(grade);
    }

    [Theory]
    [InlineData("2026-10-01", true)]
    [InlineData("2026-10-31", true)]
    [InlineData("2026-09-30", false)]
    [InlineData("2026-11-01", false)]
    [InlineData("2025-10-15", false)]
    public void NoMes_diz_se_o_dia_e_do_mes_da_data(string dia, bool esperado) =>
        PeriodoDaAgenda.Ler("mes", "2026-10-08", Hoje).NoMes(D(dia)).Should().Be(esperado);

    [Theory]
    [InlineData(null, null, "", "/agenda")]
    [InlineData("semana", null, "?visao=semana", "/agenda?visao=semana")]
    [InlineData(null, "2026-10-08", "?data=2026-10-08", "/agenda?data=2026-10-08")]
    [InlineData("dia", "2026-10-08", "?visao=dia&data=2026-10-08", "/agenda?visao=dia&data=2026-10-08")]
    [InlineData("mes", "2026-10-08", "?visao=mes&data=2026-10-08", "/agenda?visao=mes&data=2026-10-08")]
    [InlineData("xyz", "2026-13-40", "", "/agenda")]
    public void Consulta_e_endereco_levam_so_o_que_esta_no_endereco(
        string? visao, string? data, string consulta, string endereco)
    {
        var periodo = PeriodoDaAgenda.Ler(visao, data, Hoje);

        periodo.Consulta.Should().Be(consulta);
        periodo.Endereco.Should().Be(endereco);
    }

    [Fact]
    public void Trocar_a_visao_mantem_a_data_e_trocar_a_data_a_poe_no_endereco()
    {
        PeriodoDaAgenda.Ler(null, null, Hoje).ComVisao(VisaoDaAgenda.Mes)
            .Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Mes, Hoje, false));
        PeriodoDaAgenda.Ler(null, null, Hoje).ComVisao(VisaoDaAgenda.Mes).Consulta.Should().Be("?visao=mes");
        PeriodoDaAgenda.Ler(null, "2026-10-06", Hoje).ComVisao(VisaoDaAgenda.Dia).Consulta
            .Should().Be("?visao=dia&data=2026-10-06");

        PeriodoDaAgenda.Ler("semana", null, Hoje).ComData(new DateOnly(2026, 10, 6))
            .Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Semana, new DateOnly(2026, 10, 6), true));
        PeriodoDaAgenda.Ler(null, null, Hoje).ComData(Hoje).Consulta.Should().Be("?data=2026-10-08");
    }

    [Theory]
    [InlineData("2026-10-08", VisaoDaAgenda.Dia, "2026-10-07", "2026-10-09")]
    [InlineData("2026-10-08", VisaoDaAgenda.Semana, "2026-10-01", "2026-10-15")]
    [InlineData("2026-10-08", VisaoDaAgenda.Mes, "2026-09-08", "2026-11-08")]
    [InlineData("2027-01-31", VisaoDaAgenda.Mes, "2026-12-31", "2027-02-28")]
    [InlineData("2026-03-31", VisaoDaAgenda.Mes, "2026-02-28", "2026-04-30")]
    [InlineData("2028-01-31", VisaoDaAgenda.Mes, "2027-12-31", "2028-02-29")]
    [InlineData("2026-12-31", VisaoDaAgenda.Dia, "2026-12-30", "2027-01-01")]
    [InlineData("2026-12-28", VisaoDaAgenda.Semana, "2026-12-21", "2027-01-04")]
    public void Anterior_e_proximo_andam_um_dia_uma_semana_ou_um_mes(
        string data, VisaoDaAgenda unidade, string anterior, string proximo)
    {
        var periodo = PeriodoDaAgenda.Ler("semana", data, Hoje);

        // A visão fica; a data passa a estar no endereço. Um mês a partir de 31/01 cai no último dia de fevereiro.
        periodo.Anterior(unidade).Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Semana, D(anterior), true));
        periodo.Proximo(unidade).Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Semana, D(proximo), true));
    }

    [Fact]
    public void Andar_sem_data_no_endereco_poe_a_data_e_mantem_a_ausencia_de_visao()
    {
        var seguinte = PeriodoDaAgenda.Ler(null, null, Hoje).Proximo(VisaoDaAgenda.Semana);

        seguinte.Should().Be(new PeriodoDaAgenda(null, new DateOnly(2026, 10, 15), true));
        seguinte.Consulta.Should().Be("?data=2026-10-15");
        PeriodoDaAgenda.Ler("mes", "2026-10-08", Hoje).Proximo(VisaoDaAgenda.Mes).Endereco
            .Should().Be("/agenda?visao=mes&data=2026-11-08");
    }

    [Fact]
    public void Hoje_volta_a_data_de_hoje_e_mantem_a_visao()
    {
        var periodo = PeriodoDaAgenda.Ler("mes", "2027-02-10", Hoje).ParaHoje(Hoje);

        periodo.Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Mes, Hoje, true));
        periodo.Endereco.Should().Be("/agenda?visao=mes&data=2026-10-08");
    }

    [Fact]
    public void Ficha_do_evento_leva_a_origem()
    {
        var id = Guid.Parse("0199a3f2-7c1e-7000-8000-000000000001");

        PeriodoDaAgenda.Ficha(id, "?visao=dia&data=2026-10-08")
            .Should().Be("/agenda/0199a3f2-7c1e-7000-8000-000000000001?visao=dia&data=2026-10-08");
        PeriodoDaAgenda.Ficha(id, "").Should().Be("/agenda/0199a3f2-7c1e-7000-8000-000000000001");
    }

    // A ficha e o formulário repassam o que receberam no endereço, só o que a agenda aceitaria.
    [Theory]
    [InlineData("dia", "2026-10-08", "?visao=dia&data=2026-10-08")]
    [InlineData("semana", null, "?visao=semana")]
    [InlineData(null, "2026-10-08", "?data=2026-10-08")]
    [InlineData("xyz", "2026-10-08", "?data=2026-10-08")]
    [InlineData("mes", "1999-01-01", "?visao=mes")]
    [InlineData("dia&x=1", "2026-10-08&x=1", "")]
    [InlineData(null, null, "")]
    public void Consulta_repassada_so_leva_valores_validos(string? visao, string? data, string consulta)
    {
        PeriodoDaAgenda.ConsultaDeOrigem(visao, data).Should().Be(consulta);
    }

    // ---- Régua do Dia ----

    [Fact]
    public void Regua_padrao_vai_das_8_as_19_e_posiciona_o_dia_do_quadro()
    {
        var protocolo = Evento("Protocolo de documentos", new DateTime(2026, 10, 8, 9, 30, 0), 60);
        var revisao = Evento("Revisão de minutas", new DateTime(2026, 10, 8, 15, 0, 0), 60);
        var deOntem = Evento("Alinhamento com o cliente", new DateTime(2026, 10, 7, 10, 0, 0), 90);

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [deOntem, protocolo, revisao]);

        regua.HoraInicial.Should().Be(8);
        regua.HoraFinal.Should().Be(19);
        regua.Horas.Should().Be(11);
        // 09:30 é o sexto quarto depois das 08:00 e uma hora são quatro quartos; o evento de outro dia não entra.
        regua.Eventos.Should().Equal(new EventoNaRegua(protocolo, 6, 4, 1, 1), new EventoNaRegua(revisao, 28, 4, 1, 1));
    }

    [Fact]
    public void Dia_sem_eventos_tem_a_regua_padrao()
    {
        var regua = ReguaDoDia.Montar(_fuso, new DateOnly(2026, 10, 10), []);

        regua.HoraInicial.Should().Be(8);
        regua.HoraFinal.Should().Be(19);
        regua.Horas.Should().Be(11);
        regua.Eventos.Should().BeEmpty();
    }

    [Fact]
    public void Regua_se_estende_ate_a_hora_cheia_do_primeiro_inicio_e_a_seguinte_ao_ultimo_fim()
    {
        var cedo = Evento("Cedo", new DateTime(2026, 10, 8, 6, 30, 0), 30);       // 06:30 a 07:00
        var tarde = Evento("Tarde", new DateTime(2026, 10, 8, 19, 30, 0), 45);    // 19:30 a 20:15

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [cedo, tarde]);

        regua.HoraInicial.Should().Be(6);
        regua.HoraFinal.Should().Be(21);
        regua.Horas.Should().Be(15);
        regua.Eventos.Should().Equal(new EventoNaRegua(cedo, 2, 2, 1, 1), new EventoNaRegua(tarde, 54, 3, 1, 1));
    }

    [Fact]
    public void Fim_em_hora_cheia_nao_estende_uma_hora_a_mais()
    {
        var setes = Evento("Às sete", new DateTime(2026, 10, 8, 7, 0, 0), 60);     // 07:00 a 08:00
        var ateVinte = Evento("Até as vinte", new DateTime(2026, 10, 8, 17, 0, 0), 180);   // 17:00 a 20:00

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [setes, ateVinte]);

        regua.HoraInicial.Should().Be(7);
        regua.HoraFinal.Should().Be(20);
        regua.Eventos.Should().Equal(new EventoNaRegua(setes, 0, 4, 1, 1), new EventoNaRegua(ateVinte, 40, 12, 1, 1));
    }

    // Review Focus 2: o evento entra em cada dia que toca, recortado ao dia; a régua vai então a 24:00 ou a 00:00.
    [Fact]
    public void Evento_que_atravessa_a_meia_noite_entra_recortado_em_cada_dia()
    {
        var plantao = Evento("Plantão", new DateTime(2026, 10, 7, 23, 30, 0), 60);              // 23:30 a 00:30
        var ateMeiaNoite = Evento("Até a meia-noite", new DateTime(2026, 10, 7, 22, 0, 0), 120); // 22:00 a 00:00
        var tresDias = Evento("Três dias", new DateTime(2026, 10, 7, 22, 0, 0), 28 * 60);        // quarta 22:00 a sexta 02:00

        var plantaoNaQuarta = ReguaDoDia.Montar(_fuso, new DateOnly(2026, 10, 7), [plantao]);
        var plantaoNaQuinta = ReguaDoDia.Montar(_fuso, Hoje, [plantao]);
        var ateMeiaNoiteNaQuarta = ReguaDoDia.Montar(_fuso, new DateOnly(2026, 10, 7), [ateMeiaNoite]);
        var ateMeiaNoiteNaQuinta = ReguaDoDia.Montar(_fuso, Hoje, [ateMeiaNoite]);
        var tresDiasNaQuinta = ReguaDoDia.Montar(_fuso, Hoje, [tresDias]);

        plantaoNaQuarta.HoraInicial.Should().Be(8);
        plantaoNaQuarta.HoraFinal.Should().Be(24);
        plantaoNaQuarta.Eventos.Should().Equal(new EventoNaRegua(plantao, 62, 2, 1, 1));
        plantaoNaQuinta.HoraInicial.Should().Be(0);
        plantaoNaQuinta.HoraFinal.Should().Be(19);
        plantaoNaQuinta.Eventos.Should().Equal(new EventoNaRegua(plantao, 0, 2, 1, 1));
        // Quem termina à meia-noite em ponto não aparece no dia seguinte.
        ateMeiaNoiteNaQuarta.HoraFinal.Should().Be(24);
        ateMeiaNoiteNaQuarta.Eventos.Should().Equal(new EventoNaRegua(ateMeiaNoite, 56, 8, 1, 1));
        ateMeiaNoiteNaQuinta.Eventos.Should().BeEmpty();
        ateMeiaNoiteNaQuinta.HoraFinal.Should().Be(19);
        // No dia do meio, o evento ocupa o dia inteiro: 96 quartos.
        tresDiasNaQuinta.HoraInicial.Should().Be(0);
        tresDiasNaQuinta.HoraFinal.Should().Be(24);
        tresDiasNaQuinta.Horas.Should().Be(24);
        tresDiasNaQuinta.Eventos.Should().Equal(new EventoNaRegua(tresDias, 0, 96, 1, 1));
    }

    // Review Focus 3: o evento curto ganha a altura mínima de dois quartos; o início arredonda para baixo e o fim para
    // cima; o curto no fim do dia sobe para caber.
    [Fact]
    public void Evento_curto_ganha_dois_quartos_e_horario_quebrado_arredonda_para_fora()
    {
        var quinze = Evento("Quinze minutos", new DateTime(2026, 10, 8, 10, 0, 0), 15);     // 10:00 a 10:15
        var quebrado = Evento("Quebrado", new DateTime(2026, 10, 8, 14, 5, 0), 30);         // 14:05 a 14:35
        var noFim = Evento("No fim do dia", new DateTime(2026, 10, 8, 23, 50, 0), 10);      // 23:50 a 00:00

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [quinze, quebrado, noFim]);

        regua.HoraInicial.Should().Be(8);
        regua.HoraFinal.Should().Be(24);
        regua.Eventos.Should().Equal(
            new EventoNaRegua(quinze, 8, 2, 1, 1),
            new EventoNaRegua(quebrado, 24, 3, 1, 1),
            new EventoNaRegua(noFim, 62, 2, 1, 1));
    }

    // Review Focus 3: dois simultâneos lado a lado; o evento depois do grupo volta à largura inteira.
    [Fact]
    public void Simultaneos_dividem_a_largura_e_o_seguinte_volta_a_largura_inteira()
    {
        var alinhamento = Evento("Alinhamento com o cliente", new DateTime(2026, 10, 8, 10, 0, 0), 90);  // 10:00 a 11:30
        var conciliacao = Evento("Audiência de conciliação", new DateTime(2026, 10, 8, 11, 0, 0), 60);   // 11:00 a 12:00
        var depois = Evento("Depois do grupo", new DateTime(2026, 10, 8, 13, 0, 0), 60);                 // 13:00 a 14:00

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [alinhamento, conciliacao, depois]);

        regua.Eventos.Should().Equal(
            new EventoNaRegua(alinhamento, 8, 6, 1, 2),
            new EventoNaRegua(conciliacao, 12, 4, 2, 2),
            new EventoNaRegua(depois, 20, 4, 1, 1));
    }

    [Fact]
    public void Evento_ocupa_a_primeira_coluna_livre_do_grupo()
    {
        var primeiro = Evento("Primeiro", new DateTime(2026, 10, 8, 10, 0, 0), 60);    // 10:00 a 11:00
        var segundo = Evento("Segundo", new DateTime(2026, 10, 8, 10, 30, 0), 60);     // 10:30 a 11:30
        var terceiro = Evento("Terceiro", new DateTime(2026, 10, 8, 11, 0, 0), 60);    // 11:00 a 12:00

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [primeiro, segundo, terceiro]);

        // Os três formam um grupo (cada um encosta no seguinte); o terceiro reaproveita a coluna do primeiro.
        regua.Eventos.Should().Equal(
            new EventoNaRegua(primeiro, 8, 4, 1, 2),
            new EventoNaRegua(segundo, 10, 4, 2, 2),
            new EventoNaRegua(terceiro, 12, 4, 1, 2));
    }

    [Fact]
    public void Cinco_simultaneos_ficam_em_quatro_colunas_e_o_quinto_divide_a_ultima()
    {
        var eventos = Enumerable.Range(1, 5)
            .Select(n => Evento($"Simultâneo {n}", new DateTime(2026, 10, 8, 10, 0, 0), 60)).ToList();

        var regua = ReguaDoDia.Montar(_fuso, Hoje, eventos);

        regua.Eventos.Should().Equal(
            new EventoNaRegua(eventos[0], 8, 4, 1, 4),
            new EventoNaRegua(eventos[1], 8, 4, 2, 4),
            new EventoNaRegua(eventos[2], 8, 4, 3, 4),
            new EventoNaRegua(eventos[3], 8, 4, 4, 4),
            new EventoNaRegua(eventos[4], 8, 4, 4, 4));
    }

    [Fact]
    public void Eventos_do_dia_sao_os_que_tocam_o_dia_na_ordem_recebida()
    {
        var tarde = Evento("Tarde", new DateTime(2026, 10, 8, 15, 0, 0), 60);
        var deOntem = Evento("De ontem", new DateTime(2026, 10, 7, 10, 0, 0), 60);
        var plantao = Evento("Plantão", new DateTime(2026, 10, 7, 23, 30, 0), 60);               // 23:30 a 00:30
        var ateMeiaNoite = Evento("Até a meia-noite", new DateTime(2026, 10, 7, 22, 0, 0), 120);  // 22:00 a 00:00
        var manha = Evento("Manhã", new DateTime(2026, 10, 8, 9, 0, 0), 60);
        var deAmanha = Evento("De amanhã", new DateTime(2026, 10, 9, 9, 0, 0), 60);

        var doDia = RotulosDeAgenda.EventosDoDia(_fuso, [tarde, deOntem, plantao, ateMeiaNoite, manha, deAmanha], Hoje);

        // Quem atravessa a meia-noite entra nos dois dias; quem termina à meia-noite em ponto não entra no seguinte.
        doDia.Should().Equal(tarde, plantao, manha);
        RotulosDeAgenda.EventosDoDia(_fuso, [], Hoje).Should().BeEmpty();
    }

    [Theory]
    [InlineData("2026-10-08", "08")]
    [InlineData("2026-10-11", "11")]
    [InlineData("2026-10-31", "31")]
    [InlineData("2026-11-01", "01")]
    public void Numero_do_dia_tem_dois_algarismos(string dia, string numero)
    {
        RotulosDeAgenda.NumeroDoDia(D(dia)).Should().Be(numero);
    }
    // ---- Rótulos ----

    [Theory]
    [InlineData(5, "Seg", "Segunda", "Segunda-feira")]
    [InlineData(6, "Ter", "Terça", "Terça-feira")]
    [InlineData(7, "Qua", "Quarta", "Quarta-feira")]
    [InlineData(8, "Qui", "Quinta", "Quinta-feira")]
    [InlineData(9, "Sex", "Sexta", "Sexta-feira")]
    [InlineData(10, "Sáb", "Sábado", "Sábado")]
    [InlineData(11, "Dom", "Domingo", "Domingo")]
    public void Nomes_do_dia_da_semana(int dia, string sigla, string nome, string nomeLongo)
    {
        var data = new DateOnly(2026, 10, dia);

        RotulosDeAgenda.SiglaDoDia(data).Should().Be(sigla);
        RotulosDeAgenda.NomeDoDia(data).Should().Be(nome);
        RotulosDeAgenda.NomeLongoDoDia(data).Should().Be(nomeLongo);
    }

    [Fact]
    public void Rotulos_do_dia_para_leitor_de_tela()
    {
        RotulosDeAgenda.RotuloLongo(Hoje, hoje: true).Should().Be("quinta-feira, 08/10/2026, hoje");
        RotulosDeAgenda.RotuloLongo(new DateOnly(2026, 10, 10), hoje: false).Should().Be("sábado, 10/10/2026");
        RotulosDeAgenda.RotuloCurto(Hoje, hoje: true).Should().Be("Quinta, 08/10, hoje");
        RotulosDeAgenda.RotuloCurto(new DateOnly(2026, 10, 5), hoje: false).Should().Be("Segunda, 05/10");
    }

    [Fact]
    public void Subtitulos_de_cada_visao()
    {
        RotulosDeAgenda.SubtituloDaSemana(new DateOnly(2026, 10, 5)).Should().Be("Semana de 05/10 a 11/10/2026");
        RotulosDeAgenda.SubtituloDaSemana(new DateOnly(2026, 9, 28)).Should().Be("Semana de 28/09 a 04/10/2026");
        RotulosDeAgenda.SubtituloDaSemana(new DateOnly(2026, 12, 28)).Should().Be("Semana de 28/12/2026 a 03/01/2027");
        RotulosDeAgenda.SubtituloDoDia(Hoje).Should().Be("Quinta-feira, 08/10/2026");
        RotulosDeAgenda.SubtituloDoDia(new DateOnly(2026, 10, 11)).Should().Be("Domingo, 11/10/2026");
        RotulosDeAgenda.SubtituloDoMes(Hoje).Should().Be("Outubro de 2026");
        RotulosDeAgenda.SubtituloDoMes(new DateOnly(2026, 3, 1)).Should().Be("Março de 2026");
        RotulosDeAgenda.SubtituloDoMes(new DateOnly(2027, 2, 10)).Should().Be("Fevereiro de 2027");
        RotulosDeAgenda.MesDoMobile(Hoje).Should().Be("Outubro 2026");
        RotulosDeAgenda.MesDoMobile(new DateOnly(2027, 2, 10)).Should().Be("Fevereiro 2027");
    }

    [Theory]
    [InlineData(0, "0 eventos", "0 minutos")]
    [InlineData(1, "1 evento", "1 minuto")]
    [InlineData(2, "2 eventos", "2 minutos")]
    [InlineData(90, "90 eventos", "90 minutos")]
    public void Total_de_eventos_e_duracao_no_numero_certo(int quantidade, string eventos, string duracao)
    {
        RotulosDeAgenda.TotalDeEventos(quantidade).Should().Be(eventos);
        RotulosDeAgenda.Duracao(quantidade).Should().Be(duracao);
    }

    [Fact]
    public void Hora_do_cartao_mostra_reticencias_do_lado_que_cai_em_outro_dia()
    {
        var quarta = new DateOnly(2026, 10, 7);
        var plantao = Evento("Plantão", new DateTime(2026, 10, 7, 23, 30, 0), 60);               // 23:30 a 00:30
        var ateMeiaNoite = Evento("Até a meia-noite", new DateTime(2026, 10, 7, 22, 0, 0), 120);  // 22:00 a 00:00
        var tresDias = Evento("Três dias", new DateTime(2026, 10, 7, 22, 0, 0), 28 * 60);         // quarta 22:00 a sexta 02:00
        var comum = Evento("Comum", new DateTime(2026, 10, 8, 10, 0, 0), 60);

        RotulosDeAgenda.HoraDeInicio(_fuso, plantao, quarta).Should().Be("23:30");
        RotulosDeAgenda.HoraDeFim(_fuso, plantao, quarta).Should().Be("…");
        RotulosDeAgenda.HoraDeInicio(_fuso, plantao, Hoje).Should().Be("…");
        RotulosDeAgenda.HoraDeFim(_fuso, plantao, Hoje).Should().Be("00:30");
        // Terminar à meia-noite em ponto é terminar no mesmo dia.
        RotulosDeAgenda.HoraDeFim(_fuso, ateMeiaNoite, quarta).Should().Be("00:00");
        RotulosDeAgenda.HoraDeInicio(_fuso, tresDias, Hoje).Should().Be("…");
        RotulosDeAgenda.HoraDeFim(_fuso, tresDias, Hoje).Should().Be("…");
        RotulosDeAgenda.HoraDeInicio(_fuso, comum, Hoje).Should().Be("10:00");
        RotulosDeAgenda.HoraDeFim(_fuso, comum, Hoje).Should().Be("11:00");
    }

    [Fact]
    public void Linhas_do_cartao_por_largura()
    {
        var audiencia = Cartao("2ª Vara do Trabalho", "Transportes Litoral Ltda.", "Beatriz Lins");
        var prazo = Cartao(null, "Construtora Serra Azul Ltda.", "Rafael Teles");
        var pauta = Cartao("Sala de reuniões", null, "Marina Couto", "Rafael Teles", "Beatriz Lins");
        var nada = Cartao("  ", null);
        var comSumido = Cartao(null, null, "", "Pedro Produção");

        // Tablet: uma linha só, o local ou, sem local, o cliente.
        RotulosDeAgenda.LinhaDoTablet(audiencia).Should().Be("2ª Vara do Trabalho");
        RotulosDeAgenda.LinhaDoTablet(prazo).Should().Be("Construtora Serra Azul Ltda.");
        RotulosDeAgenda.LinhaDoTablet(nada).Should().BeNull();
        // Mobile: local e cliente juntos, o que houver.
        RotulosDeAgenda.LinhaDoMobile(audiencia).Should().Be("2ª Vara do Trabalho · Transportes Litoral Ltda.");
        RotulosDeAgenda.LinhaDoMobile(prazo).Should().Be("Construtora Serra Azul Ltda.");
        RotulosDeAgenda.LinhaDoMobile(pauta).Should().Be("Sala de reuniões");
        RotulosDeAgenda.LinhaDoMobile(nada).Should().BeNull();
        // Régua: local, cliente e participantes.
        RotulosDeAgenda.LinhaDaRegua(audiencia).Should().Be("2ª Vara do Trabalho · Transportes Litoral Ltda. · Beatriz Lins");
        RotulosDeAgenda.LinhaDaRegua(pauta).Should().Be("Sala de reuniões · Marina Couto, Rafael Teles, Beatriz Lins");
        RotulosDeAgenda.LinhaDaRegua(nada).Should().BeNull();
        // Participantes na ordem recebida (a do servidor); o usuário que não é mais encontrado (nome vazio) aparece como
        // tal, também na linha da régua.
        RotulosDeAgenda.Participantes(pauta).Should().Be("Marina Couto, Rafael Teles, Beatriz Lins");
        RotulosDeAgenda.Participantes(comSumido).Should().Be("Usuário não encontrado, Pedro Produção");
        RotulosDeAgenda.LinhaDaRegua(comSumido).Should().Be("Usuário não encontrado, Pedro Produção");
        RotulosDeAgenda.Participantes(nada).Should().BeNull();
    }

    [Fact]
    public void Titulo_do_dia_no_desktop_e_no_mobile()
    {
        RotulosDeAgenda.TituloDoDiaNoDesktop(Hoje, hoje: true).Should().Be("Quinta-feira · hoje");
        RotulosDeAgenda.TituloDoDiaNoDesktop(new DateOnly(2026, 10, 10), hoje: false).Should().Be("Sábado");
        RotulosDeAgenda.TituloDoDiaNoMobile(Hoje, hoje: true).Should().Be("Quinta, 08/10 · hoje");
        RotulosDeAgenda.TituloDoDiaNoMobile(new DateOnly(2026, 10, 5), hoje: false).Should().Be("Segunda, 05/10");
    }

    [Fact]
    public void Resumo_do_evento_na_ficha()
    {
        var audiencia = Detalhe(TipoEvento.Audiencia, new DateTime(2026, 10, 7, 9, 0, 0), 90, "Fórum Trabalhista, sala 4");
        var semLocal = Detalhe(TipoEvento.Audiencia, new DateTime(2026, 10, 7, 9, 0, 0), 90, null);
        var plantao = Detalhe(TipoEvento.Reuniao, new DateTime(2026, 10, 7, 23, 30, 0), 60, "Sala de reuniões");
        var ateMeiaNoite = Detalhe(TipoEvento.Prazo, new DateTime(2026, 10, 7, 22, 0, 0), 120, null);

        RotulosDeAgenda.ResumoDoEvento(_fuso, audiencia, comLocal: true)
            .Should().Be("Audiência · 07/10/2026, 09:00 – 10:30 · Fórum Trabalhista, sala 4");
        RotulosDeAgenda.ResumoDoEvento(_fuso, audiencia, comLocal: false).Should().Be("Audiência · 07/10/2026, 09:00 – 10:30");
        RotulosDeAgenda.ResumoDoEvento(_fuso, semLocal, comLocal: true).Should().Be("Audiência · 07/10/2026, 09:00 – 10:30");
        RotulosDeAgenda.ResumoDoEvento(_fuso, plantao, comLocal: true)
            .Should().Be("Reunião · 07/10/2026, 23:30 – 08/10 00:30 · Sala de reuniões");
        RotulosDeAgenda.ResumoDoEvento(_fuso, ateMeiaNoite, comLocal: true).Should().Be("Prazo · 07/10/2026, 22:00 – 00:00");
    }

    [Fact]
    public void Conflito_com_uma_pessoa_nas_tres_redacoes()
    {
        SobreposicaoDeAgenda[] uma = [Sobreposicao("Rafael Teles", new DateTime(2026, 10, 7, 10, 0, 0), new DateTime(2026, 10, 7, 11, 30, 0))];

        RotulosDeAgenda.ConflitoNoDesktop(_fuso, uma).Should().Be("Atenção: Rafael Teles já tem evento das 10:00 às 11:30 (07/10).");
        RotulosDeAgenda.ConflitoNoTablet(_fuso, uma).Should().Be("Rafael Teles já tem evento das 10:00 às 11:30 (07/10).");
        RotulosDeAgenda.ConflitoNoMobile(_fuso, uma).Should().Be("Rafael Teles já tem evento das 10:00 às 11:30 em 07/10.");
    }

    [Fact]
    public async Task Conflito_com_duas_pessoas_separa_por_ponto_e_virgula()
    {
        SobreposicaoDeAgenda[] duas =
        [
            Sobreposicao("Rafael Teles", new DateTime(2026, 10, 7, 10, 0, 0), new DateTime(2026, 10, 7, 11, 30, 0)),
            Sobreposicao("Marina Couto", new DateTime(2026, 10, 7, 11, 0, 0), new DateTime(2026, 10, 7, 12, 0, 0)),
        ];

        RotulosDeAgenda.ConflitoNoDesktop(_fuso, duas).Should().Be(
            "Atenção: Rafael Teles já tem evento das 10:00 às 11:30 (07/10); Marina Couto já tem evento das 11:00 às 12:00 (07/10).");
        RotulosDeAgenda.ConflitoNoTablet(_fuso, duas).Should().Be(
            "Rafael Teles já tem evento das 10:00 às 11:30 (07/10); Marina Couto já tem evento das 11:00 às 12:00 (07/10).");
        RotulosDeAgenda.ConflitoNoMobile(_fuso, duas).Should().Be(
            "Rafael Teles já tem evento das 10:00 às 11:30 em 07/10; Marina Couto já tem evento das 11:00 às 12:00 em 07/10.");
        // O aviso do desktop é a redação do desktop depois de "Evento salvo." em destaque.
        var aviso = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<AvisoDeEventoSalvo>(
            new DespachanteCombinado(), new() { ["Recado"] = new Recado("Evento salvo.", TipoDeAviso.Sucesso, duas) }));
        BlocoDeVisao.De(aviso, "aj-so-desktop").Should()
            .Contain("<strong>Evento salvo.</strong> " + RotulosDeAgenda.ConflitoNoDesktop(_fuso, duas));
    }

    [Fact]
    public void Conflito_que_atravessa_a_meia_noite_tem_a_mesma_forma_nas_tres_e_nome_nulo_vira_usuario()
    {
        SobreposicaoDeAgenda[] sobreposicoes =
        [
            Sobreposicao("Pedro", new DateTime(2026, 3, 10, 22, 0, 0), new DateTime(2026, 3, 11, 1, 0, 0)),
            Sobreposicao(null, new DateTime(2026, 10, 7, 10, 0, 0), new DateTime(2026, 10, 7, 11, 0, 0)),
        ];

        RotulosDeAgenda.ConflitoNoDesktop(_fuso, sobreposicoes).Should().Be(
            "Atenção: Pedro já tem evento de 10/03 22:00 a 11/03 01:00; Usuário já tem evento das 10:00 às 11:00 (07/10).");
        RotulosDeAgenda.ConflitoNoTablet(_fuso, sobreposicoes).Should().Be(
            "Pedro já tem evento de 10/03 22:00 a 11/03 01:00; Usuário já tem evento das 10:00 às 11:00 (07/10).");
        RotulosDeAgenda.ConflitoNoMobile(_fuso, sobreposicoes).Should().Be(
            "Pedro já tem evento de 10/03 22:00 a 11/03 01:00; Usuário já tem evento das 10:00 às 11:00 em 07/10.");
    }

    // F3: o nome acessível do "+N" da célula do Mês (com o plural pela contagem) e os vínculos que o usuário não vê,
    // na ficha e no formulário do evento, são textos da agenda.
    [Fact]
    public void Rotulo_do_mais_do_mes_e_vinculos_indisponiveis()
    {
        RotulosDeAgenda.RotuloDoMais(new DateOnly(2026, 10, 8), 2).Should().Be("Mais 2 eventos em 08/10");
        RotulosDeAgenda.RotuloDoMais(new DateOnly(2026, 10, 9), 1).Should().Be("Mais 1 evento em 09/10");
        RotulosDeAgenda.RotuloDoMais(new DateOnly(2027, 1, 31), 12).Should().Be("Mais 12 eventos em 31/01");
        RotulosDeAgenda.ClienteIndisponivel.Should().Be("Cliente indisponível");
        RotulosDeAgenda.ProcessoIndisponivel.Should().Be("Processo indisponível");
    }

    [Fact]
    public void Formatos_novos_nao_dependem_da_cultura_da_thread()
    {
        var plantao = Evento("Plantão", new DateTime(2026, 10, 7, 23, 30, 0), 60);
        SobreposicaoDeAgenda[] uma = [Sobreposicao("Rafael Teles", new DateTime(2026, 10, 7, 10, 0, 0), new DateTime(2026, 10, 7, 11, 30, 0))];
        var antes = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");   // outros algarismos e outro calendário

            PeriodoDaAgenda.Ler("dia", "2026-10-08", Hoje).Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Dia, Hoje, true));
            PeriodoDaAgenda.Ler("dia", "2026-10-08", Hoje).Consulta.Should().Be("?visao=dia&data=2026-10-08");
            RotulosDeAgenda.SubtituloDaSemana(new DateOnly(2026, 10, 5)).Should().Be("Semana de 05/10 a 11/10/2026");
            RotulosDeAgenda.SubtituloDoMes(Hoje).Should().Be("Outubro de 2026");
            RotulosDeAgenda.RotuloCurto(Hoje, hoje: true).Should().Be("Quinta, 08/10, hoje");
            RotulosDeAgenda.NumeroDoDia(Hoje).Should().Be("08");
            RotulosDeAgenda.HoraDeInicio(_fuso, plantao, new DateOnly(2026, 10, 7)).Should().Be("23:30");
            RotulosDeAgenda.ConflitoNoMobile(_fuso, uma).Should().Be("Rafael Teles já tem evento das 10:00 às 11:30 em 07/10.");
            RotulosDeAgenda.RotuloDoMais(Hoje, 2).Should().Be("Mais 2 eventos em 08/10");
        }
        finally
        {
            CultureInfo.CurrentCulture = antes;
        }
    }
}
