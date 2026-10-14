using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// Valor em reais digitado como se escreve no Brasil: "84.000,00", "84000", "84000,5". O ponto só separa milhares,
/// em grupos de três; a vírgula separa até duas casas. "84000.50" é recusado: não se sabe se o ponto é decimal.
/// </summary>
public static partial class ValorEmReais
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    [GeneratedRegex("^(\\d{1,3}(\\.\\d{3})+|\\d+)(,\\d{1,2})?$")]
    private static partial Regex Formato();

    public static bool TentarLer(string? texto, out decimal? valor)
    {
        valor = null;
        if (string.IsNullOrWhiteSpace(texto)) return true;

        var limpo = texto.Trim();
        if (!Formato().IsMatch(limpo)) return false;

        valor = decimal.Parse(limpo, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, Cultura);
        return true;
    }

    public static string? Escrever(decimal? valor) => valor?.ToString("N2", Cultura);
}

/// <summary>O campo de valor em reais está vazio ou no formato brasileiro.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ValorEmReaisValidoAttribute() : ValidationAttribute("Informe o valor como 84.000,00.")
{
    public override bool IsValid(object? value) => ValorEmReais.TentarLer(value as string, out _);
}
