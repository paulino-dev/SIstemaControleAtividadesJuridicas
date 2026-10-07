using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record AutenticarUsuario(string Email, string Senha) : IRequisicao<Resultado<UsuarioAutenticado>>;

public sealed class AutenticarUsuarioManipulador(
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IHasherDeSenha hasher,
    IUnidadeDeTrabalho unidade,
    IRelogio relogio) : IManipulador<AutenticarUsuario, Resultado<UsuarioAutenticado>>
{
    // Mensagem única: não revela se o e-mail existe nem se o usuário está inativo.
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

        usuario.RegistrarAcesso(relogio.AgoraUtc);
        await unidade.SalvarAsync(ct);

        return Resultado<UsuarioAutenticado>.Ok(new UsuarioAutenticado(usuario.Id, usuario.Nome, perfil.Tipo));
    }
}
