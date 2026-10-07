namespace AtividadesJuridicas.Application.Identidade;

public static class PoliticaDeSenha
{
    public const int TamanhoMinimo = 8;
    public const int TamanhoMaximo = 128;

    /// <summary>Devolve a mensagem de erro, ou nulo quando a senha é aceita.</summary>
    public static string? Validar(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < TamanhoMinimo)
            return $"A senha deve ter ao menos {TamanhoMinimo} caracteres.";
        if (senha.Length > TamanhoMaximo)
            return $"A senha deve ter até {TamanhoMaximo} caracteres.";
        return null;
    }
}
