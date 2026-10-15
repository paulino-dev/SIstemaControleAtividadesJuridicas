using System.Globalization;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.Web.Components.Atividades;

/// <summary>As frases das telas de atividade montadas a partir dos dados: a entrega, a mudança de situação, a data curta e as contagens.</summary>
public static class TextosDeAtividade
{
    /// <summary>
    /// A linha de situação da ficha: "Entrega em 09/10/2026 · Trabalhista" (curta, no mobile: "Entrega 09/10/2026 ·
    /// Trabalhista"). Sem área, sem a parte da área; sem data, "Sem data de entrega".
    /// </summary>
    public static string Entrega(DateOnly? data, string? area, bool curta)
    {
        var entrega = data is { } dia
            ? $"{(curta ? "Entrega" : "Entrega em")} {dia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}"
            : "Sem data de entrega";
        return string.IsNullOrWhiteSpace(area) ? entrega : $"{entrega} · {area}";
    }

    /// <summary>O registro do histórico: "Criada" no primeiro, "Criada → Direcionada" nos outros.</summary>
    public static string Mudanca(StatusAtividade? anterior, StatusAtividade nova) =>
        anterior is { } de
            ? $"{RotulosDeAtividade.Status(de)} → {RotulosDeAtividade.Status(nova)}"
            : RotulosDeAtividade.Status(nova);

    /// <summary>Dia, mês e hora no horário do escritório, sem o ano: "07/10 16:42" (tablet e mobile).</summary>
    public static string DataCurta(FusoDoEscritorio fuso, DateTime utc) =>
        fuso.ParaLocal(utc).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture);

    // Dentro desta classe, "Contagem" é o método abaixo: a classe de Infra vai pelo nome completo.

    /// <summary>O subtítulo da lista: "1 atividade", "8 atividades".</summary>
    public static string Contagem(int n) => global::AtividadesJuridicas.Web.Infra.Contagem.De(n, "atividade", "atividades");

    /// <summary>A parte de alerta do subtítulo: "1 atrasada", "2 atrasadas"; nula quando não há nenhuma.</summary>
    public static string? Atrasadas(int m) =>
        m > 0 ? global::AtividadesJuridicas.Web.Infra.Contagem.De(m, "atrasada", "atrasadas") : null;
}
