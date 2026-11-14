using AtividadesJuridicas.Application.Solicitacoes;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>Os nomes da urgência e das situações de uma solicitação, como as telas os escrevem, e a cor de cada situação.</summary>
public static class RotulosDeSolicitacao
{
    public static string Urgencia(UrgenciaDaSolicitacao urgencia) => urgencia switch
    {
        UrgenciaDaSolicitacao.Baixa => "Baixa",
        UrgenciaDaSolicitacao.Media => "Média",
        UrgenciaDaSolicitacao.Alta => "Alta",
        _ => urgencia.ToString(),
    };

    /// <summary>A situação como o cliente a lê no portal.</summary>
    public static string ParaOCliente(SituacaoParaOCliente situacao) => situacao switch
    {
        SituacaoParaOCliente.AguardandoAnalise => "Aguardando análise",
        SituacaoParaOCliente.Cancelada => "Cancelada",
        SituacaoParaOCliente.EmAndamento => "Em andamento",
        SituacaoParaOCliente.Concluida => "Concluída",
        _ => situacao.ToString(),
    };

    /// <summary>A situação do domínio, como a Diretoria a lê.</summary>
    public static string ParaADiretoria(SituacaoDaSolicitacao situacao) => situacao switch
    {
        SituacaoDaSolicitacao.Aguardando => "Aguardando",
        SituacaoDaSolicitacao.Cancelada => "Cancelada",
        SituacaoDaSolicitacao.EmAtividade => "Virou atividade",
        _ => situacao.ToString(),
    };

    /// <summary>A etiqueta da situação do portal: a que aguarda chama a atenção; a cancelada é neutra.</summary>
    public static string Etiqueta(SituacaoParaOCliente situacao) => situacao switch
    {
        SituacaoParaOCliente.AguardandoAnalise => "aj-etiqueta aj-etiqueta--correcao",
        SituacaoParaOCliente.EmAndamento => "aj-etiqueta aj-etiqueta--andamento",
        SituacaoParaOCliente.Concluida => "aj-etiqueta aj-etiqueta--concluida",
        _ => "aj-etiqueta aj-etiqueta--criada",
    };

    /// <summary>A etiqueta da situação da Diretoria, nas mesmas cores do portal.</summary>
    public static string Etiqueta(SituacaoDaSolicitacao situacao) => situacao switch
    {
        SituacaoDaSolicitacao.Aguardando => "aj-etiqueta aj-etiqueta--correcao",
        SituacaoDaSolicitacao.EmAtividade => "aj-etiqueta aj-etiqueta--andamento",
        _ => "aj-etiqueta aj-etiqueta--criada",
    };
}
