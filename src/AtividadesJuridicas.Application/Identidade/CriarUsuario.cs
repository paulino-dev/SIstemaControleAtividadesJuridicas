using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record CriarUsuario(string Nome, string Email, string Senha, Guid PerfilId, string? NumeroOab)
    : IRequisicao<Resultado<Guid>>;

public sealed class CriarUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IHasherDeSenha hasher,
    IUnidadeDeTrabalho unidade) : IManipulador<CriarUsuario, Resultado<Guid>>
{
    private const string EmailEmUso = "Já existe um usuário com este e-mail.";

    public async Task<Resultado<Guid>> ManipularAsync(CriarUsuario requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Guid>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (!Email.TentarCriar(requisicao.Email, out var email))
            return Resultado<Guid>.Falha(TipoErro.Validacao, "E-mail inválido.");
        if (PoliticaDeSenha.Validar(requisicao.Senha) is { } erroSenha)
            return Resultado<Guid>.Falha(TipoErro.Validacao, erroSenha);

        var perfil = await perfis.ObterPorIdAsync(requisicao.PerfilId, ct);
        if (perfil is null)
            return Resultado<Guid>.Falha(TipoErro.Validacao, "Perfil não encontrado.");

        if (await usuarios.ExisteEmailAsync(email, null, ct))
            return Resultado<Guid>.Falha(TipoErro.Conflito, EmailEmUso);

        try
        {
            var usuario = Usuario.Criar(requisicao.Nome, email, hasher.Gerar(requisicao.Senha), perfil, requisicao.NumeroOab);
            usuarios.Adicionar(usuario);
            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(usuario.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado<Guid>.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado<Guid>.Falha(TipoErro.Conflito, EmailEmUso);
        }
    }
}
