using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

public enum StatusProcesso
{
    Ativo = 1,
    Suspenso = 2,
    Arquivado = 3,
    Encerrado = 4,
}

public sealed record DadosDoProcesso(
    NumeroCnj? NumeroCnj, string Titulo, Guid AreaAtuacaoId, string? Orgao, string? Comarca,
    string? ParteContraria, DateOnly? DataDistribuicao, decimal? ValorCausa, string? Observacoes);

public sealed record ClienteDoProcesso(Guid ClienteId, PoloProcessual Polo);

/// <summary>O que mudou nos vínculos de um processo, pelos Ids dos clientes.</summary>
public sealed record AlteracaoDeClientes(
    IReadOnlyList<Guid> Adicionados, IReadOnlyList<Guid> Removidos, IReadOnlyList<Guid> PoloAlterado)
{
    public bool Houve => Adicionados.Count > 0 || Removidos.Count > 0 || PoloAlterado.Count > 0;
}

public sealed class Processo : Entidade
{
    public const int TamanhoMaximoTitulo = 200;
    public const int TamanhoMaximoOrgao = 200;
    public const int TamanhoMaximoComarca = 100;
    public const int TamanhoMaximoParteContraria = 200;
    public const int TamanhoMaximoObservacoes = 2000;

    private readonly List<ProcessoCliente> _clientes = [];

    private Processo() { }

    public NumeroCnj? NumeroCnj { get; private set; }
    public string Titulo { get; private set; } = null!;
    public Guid AreaAtuacaoId { get; private set; }
    public StatusProcesso Status { get; private set; }
    public string? Orgao { get; private set; }
    public string? Comarca { get; private set; }
    public string? ParteContraria { get; private set; }
    public DateOnly? DataDistribuicao { get; private set; }
    public decimal? ValorCausa { get; private set; }
    public string? Observacoes { get; private set; }

    public IReadOnlyCollection<ProcessoCliente> Clientes => _clientes;

    public static Processo Criar(DadosDoProcesso dados, IReadOnlyCollection<ClienteDoProcesso> clientes)
    {
        Validar(dados);
        Validar(clientes);

        var processo = new Processo { Status = StatusProcesso.Ativo };
        processo.Aplicar(dados);
        foreach (var cliente in clientes)
            processo._clientes.Add(new ProcessoCliente(processo.Id, cliente.ClienteId, cliente.Polo));
        return processo;
    }

    public void Alterar(DadosDoProcesso dados)
    {
        Validar(dados);
        Aplicar(dados);
    }

    /// <summary>
    /// Deixa o processo com exatamente os clientes informados. O vínculo de quem permanece é
    /// preservado (só o polo pode mudar); o de quem sai é removido da coleção.
    /// </summary>
    public AlteracaoDeClientes DefinirClientes(IReadOnlyCollection<ClienteDoProcesso> clientes)
    {
        Validar(clientes);

        var desejados = clientes.ToDictionary(c => c.ClienteId, c => c.Polo);
        var removidos = _clientes.Where(v => !desejados.ContainsKey(v.ClienteId)).ToList();
        var poloAlterado = new List<Guid>();
        var adicionados = new List<Guid>();

        foreach (var vinculo in removidos) _clientes.Remove(vinculo);

        foreach (var (clienteId, polo) in desejados)
        {
            var existente = _clientes.FirstOrDefault(v => v.ClienteId == clienteId);
            if (existente is null)
            {
                _clientes.Add(new ProcessoCliente(Id, clienteId, polo));
                adicionados.Add(clienteId);
            }
            else if (existente.Polo != polo)
            {
                existente.AlterarPolo(polo);
                poloAlterado.Add(clienteId);
            }
        }

        return new AlteracaoDeClientes(adicionados, [.. removidos.Select(v => v.ClienteId)], poloAlterado);
    }

    public void AlterarStatus(StatusProcesso status)
    {
        if (!Enum.IsDefined(status))
            throw new ExcecaoDeDominio("Situação de processo inválida.");
        Status = status;
    }

    private static void Validar(DadosDoProcesso dados)
    {
        ArgumentNullException.ThrowIfNull(dados);
        if (string.IsNullOrWhiteSpace(dados.Titulo))
            throw new ExcecaoDeDominio("O título do processo é obrigatório.");
        if (dados.AreaAtuacaoId == Guid.Empty)
            throw new ExcecaoDeDominio("A área de atuação é obrigatória.");
        if (dados.ValorCausa is < 0)
            throw new ExcecaoDeDominio("O valor da causa não pode ser negativo.");
        Limite(dados.Titulo, TamanhoMaximoTitulo, "título");
        Limite(dados.Orgao, TamanhoMaximoOrgao, "órgão");
        Limite(dados.Comarca, TamanhoMaximoComarca, "comarca");
        Limite(dados.ParteContraria, TamanhoMaximoParteContraria, "parte contrária");
        Limite(dados.Observacoes, TamanhoMaximoObservacoes, "observações");
    }

    private static void Validar(IReadOnlyCollection<ClienteDoProcesso> clientes)
    {
        ArgumentNullException.ThrowIfNull(clientes);
        if (clientes.Count == 0)
            throw new ExcecaoDeDominio("O processo precisa de ao menos um cliente.");
        if (clientes.Any(c => c.ClienteId == Guid.Empty))
            throw new ExcecaoDeDominio("Cliente inválido.");
        if (clientes.Any(c => !Enum.IsDefined(c.Polo)))
            throw new ExcecaoDeDominio("Polo inválido.");
        if (clientes.Select(c => c.ClienteId).Distinct().Count() != clientes.Count)
            throw new ExcecaoDeDominio("Um cliente não pode se repetir no mesmo processo.");
    }

    private void Aplicar(DadosDoProcesso dados)
    {
        NumeroCnj = dados.NumeroCnj;
        Titulo = dados.Titulo.Trim();
        AreaAtuacaoId = dados.AreaAtuacaoId;
        Orgao = Opcional(dados.Orgao);
        Comarca = Opcional(dados.Comarca);
        ParteContraria = Opcional(dados.ParteContraria);
        DataDistribuicao = dados.DataDistribuicao;
        ValorCausa = dados.ValorCausa;
        Observacoes = Opcional(dados.Observacoes);
    }

    private static void Limite(string? valor, int maximo, string campo)
    {
        if (valor is not null && valor.Trim().Length > maximo)
            throw new ExcecaoDeDominio($"O campo {campo} deve ter até {maximo} caracteres.");
    }

    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
