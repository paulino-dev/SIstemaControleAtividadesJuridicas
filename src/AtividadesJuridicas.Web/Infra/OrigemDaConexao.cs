namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// IP de quem abriu a página. Um circuito Blazor não tem HttpContext, então o valor é
/// capturado na renderização inicial e entregue ao circuito como parâmetro do componente raiz.
/// </summary>
public sealed class OrigemDaConexao
{
    public string? Ip { get; set; }
}
