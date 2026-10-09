using System.Globalization;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.Web.Components.Produtores;

/// <summary>Os textos em português e o cálculo de mês que o painel de produtividade mostra.</summary>
public static class RotulosDeProdutividade
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Um mês é representado pelo seu primeiro dia.</summary>
    public static DateOnly MesDe(DateOnly dia) => new(dia.Year, dia.Month, 1);

    /// <summary>O mês em que o instante cai, no fuso do escritório.</summary>
    public static DateOnly MesDe(FusoDoEscritorio fuso, DateTime utc) => MesDe(DateOnly.FromDateTime(fuso.ParaLocal(utc)));

    /// <summary>
    /// Do mês de <paramref name="hoje"/> para trás, mês a mês, até o de <paramref name="maisAntigo"/>. A lista sempre
    /// traz o mês corrente e nunca um mês futuro.
    /// </summary>
    public static IReadOnlyList<DateOnly> Lista(DateOnly hoje, DateOnly maisAntigo)
    {
        var limite = MesDe(maisAntigo);
        var meses = new List<DateOnly>();
        for (var mes = MesDe(hoje); meses.Count == 0 || mes >= limite; mes = mes.AddMonths(-1)) meses.Add(mes);
        return meses;
    }

    /// <summary>O mês inteiro, do primeiro instante dele ao primeiro do mês seguinte, em UTC.</summary>
    public static (DateTime DeUtc, DateTime AteUtc) Intervalo(FusoDoEscritorio fuso, DateOnly mes)
    {
        var primeiroDia = MesDe(mes);
        return (fuso.InicioDoDiaEmUtc(primeiroDia), fuso.InicioDoDiaEmUtc(primeiroDia.AddMonths(1)));
    }

    /// <summary>"outubro/2026", com algarismos e calendário fixos, qualquer que seja a cultura da conexão.</summary>
    public static string Mes(DateOnly mes) => mes.ToString("MMMM'/'yyyy", Cultura);

    /// <summary>"2026-10": o valor da opção no seletor.</summary>
    public static string Chave(DateOnly mes) => mes.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    /// <summary>Lê de volta uma chave escrita por <see cref="Chave"/>.</summary>
    public static bool TentarLer(string? chave, out DateOnly mes) =>
        DateOnly.TryParseExact(chave, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out mes);

    public static string Titulo(string produtorNome) => $"Produtividade — {produtorNome}";

    /// <summary>"Complexidade 2": o cabeçalho da coluna de totais.</summary>
    public static string Coluna(int complexidade) =>
        "Complexidade " + complexidade.ToString(CultureInfo.InvariantCulture);

    /// <summary>"Complexidade 2 (5)": o título do bloco, com a quantidade.</summary>
    public static string Grupo(GrupoDeComplexidade grupo) =>
        $"{Coluna(grupo.Complexidade)} ({grupo.Total.ToString(CultureInfo.InvariantCulture)})";
}
