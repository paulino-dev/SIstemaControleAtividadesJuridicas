using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Domain.Cadastros;

public enum TipoCliente
{
    PessoaFisica = 1,
    PessoaJuridica = 2,
}

/// <summary>Dados comuns aos dois tipos de cliente.</summary>
public sealed record DadosDoCliente(string Nome, Email? Email, string? Telefone, Endereco Endereco, string? Observacoes);

public abstract class Cliente : Entidade
{
    public const int TamanhoMaximoNome = 200;
    public const int TamanhoMaximoTelefone = 30;
    public const int TamanhoMaximoObservacoes = 2000;

    protected Cliente(TipoCliente tipo)
    {
        Tipo = tipo;
        Ativo = true;
    }

    /// <summary>Fixado na criação; um cliente não muda de tipo.</summary>
    public TipoCliente Tipo { get; private set; }

    /// <summary>Nome completo da pessoa física ou razão social da pessoa jurídica.</summary>
    public string Nome { get; private set; } = null!;

    public Email? Email { get; private set; }
    public string? Telefone { get; private set; }
    public Endereco Endereco { get; private set; } = Endereco.Vazio;
    public string? Observacoes { get; private set; }
    public bool Ativo { get; private set; }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;

    /// <summary>Valida tudo antes de alterar qualquer coisa, para não deixar o cliente pela metade.</summary>
    protected static void Validar(DadosDoCliente dados)
    {
        ArgumentNullException.ThrowIfNull(dados);
        ArgumentNullException.ThrowIfNull(dados.Endereco);
        if (string.IsNullOrWhiteSpace(dados.Nome))
            throw new ExcecaoDeDominio("O nome do cliente é obrigatório.");
        Limite(dados.Nome, TamanhoMaximoNome, "nome");
        Limite(dados.Telefone, TamanhoMaximoTelefone, "telefone");
        Limite(dados.Observacoes, TamanhoMaximoObservacoes, "observações");
    }

    protected void Aplicar(DadosDoCliente dados)
    {
        Nome = dados.Nome.Trim();
        Email = dados.Email;
        Telefone = Opcional(dados.Telefone);
        Endereco = dados.Endereco;
        Observacoes = Opcional(dados.Observacoes);
    }

    protected static void Limite(string? valor, int maximo, string campo)
    {
        if (valor is not null && valor.Trim().Length > maximo)
            throw new ExcecaoDeDominio($"O campo {campo} deve ter até {maximo} caracteres.");
    }

    protected static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
