using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Os eventos dos quadros da agenda: a semana de 05/10 a 11/10/2026, com hoje na quinta-feira, 08/10, no fuso do
/// escritório (America/Sao_Paulo). Os testes dos componentes de visão (Semana, Dia e Mês) recebem estes eventos
/// prontos, como a página os entrega depois da consulta.
/// </summary>
public static class QuadroDaAgenda
{
    // Declarado antes dos eventos: os campos estáticos são iniciados na ordem do texto.
    private static readonly FusoDoEscritorio Fuso = new("America/Sao_Paulo");

    /// <summary>Quinta-feira, 08/10/2026: o "hoje" dos quadros.</summary>
    public static readonly DateOnly Hoje = new(2026, 10, 8);

    public static readonly EventoResumo PautaDaSemana = Evento(new DateTime(2026, 10, 5, 9, 0, 0), 60, TipoEvento.Reuniao,
        "Pauta da semana", "Sala de reuniões", null, "Marina Couto", "Rafael Teles", "Beatriz Lins");

    public static readonly EventoResumo AudienciaDeInstrucao = Evento(new DateTime(2026, 10, 5, 14, 0, 0), 90, TipoEvento.Audiencia,
        "Audiência de instrução", "2ª Vara do Trabalho", "Transportes Litoral Ltda.", "Beatriz Lins");

    public static readonly EventoResumo Contestacao = Evento(new DateTime(2026, 10, 6, 17, 0, 0), 60, TipoEvento.Prazo,
        "Contestação — cobrança de ISS", null, "Construtora Serra Azul Ltda.", "Rafael Teles");

    public static readonly EventoResumo Alinhamento = Evento(new DateTime(2026, 10, 7, 10, 0, 0), 90, TipoEvento.Reuniao,
        "Alinhamento com o cliente", "Videoconferência", "Construtora Serra Azul Ltda.", "Rafael Teles", "Marina Couto");

    public static readonly EventoResumo AudienciaDeConciliacao = Evento(new DateTime(2026, 10, 7, 11, 0, 0), 60, TipoEvento.Audiencia,
        "Audiência de conciliação", "Fórum Cível, sala 4", "Joana Figueiredo", "Rafael Teles");

    public static readonly EventoResumo ProtocoloDeDocumentos = Evento(new DateTime(2026, 10, 8, 9, 30, 0), 60, TipoEvento.Outro,
        "Protocolo de documentos", "Cartório do 1º Ofício", null, "Otávio Brandão");

    public static readonly EventoResumo RevisaoDeMinutas = Evento(new DateTime(2026, 10, 8, 15, 0, 0), 60, TipoEvento.Reuniao,
        "Revisão de minutas", "Sala de reuniões", null, "Marina Couto", "Camila Prado");

    public static readonly EventoResumo RecursoOrdinario = Evento(new DateTime(2026, 10, 9, 16, 0, 0), 60, TipoEvento.Prazo,
        "Recurso ordinário", null, "Transportes Litoral Ltda.", "Beatriz Lins");

    /// <summary>Os oito eventos da semana do quadro, na ordem em que o servidor os manda (início, depois id).</summary>
    public static IReadOnlyList<EventoResumo> Semana { get; } =
    [
        PautaDaSemana, AudienciaDeInstrucao, Contestacao, Alinhamento, AudienciaDeConciliacao,
        ProtocoloDeDocumentos, RevisaoDeMinutas, RecursoOrdinario,
    ];

    /// <summary>
    /// Um evento que começa em <paramref name="inicioLocal"/> (horário do escritório, <see cref="DateTimeKind.Unspecified"/>)
    /// e dura <paramref name="minutos"/>. Sem processo: nenhum cartão o mostra.
    /// </summary>
    public static EventoResumo Evento(DateTime inicioLocal, int minutos, TipoEvento tipo, string titulo,
        string? local = null, string? cliente = null, params string[] participantes)
    {
        var inicio = Fuso.ParaUtc(inicioLocal);
        return new EventoResumo(Guid.NewGuid(), titulo, tipo, inicio, inicio.AddMinutes(minutos), local, cliente, null, participantes);
    }
}
