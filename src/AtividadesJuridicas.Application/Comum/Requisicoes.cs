namespace AtividadesJuridicas.Application.Comum;

/// <summary>Marca um comando ou consulta e declara o tipo da resposta.</summary>
public interface IRequisicao<TResposta>;

/// <summary>
/// Marca a requisição que um ator de perfil Cliente pode executar. Tudo o que não tem esta marca é negado a ele
/// pelo executor, antes do caso de uso. Marcar mais uma é abrir uma porta do escritório ao cliente: a lista
/// completa está fixada em PortaoDoClienteTests.
/// </summary>
public interface IRequisicaoAbertaAoCliente;

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
