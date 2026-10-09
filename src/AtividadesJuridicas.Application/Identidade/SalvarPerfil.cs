using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary>
/// Cria o perfil quando <paramref name="Id"/> é nulo; caso contrário, renomeia. O tipo só vale na
/// criação: num perfil existente ele tem de ser o mesmo.
/// </summary>
public sealed record SalvarPerfil(Guid? Id, string Nome, TipoPerfil Tipo) : IRequisicao<Resultado<Guid>>;

public sealed class SalvarPerfilManipulador(
    IContextoDeAcesso contexto,
    IPerfilRepositorio perfis,
    IAcessoInicialDePerfil acessoInicial,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<SalvarPerfil, Resultado<Guid>>
{
    private const string NomeEmUso = "Já existe um perfil com este nome.";

    public async Task<Resultado<Guid>> ManipularAsync(SalvarPerfil requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        Perfil? existente = null;
        if (requisicao.Id is { } id)
        {
            existente = await perfis.ObterPorIdAsync(id, ct);
            if (existente is null) return Falha(TipoErro.NaoEncontrado, "Perfil não encontrado.");
        }

        // O perfil Cliente nasce no semeador e é único. Recusar aqui também impede que ele entre, pela
        // inclusão inicial, na lista de acesso dos Documentos Gerais.
        if (existente is null && requisicao.Tipo == TipoPerfil.Cliente)
            return Falha(TipoErro.Validacao, "O perfil de cliente é único e já existe.");

        try
        {
            if (!string.IsNullOrWhiteSpace(requisicao.Nome) &&
                await perfis.ExisteNomeAsync(requisicao.Nome, existente?.Id, ct))
                return Falha(TipoErro.Conflito, NomeEmUso);

            if (existente is not null)
            {
                if (requisicao.Tipo != existente.Tipo)
                    return Falha(TipoErro.Validacao, "O tipo do perfil não pode ser alterado.");

                existente.Renomear(requisicao.Nome);
                await unidade.SalvarAsync(ct);
                return Resultado<Guid>.Ok(existente.Id);
            }

            var perfil = Perfil.Criar(requisicao.Nome, requisicao.Tipo);

            // O perfil e o acesso dele aos Documentos Gerais existentes entram juntos, ou nenhum entra.
            await unidade.EmTransacaoAsync(async () =>
            {
                perfis.Adicionar(perfil);
                await unidade.SalvarAsync(ct);

                var documentos = await acessoInicial.IncluirNosDocumentosGeraisAsync(perfil.Id, ct);
                if (documentos == 0) return;

                // A inclusão é uma gravação por fora dos interceptores: este evento é o registro dela.
                auditoria.Anotar(EventosDeSeguranca.PerfilIncluidoNosDocumentosGerais, "Perfil", perfil.Id, new { documentos });
                await unidade.SalvarAsync(ct);
            }, ct);

            return Resultado<Guid>.Ok(perfil.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Falha(TipoErro.Conflito, NomeEmUso);
        }
    }

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
