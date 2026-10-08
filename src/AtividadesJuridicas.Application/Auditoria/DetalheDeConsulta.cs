using System.Text.Encodings.Web;
using System.Text.Json;

namespace AtividadesJuridicas.Application.Auditoria;

/// <summary>
/// Serializa os filtros de uma consulta para o detalhe do registro de acesso. Mantém os acentos legíveis
/// (a coluna é texto simples, só de inserção, e é exibida como está).
/// </summary>
public static class DetalheDeConsulta
{
    private static readonly JsonSerializerOptions Opcoes = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string Serializar(object filtros) => JsonSerializer.Serialize(filtros, Opcoes);
}
