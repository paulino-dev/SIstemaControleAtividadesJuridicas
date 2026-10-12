using System.Globalization;
using System.Text.Json;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// Os dados de um registro de auditoria em texto de ler: "Status: Feita → Concluida · Nome: Ana". Os dados são
/// gravados em JSON (um objeto com os campos; numa alteração, cada campo traz "de" e "para"). Nada é descartado: o
/// que não tem essa forma aparece como foi gravado.
/// </summary>
public static class ResumoDeAuditoria
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public static string De(string? dados, FusoDoEscritorio fuso)
    {
        ArgumentNullException.ThrowIfNull(fuso);
        if (string.IsNullOrWhiteSpace(dados)) return "—";

        try
        {
            using var documento = JsonDocument.Parse(dados);
            if (documento.RootElement.ValueKind != JsonValueKind.Object) return dados;

            var partes = documento.RootElement.EnumerateObject()
                .Select(campo => $"{campo.Name}: {Valor(campo.Value, fuso)}")
                .ToList();
            return partes.Count == 0 ? "—" : string.Join(" · ", partes);
        }
        catch (JsonException)
        {
            return dados;
        }
    }

    private static string Valor(JsonElement valor, FusoDoEscritorio fuso) =>
        valor.ValueKind == JsonValueKind.Object && SoDeEPara(valor)
            ? $"{Simples(valor.GetProperty("de"), fuso)} → {Simples(valor.GetProperty("para"), fuso)}"
            : Simples(valor, fuso);

    private static bool SoDeEPara(JsonElement objeto)
    {
        var nomes = objeto.EnumerateObject().Select(p => p.Name).ToList();
        return nomes.Count == 2 && nomes.Contains("de") && nomes.Contains("para");
    }

    private static string Simples(JsonElement valor, FusoDoEscritorio fuso) => valor.ValueKind switch
    {
        JsonValueKind.Null => "null",
        JsonValueKind.String => Texto(valor.GetString()!, fuso),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => valor.GetRawText(),   // número, lista e objeto ficam como foram gravados
    };

    private static string Texto(string texto, FusoDoEscritorio fuso)
    {
        // Instante gravado em UTC: a data e a hora do escritório.
        if (texto.Length > 10 && texto[10] == 'T' &&
            DateTimeOffset.TryParse(texto, Cultura, DateTimeStyles.AssumeUniversal, out var instante))
            return fuso.Formatar(instante.UtcDateTime);

        // Data sem hora.
        if (DateOnly.TryParseExact(texto, "yyyy-MM-dd", Cultura, DateTimeStyles.None, out var dia))
            return dia.ToString("dd/MM/yyyy", Cultura);

        return texto;
    }
}
