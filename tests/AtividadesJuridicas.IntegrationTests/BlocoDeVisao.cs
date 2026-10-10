using System.Text;
using System.Text.RegularExpressions;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Recorta do HTML o conteúdo dos blocos de uma visão (<c>aj-so-desktop</c>, <c>aj-tablet-mobile</c>…), para a
/// asserção olhar uma visão só. Bloco vazio aceita qualquer <c>NotContain</c>: toda asserção negativa sobre um
/// bloco vem com uma positiva sobre o mesmo bloco.
/// </summary>
public static class BlocoDeVisao
{
    public static string De(string html, string classe)
    {
        var blocos = new StringBuilder();
        foreach (Match abertura in Regex.Matches(html, $"<(div|span) class=\"{Regex.Escape(classe)}\">"))
        {
            var elemento = abertura.Groups[1].Value;
            var inicio = abertura.Index + abertura.Length;
            var profundidade = 1;
            var fim = -1;
            foreach (Match marca in Regex.Matches(html[inicio..], $"<(/?){elemento}\\b"))
            {
                profundidade += marca.Groups[1].Length == 0 ? 1 : -1;
                if (profundidade > 0) continue;

                fim = inicio + marca.Index;
                break;
            }

            // HTML quebrado não pode passar por bloco vazio.
            if (fim < 0) throw new InvalidOperationException($"O bloco {classe} não foi fechado.");
            blocos.Append(html, inicio, fim - inicio);
        }

        return blocos.ToString();
    }
}
