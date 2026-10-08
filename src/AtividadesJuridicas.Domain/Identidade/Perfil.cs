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
        var nomeValido = NomeValido(nome);
        if (!Enum.IsDefined(tipo))
            throw new ExcecaoDeDominio("Tipo de perfil inválido.");

        return new Perfil { Nome = nomeValido, Tipo = tipo, Ativo = true };
    }

    /// <summary>O tipo não muda depois de criado: as regras de acesso dependem dele.</summary>
    public void Renomear(string nome) => Nome = NomeValido(nome);

    private static string NomeValido(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ExcecaoDeDominio("O nome do perfil é obrigatório.");
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome do perfil deve ter até {TamanhoMaximoNome} caracteres.");
        return nome.Trim();
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;
}
