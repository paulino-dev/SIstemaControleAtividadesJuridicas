using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record SessaoIniciada(Guid UsuarioId, string Nome, TipoPerfil TipoPerfil, Guid VersaoDeSessao);

public sealed record AutenticarUsuario(string Email, string Senha) : IRequisicao<Resultado<SessaoIniciada>>;

public sealed class AutenticarUsuarioManipulador(
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IHasherDeSenha hasher,
    IRelogio relogio) : IManipulador<AutenticarUsuario, Resultado<SessaoIniciada>>
{
    // Mensagem uniforme para todas as falhas de credencial (e-mail inexistente, usuário inativo,
    // senha errada). Isto não impede inferência pelo tempo de resposta.
    private static readonly Resultado<SessaoIniciada> Invalido =
        Resultado<SessaoIniciada>.Falha(TipoErro.AcessoNegado, "E-mail ou senha inválidos.");

    public async Task<Resultado<SessaoIniciada>> ManipularAsync(AutenticarUsuario requisicao, CancellationToken ct)
    {
        if (!Email.TentarCriar(requisicao.Email, out var email)) return Invalido;

        var usuario = await usuarios.ObterPorEmailAsync(email, ct);
        if (usuario is null || !usuario.Ativo) return Invalido;
        if (!hasher.Verificar(requisicao.Senha ?? string.Empty, usuario.SenhaHash)) return Invalido;

        var perfil = await perfis.ObterPorIdAsync(usuario.PerfilId, ct);
        if (perfil is null) return Invalido;

        await usuarios.RegistrarAcessoAsync(usuario.Id, relogio.AgoraUtc, ct);

        return Resultado<SessaoIniciada>.Ok(
            new SessaoIniciada(usuario.Id, usuario.Nome, perfil.Tipo, usuario.VersaoDeSessao));
    }
}
