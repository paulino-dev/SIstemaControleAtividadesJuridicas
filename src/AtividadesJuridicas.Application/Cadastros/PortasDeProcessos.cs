using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Application.Cadastros;

public static class ProcessosOpcoes
{
    public const int TamanhoDaPagina = 20;
}

public sealed record FiltroDeProcessos(string? Busca = null, Guid? ClienteId = null, StatusProcesso? Status = null);

public interface IProcessoRepositorio
{
    /// <summary>O processo com os seus vínculos de cliente.</summary>
    Task<Processo?> ObterPorIdAsync(Guid id, CancellationToken ct);

    Task<bool> ExisteNumeroCnjAsync(NumeroCnj numero, Guid? excetoId, CancellationToken ct);

    /// <summary>
    /// Ordenado por título. Texto que é um número CNJ válido procura pelo número exato; qualquer outro
    /// procura no título e na parte contrária, sem diferenciar maiúsculas nem acentos, tratando % e _
    /// como caracteres comuns.
    /// </summary>
    Task<Pagina<ProcessoResumo>> ListarAsync(FiltroDeProcessos filtro, int pagina, int tamanho, CancellationToken ct);

    /// <summary>Quantos processos a busca e o cliente do filtro encontram; quem chama zera a situação para contar todas.</summary>
    Task<int> ContarAsync(FiltroDeProcessos filtro, CancellationToken ct);

    Task<ProcessoDetalhe?> ObterDetalheAsync(Guid id, CancellationToken ct);

    void Adicionar(Processo processo);
}

/// <summary>O que o cadastro de processos precisa saber das atividades e pedir a elas.</summary>
public interface IAtividadesDoProcesso
{
    /// <summary>Dentre os clientes informados, os que têm atividade viva neste processo.</summary>
    Task<IReadOnlyList<Guid>> ClientesComAtividadeAsync(Guid processoId, IReadOnlyCollection<Guid> clienteIds, CancellationToken ct);

    /// <summary>
    /// Faz as atividades do processo acompanharem a nova área. Só altera as entidades no contexto da
    /// requisição: quem grava é o SalvarAsync do caso de uso, na mesma transação. Devolve quantas mudaram.
    /// </summary>
    Task<int> AcompanharAreaAsync(Guid processoId, Guid areaAtuacaoId, CancellationToken ct);
}

public sealed record DadosDoProcessoEntrada(
    string? NumeroCnj, string Titulo, Guid AreaAtuacaoId, string? Orgao, string? Comarca, string? ParteContraria,
    DateOnly? DataDistribuicao, decimal? ValorCausa, string? Observacoes);

public sealed record ClienteDoProcessoEntrada(Guid ClienteId, PoloProcessual Polo);

/// <summary><paramref name="NumeroCnj"/> vem formatado; <paramref name="Clientes"/> são os nomes, em ordem alfabética.</summary>
public sealed record ProcessoResumo(
    Guid Id, string? NumeroCnj, string Titulo, string AreaNome, StatusProcesso Status,
    IReadOnlyList<string> Clientes, string? ParteContraria);

/// <summary><paramref name="TodasAsSituacoes"/>: quantos processos a busca e o cliente encontram, em qualquer situação.</summary>
public sealed record PaginaDeProcessos(
    IReadOnlyList<ProcessoResumo> Itens, int Total, int Numero, int Tamanho, int TodasAsSituacoes)
    : Pagina<ProcessoResumo>(Itens, Total, Numero, Tamanho);

/// <summary><paramref name="Documento"/> vem formatado (CPF ou CNPJ).</summary>
public sealed record ClienteDoProcessoDetalhe(Guid ClienteId, string Nome, string Documento, PoloProcessual Polo, bool Ativo);

/// <summary><paramref name="NumeroCnj"/> vem formatado; <paramref name="Clientes"/> em ordem alfabética.</summary>
public sealed record ProcessoDetalhe(
    Guid Id, string? NumeroCnj, string Titulo, Guid AreaAtuacaoId, string AreaNome, StatusProcesso Status,
    string? Orgao, string? Comarca, string? ParteContraria, DateOnly? DataDistribuicao, decimal? ValorCausa,
    string? Observacoes, IReadOnlyList<ClienteDoProcessoDetalhe> Clientes);
