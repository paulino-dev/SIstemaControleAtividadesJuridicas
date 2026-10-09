using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary><paramref name="ClienteId"/>: obrigatório para usuário de cliente e proibido para usuário interno.</summary>
public sealed record EditarUsuario(
    Guid Id, string Nome, string Email, Guid PerfilId, string? NumeroOab, Guid? ClienteId = null)
    : IRequisicao<Resultado>;

public sealed class EditarUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria,
    IClientesDaIdentidade clientes) : IManipulador<EditarUsuario, Resultado>
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

        // Quem é de cliente não vira equipe, e vice-versa; o cliente só se informa a quem é de cliente.
        if ((perfil.Tipo == TipoPerfil.Cliente) != usuario.EhDeCliente)
            return Resultado.Falha(TipoErro.Validacao, Usuario.TrocaEntreTipos);
        if (!usuario.EhDeCliente && requisicao.ClienteId is not null)
            return Resultado.Falha(TipoErro.Validacao, Usuario.SoDeCliente);
        if (usuario.EhDeCliente)
        {
            if (requisicao.ClienteId is not { } novoClienteId)
                return Resultado.Falha(TipoErro.Validacao, "Informe o cliente do usuário.");

            // O cliente de sempre não é revalidado: um cliente inativado não impede editar o nome ou o e-mail.
            if (novoClienteId != usuario.ClienteId)
            {
                var novo = await clientes.ObterAsync(novoClienteId, ct);
                if (novo is null)
                    return Resultado.Falha(TipoErro.Validacao, "Cliente não encontrado.");
                if (!novo.Ativo)
                    return Resultado.Falha(TipoErro.Validacao, "O cliente está inativo.");
            }
        }

        if (await usuarios.ExisteEmailAsync(email, usuario.Id, ct))
            return Resultado.Falha(TipoErro.Conflito, EmailEmUso);

        // Verificado antes de qualquer alteração, para não deixar a entidade pela metade.
        var deixaDeSerDiretor = perfil.Id != usuario.PerfilId && perfil.Tipo != TipoPerfil.Diretoria;
        if (deixaDeSerDiretor && await UltimoDiretor.EhAsync(usuario, usuarios, perfis, ct))
            return Resultado.Falha(TipoErro.Conflito, UltimoDiretor.Mensagem);

        try
        {
            if (perfil.Id != usuario.PerfilId)
            {
                auditoria.Anotar(EventosDeSeguranca.PerfilAlterado, "Usuario", usuario.Id,
                    new { de = usuario.PerfilId, para = perfil.Id });
                usuario.AlterarPerfil(perfil);
            }
            if (requisicao.ClienteId is { } clienteId && usuario.ClienteId is { } anterior)
            {
                // VincularAoCliente só devolve verdadeiro quando o cliente muda; aí as sessões deixam de valer.
                if (usuario.VincularAoCliente(clienteId))
                    auditoria.Anotar(EventosDeSeguranca.UsuarioClienteAlterado, "Usuario", usuario.Id,
                        new { de = anterior, para = clienteId });
            }
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
