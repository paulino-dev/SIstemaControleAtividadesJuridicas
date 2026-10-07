using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record RedefinirSenhaUsuario(Guid Id, string NovaSenha) : IRequisicao<Resultado>;

public sealed class RedefinirSenhaUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IHasherDeSenha hasher,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<RedefinirSenhaUsuario, Resultado>
{
    public async Task<Resultado> ManipularAsync(RedefinirSenhaUsuario requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (PoliticaDeSenha.Validar(requisicao.NovaSenha) is { } erro)
            return Resultado.Falha(TipoErro.Validacao, erro);

        var usuario = await usuarios.ObterPorIdAsync(requisicao.Id, ct);
        if (usuario is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Usuário não encontrado.");

        usuario.DefinirSenha(hasher.Gerar(requisicao.NovaSenha));
        usuario.Desbloquear();
        auditoria.Anotar(EventosDeSeguranca.SenhaRedefinida, "Usuario", usuario.Id);
        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
