using System.Globalization;
using System.Net;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Components.Agenda;
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

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    /// <summary>Hoje, no fuso do escritório, às <paramref name="hora"/>: sempre dentro da semana corrente.</summary>
    private DateTime HojeAs(int hora, int minuto = 0)
    {
        var hoje = DateOnly.FromDateTime(_fuso.ParaLocal(DateTime.UtcNow));
        return _fuso.ParaUtc(hoje.ToDateTime(new TimeOnly(hora, minuto)));
    }

    private async Task CriarEventosAsync()
    {
        // Participa o Pedro; o diretor é o criador e também participa.
        var doPedro = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Audiência de instrução do Pedro", null, "Fórum Central", TipoEvento.Audiencia, HojeAs(10), 60,
            null, null, null, [_pedro]));
        doPedro.Sucesso.Should().BeTrue(doPedro.Mensagem);

        // Só a Diretoria participa: o Pedro não pode ver.
        var restrito = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Reunião reservada da diretoria", null, null, TipoEvento.Reuniao, HojeAs(14, 30), 90,
            null, null, null, []));
        restrito.Sucesso.Should().BeTrue(restrito.Mensagem);
    }

    [Fact]
    public async Task Diretor_ve_os_eventos_da_semana()
    {
        await CriarEventosAsync();
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/agenda"));

        html.Should().Contain("Agenda").And.Contain("Semana anterior").And.Contain("Hoje").And.Contain("Próxima semana")
            .And.Contain("Novo evento").And.NotContain("Seu acesso foi encerrado");
        html.Should().Contain("Audiência de instrução do Pedro").And.Contain("Reunião reservada da diretoria");

        // Células da tabela (as palavras também aparecem nos filtros): horário no fuso do escritório, tipo, local, participantes.
        html.Should().MatchRegex(@"<td[^>]*>\s*10:00 – 11:00\s*</td>");
        html.Should().MatchRegex(@"<td[^>]*>\s*14:30 – 16:00\s*</td>");
        html.Should().MatchRegex(@"<td>\s*Audiência\s*</td>");
        html.Should().MatchRegex(@"<td>\s*Reunião\s*</td>");
        html.Should().MatchRegex(@"<td>\s*Fórum Central\s*</td>");
        // Os participantes estão na célula da linha, ordenados por nome, e não em qualquer parte da página.
        html.Should().MatchRegex(@"<td>\s*Diana Diretora, Pedro Produção\s*</td>");
        html.Should().MatchRegex(@"<td>\s*Diana Diretora\s*</td>");
        html.Should().Contain("Abrir");

        // Dia sem eventos é dito, e a semana tem sete dias.
        html.Should().Contain("Sem eventos.");
        html.Split("class=\"dia\"").Length.Should().Be(8);

        // O dia de hoje é um dos cabeçalhos, escrito com dia da semana e data.
        var hoje = DateOnly.FromDateTime(_fuso.ParaLocal(DateTime.UtcNow));
        html.Should().Contain(hoje.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));

        // O filtro por participante é só da Diretoria.
        html.Should().MatchRegex(@"Participante\s*<select");
    }

    [Fact]
    public async Task Producao_ve_so_os_seus()
    {
        await CriarEventosAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/agenda"));

        html.Should().Contain("Audiência de instrução do Pedro").And.Contain("Novo evento").And.Contain("Abrir");
        html.Should().MatchRegex(@"<td[^>]*>\s*10:00 – 11:00\s*</td>");
        html.Should().NotContain("Reunião reservada da diretoria").And.NotContain("14:30")
            .And.NotContain("Seu acesso foi encerrado");
        html.Should().NotMatchRegex(@"Participante\s*<select");
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

        var inicio = System.Text.RegularExpressions.Regex.Match(htmlDoDiretor, @"Participante\s*<select");
        inicio.Success.Should().BeTrue();
        var select = htmlDoDiretor[inicio.Index..htmlDoDiretor.IndexOf("</select>", inicio.Index, StringComparison.Ordinal)];
        select.Should().MatchRegex(@">\s*Iara Inativa \(inativo\)\s*<");
        var nomes = System.Text.RegularExpressions.Regex.Matches(select, @"<option[^>]*>\s*([^<]*?)\s*</option>")
            .Select(m => m.Groups[1].Value).ToList();
        nomes.Should().Equal("Todos", "Diana Diretora", "Iara Inativa (inativo)", "Pedro Produção");

        htmlDoPedro.Should().NotMatchRegex(@"Participante\s*<select").And.NotContain("Iara");
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
    public void Aviso_de_sobreposicao_diz_que_o_evento_foi_salvo_e_lista_cada_pessoa_no_horario_do_escritorio()
    {
        var sobreposicoes = new[]
        {
            new SobreposicaoDeAgenda(Guid.NewGuid(), "Pedro", _fuso.ParaUtc(new DateTime(2026, 10, 7, 10, 0, 0)),
                _fuso.ParaUtc(new DateTime(2026, 10, 7, 11, 30, 0))),
        };

        RotulosDeAgenda.MensagemDeSalvo(_fuso, sobreposicoes).Should()
            .Be("Evento salvo. Atenção: Pedro já tem evento das 10:00 às 11:30 (07/10).");
        RotulosDeAgenda.MensagemDeSalvo(_fuso, []).Should().Be("Evento salvo.");
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

        RotulosDeAgenda.Sobreposicao(_fuso, atravessa).Should().Be("Pedro já tem evento de 10/03 22:00 a 11/03 01:00");
        RotulosDeAgenda.Sobreposicao(_fuso, ateMeiaNoite).Should().Be("Paula já tem evento das 22:00 às 00:00 (10/03)");
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
            RotulosDeAgenda.Sobreposicao(_fuso, new SobreposicaoDeAgenda(Guid.NewGuid(), "Pedro", evento.InicioEm,
                evento.InicioEm.AddMinutes(30))).Should().Be("Pedro já tem evento das 23:30 às 00:00 (07/10)");
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
