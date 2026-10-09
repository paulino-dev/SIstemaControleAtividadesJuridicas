using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary>As duas condições que o vínculo com o cliente impõe ao acesso de um usuário.</summary>
public static class AcessoPeloCliente
{
    /// <summary>Só usuário de perfil Cliente tem cliente; o par incoerente (edição direta no banco) não tem acesso.</summary>
    public static bool ParCoerente(Usuario usuario, Perfil perfil) =>
        (perfil.Tipo == TipoPerfil.Cliente) == usuario.EhDeCliente;

    /// <summary>Verdadeiro para a equipe, sem consultar clientes; para o usuário de cliente, só com o cliente existente e ativo.</summary>
    public static async Task<bool> ClienteAtivoAsync(Usuario usuario, IClientesDaIdentidade clientes, CancellationToken ct) =>
        usuario.ClienteId is not { } clienteId || await clientes.ObterAsync(clienteId, ct) is { Ativo: true };
}
