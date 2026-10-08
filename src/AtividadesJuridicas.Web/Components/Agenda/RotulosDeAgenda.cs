using System.Globalization;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.Web.Components.Agenda;

/// <summary>
/// O que o formulário devolve depois de gravar: o id, os avisos de sobreposição e o início gravado (em UTC), para a
/// página mostrar a semana em que o evento caiu.
/// </summary>
public sealed record EventoGravado(Guid Id, IReadOnlyList<SobreposicaoDeAgenda> Sobreposicoes, DateTime InicioEm);

/// <summary>Os textos em português e o cálculo de dia que a agenda mostra.</summary>
public static class RotulosDeAgenda
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public static string Tipo(TipoEvento tipo) => tipo switch
    {
        TipoEvento.Audiencia => "Audiência",
        TipoEvento.Reuniao => "Reunião",
        TipoEvento.Prazo => "Prazo",
        TipoEvento.Outro => "Outro",
        _ => tipo.ToString(),
    };

    public const string AtividadeSemAcesso = "Atividade vinculada (sem acesso)";

    /// <summary>"segunda-feira, 12/10/2026".</summary>
    public static string Dia(DateOnly dia) => dia.ToString("dddd, dd/MM/yyyy", Cultura);

    /// <summary>
    /// O evento aparece no dia se o intervalo <c>[início, fim)</c>, em horário do escritório, toca esse dia. O dia do
    /// fim é o do último instante do evento: quem termina à meia-noite em ponto não aparece no dia seguinte.
    /// </summary>
    public static bool TocaODia(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia)
    {
        var primeiro = DateOnly.FromDateTime(fuso.ParaLocal(evento.InicioEm));
        var ultimo = DateOnly.FromDateTime(fuso.ParaLocal(evento.FimEm.AddTicks(-1)));
        return primeiro <= dia && dia <= ultimo;
    }

    /// <summary>
    /// "HH:mm – HH:mm" no horário do escritório; o lado que cai em outro dia que não o listado leva a data
    /// ("23:30 – 08/10 00:30").
    /// </summary>
    public static string Intervalo(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia)
    {
        var inicio = fuso.ParaLocal(evento.InicioEm);
        var fim = fuso.ParaLocal(evento.FimEm);
        var diaDoFim = DateOnly.FromDateTime(fuso.ParaLocal(evento.FimEm.AddTicks(-1)));

        var de = DateOnly.FromDateTime(inicio) == dia ? inicio.ToString("HH:mm") : inicio.ToString("dd/MM HH:mm");
        var ate = diaDoFim == dia ? fim.ToString("HH:mm") : fim.ToString("dd/MM HH:mm");
        return $"{de} – {ate}";
    }

    /// <summary>"Fulano já tem evento das 10:00 às 11:30 (07/10)", um por pessoa e intervalo.</summary>
    public static string Sobreposicao(FusoDoEscritorio fuso, SobreposicaoDeAgenda s)
    {
        var inicio = fuso.ParaLocal(s.InicioEm);
        var fim = fuso.ParaLocal(s.FimEm);
        return $"{s.UsuarioNome ?? "Usuário"} já tem evento das {inicio:HH:mm} às {fim:HH:mm} ({inicio:dd/MM})";
    }

    /// <summary>A mensagem depois de gravar: o evento foi salvo; a sobreposição é só um aviso.</summary>
    public static string MensagemDeSalvo(FusoDoEscritorio fuso, IReadOnlyList<SobreposicaoDeAgenda> sobreposicoes) =>
        sobreposicoes.Count == 0
            ? "Evento salvo."
            : "Evento salvo. Atenção: " + string.Join("; ", sobreposicoes.Select(s => Sobreposicao(fuso, s))) + ".";
}
