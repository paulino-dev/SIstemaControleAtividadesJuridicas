using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using Microsoft.AspNetCore.Components.Authorization;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// Executa cada requisição em um escopo de DI próprio. O escopo de um circuito Blazor dura
/// a sessão inteira do usuário; um DbContext com essa vida acumularia entidades rastreadas
/// e falharia com operações concorrentes do mesmo componente.
/// </summary>
public sealed class Despachante(IServiceScopeFactory fabrica, AuthenticationStateProvider autenticacao,
    ILogger<Despachante> log) : IDespachante
{
    public async Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default)
    {
        var estado = await autenticacao.GetAuthenticationStateAsync();
        Guid? usuarioId = Guid.TryParse(estado.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        await using var escopo = fabrica.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<IdentidadeAtual>().Definir(usuarioId);

        var contrato = typeof(IManipulador<,>).MakeGenericType(requisicao.GetType(), typeof(TResposta));
        var manipulador = escopo.ServiceProvider.GetRequiredService(contrato);
        var metodo = contrato.GetMethod("ManipularAsync")!;

        try
        {
            return await (Task<TResposta>)metodo.Invoke(manipulador, [requisicao, ct])!;
        }
        catch (Exception ex)
        {
            // Invoke embrulha lançamentos síncronos; usa a exceção real, com a pilha original.
            var real = ex is TargetInvocationException { InnerException: { } interna } ? interna : ex;

            // Registra só o tipo da requisição: requisições carregam senhas.
            log.LogError(real, "Falha inesperada ao tratar {TipoDaRequisicao}.", requisicao.GetType().Name);
            ExceptionDispatchInfo.Capture(real).Throw();
            throw;
        }
    }
}
