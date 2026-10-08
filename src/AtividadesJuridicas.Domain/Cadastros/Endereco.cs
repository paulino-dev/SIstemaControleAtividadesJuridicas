using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

/// <summary>Objeto de valor: todos os campos são opcionais e o endereço pode estar vazio.</summary>
public sealed record Endereco
{
    private Endereco() { }

    public string? Logradouro { get; private set; }
    public string? Numero { get; private set; }
    public string? Complemento { get; private set; }
    public string? Bairro { get; private set; }
    public string? Cidade { get; private set; }

    /// <summary>Duas letras maiúsculas.</summary>
    public string? Uf { get; private set; }

    /// <summary>Oito dígitos, sem pontuação.</summary>
    public string? Cep { get; private set; }

    public static Endereco Vazio => new();

    public bool EstaVazio =>
        Logradouro is null && Numero is null && Complemento is null && Bairro is null &&
        Cidade is null && Uf is null && Cep is null;

    public static Endereco Criar(
        string? logradouro, string? numero, string? complemento, string? bairro, string? cidade, string? uf, string? cep) =>
        new()
        {
            Logradouro = Texto(logradouro, 200, "logradouro"),
            Numero = Texto(numero, 20, "número"),
            Complemento = Texto(complemento, 100, "complemento"),
            Bairro = Texto(bairro, 100, "bairro"),
            Cidade = Texto(cidade, 100, "cidade"),
            Uf = NormalizarUf(uf),
            Cep = NormalizarCep(cep),
        };

    private static string? Texto(string? valor, int maximo, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var aparado = valor.Trim();
        return aparado.Length <= maximo
            ? aparado
            : throw new ExcecaoDeDominio($"O campo {campo} do endereço deve ter até {maximo} caracteres.");
    }

    private static string? NormalizarUf(string? uf)
    {
        if (string.IsNullOrWhiteSpace(uf)) return null;
        var aparada = uf.Trim().ToUpperInvariant();
        return aparada.Length == 2 && aparada.All(c => c is >= 'A' and <= 'Z')
            ? aparada
            : throw new ExcecaoDeDominio("A UF deve ter duas letras.");
    }

    private static string? NormalizarCep(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep)) return null;
        var limpo = new string([.. cep.Where(c => c is not ('.' or '-' or ' '))]);
        return limpo.Length == 8 && limpo.All(char.IsAsciiDigit)
            ? limpo
            : throw new ExcecaoDeDominio("O CEP deve ter oito dígitos.");
    }
}
