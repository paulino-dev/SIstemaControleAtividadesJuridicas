using System.Net;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadeDevolucaoWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _feita, _concluida;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();

        _feita = await FeitaAsync("Minuta de acordo trabalhista");
        _concluida = await FeitaAsync("Revisão de contrato de locação");
        (await _app.ExecutarAsync(_admin, new AprovarAtividade(_concluida))).Sucesso.Should().BeTrue();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    // Direcionada ao Pedro, com a Diana revisora, e a parte do Pedro concluída: a atividade fica Feita.
    private async Task<Guid> FeitaAsync(string titulo)
    {
        var criada = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, titulo, "Descrição da atividade.", new DateOnly(2026, 10, 9), null, null, null, 2));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        (await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            criada.Valor, [new ResponsavelEntrada(_pedro, "Redigir.")], _admin))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(_pedro, new ConcluirMinhaParte(criada.Valor))).Sucesso.Should().BeTrue();
        return criada.Valor;
    }

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Devolver_atividade_feita_traz_o_motivo_os_responsaveis_marcados_e_as_acoes_de_cada_visao()
    {
        var html = await PaginaAsync($"/atividades/{_feita}/devolver");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Devolver para correção</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Minuta de acordo trabalhista</p>")
            .And.MatchRegex($"(?s)<a href=\"/atividades/{_feita}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Minuta de acordo trabalhista</a>")
            .And.MatchRegex("<form[^>]*id=\"formulario-do-motivo\"[^>]*class=\"aj-painel aj-painel--principal aj-painel--medio\"")
            .And.MatchRegex("(?s)<label class=\"aj-rotulo\">\\s*Motivo\\s*<textarea[^>]*maxlength=\"2000\"")
            // O InputTextArea soma à classe a do estado do campo ("valid"): por isso o [ "] depois de aj-campo.
            .And.MatchRegex("<textarea(?=[^>]*rows=\"4\")[^>]*class=\"aj-campo[ \"]")
            .And.MatchRegex("(?s)<fieldset class=\"aj-conjunto\">\\s*<legend class=\"aj-rotulo\">Responsáveis que voltam a ter a parte pendente</legend>\\s*<label class=\"aj-opcao\"><input type=\"checkbox\" class=\"aj-opcao__caixa\" checked[^>]*>Pedro Produção</label>");
        html.Should().NotContain("Esta atividade não permite mais essa ação.").And.NotContain("Informe o motivo.");

        // Desktop: a barra de ações no fluxo, com 680px no máximo, logo abaixo do painel.
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex(
            $"(?s)<div class=\"aj-acoes aj-acoes--medio\">\\s*<a href=\"/atividades/{_feita}\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-motivo\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Devolver</button>");
        desktop.Should().NotContain("aj-acoes--fixa");
        // Tablet e mobile: a barra fixa embaixo, com Cancelar secundário.
        var tablet = BlocoDeVisao.De(html, "aj-so-tablet");
        tablet.Should().MatchRegex(
            $"(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/atividades/{_feita}\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>\\s*<button type=\"submit\"[^>]*form=\"formulario-do-motivo\"[^>]*>.*?Devolver</button>");
        tablet.Should().NotContain("aj-acoes--medio");
        BlocoDeVisao.De(html, "aj-so-mobile").Should()
            .Contain("<span class=\"aj-ajuda\">Minuta de acordo trabalhista</span>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a [^>]*class=\"aj-botao aj-botao--secundario\">Cancelar</a>.*?Devolver</button>");

        html.Should().MatchRegex($"<a href=\"/atividades/{_feita}\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para a atividade\">")
            .And.Contain("<h1>Devolver para correção</h1>");
    }

    [Fact]
    public async Task Reabrir_atividade_concluida_e_a_mesma_tela_com_o_titulo_e_o_botao_de_reabrir()
    {
        var html = await PaginaAsync($"/atividades/{_concluida}/reabrir");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Reabrir atividade</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Revisão de contrato de locação</p>")
            .And.Contain("Responsáveis que voltam a ter a parte pendente");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-motivo\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Reabrir</button>");
        html.Should().NotContain("Devolver para correção").And.NotContain("Devolver</button>");
    }

    [Theory]
    [InlineData("devolver")]
    [InlineData("reabrir")]
    public async Task Acao_que_a_atividade_nao_permite_avisa_sem_formulario(string acao)
    {
        // Devolver uma concluída; reabrir uma feita.
        var atividade = acao == "devolver" ? _concluida : _feita;

        var html = await PaginaAsync($"/atividades/{atividade}/{acao}");

        html.Should().Contain("Esta atividade não permite mais essa ação.")
            .And.MatchRegex($"<a href=\"/atividades/{atividade}\" class=\"aj-voltar\">");
        html.Should().NotContain("formulario-do-motivo").And.NotContain("Responsáveis que voltam a ter a parte pendente");
    }

    [Fact]
    public async Task Atividade_que_nao_existe_mostra_nao_encontrada_com_o_voltar_para_a_lista()
    {
        var html = await PaginaAsync($"/atividades/{Guid.NewGuid()}/devolver");

        html.Should().Contain("Atividade não encontrada.")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">")
            .And.Contain("aria-label=\"Voltar para Atividades\"");
        html.Should().NotContain("formulario-do-motivo");
    }

    [Theory]
    [InlineData("devolver", "Devolver para correção")]
    [InlineData("reabrir", "Reabrir atividade")]
    public async Task Producao_ve_acesso_negado_sem_consultar_a_atividade(string acao, string titulo)
    {
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync($"/atividades/{_feita}/{acao}"));

        html.Should().Contain("Acesso negado.").And.MatchRegex($"<h1 class=\"aj-titulo\">{titulo}</h1>");
        html.Should().NotContain("Minuta de acordo trabalhista").And.NotContain("formulario-do-motivo");
        (await _app.EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .CountAsync(r => r.UsuarioId == _pedro && r.TipoRecurso == "Atividade" && r.RecursoId == _feita)))
            .Should().Be(0);
    }
}
