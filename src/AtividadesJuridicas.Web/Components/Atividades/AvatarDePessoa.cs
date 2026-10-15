namespace AtividadesJuridicas.Web.Components.Atividades;

/// <summary>
/// O avatar de uma pessoa nas telas de atividade (cartões, responsáveis, comentários). A sigla e a cor saem do nome,
/// porque o resumo da atividade só traz nomes. A cor é estável entre execuções: vem da soma dos códigos dos
/// caracteres, e não do GetHashCode de string, que muda a cada execução.
/// </summary>
public static class AvatarDePessoa
{
    private static readonly string[] Cores = ["aj-avatar--azul", "aj-avatar--verde", "aj-avatar--ambar", "aj-avatar--lilas"];

    /// <summary>
    /// As iniciais do primeiro e do último nome ("Beatriz Lins" → "BL"); de um nome só, as duas primeiras letras;
    /// sem nome, "?".
    /// </summary>
    public static string Sigla(string nome)
    {
        var partes = nome.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant(),
            _ => $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant(),
        };
    }

    /// <summary>A classe de cor do avatar: a soma dos códigos dos caracteres do nome aparado, módulo 4.</summary>
    public static string Classe(string nome) => Cores[(int)(nome.Trim().Sum(c => (long)c) % Cores.Length)];
}
