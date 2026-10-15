using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>Os nomes das situações e dos polos do processo, como as telas os escrevem.</summary>
public static class RotulosDeProcesso
{
    public static string Situacao(StatusProcesso status) => status switch
    {
        StatusProcesso.Ativo => "Ativo",
        StatusProcesso.Suspenso => "Suspenso",
        StatusProcesso.Arquivado => "Arquivado",
        StatusProcesso.Encerrado => "Encerrado",
        _ => status.ToString(),
    };

    /// <summary>O nome do chip de filtro: "Ativos", "Suspensos".</summary>
    public static string SituacaoNoPlural(StatusProcesso status) => $"{Situacao(status)}s";

    public static string Polo(PoloProcessual polo) => polo switch
    {
        PoloProcessual.Ativo => "Ativo",
        PoloProcessual.Passivo => "Passivo",
        PoloProcessual.Terceiro => "Terceiro",
        _ => polo.ToString(),
    };

    public static string Numero(string? numeroCnj) => string.IsNullOrWhiteSpace(numeroCnj) ? "Sem número" : numeroCnj;
}
