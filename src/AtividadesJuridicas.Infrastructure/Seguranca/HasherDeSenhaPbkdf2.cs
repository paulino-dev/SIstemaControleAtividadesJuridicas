using System.Security.Cryptography;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Infrastructure.Seguranca;

/// <summary>Formato gravado: <c>pbkdf2-sha256$iteracoes$salt(base64)$hash(base64)</c>.</summary>
public sealed class HasherDeSenhaPbkdf2 : IHasherDeSenha
{
    private const string Prefixo = "pbkdf2-sha256";
    private const int Iteracoes = 600_000;
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;

    private readonly Lazy<string> _descarte;

    public HasherDeSenhaPbkdf2() =>
        _descarte = new Lazy<string>(() => Gerar(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));

    public string HashDeDescarte => _descarte.Value;

    public string Gerar(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return $"{Prefixo}${Iteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string senha, string hash)
    {
        var partes = hash.Split('$');
        if (partes.Length != 4 || partes[0] != Prefixo) return false;
        if (!int.TryParse(partes[1], out var iteracoes) || iteracoes <= 0) return false;

        var salt = new byte[TamanhoSalt];
        var esperado = new byte[TamanhoHash];
        if (!Convert.TryFromBase64String(partes[2], salt, out var lidosSalt) || lidosSalt != TamanhoSalt) return false;
        if (!Convert.TryFromBase64String(partes[3], esperado, out var lidosHash) || lidosHash != TamanhoHash) return false;

        var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
