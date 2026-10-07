using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record DefinirAtivoUsuario(Guid Id, bool Ativo) : IRequisicao<Resultado>;

public sealed class DefinirAtivoUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<DefinirAtivoUsuario, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirAtivoUsuario requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var usuario = await usuarios.ObterPorIdAsync(requisicao.Id, ct);
        if (usuario is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Usuário não encontrado.");

        if (usuario.Ativo == requisicao.Ativo) return Resultado.Ok();

        if (requisicao.Ativo)
        {
            usuario.Ativar();
            auditoria.Anotar(EventosDeSeguranca.UsuarioAtivado, "Usuario", usuario.Id);
        }
        else
        {
            if (await UltimoDiretor.EhAsync(usuario, usuarios, perfis, ct))
                return Resultado.Falha(TipoErro.Conflito, UltimoDiretor.Mensagem);
            usuario.Inativar();
            auditoria.Anotar(EventosDeSeguranca.UsuarioInativado, "Usuario", usuario.Id);
        }

        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
