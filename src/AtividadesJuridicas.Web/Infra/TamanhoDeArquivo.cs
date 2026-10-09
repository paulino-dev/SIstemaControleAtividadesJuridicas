using System.Globalization;

namespace AtividadesJuridicas.Web.Infra;

public static class TamanhoDeArquivo
{
    private const long UmMegabyte = 1024 * 1024;

    // A aplicação não fixa a cultura da thread: o formato é sempre o do escritório, como em FusoDoEscritorio.
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public static string Formatar(long bytes) => bytes switch
    {
        >= UmMegabyte => string.Create(Cultura, $"{bytes / (double)UmMegabyte:0.#} MB"),
        >= 1024 => string.Create(Cultura, $"{bytes / 1024d:0.#} KB"),
        _ => $"{bytes} B",
    };
}
