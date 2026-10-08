using System.Diagnostics.CodeAnalysis;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

/// <summary>
/// CNPJ numérico ou alfanumérico: as doze primeiras posições aceitam dígitos e letras maiúsculas;
/// os dois dígitos verificadores são sempre numéricos.
/// </summary>
public sealed record Cnpj
{
    /// <summary>Os catorze caracteres, sem pontuação, em maiúsculas.</summary>
    public string Valor { get; }

    private Cnpj(string valor) => Valor = valor;

    public string Formatado => $"{Valor[..2]}.{Valor[2..5]}.{Valor[5..8]}/{Valor[8..12]}-{Valor[12..]}";

    public static Cnpj Criar(string? texto) =>
        TentarCriar(texto, out var cnpj) ? cnpj : throw new ExcecaoDeDominio("CNPJ inválido.");

    public static bool TentarCriar(string? texto, [NotNullWhen(true)] out Cnpj? cnpj)
    {
        cnpj = null;
        if (texto is null) return false;

        var limpo = new string([.. texto.Where(c => c is not ('.' or '/' or '-' or ' '))]).ToUpperInvariant();
        if (limpo.Length != 14) return false;
        if (!limpo[..12].All(c => char.IsAsciiDigit(c) || c is >= 'A' and <= 'Z')) return false;
        if (!char.IsAsciiDigit(limpo[12]) || !char.IsAsciiDigit(limpo[13])) return false;
        if (limpo.Distinct().Count() == 1) return false;

        if (Digito(limpo.AsSpan(0, 12)) != limpo[12] - '0' || Digito(limpo.AsSpan(0, 13)) != limpo[13] - '0')
            return false;

        cnpj = new Cnpj(limpo);
        return true;
    }

    // Módulo 11 com pesos de 2 a 9 a partir da direita; cada caractere vale o seu código ASCII menos 48.
    private static int Digito(ReadOnlySpan<char> caracteres)
    {
        var soma = 0;
        var peso = 2;
        for (var i = caracteres.Length - 1; i >= 0; i--)
        {
            soma += (caracteres[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public override string ToString() => Formatado;
}
