using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Usado pela interface para decidir o que exibir. Nulo quando o usuário não tem mais acesso.</summary>
public sealed record ObterMeuAcesso : IRequisicao<UsuarioAutenticado?>, IRequisicaoAbertaAoCliente;

public sealed class ObterMeuAcessoManipulador(IContextoDeAcesso contexto)
    : IManipulador<ObterMeuAcesso, UsuarioAutenticado?>
{
    public Task<UsuarioAutenticado?> ManipularAsync(ObterMeuAcesso requisicao, CancellationToken ct) =>
        contexto.ObterAsync(ct);
}
