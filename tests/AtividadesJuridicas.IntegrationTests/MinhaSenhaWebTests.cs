using System.Net;
using System.Text.RegularExpressions;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>A tela Minha senha: o painel, o voltar do mobile e um botão por visão.</summary>
[Collection(ColecaoBanco.Nome)]
public class MinhaSenhaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(HttpClient cliente, string rota) =>
        WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));

    [Fact]
    public async Task Minha_senha_tem_o_painel_o_voltar_do_mobile_e_um_botao_por_visao()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, "/minha-senha");

        var corpo = Regex.Match(html, "<main[^>]*>.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().Contain("<h1 class=\"aj-titulo\">Minha senha</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Depois de alterar, você entra de novo com a nova senha.</p>")
            .And.Contain("<span class=\"aj-ajuda\">Depois de alterar, você entra de novo com a nova senha.</span>")
            .And.MatchRegex("<form[^>]*class=\"aj-painel aj-painel--estreito\"")
            .And.Contain("Senha atual").And.Contain("Nova senha").And.Contain("Confirme a nova senha");
        Regex.Matches(corpo, "type=\"password\"").Should().HaveCount(3);
        Regex.Matches(corpo, "<input[^>]*class=\"aj-campo").Should().HaveCount(3);
        // Um botão de envio para desktop e tablet, outro para o mobile, os dois dentro do formulário.
        var formulario = Regex.Match(corpo, "<form.*?</form>", RegexOptions.Singleline).Value;
        Regex.Matches(formulario, "<button type=\"submit\" class=\"aj-botao aj-botao--primario\">Alterar senha</button>")
            .Should().HaveCount(2);
        formulario.Should().MatchRegex("<div class=\"aj-desktop-tablet\">\\s*<div class=\"aj-painel__acoes\">\\s*<button type=\"submit\"")
            .And.MatchRegex("<div class=\"aj-so-mobile\">\\s*<div class=\"aj-acoes aj-acoes--fixa\">\\s*<button type=\"submit\"");

        // A barra do mobile é a de voltar, e a tela não tem link dentro do conteúdo.
        var barra = Regex.Match(html, "<header class=\"aj-barra-superior aj-barra-superior--voltar\">.*?</header>",
            RegexOptions.Singleline).Value;
        barra.Should().MatchRegex("<a href=\"/\"[^>]*aria-label=\"Voltar para Início\"")
            .And.Contain("<h1>Minha senha</h1>");
        corpo.Should().NotContain("href=");
    }
}
