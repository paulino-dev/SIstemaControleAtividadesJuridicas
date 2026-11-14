using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.Application.Solicitacoes;

public static class SolicitacoesOpcoes
{
    public const int TamanhoDaPagina = 20;
    public const int MaximoDeDocumentos = 10;
    public const int TamanhoDoTrecho = 140;
    public const string NaoEncontrada = "Solicitação não encontrada.";
    public const string Alterada = "A solicitação foi alterada. Recarregue e tente de novo.";
    public const string SoQuemAbriu = "Só quem abriu a solicitação pode alterá-la.";
    public const string LimiteDeDocumentos = "Anexe até 10 documentos por solicitação.";
    public const string AlteradaDuranteACriacao =
        "O cliente alterou a solicitação enquanto você criava a atividade. Confira os dados atualizados.";
    public const string DocumentoForaDaSolicitacao = "Um documento marcado não está mais nesta solicitação.";

    /// <summary>
    /// Os primeiros <see cref="TamanhoDoTrecho"/> caracteres da descrição, com "…" quando ela foi cortada. O corte
    /// não parte um par substituto (um emoji, por exemplo) ao meio.
    /// </summary>
    public static string Trecho(string descricao)
    {
        if (descricao.Length <= TamanhoDoTrecho) return descricao;

        var corte = char.IsHighSurrogate(descricao[TamanhoDoTrecho - 1]) ? TamanhoDoTrecho - 1 : TamanhoDoTrecho;
        return descricao[..corte] + "…";
    }
}

/// <summary>
/// A situação que o cliente enxerga. As duas últimas vêm da atividade viva vinculada; sem atividade viva
/// vale a situação da própria solicitação.
/// </summary>
public enum SituacaoParaOCliente
{
    AguardandoAnalise = 1,
    Cancelada = 2,
    EmAndamento = 3,
    Concluida = 4,
}

/// <summary>A linha de solicitação que o portal mostra. <paramref name="Documentos"/>: os documentos vivos dela.</summary>
public sealed record SolicitacaoDoPortal(
    Guid Id, DateTime AbertaEm, string Trecho, UrgenciaDaSolicitacao Urgencia, SituacaoParaOCliente Situacao,
    string SolicitanteNome, int Documentos);

/// <summary>A linha de solicitação da Diretoria, com a situação do domínio.</summary>
public sealed record SolicitacaoResumo(
    Guid Id, DateTime AbertaEm, Guid ClienteId, string ClienteNome, string SolicitanteNome,
    UrgenciaDaSolicitacao Urgencia, string Trecho, int Documentos, SituacaoDaSolicitacao Situacao);

/// <summary>A atividade viva que nasceu de uma solicitação.</summary>
public sealed record AtividadeDaSolicitacao(Guid Id, string Titulo, StatusAtividade Status, DateTime? ConcluidaEm);

public interface ISolicitacaoRepositorio
{
    /// <summary>A solicitação rastreada, para alterá-la. Nulo se não existe ou foi excluída.</summary>
    Task<SolicitacaoDoCliente?> ObterPorIdAsync(Guid id, CancellationToken ct);

    void Adicionar(SolicitacaoDoCliente solicitacao);

    /// <summary>
    /// As solicitações vivas do cliente, da mais recente para a mais antiga. A situação (e o filtro por ela) é a
    /// vista pelo cliente: segue a atividade viva vinculada.
    /// </summary>
    Task<Pagina<SolicitacaoDoPortal>> ListarDoClienteAsync(
        Guid clienteId, SituacaoParaOCliente? situacao, int pagina, int tamanho, CancellationToken ct);

    /// <summary>
    /// Todas as solicitações vivas, da mais antiga para a mais nova. A situação (e o filtro por ela) é a do domínio.
    /// </summary>
    Task<Pagina<SolicitacaoResumo>> ListarAsync(
        SituacaoDaSolicitacao? situacao, Guid? clienteId, int pagina, int tamanho, CancellationToken ct);

    /// <summary>A atividade vinculada, só se ela estiver viva (não excluída).</summary>
    Task<AtividadeDaSolicitacao?> ObterAtividadeAsync(Guid atividadeId, CancellationToken ct);

    /// <summary>O nome do usuário, mesmo que ele tenha sido excluído logicamente.</summary>
    Task<string?> NomeDoUsuarioAsync(Guid usuarioId, CancellationToken ct);

    /// <summary>O nome do cliente, mesmo que ele tenha sido excluído logicamente.</summary>
    Task<string?> NomeDoClienteAsync(Guid clienteId, CancellationToken ct);
}
