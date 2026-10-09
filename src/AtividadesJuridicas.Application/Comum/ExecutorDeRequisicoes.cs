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
public sealed class ExecutorDeRequisicoes(IServiceProvider servicos, IAuditoria auditoria, IContextoDeAcesso contexto)
{
    public async Task<TResposta> ExecutarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct)
    {
        // Portão do cliente: fechado por padrão. Um ator que não seja da equipe (hoje, o perfil Cliente) só roda o
        // que foi marcado como aberto a ele; para o resto, o caso de uso nem é procurado. Requisição aberta não
        // consulta o ator aqui.
        var barrada = requisicao is not IRequisicaoAbertaAoCliente
                      && await contexto.ObterAsync(ct) is { EhInterno: false };

        try
        {
            var resposta = barrada ? NegarAoCliente(requisicao) : await ManipularAsync(requisicao, ct);

            // O login tem eventos próprios; as demais recusas são tentativas de acesso negadas.
            // A recusa é registrada mesmo que a requisição tenha sido cancelada.
            if (resposta is Resultado { Erro: TipoErro.AcessoNegado } && requisicao is not AutenticarUsuario)
            {
                object dados = requisicao is IConsultaAuditavel alvo
                    ? new { requisicao = requisicao.GetType().Name, recursoId = alvo.RecursoId, detalhe = alvo.Detalhe }
                    : new { requisicao = requisicao.GetType().Name };
                await auditoria.RegistrarAsync(
                    EventosDeSeguranca.AcessoNegado, dados: dados, ct: CancellationToken.None);
            }

            // Se o registro do acesso falhar, a exceção sobe e a consulta falha.
            if (requisicao is IConsultaAuditavel consulta && resposta is not Resultado { Sucesso: false })
            {
                try
                {
                    await auditoria.RegistrarAcessoAsync(consulta, ct);
                }
                catch
                {
                    // O fluxo que o manipulador abriu (um arquivo, por exemplo) não pode vazar.
                    await DescartarAsync(resposta);
                    throw;
                }
            }

            return resposta;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Invoke embrulha lançamentos síncronos; relança a exceção real, com a pilha original.
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private Task<TResposta> ManipularAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct)
    {
        var contrato = typeof(IManipulador<,>).MakeGenericType(requisicao.GetType(), typeof(TResposta));
        var manipulador = servicos.GetRequiredService(contrato);
        return (Task<TResposta>)contrato.GetMethod("ManipularAsync")!.Invoke(manipulador, [requisicao, ct])!;
    }

    /// <summary>A mesma resposta que um caso de uso dá a quem não tem acesso.</summary>
    private static TResposta NegarAoCliente<TResposta>(IRequisicao<TResposta> requisicao)
    {
        const string mensagem = "Acesso negado.";
        var tipo = typeof(TResposta);

        if (tipo == typeof(Resultado))
            return (TResposta)(object)Resultado.Falha(TipoErro.AcessoNegado, mensagem);

        if (tipo.IsGenericType && tipo.GetGenericTypeDefinition() == typeof(Resultado<>))
            return (TResposta)tipo
                .GetMethod(nameof(Resultado.Falha), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!
                .Invoke(null, [TipoErro.AcessoNegado, mensagem])!;

        // Requisição que não responde com Resultado tem de ser marcada como aberta: PortaoDoClienteTests cobra isso.
        throw new InvalidOperationException(
            $"{requisicao.GetType().Name} não responde com Resultado e não está aberta ao cliente: o portão não tem como negá-la.");
    }

    /// <summary>Fecha a resposta, ou o valor que um <c>Resultado&lt;T&gt;</c> carrega, quando ela precisa ser fechada.</summary>
    private static async Task DescartarAsync(object? resposta)
    {
        var valor = resposta is Resultado
            ? resposta.GetType().GetProperty(nameof(Resultado<object>.Valor))?.GetValue(resposta)
            : resposta;

        try
        {
            switch (valor)
            {
                case IAsyncDisposable assincrono: await assincrono.DisposeAsync(); break;
                case IDisposable sincrono: sincrono.Dispose(); break;
            }
        }
        catch (Exception)
        {
            // A exceção que está subindo é a que importa.
        }
    }
}
