using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Atividades;

public sealed record ConcluirMinhaParte(Guid Id) : IRequisicao<Resultado>;

public sealed class ConcluirMinhaParteManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IRelogio relogio,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<ConcluirMinhaParte, Resultado>
{
    public async Task<Resultado> ManipularAsync(ConcluirMinhaParte requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.Id, ct);
        if (atividade is null) return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);

        if (!AcessoAAtividade.PodeVer(ator, atividade))
        {
            await AcessoAAtividade.RegistrarNegadoAsync(auditoria, atividade.Id, nameof(ConcluirMinhaParte));
            return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);
        }

        try
        {
            atividade.ConcluirParte(AcessoAAtividade.Pessoa(ator), relogio.AgoraUtc);
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
