using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record SessaoIniciada(Guid UsuarioId, string Nome, TipoPerfil TipoPerfil, Guid VersaoDeSessao);

public sealed record AutenticarUsuario(string Email, string Senha) : IRequisicao<Resultado<SessaoIniciada>>;

public sealed class AutenticarUsuarioManipulador(
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IHasherDeSenha hasher,
    IRelogio relogio,
    IAuditoria auditoria) : IManipulador<AutenticarUsuario, Resultado<SessaoIniciada>>
{
    // Mesma mensagem para e-mail inexistente, usuário inativo e senha errada.
    private static readonly Resultado<SessaoIniciada> Invalido =
        Resultado<SessaoIniciada>.Falha(TipoErro.AcessoNegado, "E-mail ou senha inválidos.");

    private static readonly Resultado<SessaoIniciada> Bloqueado = Resultado<SessaoIniciada>.Falha(
        TipoErro.AcessoNegado, "Muitas tentativas de login. Aguarde 15 minutos e tente novamente.");

    public async Task<Resultado<SessaoIniciada>> ManipularAsync(AutenticarUsuario requisicao, CancellationToken ct)
    {
        var senha = requisicao.Senha ?? string.Empty;
        var usuario = Email.TentarCriar(requisicao.Email, out var email)
            ? await usuarios.ObterPorEmailAsync(email, ct)
            : null;

        if (usuario is null || !usuario.Ativo)
        {
            // Gasta o tempo de uma verificação, para a resposta não revelar que o e-mail não existe.
            hasher.Verificar(senha, hasher.HashDeDescarte);
            await auditoria.RegistrarAsync(EventosDeSeguranca.LoginFalha, ct: ct);
            return Invalido;
        }

        var agora = relogio.AgoraUtc;

        if (usuario.EstaBloqueado(agora))
        {
            await auditoria.RegistrarAsync(EventosDeSeguranca.LoginFalha, "Usuario", usuario.Id,
                new { motivo = "bloqueado" }, ct);
            return Bloqueado;
        }

        if (!hasher.Verificar(senha, usuario.SenhaHash))
        {
            var bloqueou = usuario.RegistrarFalhaDeLogin(agora);
            await usuarios.GravarEstadoDeLoginAsync(usuario, ct);
            await auditoria.RegistrarAsync(
                bloqueou ? EventosDeSeguranca.LoginBloqueado : EventosDeSeguranca.LoginFalha,
                "Usuario", usuario.Id, ct: ct);
            return bloqueou ? Bloqueado : Invalido;
        }

        var perfil = await perfis.ObterPorIdAsync(usuario.PerfilId, ct);
        if (perfil is null) return Invalido;

        usuario.RegistrarAcesso(agora);
        await usuarios.GravarEstadoDeLoginAsync(usuario, ct);
        await auditoria.RegistrarAsync(EventosDeSeguranca.LoginSucesso, "Usuario", usuario.Id, ct: ct);

        return Resultado<SessaoIniciada>.Ok(
            new SessaoIniciada(usuario.Id, usuario.Nome, perfil.Tipo, usuario.VersaoDeSessao));
    }
}
