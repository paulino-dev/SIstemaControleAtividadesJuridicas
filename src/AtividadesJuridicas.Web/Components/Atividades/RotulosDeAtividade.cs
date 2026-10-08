using System.Globalization;
using AtividadesJuridicas.Domain.Atividades;

namespace AtividadesJuridicas.Web.Components.Atividades;

/// <summary>Os textos em português que a tela mostra para os valores do domínio.</summary>
public static class RotulosDeAtividade
{
    public static string Status(StatusAtividade status) => status switch
    {
        StatusAtividade.Criada => "Criada",
        StatusAtividade.Direcionada => "Direcionada",
        StatusAtividade.Feita => "Feita",
        StatusAtividade.ParaCorrecao => "Para correção",
        StatusAtividade.Corrigida => "Corrigida",
        StatusAtividade.Concluida => "Concluída",
        StatusAtividade.Reaberta => "Reaberta",
        _ => status.ToString(),
    };

    public static string Data(DateOnly? data) =>
        data is { } d ? d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "—";
}
