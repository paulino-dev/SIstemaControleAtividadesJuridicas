using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Identidade;

public sealed class Perfil : Entidade
{
    public const int TamanhoMaximoNome = 100;

    private Perfil() { }

    public string Nome { get; private set; } = null!;
    public TipoPerfil Tipo { get; private set; }
    public bool Ativo { get; private set; }

    public static Perfil Criar(string nome, TipoPerfil tipo)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ExcecaoDeDominio("O nome do perfil é obrigatório.");
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome do perfil deve ter até {TamanhoMaximoNome} caracteres.");
        if (!Enum.IsDefined(tipo))
            throw new ExcecaoDeDominio("Tipo de perfil inválido.");

        return new Perfil { Nome = nome.Trim(), Tipo = tipo, Ativo = true };
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;
}
