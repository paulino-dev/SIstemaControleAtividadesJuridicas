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

/// <summary>
/// <paramref name="NoEscopo"/>: todas as atividades que o usuário pode listar, sem nenhum filtro (a Diretoria, o
/// escritório inteiro; os demais, as atividades em que são responsáveis). <paramref name="Atrasadas"/>: as atrasadas
/// desse mesmo escopo. São os contadores do subtítulo e dos chips da lista, que não mudam com o filtro.
/// </summary>
public sealed record PaginaDeAtividades(
    IReadOnlyList<AtividadeResumo> Itens, int Total, int Numero, int Tamanho, int NoEscopo, int Atrasadas)
    : Pagina<AtividadeResumo>(Itens, Total, Numero, Tamanho);

public sealed record ContagemDeAtividades(int Total, int Atrasadas);

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

public static class ProdutividadeOpcoes
{
    /// <summary>O mesmo limite da agenda. A tela pede um mês por vez.</summary>
    public const int PeriodoMaximoEmDias = 92;

    public const string ProdutorNaoEncontrado = "Produtor não encontrado.";
}

/// <summary>Uma atividade concluída em que o usuário é responsável.</summary>
public sealed record AtividadeRealizada(Guid Id, string Titulo, int Complexidade, DateTime ConcluidaEm, string? ClienteNome);

/// <summary>As atividades realizadas de uma complexidade, da conclusão mais antiga para a mais recente.</summary>
public sealed record GrupoDeComplexidade(int Complexidade, IReadOnlyList<AtividadeRealizada> Atividades)
{
    public int Total => Atividades.Count;
}

/// <summary>
/// <paramref name="Grupos"/>: um por complexidade, em ordem, sempre presentes mesmo vazios.
/// <paramref name="PrimeiraConclusaoEm"/>: a conclusão mais antiga entre as atividades hoje concluídas do produtor.
/// </summary>
public sealed record ProdutividadeDoProdutor(
    Guid ProdutorId, string ProdutorNome, IReadOnlyList<GrupoDeComplexidade> Grupos, DateTime? PrimeiraConclusaoEm)
{
    public int Total => Grupos.Sum(g => g.Total);
}

/// <summary>A situação que o cliente enxerga: os sete status internos se resumem a duas.</summary>
public enum SituacaoDoAndamento { EmAndamento = 1, Concluida = 2 }

public static class Andamento
{
    public static SituacaoDoAndamento SituacaoDe(StatusAtividade status) =>
        status == StatusAtividade.Concluida ? SituacaoDoAndamento.Concluida : SituacaoDoAndamento.EmAndamento;
}

/// <summary>
/// O que o cliente lê de uma atividade sua. Sem Id, descrição, responsáveis, direcionamentos nem status interno.
/// <paramref name="Processo"/>: o número CNJ formatado ou, na falta dele, o título do processo.
/// <paramref name="ConcluidaEm"/>: só quando a situação é <see cref="SituacaoDoAndamento.Concluida"/>.
/// </summary>
public sealed record AtividadeDoAndamento(
    string Titulo, string? AreaNome, string? Processo, SituacaoDoAndamento Situacao, DateTime? ConcluidaEm);

/// <summary><paramref name="EmAndamento"/> e <paramref name="Concluidas"/>: todas as atividades do cliente, qualquer que seja o filtro.</summary>
public sealed record AndamentoDoCliente(Pagina<AtividadeDoAndamento> Atividades, int EmAndamento, int Concluidas);

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

    /// <summary>
    /// Quantas atividades vivas há no escopo (todas, ou só aquelas em que <paramref name="responsavelId"/> é responsável)
    /// e quantas delas estão atrasadas: não concluídas, com entrega antes de <paramref name="hoje"/>. Nenhum outro filtro.
    /// </summary>
    Task<ContagemDeAtividades> ContarAsync(Guid? responsavelId, DateOnly hoje, CancellationToken ct);

    void Adicionar(Atividade atividade);

    /// <summary>Exclusão lógica da atividade e das suas filhas.</summary>
    void Remover(Atividade atividade);

    /// <summary>
    /// As atividades concluídas em <c>[deUtc, ateUtc)</c> em que o usuário é responsável, da conclusão mais antiga
    /// para a mais recente. O cliente excluído logicamente continua identificado pelo nome.
    /// </summary>
    Task<IReadOnlyList<AtividadeRealizada>> ListarRealizadasAsync(
        Guid responsavelId, DateTime deUtc, DateTime ateUtc, CancellationToken ct);

    /// <summary>
    /// A conclusão mais antiga entre as atividades hoje concluídas em que o usuário é responsável; nulo se não há
    /// nenhuma.
    /// </summary>
    Task<DateTime?> ObterPrimeiraConclusaoAsync(Guid responsavelId, CancellationToken ct);

    /// <summary>
    /// As atividades vivas do cliente, em andamento primeiro (as mais recentes) e depois as concluídas (da conclusão mais
    /// recente para a mais antiga), com os totais do cliente, que não dependem de <paramref name="situacao"/>.
    /// </summary>
    Task<AndamentoDoCliente> ListarAndamentoAsync(
        Guid clienteId, SituacaoDoAndamento? situacao, int pagina, int tamanho, CancellationToken ct);
}
