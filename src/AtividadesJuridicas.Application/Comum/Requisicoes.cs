namespace AtividadesJuridicas.Application.Comum;

/// <summary>Marca um comando ou consulta e declara o tipo da resposta.</summary>
public interface IRequisicao<TResposta>;

public interface IManipulador<in TRequisicao, TResposta>
    where TRequisicao : IRequisicao<TResposta>
{
    Task<TResposta> ManipularAsync(TRequisicao requisicao, CancellationToken ct);
}

/// <summary>
/// Ponto único de entrada da interface para os casos de uso. Cada envio roda em
/// um escopo de DI próprio, para que o DbContext não viva o tempo do circuito Blazor.
/// </summary>
public interface IDespachante
{
    Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default);
}
