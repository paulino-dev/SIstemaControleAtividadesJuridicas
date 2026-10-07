namespace AtividadesJuridicas.Application.Comum;

public enum TipoErro
{
    Validacao,
    NaoEncontrado,
    AcessoNegado,
    Conflito,
}

public class Resultado
{
    protected Resultado(bool sucesso, TipoErro? erro, string? mensagem)
    {
        Sucesso = sucesso;
        Erro = erro;
        Mensagem = mensagem;
    }

    public bool Sucesso { get; }
    public TipoErro? Erro { get; }
    public string? Mensagem { get; }

    public static Resultado Ok() => new(true, null, null);

    public static Resultado Falha(TipoErro erro, string mensagem) => new(false, erro, mensagem);
}

public sealed class Resultado<T> : Resultado
{
    private Resultado(T? valor, bool sucesso, TipoErro? erro, string? mensagem)
        : base(sucesso, erro, mensagem) => Valor = valor;

    public T? Valor { get; }

    public static Resultado<T> Ok(T valor) => new(valor, true, null, null);

    public static new Resultado<T> Falha(TipoErro erro, string mensagem) => new(default, false, erro, mensagem);
}
