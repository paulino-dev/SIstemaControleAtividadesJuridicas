using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.Application.Comum;

/// <summary>
/// Ponto único por onde todo caso de uso roda, dentro do escopo já aberto para a requisição.
/// É aqui que entram os comportamentos comuns a todos eles.
/// </summary>
public sealed class ExecutorDeRequisicoes(IServiceProvider servicos)
{
    public async Task<TResposta> ExecutarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct)
    {
        var contrato = typeof(IManipulador<,>).MakeGenericType(requisicao.GetType(), typeof(TResposta));
        var manipulador = servicos.GetRequiredService(contrato);
        var metodo = contrato.GetMethod("ManipularAsync")!;

        try
        {
            return await (Task<TResposta>)metodo.Invoke(manipulador, [requisicao, ct])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Invoke embrulha lançamentos síncronos; relança a exceção real, com a pilha original.
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
