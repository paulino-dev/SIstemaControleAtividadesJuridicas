using System.Reflection;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Identidade;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.Application.Comum;

/// <summary>
/// Ponto único por onde todo caso de uso roda, dentro do escopo já aberto para a requisição.
/// É aqui que entram os comportamentos comuns a todos eles.
/// </summary>
public sealed class ExecutorDeRequisicoes(IServiceProvider servicos, IAuditoria auditoria)
{
    public async Task<TResposta> ExecutarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct)
    {
        var contrato = typeof(IManipulador<,>).MakeGenericType(requisicao.GetType(), typeof(TResposta));
        var manipulador = servicos.GetRequiredService(contrato);
        var metodo = contrato.GetMethod("ManipularAsync")!;

        try
        {
            var resposta = await (Task<TResposta>)metodo.Invoke(manipulador, [requisicao, ct])!;

            // O login tem eventos próprios; as demais recusas são tentativas de acesso negadas.
            if (resposta is Resultado { Erro: TipoErro.AcessoNegado } && requisicao is not AutenticarUsuario)
                await auditoria.RegistrarAsync(
                    EventosDeSeguranca.AcessoNegado, dados: new { requisicao = requisicao.GetType().Name }, ct: ct);

            // Se o registro do acesso falhar, a exceção sobe e a consulta falha.
            if (requisicao is IConsultaAuditavel consulta && resposta is not Resultado { Sucesso: false })
                await auditoria.RegistrarAcessoAsync(consulta, ct);

            return resposta;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Invoke embrulha lançamentos síncronos; relança a exceção real, com a pilha original.
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
