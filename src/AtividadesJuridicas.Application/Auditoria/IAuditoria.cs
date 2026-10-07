namespace AtividadesJuridicas.Application.Auditoria;

public static class EventosDeSeguranca
{
    public const string LoginSucesso = "login_sucesso";
    public const string LoginFalha = "login_falha";
    public const string LoginBloqueado = "login_bloqueado";
    public const string Logout = "logout";
    public const string SenhaAlterada = "senha_alterada";
    public const string SenhaRedefinida = "senha_redefinida";
    public const string PerfilAlterado = "perfil_alterado";
    public const string UsuarioAtivado = "usuario_ativado";
    public const string UsuarioInativado = "usuario_inativado";
    public const string AcessoNegado = "acesso_negado";
}

public interface IAuditoria
{
    /// <summary>
    /// Deixa o evento pendente; ele é gravado pelo próximo <c>SalvarAsync</c>, na mesma transação
    /// da alteração que o acompanha. Se nada for salvo, o evento não é gravado.
    /// </summary>
    void Anotar(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null);

    /// <summary>
    /// Grava o evento imediatamente, em conexão própria. Não grava nem descarta o que estiver
    /// pendente no escopo. Para fatos que não acompanham uma alteração (login, acesso negado).
    /// </summary>
    Task RegistrarAsync(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null,
        CancellationToken ct = default);
}
