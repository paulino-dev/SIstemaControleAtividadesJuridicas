using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Despachante de teste que devolve a resposta combinada para cada tipo de requisição. Serve aos estados que o
/// banco de verdade não produz por encomenda: falha de consulta e acesso encerrado.
/// </summary>
public sealed class DespachanteCombinado : IDespachante
{
    private readonly Dictionary<Type, object?> _respostas = [];

    public DespachanteCombinado Responde<TRequisicao, TResposta>(TResposta resposta)
        where TRequisicao : IRequisicao<TResposta>
    {
        _respostas[typeof(TRequisicao)] = resposta;
        return this;
    }

    public Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default) =>
        _respostas.TryGetValue(requisicao.GetType(), out var resposta)
            ? Task.FromResult((TResposta)resposta!)
            : throw new InvalidOperationException($"Nenhuma resposta combinada para {requisicao.GetType().Name}.");
}
