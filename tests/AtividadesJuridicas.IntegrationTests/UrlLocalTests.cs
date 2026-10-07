using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

public class UrlLocalTests
{
    [Theory]
    [InlineData("/", "/")]
    [InlineData("/usuarios", "/usuarios")]
    [InlineData("/usuarios?pagina=2", "/usuarios?pagina=2")]
    public void Caminho_local_e_mantido(string entrada, string esperado)
    {
        UrlLocal.Normalizar(entrada).Should().Be(esperado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("usuarios")]
    [InlineData("https://mal.example")]
    [InlineData("//mal.example")]
    [InlineData("/\\mal.example")]
    [InlineData("/conta/entrar")]
    [InlineData("/conta/entrar?ReturnUrl=%2F")]
    public void Qualquer_outra_coisa_vira_a_raiz(string? entrada)
    {
        UrlLocal.Normalizar(entrada).Should().Be("/");
    }
}
