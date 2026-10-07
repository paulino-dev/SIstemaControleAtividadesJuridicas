using System.Globalization;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>Converte as datas, gravadas em UTC, para o fuso configurado do escritório.</summary>
public sealed class FusoDoEscritorio(string idDoFuso)
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");
    private readonly TimeZoneInfo _fuso = TimeZoneInfo.FindSystemTimeZoneById(idDoFuso);

    public DateTime ParaLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _fuso);

    public string Formatar(DateTime? utc) =>
        utc is { } valor ? ParaLocal(valor).ToString("dd/MM/yyyy HH:mm", Cultura) : "—";
}
