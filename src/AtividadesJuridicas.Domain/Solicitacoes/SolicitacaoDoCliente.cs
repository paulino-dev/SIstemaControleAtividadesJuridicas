using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Solicitacoes;

/// <summary>
/// Um pedido que o cliente faz ao escritório pelo portal. Os documentos anexados não fazem parte do agregado
/// (são <c>Documento</c>), mas mexer neles também só vale enquanto a solicitação aguarda.
/// </summary>
public sealed class SolicitacaoDoCliente : Entidade
{
    /// <summary>O limite do comentário de atividade, para que a descrição caiba nele inteira.</summary>
    public const int TamanhoMaximoDescricao = AtividadeComentario.TamanhoMaximoTexto;

    public const string NaoPodeMaisSerAlterada = "Esta solicitação não pode mais ser alterada.";
    public const string JaVirouAtividade = "Esta solicitação já virou atividade.";
    public const string CanceladaPeloCliente = "Cliente cancelou a solicitação, mesmo assim deseja criar a atividade?";
    public const string AtividadeDeOutroCliente = "A atividade precisa ser do cliente da solicitação.";

    private SolicitacaoDoCliente() { }

    public Guid ClienteId { get; private set; }
    public Guid SolicitanteId { get; private set; }
    public string Descricao { get; private set; } = null!;
    public UrgenciaDaSolicitacao Urgencia { get; private set; }

    /// <summary>Preenchido quando o cliente cancela. Preservado se a solicitação virar atividade.</summary>
    public DateTime? CanceladaEm { get; private set; }

    /// <summary>Preenchido quando uma atividade é criada a partir da solicitação; limpo se ela for desfeita.</summary>
    public Guid? AtividadeId { get; private set; }

    /// <summary>Versão da linha, mantida pelo banco (controle de concorrência otimista).</summary>
    [NaoAuditar]
    public uint Versao { get; private set; }

    /// <summary>
    /// Aumenta a cada mudança nos documentos da solicitação. Faz essa mudança gravar também a linha da
    /// solicitação, acionando a verificação de concorrência: quem abriu a tela antes da mudança é avisado.
    /// </summary>
    [NaoAuditar]
    public int VersaoDosDocumentos { get; private set; }

    public SituacaoDaSolicitacao Situacao =>
        AtividadeId is not null ? SituacaoDaSolicitacao.EmAtividade
        : CanceladaEm is not null ? SituacaoDaSolicitacao.Cancelada
        : SituacaoDaSolicitacao.Aguardando;

    public static SolicitacaoDoCliente Abrir(
        Guid clienteId, Guid solicitanteId, string descricao, UrgenciaDaSolicitacao urgencia)
    {
        if (clienteId == Guid.Empty) throw new ArgumentException("Informe o cliente.", nameof(clienteId));
        if (solicitanteId == Guid.Empty) throw new ArgumentException("Informe o solicitante.", nameof(solicitanteId));
        var descricaoValida = DescricaoValida(descricao);
        ExigirUrgencia(urgencia);

        return new SolicitacaoDoCliente
        {
            ClienteId = clienteId,
            SolicitanteId = solicitanteId,
            Descricao = descricaoValida,
            Urgencia = urgencia,
        };
    }

    public void Alterar(string descricao, UrgenciaDaSolicitacao urgencia)
    {
        ExigirAguardando();
        var descricaoValida = DescricaoValida(descricao);
        ExigirUrgencia(urgencia);

        Descricao = descricaoValida;
        Urgencia = urgencia;
    }

    /// <summary>Não há volta: o cliente cancela uma vez e a solicitação deixa de aceitar mudanças.</summary>
    public void Cancelar(DateTime agoraUtc)
    {
        if (agoraUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A data deve estar em UTC.", nameof(agoraUtc));
        ExigirAguardando();

        CanceladaEm = agoraUtc;
    }

    /// <summary>Chamado ao anexar ou remover um documento da solicitação.</summary>
    public void RegistrarMudancaNosDocumentos()
    {
        ExigirAguardando();

        VersaoDosDocumentos++;
    }

    /// <summary>
    /// Liga a solicitação à atividade criada a partir dela. Uma solicitação cancelada pelo cliente só vira
    /// atividade se a Diretoria confirmar; o cancelamento fica registrado. A atividade tem de ter nascido desta
    /// solicitação (erro de programação, se não) e ser do cliente dela.
    /// </summary>
    public void VincularAtividade(Atividade atividade, bool confirmouCancelada)
    {
        ArgumentNullException.ThrowIfNull(atividade);
        if (atividade.SolicitacaoId != Id)
            throw new ArgumentException("A atividade não foi criada a partir desta solicitação.", nameof(atividade));
        if (atividade.ClienteId != ClienteId) throw new ExcecaoDeDominio(AtividadeDeOutroCliente);
        if (Situacao == SituacaoDaSolicitacao.EmAtividade) throw new ExcecaoDeDominio(JaVirouAtividade);
        if (Situacao == SituacaoDaSolicitacao.Cancelada && !confirmouCancelada)
            throw new ExcecaoDeDominio(CanceladaPeloCliente);

        AtividadeId = atividade.Id;
    }

    /// <summary>
    /// Desfaz o vínculo (a atividade foi excluída): a solicitação volta a <see cref="SituacaoDaSolicitacao.Aguardando"/>
    /// ou a <see cref="SituacaoDaSolicitacao.Cancelada"/>, conforme <see cref="CanceladaEm"/>. Sem atividade, não faz nada.
    /// </summary>
    public void DesvincularAtividade() => AtividadeId = null;

    public bool EhDoSolicitante(Guid usuarioId) => SolicitanteId == usuarioId;

    private void ExigirAguardando()
    {
        if (Situacao != SituacaoDaSolicitacao.Aguardando)
            throw new ExcecaoDeDominio(NaoPodeMaisSerAlterada);
    }

    private static void ExigirUrgencia(UrgenciaDaSolicitacao urgencia)
    {
        if (!Enum.IsDefined(urgencia))
            throw new ExcecaoDeDominio("Urgência inválida.");
    }

    private static string DescricaoValida(string? descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ExcecaoDeDominio("A descrição da solicitação é obrigatória.");
        var aparada = descricao.Trim();
        if (aparada.Length > TamanhoMaximoDescricao)
            throw new ExcecaoDeDominio($"A descrição da solicitação deve ter até {TamanhoMaximoDescricao} caracteres.");
        return aparada;
    }
}
