namespace AtividadesJuridicas.Web.Components.Layout;

/// <summary>
/// O avatar de uma pessoa nas telas de atividade e da agenda (cartões, responsáveis, comentários, participantes). A
/// sigla e a cor saem do nome, porque os resumos só trazem nomes. A cor é estável entre execuções: vem da soma dos
/// códigos dos caracteres, e não do GetHashCode de string, que muda a cada execução.
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

    /// <summary>
    /// A classe de cor do avatar na lista de participantes da agenda: o usuário logado ("você") leva o avatar dourado,
    /// que é o <c>aj-avatar</c> sem modificador de cor (classe vazia); os demais, a cor do nome.
    /// </summary>
    public static string Classe(string nome, bool proprio) => proprio ? string.Empty : Classe(nome);
}
