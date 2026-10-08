using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>Deixa a lista de acesso do documento com exatamente os perfis e usuários informados.</summary>
public sealed record DefinirAcessoDoDocumento(Guid Id, IReadOnlyList<Guid> Perfis, IReadOnlyList<Guid> Usuarios)
    : IRequisicao<Resultado>;

public sealed class DefinirAcessoDoDocumentoManipulador(
    IContextoDeAcesso contexto,
    IDocumentoRepositorio documentos,
    IPerfilRepositorio perfis,
    IUsuarioRepositorio usuarios,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<DefinirAcessoDoDocumento, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirAcessoDoDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var documento = await documentos.ObterPorIdAsync(requisicao.Id, ct);
        if (documento is null) return Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado.");

        var idsDePerfis = (requisicao.Perfis ?? []).Distinct().ToList();
        var perfisPedidos = await perfis.ObterVariosAsync(idsDePerfis, ct);
        if (perfisPedidos.Count != idsDePerfis.Count)
            return Resultado.Falha(TipoErro.Validacao, "Perfil não encontrado.");

        var idsDeUsuarios = (requisicao.Usuarios ?? []).Distinct().ToList();
        var usuariosPedidos = await usuarios.ObterVariosAsync(idsDeUsuarios, ct);
        if (usuariosPedidos.Count != idsDeUsuarios.Count)
            return Resultado.Falha(TipoErro.Validacao, "Usuário não encontrado.");

        // O tipo do perfil de cada usuário decide a regra do contrato de honorários.
        var perfisDosUsuarios = (await perfis.ObterVariosAsync([.. usuariosPedidos.Select(u => u.PerfilId).Distinct()], ct))
            .ToDictionary(p => p.Id);
        if (usuariosPedidos.Any(u => !perfisDosUsuarios.ContainsKey(u.PerfilId)))
            return Resultado.Falha(TipoErro.Validacao, "Perfil não encontrado.");

        try
        {
            var alteracao = documento.DefinirAcesso(
                perfisPedidos, [.. usuariosPedidos.Select(u => new UsuarioComPerfil(u, perfisDosUsuarios[u.PerfilId]))]);
            if (!alteracao.Houve) return Resultado.Ok();

            auditoria.Anotar(EventosDeSeguranca.DocumentoAcessoAlterado, "Documento", documento.Id, new
            {
                perfisAdicionados = alteracao.PerfisAdicionados,
                perfisRemovidos = alteracao.PerfisRemovidos,
                usuariosAdicionados = alteracao.UsuariosAdicionados,
                usuariosRemovidos = alteracao.UsuariosRemovidos,
            });

            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado.Falha(TipoErro.Conflito,
                "A lista de acesso foi alterada por outra pessoa. Recarregue e tente novamente.");
        }
    }
}
