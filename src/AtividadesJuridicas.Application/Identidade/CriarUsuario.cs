using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary><paramref name="ClienteId"/>: só para o perfil de tipo Cliente, e obrigatório nele.</summary>
public sealed record CriarUsuario(
    string Nome, string Email, string Senha, Guid PerfilId, string? NumeroOab, Guid? ClienteId = null)
    : IRequisicao<Resultado<Guid>>;

public sealed class CriarUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IHasherDeSenha hasher,
    IUnidadeDeTrabalho unidade,
    IClientesDaIdentidade clientes) : IManipulador<CriarUsuario, Resultado<Guid>>
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

        var deCliente = perfil.Tipo == TipoPerfil.Cliente;
        if (deCliente)
        {
            if (requisicao.ClienteId is not { } clienteId)
                return Resultado<Guid>.Falha(TipoErro.Validacao, "Informe o cliente do usuário.");

            var cliente = await clientes.ObterAsync(clienteId, ct);
            if (cliente is null)
                return Resultado<Guid>.Falha(TipoErro.Validacao, "Cliente não encontrado.");
            if (!cliente.Ativo)
                return Resultado<Guid>.Falha(TipoErro.Validacao, "O cliente está inativo.");
        }
        else if (requisicao.ClienteId is not null)
            return Resultado<Guid>.Falha(TipoErro.Validacao, Usuario.SoDeCliente);

        if (await usuarios.ExisteEmailAsync(email, null, ct))
            return Resultado<Guid>.Falha(TipoErro.Conflito, EmailEmUso);

        try
        {
            // A OAB é coisa da equipe: a informada para um usuário de cliente é ignorada.
            var usuario = Usuario.Criar(requisicao.Nome, email, hasher.Gerar(requisicao.Senha), perfil,
                deCliente ? null : requisicao.NumeroOab, requisicao.ClienteId);
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
