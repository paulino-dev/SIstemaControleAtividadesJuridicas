using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record DefinirAtivoUsuario(Guid Id, bool Ativo) : IRequisicao<Resultado>;

public sealed class DefinirAtivoUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IUnidadeDeTrabalho unidade) : IManipulador<DefinirAtivoUsuario, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirAtivoUsuario requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var usuario = await usuarios.ObterPorIdAsync(requisicao.Id, ct);
        if (usuario is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Usuário não encontrado.");

        if (requisicao.Ativo)
        {
            usuario.Ativar();
        }
        else
        {
            if (await UltimoDiretor.EhAsync(usuario, usuarios, perfis, ct))
                return Resultado.Falha(TipoErro.Conflito, UltimoDiretor.Mensagem);
            usuario.Inativar();
        }

        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
