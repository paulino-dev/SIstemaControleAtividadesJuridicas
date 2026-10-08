using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Atividades;

public sealed record ResponsavelEntrada(Guid UsuarioId, string? Direcionamento);

/// <summary>Deixa a atividade com exatamente os responsáveis informados; <paramref name="RevisorId"/> nulo mantém ou deduz o revisor.</summary>
public sealed record DefinirResponsaveisDaAtividade(Guid Id, IReadOnlyList<ResponsavelEntrada> Responsaveis, Guid? RevisorId, uint? VersaoEsperada = null)
    : IRequisicao<Resultado>;

public sealed class DefinirResponsaveisDaAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IRelogio relogio,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<DefinirResponsaveisDaAtividade, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirResponsaveisDaAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.Id, ct);
        if (atividade is null) return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);
        if (requisicao.VersaoEsperada is { } esperada && esperada != atividade.Versao)
            return Resultado.Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);

        var pedidos = requisicao.Responsaveis ?? [];
        var ids = pedidos.Select(r => r.UsuarioId).ToList();
        if (requisicao.RevisorId is { } revisorId) ids.Add(revisorId);

        var pessoas = await PessoasDaAtividade.CarregarAsync(usuarios, perfis, ids, ct);
        if (pessoas is null) return Resultado.Falha(TipoErro.Validacao, "Usuário não encontrado.");

        try
        {
            var alteracao = atividade.DefinirResponsaveis(
                [.. pedidos.Select(r => new ResponsavelInformado(pessoas[r.UsuarioId], r.Direcionamento))],
                requisicao.RevisorId is { } id ? pessoas[id] : null,
                relogio.AgoraUtc, ator.Id);

            if (alteracao.Houve)
                auditoria.Anotar(EventosDeAtividade.ResponsaveisAlterados, "Atividade", atividade.Id, new
                {
                    adicionados = alteracao.Adicionados,
                    removidos = alteracao.Removidos,
                    direcionamentoAlterado = alteracao.DirecionamentoAlterado,
                });

            // Mesmo sem mudança nos responsáveis, a atividade pode ter avançado: salva sempre.
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado.Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
        }
        catch (ExcecaoDeConcorrencia)
        {
            return Resultado.Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
        }
    }
}
