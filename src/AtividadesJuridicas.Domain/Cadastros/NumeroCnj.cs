using System.Diagnostics.CodeAnalysis;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

/// <summary>Numeração única do CNJ: NNNNNNN-DD.AAAA.J.TR.OOOO, com dígito verificador módulo 97.</summary>
public sealed record NumeroCnj
{
    /// <summary>Os vinte dígitos, sem pontuação.</summary>
    public string Valor { get; }

    private NumeroCnj(string valor) => Valor = valor;

    public string Formatado =>
        $"{Valor[..7]}-{Valor[7..9]}.{Valor[9..13]}.{Valor[13]}.{Valor[14..16]}.{Valor[16..]}";

    public static NumeroCnj Criar(string? texto) =>
        TentarCriar(texto, out var numero) ? numero : throw new ExcecaoDeDominio("Número CNJ inválido.");

    public static bool TentarCriar(string? texto, [NotNullWhen(true)] out NumeroCnj? numero)
    {
        numero = null;
        if (texto is null) return false;
        if (texto.Any(c => !char.IsAsciiDigit(c) && c is not ('.' or '-' or ' '))) return false;

        var digitos = new string([.. texto.Where(char.IsAsciiDigit)]);
        if (digitos.Length != 20) return false;

        // O verificador é calculado sobre o número sem os dois dígitos, seguido de "00".
        var semVerificador = digitos[..7] + digitos[9..] + "00";
        var resto = 0;
        foreach (var c in semVerificador) resto = (resto * 10 + (c - '0')) % 97;

        if (98 - resto != int.Parse(digitos[7..9])) return false;

        numero = new NumeroCnj(digitos);
        return true;
    }

    public override string ToString() => Formatado;
}
