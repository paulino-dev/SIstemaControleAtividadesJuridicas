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

        // A tentativa é contada de forma atômica ANTES de verificar a senha: o número de senhas
        // verificadas por janela de bloqueio fica limitado, mesmo com requisições simultâneas.
        var tentativa = await usuarios.ReservarTentativaDeLoginAsync(usuario.Id, agora, ct);

        if (tentativa == TentativaDeLogin.Recusada)
        {
            await auditoria.RegistrarAsync(EventosDeSeguranca.LoginFalha, "Usuario", usuario.Id,
                new { motivo = "bloqueado" }, ator: usuario.Id, ct: ct);
            return Bloqueado;
        }

        if (!hasher.Verificar(senha, usuario.SenhaHash))
        {
            var bloqueou = tentativa == TentativaDeLogin.AdmitidaEBloqueou;
            await auditoria.RegistrarAsync(
                bloqueou ? EventosDeSeguranca.LoginBloqueado : EventosDeSeguranca.LoginFalha,
                "Usuario", usuario.Id, ator: usuario.Id, ct: ct);
            return bloqueou ? Bloqueado : Invalido;
        }

        var perfil = await perfis.ObterPorIdAsync(usuario.PerfilId, ct);
        if (perfil is null)
        {
            await auditoria.RegistrarAsync(EventosDeSeguranca.LoginFalha, "Usuario", usuario.Id, ator: usuario.Id, ct: ct);
            return Invalido;
        }

        await usuarios.RegistrarLoginBemSucedidoAsync(usuario.Id, agora, ct);
        await auditoria.RegistrarAsync(EventosDeSeguranca.LoginSucesso, "Usuario", usuario.Id, ator: usuario.Id, ct: ct);

        return Resultado<SessaoIniciada>.Ok(
            new SessaoIniciada(usuario.Id, usuario.Nome, perfil.Tipo, usuario.VersaoDeSessao));
    }
}
