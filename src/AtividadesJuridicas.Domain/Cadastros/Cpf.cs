using System.Diagnostics.CodeAnalysis;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

public sealed record Cpf
{
    /// <summary>Os onze dígitos, sem pontuação.</summary>
    public string Valor { get; }

    private Cpf(string valor) => Valor = valor;

    public string Formatado => $"{Valor[..3]}.{Valor[3..6]}.{Valor[6..9]}-{Valor[9..]}";

    public static Cpf Criar(string? texto) =>
        TentarCriar(texto, out var cpf) ? cpf : throw new ExcecaoDeDominio("CPF inválido.");

    public static bool TentarCriar(string? texto, [NotNullWhen(true)] out Cpf? cpf)
    {
        cpf = null;
        if (texto is null) return false;
        if (texto.Any(c => !char.IsAsciiDigit(c) && c is not ('.' or '-' or ' '))) return false;

        var digitos = new string([.. texto.Where(char.IsAsciiDigit)]);
        if (digitos.Length != 11 || digitos.Distinct().Count() == 1) return false;
        if (Digito(digitos, 9) != digitos[9] - '0' || Digito(digitos, 10) != digitos[10] - '0') return false;

        cpf = new Cpf(digitos);
        return true;
    }

    private static int Digito(string digitos, int quantidade)
    {
        var soma = 0;
        for (var i = 0; i < quantidade; i++) soma += (digitos[i] - '0') * (quantidade + 1 - i);
        var resto = soma * 10 % 11;
        return resto == 10 ? 0 : resto;
    }

    public override string ToString() => Formatado;
}
