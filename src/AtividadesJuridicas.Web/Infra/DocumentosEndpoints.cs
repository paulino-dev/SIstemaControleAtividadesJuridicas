using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;

namespace AtividadesJuridicas.Web.Infra;

public static class DocumentosEndpoints
{
    /// <summary>
    /// Download de documento. A rota exige usuário autenticado (política padrão); quem decide o acesso
    /// é o caso de uso, que também registra a consulta ou a tentativa negada.
    /// </summary>
    public static IEndpointRouteBuilder MapDocumentos(this IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/documentos/{id:guid}/baixar",
            async (Guid id, HttpContext contexto, ExecutorDeRequisicoes executor) =>
            {
                IdentidadeHttp.Definir(contexto);

                var resultado = await executor.ExecutarAsync(new BaixarDocumento(id), contexto.RequestAborted);
                if (resultado.Valor is not { } arquivo)
                    return resultado.Erro == TipoErro.AcessoNegado
                        ? Results.StatusCode(StatusCodes.Status403Forbidden)
                        : Results.NotFound();

                // Sempre anexo, com tipo neutro: o conteúdo enviado nunca é interpretado pelo navegador.
                contexto.Response.Headers.XContentTypeOptions = "nosniff";
                contexto.Response.Headers.CacheControl = "no-store";
                return Results.File(arquivo.Conteudo, DocumentosOpcoes.TipoDeConteudoPadrao, arquivo.NomeArquivo);
            });

        return rotas;
    }
}
