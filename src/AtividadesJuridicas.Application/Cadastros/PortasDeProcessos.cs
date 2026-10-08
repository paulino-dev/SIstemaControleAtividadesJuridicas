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

    Task<ProcessoDetalhe?> ObterDetalheAsync(Guid id, CancellationToken ct);

    void Adicionar(Processo processo);
}

public sealed record DadosDoProcessoEntrada(
    string? NumeroCnj, string Titulo, Guid AreaAtuacaoId, string? Orgao, string? Comarca, string? ParteContraria,
    DateOnly? DataDistribuicao, decimal? ValorCausa, string? Observacoes);

public sealed record ClienteDoProcessoEntrada(Guid ClienteId, PoloProcessual Polo);

/// <summary><paramref name="NumeroCnj"/> vem formatado; <paramref name="Clientes"/> são os nomes, em ordem alfabética.</summary>
public sealed record ProcessoResumo(
    Guid Id, string? NumeroCnj, string Titulo, string AreaNome, StatusProcesso Status,
    IReadOnlyList<string> Clientes, string? ParteContraria);

/// <summary><paramref name="Documento"/> vem formatado (CPF ou CNPJ).</summary>
public sealed record ClienteDoProcessoDetalhe(Guid ClienteId, string Nome, string Documento, PoloProcessual Polo, bool Ativo);

/// <summary><paramref name="NumeroCnj"/> vem formatado; <paramref name="Clientes"/> em ordem alfabética.</summary>
public sealed record ProcessoDetalhe(
    Guid Id, string? NumeroCnj, string Titulo, Guid AreaAtuacaoId, string AreaNome, StatusProcesso Status,
    string? Orgao, string? Comarca, string? ParteContraria, DateOnly? DataDistribuicao, decimal? ValorCausa,
    string? Observacoes, IReadOnlyList<ClienteDoProcessoDetalhe> Clientes);
