namespace AtividadesJuridicas.Domain.Solicitacoes;

/// <summary>O quanto o cliente tem pressa. É informação para a equipe: não muda nenhuma regra.</summary>
public enum UrgenciaDaSolicitacao
{
    Baixa = 1,
    Media = 2,
    Alta = 3,
}

/// <summary>Derivada dos campos da solicitação e nunca gravada.</summary>
public enum SituacaoDaSolicitacao
{
    Aguardando = 1,
    Cancelada = 2,
    EmAtividade = 3,
}
