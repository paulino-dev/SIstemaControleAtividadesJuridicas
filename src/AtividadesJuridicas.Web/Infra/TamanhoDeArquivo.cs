namespace AtividadesJuridicas.Web.Infra;

public static class TamanhoDeArquivo
{
    private const long UmMegabyte = 1024 * 1024;

    public static string Formatar(long bytes) => bytes switch
    {
        >= UmMegabyte => $"{bytes / (double)UmMegabyte:0.#} MB",
        >= 1024 => $"{bytes / 1024d:0.#} KB",
        _ => $"{bytes} B",
    };
}
