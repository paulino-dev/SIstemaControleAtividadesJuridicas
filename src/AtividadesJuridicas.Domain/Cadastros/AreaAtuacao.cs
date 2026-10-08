using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

/// <summary>Ramo do direito (Trabalhista, Cível, Tributário…).</summary>
public sealed class AreaAtuacao : Entidade
{
    public const int TamanhoMaximoNome = 100;

    private AreaAtuacao() { }

    public string Nome { get; private set; } = null!;
    public bool Ativo { get; private set; }

    public static AreaAtuacao Criar(string nome)
    {
        var area = new AreaAtuacao { Ativo = true };
        area.Renomear(nome);
        return area;
    }

    public void Renomear(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ExcecaoDeDominio("O nome da área é obrigatório.");
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome da área deve ter até {TamanhoMaximoNome} caracteres.");
        Nome = nome.Trim();
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;
}
