using System.Diagnostics.CodeAnalysis;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Identidade;

public sealed record Email
{
    public const int TamanhoMaximo = 254;

    public string Valor { get; }

    private Email(string valor) => Valor = valor;

    public static Email Criar(string? texto) =>
        TentarCriar(texto, out var email) ? email : throw new ExcecaoDeDominio("E-mail inválido.");

    public static bool TentarCriar(string? texto, [NotNullWhen(true)] out Email? email)
    {
        email = null;
        var valor = texto?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(valor) || valor.Length > TamanhoMaximo) return false;
        if (valor.Any(char.IsWhiteSpace)) return false;

        var arroba = valor.IndexOf('@');
        if (arroba <= 0 || arroba != valor.LastIndexOf('@')) return false;

        var dominio = valor[(arroba + 1)..];
        if (dominio.Length < 3 || !dominio.Contains('.')) return false;
        if (dominio.StartsWith('.') || dominio.EndsWith('.')) return false;

        email = new Email(valor);
        return true;
    }

    public override string ToString() => Valor;
}
