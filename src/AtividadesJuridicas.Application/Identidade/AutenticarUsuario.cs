using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record AutenticarUsuario(string Email, string Senha) : IRequisicao<Resultado<UsuarioAutenticado>>;

public sealed class AutenticarUsuarioManipulador(
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IHasherDeSenha hasher,
    IRelogio relogio) : IManipulador<AutenticarUsuario, Resultado<UsuarioAutenticado>>
{
    // Mensagem uniforme para todas as falhas de credencial (e-mail inexistente, usuário inativo,
    // senha errada). Isto não impede inferência pelo tempo de resposta.
    private static readonly Resultado<UsuarioAutenticado> Invalido =
        Resultado<UsuarioAutenticado>.Falha(TipoErro.AcessoNegado, "E-mail ou senha inválidos.");

    public async Task<Resultado<UsuarioAutenticado>> ManipularAsync(AutenticarUsuario requisicao, CancellationToken ct)
    {
        if (!Email.TentarCriar(requisicao.Email, out var email)) return Invalido;

        var usuario = await usuarios.ObterPorEmailAsync(email, ct);
        if (usuario is null || !usuario.Ativo) return Invalido;
        if (!hasher.Verificar(requisicao.Senha ?? string.Empty, usuario.SenhaHash)) return Invalido;

        var perfil = await perfis.ObterPorIdAsync(usuario.PerfilId, ct);
        if (perfil is null) return Invalido;

        await usuarios.RegistrarAcessoAsync(usuario.Id, relogio.AgoraUtc, ct);

        return Resultado<UsuarioAutenticado>.Ok(new UsuarioAutenticado(usuario.Id, usuario.Nome, perfil.Tipo));
    }
}
