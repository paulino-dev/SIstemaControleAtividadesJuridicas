using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record EditarUsuario(Guid Id, string Nome, string Email, Guid PerfilId, string? NumeroOab)
    : IRequisicao<Resultado>;

public sealed class EditarUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IUnidadeDeTrabalho unidade) : IManipulador<EditarUsuario, Resultado>
{
    private const string EmailEmUso = "Já existe um usuário com este e-mail.";

    public async Task<Resultado> ManipularAsync(EditarUsuario requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var usuario = await usuarios.ObterPorIdAsync(requisicao.Id, ct);
        if (usuario is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Usuário não encontrado.");

        if (!Email.TentarCriar(requisicao.Email, out var email))
            return Resultado.Falha(TipoErro.Validacao, "E-mail inválido.");

        var perfil = await perfis.ObterPorIdAsync(requisicao.PerfilId, ct);
        if (perfil is null)
            return Resultado.Falha(TipoErro.Validacao, "Perfil não encontrado.");

        if (await usuarios.ExisteEmailAsync(email, usuario.Id, ct))
            return Resultado.Falha(TipoErro.Conflito, EmailEmUso);

        // Verificado antes de qualquer alteração, para não deixar a entidade pela metade.
        var deixaDeSerDiretor = perfil.Id != usuario.PerfilId && perfil.Tipo != TipoPerfil.Diretoria;
        if (deixaDeSerDiretor && await UltimoDiretor.EhAsync(usuario, usuarios, perfis, ct))
            return Resultado.Falha(TipoErro.Conflito, UltimoDiretor.Mensagem);

        try
        {
            if (perfil.Id != usuario.PerfilId) usuario.AlterarPerfil(perfil);
            usuario.AlterarDados(requisicao.Nome, email, requisicao.NumeroOab);
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado.Falha(TipoErro.Conflito, EmailEmUso);
        }
    }
}
