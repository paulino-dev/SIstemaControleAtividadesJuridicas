namespace AtividadesJuridicas.Web.Components.Agenda;

/// <summary>Onde o cartão de evento está, e por isso quais linhas ele mostra (os quadros da agenda).</summary>
public enum FormaDoCartao
{
    /// <summary>Coluna da Semana do desktop: tipo, horário, título, local, cliente e participantes, um por linha.</summary>
    Coluna,

    /// <summary>Seção do dia (tablet; Dia do tablet e Semana do mobile): hora em coluna, tipo, título e o local (ou o cliente).</summary>
    Secao,

    /// <summary>Lista do dia no mobile: hora em coluna, tipo, título, "local · cliente" e os participantes.</summary>
    Folgado,

    /// <summary>Régua do Dia no desktop: tipo, horário, título e "local · cliente · participantes", em linha.</summary>
    Linha,

    /// <summary>Célula do Mês (desktop e tablet): o tipo só para leitor, a hora de início (no dia em que começa) e o título.</summary>
    Mes,
}
