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

    /// <summary>O modificador da etiqueta do design system (<c>aj-etiqueta--…</c>) para a situação.</summary>
    public static string ClasseDaEtiqueta(StatusAtividade status) => status switch
    {
        StatusAtividade.Criada => "criada",
        StatusAtividade.Direcionada => "direcionada",
        StatusAtividade.Feita => "feita",
        StatusAtividade.ParaCorrecao => "correcao",
        StatusAtividade.Corrigida => "corrigida",
        StatusAtividade.Concluida => "concluida",
        StatusAtividade.Reaberta => "reaberta",
        _ => "criada",
    };

    public static string Data(DateOnly? data) =>
        data is { } d ? d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "—";
}
