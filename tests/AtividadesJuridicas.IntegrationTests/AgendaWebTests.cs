using System.Net;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Auditoria;
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
        html.Should().Contain("Pedro Produção").And.Contain("Diana Diretora");
        html.Should().Contain("Abrir");

        // Dia sem eventos é dito, e a semana tem sete dias.
        html.Should().Contain("Sem eventos.");
        html.Split("class=\"dia\"").Length.Should().Be(8);

        // O dia de hoje é um dos cabeçalhos, escrito com dia da semana e data.
        var hoje = DateOnly.FromDateTime(_fuso.ParaLocal(DateTime.UtcNow));
        html.Should().Contain(hoje.ToString("dd/MM/yyyy"));

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
}
