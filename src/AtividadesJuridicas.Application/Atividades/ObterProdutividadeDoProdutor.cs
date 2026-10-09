using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>
/// As atividades que o produtor realizou no período <c>[DeUtc, AteUtc)</c>, em UTC, separadas por complexidade.
/// Realizada é a atividade concluída em que ele é responsável; o período olha para a data da conclusão.
/// </summary>
public sealed record ObterProdutividadeDoProdutor(Guid ProdutorId, DateTime DeUtc, DateTime AteUtc)
    : IRequisicao<Resultado<ProdutividadeDoProdutor>>, IConsultaAuditavel
{
    public string TipoRecurso => "Produtividade";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => ProdutorId;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { DeUtc, AteUtc });
}

public sealed class ObterProdutividadeDoProdutorManipulador(
    IContextoDeAcesso contexto, IUsuarioRepositorio usuarios, IPerfilRepositorio perfis, IAtividadeRepositorio atividades)
    : IManipulador<ObterProdutividadeDoProdutor, Resultado<ProdutividadeDoProdutor>>
{
    public async Task<Resultado<ProdutividadeDoProdutor>> ManipularAsync(
        ObterProdutividadeDoProdutor requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (requisicao.DeUtc.Kind != DateTimeKind.Utc || requisicao.AteUtc.Kind != DateTimeKind.Utc
            || requisicao.DeUtc >= requisicao.AteUtc)
            return Falha(TipoErro.Validacao, "Período inválido.");
        if (requisicao.AteUtc - requisicao.DeUtc > TimeSpan.FromDays(ProdutividadeOpcoes.PeriodoMaximoEmDias))
            return Falha(TipoErro.Validacao, $"O período deve ter no máximo {ProdutividadeOpcoes.PeriodoMaximoEmDias} dias.");

        // Produtor é quem tem hoje um perfil de Produção, ativo ou não. Qualquer outro Id responde como inexistente.
        var produtor = await usuarios.ObterPorIdAsync(requisicao.ProdutorId, ct);
        var perfil = produtor is null ? null : await perfis.ObterPorIdAsync(produtor.PerfilId, ct);
        if (produtor is null || perfil is not { Tipo: TipoPerfil.Producao })
            return Falha(TipoErro.NaoEncontrado, ProdutividadeOpcoes.ProdutorNaoEncontrado);

        var realizadas = await atividades.ListarRealizadasAsync(produtor.Id, requisicao.DeUtc, requisicao.AteUtc, ct);

        // Um grupo por complexidade, mesmo vazio, para a tela mostrar sempre as três colunas.
        var grupos = Enumerable
            .Range(Atividade.ComplexidadeMinima, Atividade.ComplexidadeMaxima - Atividade.ComplexidadeMinima + 1)
            .Select(complexidade => new GrupoDeComplexidade(
                complexidade, [.. realizadas.Where(a => a.Complexidade == complexidade)]))
            .ToList();

        return Resultado<ProdutividadeDoProdutor>.Ok(new ProdutividadeDoProdutor(
            produtor.Id, produtor.Nome, grupos, await atividades.ObterPrimeiraConclusaoAsync(produtor.Id, ct)));
    }

    private static Resultado<ProdutividadeDoProdutor> Falha(TipoErro erro, string mensagem) =>
        Resultado<ProdutividadeDoProdutor>.Falha(erro, mensagem);
}
