using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Application.Agenda;

/// <summary>Exclusão lógica do evento e dos participantes.</summary>
public sealed record ExcluirEvento(Guid Id, uint? VersaoEsperada = null) : IRequisicao<Resultado>;

public sealed class ExcluirEventoManipulador(
    IContextoDeAcesso contexto, IEventoRepositorio eventos, IUnidadeDeTrabalho unidade, IAuditoria auditoria)
    : IManipulador<ExcluirEvento, Resultado>
{
    public async Task<Resultado> ManipularAsync(ExcluirEvento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var evento = await eventos.ObterPorIdAsync(requisicao.Id, ct);
        if (evento is null) return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAoEvento.NaoEncontrado);
        if (!AcessoAoEvento.PodeVer(ator, evento))
        {
            await AcessoAoEvento.RegistrarNegadoAsync(auditoria, evento.Id, nameof(ExcluirEvento));
            return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAoEvento.NaoEncontrado);
        }

        if (!AcessoAoEvento.PodeGerenciar(ator, evento)) return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");
        if (requisicao.VersaoEsperada is { } esperada && esperada != evento.Versao)
            return Resultado.Falha(TipoErro.Conflito, AcessoAoEvento.Alterado);

        try
        {
            eventos.Remover(evento);
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado.Falha(TipoErro.Conflito, AcessoAoEvento.Alterado);
        }
        catch (ExcecaoDeConcorrencia)
        {
            return Resultado.Falha(TipoErro.Conflito, AcessoAoEvento.Alterado);
        }
    }
}
