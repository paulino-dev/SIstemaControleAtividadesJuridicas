using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Atividades;

namespace AtividadesJuridicas.Application.Atividades;

public static class AtividadesOpcoes
{
    public const int TamanhoDaPagina = 20;
}

/// <summary><paramref name="Hoje"/>: a data de hoje no fuso do escritório, para decidir o que está atrasado.</summary>
public sealed record FiltroDeAtividades(
    StatusAtividade? Status, Guid? ResponsavelId, Guid? RevisorId, Guid? ClienteId, Guid? ProcessoId, bool SomenteAtrasadas,
    string? Busca, DateOnly Hoje);

public sealed record AtividadeResumo(
    Guid Id, string Titulo, StatusAtividade Status, DateOnly? DataEntrega, bool Atrasada, string? ClienteNome,
    string? ProcessoTitulo, string? AreaNome, IReadOnlyList<string> Responsaveis, string? RevisorNome, int Complexidade);

/// <summary><paramref name="Direcionamento"/> só vem para a Diretoria e para o próprio responsável.</summary>
public sealed record ResponsavelDetalhe(
    Guid UsuarioId, string Nome, bool DirecionamentoVisivel, string? Direcionamento, DateTime? ConcluidoEm);

public sealed record HistoricoDetalhe(
    StatusAtividade? StatusAnterior, StatusAtividade StatusNovo, string? UsuarioNome, DateTime DataHora, string? Motivo);

/// <summary>O que o usuário pode fazer com a atividade agora.</summary>
public sealed record AcoesDaAtividade(
    bool Editar, bool DefinirResponsaveis, bool TrocarRevisor, bool ConcluirMinhaParte, bool AprovarOuDevolver,
    bool Reabrir, bool Excluir);

/// <summary><paramref name="Versao"/>: a versão da linha, que os formulários devolvem como versão esperada.</summary>
public sealed record AtividadeDetalhe(
    Guid Id, string Titulo, string Descricao, DateOnly? DataEntrega, StatusAtividade Status, bool Atrasada,
    DateTime? ConcluidaEm, Guid? ClienteId, string? ClienteNome, Guid? ProcessoId, string? ProcessoTitulo,
    Guid? AreaAtuacaoId, string? AreaNome, TipoAutor TipoAutor, string? AutorNome, Guid? RevisorId, string? RevisorNome,
    IReadOnlyList<ResponsavelDetalhe> Responsaveis, IReadOnlyList<HistoricoDetalhe> Historico, AcoesDaAtividade Acoes,
    uint Versao, int Complexidade);

/// <summary>O cliente que a atividade tem agora, lido do banco sem rastreamento.</summary>
public sealed record ClienteAtualDaAtividade(Guid? ClienteId);

public interface IAtividadeRepositorio
{
    /// <summary>A atividade com todos os responsáveis e todo o histórico.</summary>
    Task<Atividade?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// O cliente que a atividade tem agora, lido do banco sem devolver a instância já rastreada neste escopo
    /// (que pode estar defasada). Nulo quando a atividade não existe mais ou foi excluída.
    /// </summary>
    Task<ClienteAtualDaAtividade?> ObterClienteAtualAsync(Guid atividadeId, CancellationToken ct);

    /// <summary>
    /// Ordenada por data de entrega (sem data por último) e, depois, pelas mais recentes. A busca procura no título,
    /// sem diferenciar maiúsculas nem acentos, tratando % e _ como caracteres comuns.
    /// </summary>
    Task<Pagina<AtividadeResumo>> ListarAsync(FiltroDeAtividades filtro, int pagina, int tamanho, CancellationToken ct);

    void Adicionar(Atividade atividade);

    /// <summary>Exclusão lógica da atividade e das suas filhas.</summary>
    void Remover(Atividade atividade);
}
