using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>As frases da confirmação de inativação de um usuário.</summary>
public static class TextosDaInativacao
{
    public static string Pergunta(string nome) => $"Confirmar a inativação de {nome}?";

    /// <summary>O que o usuário deixa pendente; nulo quando não há nada pendente (só a pergunta aparece).</summary>
    public static string? Pendencias(string nome, PendenciasDoUsuario p)
    {
        var partes = new List<string>();
        if (p.PartesPendentes > 0)
            partes.Add($"{p.PartesPendentes} {(p.PartesPendentes == 1 ? "parte pendente" : "partes pendentes")}");
        if (p.RevisoesEmAberto > 0)
            partes.Add($"{p.RevisoesEmAberto} {(p.RevisoesEmAberto == 1 ? "revisão em aberto" : "revisões em aberto")}");
        if (p.EventosFuturos > 0)
            partes.Add($"{p.EventosFuturos} {(p.EventosFuturos == 1 ? "evento futuro" : "eventos futuros")}");
        if (partes.Count == 0) return null;

        var lista = partes.Count == 1 ? partes[0] : string.Join(", ", partes[..^1]) + " e " + partes[^1];
        return $"Este usuário tem {lista}. Esses itens continuam atribuídos a {nome} até serem redistribuídos.";
    }
}
