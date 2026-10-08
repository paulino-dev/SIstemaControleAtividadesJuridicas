using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ObterProcesso(Guid Id) : IRequisicao<Resultado<ProcessoDetalhe>>, IConsultaAuditavel
{
    public string TipoRecurso => "Processo";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => Id;
}

public sealed class ObterProcessoManipulador(IContextoDeAcesso contexto, IProcessoRepositorio processos)
    : IManipulador<ObterProcesso, Resultado<ProcessoDetalhe>>
{
    public async Task<Resultado<ProcessoDetalhe>> ManipularAsync(ObterProcesso requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<ProcessoDetalhe>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var detalhe = await processos.ObterDetalheAsync(requisicao.Id, ct);
        return detalhe is null
            ? Resultado<ProcessoDetalhe>.Falha(TipoErro.NaoEncontrado, "Processo não encontrado.")
            : Resultado<ProcessoDetalhe>.Ok(detalhe);
    }
}
