using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Infrastructure.Comum;

/// <summary>A data de hoje no fuso do escritório: o dia vira à meia-noite de lá, não à meia-noite UTC.</summary>
public sealed class CalendarioDoEscritorio(TimeZoneInfo fuso, IRelogio relogio) : ICalendarioDoEscritorio
{
    public DateOnly Hoje => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(relogio.AgoraUtc, fuso));
}
