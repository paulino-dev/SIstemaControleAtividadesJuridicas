using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Web.Components.Agenda;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O aviso que a agenda e a ficha do evento mostram no topo depois de salvar ou excluir: o recado simples, ou, com
/// conflito de agenda, uma frase de atenção por visão (as redações dos quadros AgendaA, AgendaTablet e AgendaMobile).
/// </summary>
public class AvisoDeEventoSalvoTests
{
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    private SobreposicaoDeAgenda Sobreposicao(string nome, DateTime inicioLocal, DateTime fimLocal) =>
        new(Guid.NewGuid(), nome, _fuso.ParaUtc(inicioLocal), _fuso.ParaUtc(fimLocal));

    // O desenho com despachante registra o fuso do escritório, que o componente injeta.
    private static async Task<string> DesenharAsync(Recado recado) =>
        WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<AvisoDeEventoSalvo>(
            new DespachanteCombinado(), new() { ["Recado"] = recado }));

    [Fact]
    public async Task Sem_sobreposicao_e_um_aviso_de_sucesso_so_sem_involucro()
    {
        var salvo = await DesenharAsync(new Recado("Evento salvo.", TipoDeAviso.Sucesso));
        var listaVazia = await DesenharAsync(new Recado("Evento salvo.", TipoDeAviso.Sucesso, []));
        var excluido = await DesenharAsync(new Recado("Evento excluído.", TipoDeAviso.Sucesso));

        salvo.Should().MatchRegex(
            "<p class=\"aj-aviso aj-aviso--sucesso\" role=\"status\">\\s*<svg[^>]*>.*?</svg>\\s*<span>Evento salvo\\.</span></p>")
            .And.NotContain("aj-so-").And.NotContain("aj-aviso--atencao").And.NotContain("<strong>");
        Regex.Matches(salvo, "class=\"aj-aviso ").Should().ContainSingle();
        listaVazia.Should().Be(salvo);
        excluido.Should().MatchRegex(
            "<p class=\"aj-aviso aj-aviso--sucesso\" role=\"status\">\\s*<svg[^>]*>.*?</svg>\\s*<span>Evento excluído\\.</span></p>")
            .And.NotContain("aj-so-");
    }

    [Fact]
    public async Task Com_sobreposicoes_traz_uma_frase_de_atencao_por_visao()
    {
        var html = await DesenharAsync(new Recado("Evento salvo.", TipoDeAviso.Sucesso,
        [
            Sobreposicao("Rafael Teles", new DateTime(2026, 10, 7, 10, 0, 0), new DateTime(2026, 10, 7, 11, 30, 0)),
            Sobreposicao("Marina Couto", new DateTime(2026, 10, 7, 11, 0, 0), new DateTime(2026, 10, 7, 12, 0, 0)),
        ]));

        const string Abertura = "^\\s*<p class=\"aj-aviso aj-aviso--atencao aj-aviso--frase\" role=\"status\">\\s*<svg[^>]*>.*?</svg>\\s*<span>";
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(Abertura +
            "<strong>Evento salvo\\.</strong> Atenção: Rafael Teles já tem evento das 10:00 às 11:30 \\(07/10\\); Marina Couto já tem evento das 11:00 às 12:00 \\(07/10\\)\\.</span></p>\\s*$");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex(Abertura +
            "<strong>Evento salvo\\.</strong> Rafael Teles já tem evento das 10:00 às 11:30 \\(07/10\\); Marina Couto já tem evento das 11:00 às 12:00 \\(07/10\\)\\.</span></p>\\s*$")
            .And.NotContain("Atenção");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex(Abertura +
            "Rafael Teles já tem evento das 10:00 às 11:30 em 07/10; Marina Couto já tem evento das 11:00 às 12:00 em 07/10\\.</span></p>\\s*$")
            .And.NotContain("<strong>");
        // Nenhum aviso de sucesso a mais: cada visão tem só a sua frase.
        html.Should().NotContain("aj-aviso--sucesso");
        Regex.Matches(html, "aj-aviso--frase").Should().HaveCount(3);
    }
}
