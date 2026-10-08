using System.Globalization;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>Converte as datas, gravadas em UTC, para o fuso configurado do escritório.</summary>
public sealed class FusoDoEscritorio(string idDoFuso)
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");
    private readonly TimeZoneInfo _fuso = TimeZoneInfo.FindSystemTimeZoneById(idDoFuso);

    public DateTime ParaLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _fuso);

    /// <summary>Instante UTC em que o dia começa no fuso do escritório.</summary>
    public DateTime InicioDoDiaEmUtc(DateOnly dia)
    {
        var local = dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        // Meia-noite inexistente (horário de verão começa à 00:00): o dia começa no primeiro instante que existe.
        while (_fuso.IsInvalidTime(local)) local = local.AddMinutes(30);

        // Meia-noite repetida (relógio volta à 00:00): vale a primeira ocorrência (a de maior deslocamento),
        // para que o intervalo do dia cubra também a hora repetida.
        if (_fuso.IsAmbiguousTime(local))
        {
            var deslocamento = _fuso.GetAmbiguousTimeOffsets(local).Max();
            return new DateTimeOffset(local, deslocamento).UtcDateTime;
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, _fuso);
    }

    /// <summary>
    /// Converte uma data e hora do escritório para UTC, com a mesma política de <see cref="InicioDoDiaEmUtc"/>:
    /// horário inexistente (salto do horário de verão) vai para o primeiro instante válido; horário ambíguo (hora
    /// repetida) vale a primeira ocorrência. Os segundos são desprezados: a tela trabalha em minutos.
    /// </summary>
    public DateTime ParaUtc(DateTime local)
    {
        var minuto = new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, DateTimeKind.Unspecified);

        // O primeiro instante válido é o fim da lacuna (um minuto de cada vez, nunca mais de algumas horas).
        while (_fuso.IsInvalidTime(minuto)) minuto = minuto.AddMinutes(1);

        if (_fuso.IsAmbiguousTime(minuto))
        {
            var deslocamento = _fuso.GetAmbiguousTimeOffsets(minuto).Max();
            return new DateTimeOffset(minuto, deslocamento).UtcDateTime;
        }

        return TimeZoneInfo.ConvertTimeToUtc(minuto, _fuso);
    }

    public string Formatar(DateTime? utc) =>
        utc is { } valor ? ParaLocal(valor).ToString("dd/MM/yyyy HH:mm", Cultura) : "—";
}
