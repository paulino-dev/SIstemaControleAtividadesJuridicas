using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Despachante de teste que devolve a resposta combinada para cada tipo de requisição. Serve aos estados que o
/// banco de verdade não produz por encomenda (falha de consulta, acesso encerrado, resposta demorada) e guarda as
/// requisições recebidas, para o teste conferir o que a tela pediu.
/// </summary>
public sealed class DespachanteCombinado : IDespachante
{
    private readonly Dictionary<Type, Func<object, Task<object?>>> _respostas = [];
    private readonly List<object> _recebidas = [];

    /// <summary>As requisições na ordem em que chegaram.</summary>
    public IReadOnlyList<object> Recebidas => _recebidas;

    public DespachanteCombinado Responde<TRequisicao, TResposta>(TResposta resposta)
        where TRequisicao : IRequisicao<TResposta> =>
        Responde<TRequisicao, TResposta>(_ => resposta);

    /// <summary>A resposta calculada a partir da requisição (a página pedida, a categoria).</summary>
    public DespachanteCombinado Responde<TRequisicao, TResposta>(Func<TRequisicao, TResposta> resposta)
        where TRequisicao : IRequisicao<TResposta>
    {
        _respostas[typeof(TRequisicao)] = requisicao => Task.FromResult<object?>(resposta((TRequisicao)requisicao));
        return this;
    }

    /// <summary>A resposta chega quando o teste completar a tarefa: o que a tela mostra enquanto espera.</summary>
    public DespachanteCombinado RespondeDepois<TRequisicao, TResposta>(Task<TResposta> resposta)
        where TRequisicao : IRequisicao<TResposta>
    {
        _respostas[typeof(TRequisicao)] = async _ => await resposta;
        return this;
    }

    public async Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default)
    {
        _recebidas.Add(requisicao);
        if (!_respostas.TryGetValue(requisicao.GetType(), out var resposta))
            throw new InvalidOperationException($"Nenhuma resposta combinada para {requisicao.GetType().Name}.");
        return (TResposta)(await resposta(requisicao))!;
    }
}
