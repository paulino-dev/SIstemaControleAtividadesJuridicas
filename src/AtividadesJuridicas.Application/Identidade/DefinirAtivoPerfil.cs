using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record DefinirAtivoPerfil(Guid Id, bool Ativo) : IRequisicao<Resultado>;

public sealed class DefinirAtivoPerfilManipulador(
    IContextoDeAcesso contexto,
    IPerfilRepositorio perfis,
    IUsuarioRepositorio usuarios,
    IUnidadeDeTrabalho unidade) : IManipulador<DefinirAtivoPerfil, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirAtivoPerfil requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var perfil = await perfis.ObterPorIdAsync(requisicao.Id, ct);
        if (perfil is null) return Resultado.Falha(TipoErro.NaoEncontrado, "Perfil não encontrado.");

        if (perfil.Ativo == requisicao.Ativo) return Resultado.Ok();

        if (requisicao.Ativo)
        {
            perfil.Ativar();
        }
        else
        {
            // Perfil inativo não pode estar atribuído a quem ainda usa o sistema.
            if (await usuarios.ContarAtivosComPerfilAsync(perfil.Id, ct) > 0)
                return Resultado.Falha(TipoErro.Conflito,
                    "Há usuários ativos com este perfil. Troque o perfil deles antes de inativar.");
            perfil.Inativar();
        }

        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
