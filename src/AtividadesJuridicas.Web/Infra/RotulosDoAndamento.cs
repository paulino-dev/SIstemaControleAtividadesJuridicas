using AtividadesJuridicas.Application.Atividades;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>Como o cliente lê a situação de uma atividade sua.</summary>
public static class RotulosDoAndamento
{
    public static string Da(SituacaoDoAndamento situacao) => situacao switch
    {
        SituacaoDoAndamento.EmAndamento => "Em andamento",
        SituacaoDoAndamento.Concluida => "Concluída",
        _ => throw new ArgumentOutOfRangeException(nameof(situacao), situacao, "Situação desconhecida."),
    };
}
