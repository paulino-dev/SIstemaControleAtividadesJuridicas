using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using Microsoft.AspNetCore.Components.Authorization;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// Executa cada requisição em um escopo de DI próprio. O escopo de um circuito Blazor dura
/// a sessão inteira do usuário; um DbContext com essa vida acumularia entidades rastreadas
/// e falharia com operações concorrentes do mesmo componente.
/// </summary>
public sealed class Despachante(
    IServiceScopeFactory fabrica,
    AuthenticationStateProvider autenticacao,
    OrigemDaConexao origem,
    ILogger<Despachante> log) : IDespachante
{
    public async Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default)
    {
        var estado = await autenticacao.GetAuthenticationStateAsync();

        await using var escopo = fabrica.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<IdentidadeAtual>()
            .Definir(IdentidadeHttp.UsuarioId(estado.User), origem.Ip);

        try
        {
            return await escopo.ServiceProvider.GetRequiredService<ExecutorDeRequisicoes>()
                .ExecutarAsync(requisicao, ct);
        }
        catch (Exception ex)
        {
            // Registra só o tipo da requisição: requisições carregam senhas.
            log.LogError(ex, "Falha inesperada ao tratar {TipoDaRequisicao}.", requisicao.GetType().Name);
            throw;
        }
    }
}
