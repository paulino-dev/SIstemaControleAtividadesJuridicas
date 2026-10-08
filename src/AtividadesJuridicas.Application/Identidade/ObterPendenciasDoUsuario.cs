using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Apoio da tela de usuários antes de inativar alguém; não é uma consulta auditável.</summary>
public sealed record ObterPendenciasDoUsuario(Guid Id) : IRequisicao<Resultado<PendenciasDoUsuario>>;

public sealed class ObterPendenciasDoUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IPendenciasDoUsuario pendencias,
    IRelogio relogio) : IManipulador<ObterPendenciasDoUsuario, Resultado<PendenciasDoUsuario>>
{
    public async Task<Resultado<PendenciasDoUsuario>> ManipularAsync(ObterPendenciasDoUsuario requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<PendenciasDoUsuario>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (await usuarios.ObterPorIdAsync(requisicao.Id, ct) is null)
            return Resultado<PendenciasDoUsuario>.Falha(TipoErro.NaoEncontrado, "Usuário não encontrado.");

        return Resultado<PendenciasDoUsuario>.Ok(await pendencias.ContarAsync(requisicao.Id, relogio.AgoraUtc, ct));
    }
}
