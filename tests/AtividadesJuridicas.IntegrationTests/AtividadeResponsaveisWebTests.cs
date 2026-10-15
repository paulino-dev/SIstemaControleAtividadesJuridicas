using System.Net;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadeResponsaveisWebTests(BancoFixture banco) : IAsyncLifetime
{
    private const string Ajuda =
        "Marque quem é responsável e escreva o direcionamento de cada um. Quem está desmarcado deixa de ser responsável.";

    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _minuta, _recurso, _revisao;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();

        // Direcionada: a Diretoria redefine os responsáveis (e o revisor) na tela inteira.
        _minuta = await DirecionadaAsync("Minuta de acordo trabalhista", "Redigir a minuta com as condições discutidas na audiência.");

        // Feita: os responsáveis não mudam mais, só o revisor.
        _recurso = await DirecionadaAsync("Recurso ordinário", "Redigir o recurso.");
        (await _app.ExecutarAsync(_pedro, new ConcluirMinhaParte(_recurso))).Sucesso.Should().BeTrue();

        // Concluída: nem os responsáveis nem o revisor mudam.
        _revisao = await DirecionadaAsync("Revisão de contrato de locação", "Revisar as cláusulas.");
        (await _app.ExecutarAsync(_pedro, new ConcluirMinhaParte(_revisao))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(_admin, new AprovarAtividade(_revisao))).Sucesso.Should().BeTrue();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<Guid> DirecionadaAsync(string titulo, string direcionamento)
    {
        var criada = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, titulo, "Descrição da atividade.", new DateOnly(2026, 10, 9), null, null, null, 2));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        var direcionada = await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            criada.Valor, [new ResponsavelEntrada(_pedro, direcionamento)], _admin));
        direcionada.Sucesso.Should().BeTrue(direcionada.Mensagem);
        return criada.Valor;
    }

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Atividade_que_permite_redefinir_traz_a_tela_inteira_com_as_opcoes_o_direcionamento_e_o_revisor()
    {
        var html = await PaginaAsync($"/atividades/{_minuta}/responsaveis");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Responsáveis</h1>")
            .And.Contain($"<p class=\"aj-subtitulo\">{Ajuda}</p>")
            .And.MatchRegex($"(?s)<a href=\"/atividades/{_minuta}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Minuta de acordo trabalhista</a>")
            .And.MatchRegex("<form[^>]*id=\"formulario-de-responsaveis\"[^>]*class=\"aj-lado-a-lado aj-lado-a-lado--formulario\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-painel--sempre-destacado aj-lado-a-lado__lista\">\\s*<h2>Minuta de acordo trabalhista</h2>")
            // A diretora não é responsável: a opção vem desmarcada, com o perfil na nota.
            .And.MatchRegex("<label class=\"aj-opcao\"><input type=\"checkbox\" class=\"aj-opcao__caixa\"(?:(?!checked)[^>])*>Diana Diretora<span class=\"aj-opcao__nota\">\\(Diretor\\)</span></label>")
            // O Pedro é responsável: marcado, com o direcionamento logo abaixo.
            .And.MatchRegex("<label class=\"aj-opcao\"><input type=\"checkbox\" class=\"aj-opcao__caixa\" checked[^>]*>Pedro Produção<span class=\"aj-opcao__nota\">\\(Produção\\)</span></label>\\s*<label class=\"aj-rotulo aj-rotulo--recuado\">Direcionamento de Pedro Produção<textarea class=\"aj-campo\" rows=\"2\" maxlength=\"4000\"")
            .And.Contain("Redigir a minuta com as condições discutidas na audiência.")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-lado-a-lado__painel\">\\s*<h2>Revisor</h2>\\s*<label class=\"aj-rotulo\"><span class=\"aj-so-leitor\">Revisor</span><select class=\"aj-campo\"")
            .And.Contain(">Manter / deduzir (atual: Diana Diretora)</option>");
        html.Should().NotContain("Direcionamento de Diana Diretora").And.NotContain("Esta atividade não permite mais essa ação.");

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            $"(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/atividades/{_minuta}\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-de-responsaveis\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Salvar responsáveis</button>");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex(
            $"(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/atividades/{_minuta}\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>.*?Salvar responsáveis</button>");
        var mobile = BlocoDeVisao.De(html, "aj-so-mobile");
        mobile.Should().Contain($"<span class=\"aj-ajuda\">{Ajuda}</span>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a [^>]*class=\"aj-botao aj-botao--secundario\">Cancelar</a>.*?Salvar</button>");
        mobile.Should().NotContain("Salvar responsáveis");

        // A barra de voltar do mobile diz "Voltar para a atividade", não o título.
        html.Should().MatchRegex($"<a href=\"/atividades/{_minuta}\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para a atividade\">")
            .And.Contain("<h1>Responsáveis</h1>");
    }

    [Fact]
    public async Task Atividade_feita_abre_so_a_troca_de_revisor_com_o_atual_escolhido()
    {
        var html = await PaginaAsync($"/atividades/{_recurso}/responsaveis");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Trocar revisor</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Recurso ordinário</p>")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-painel--sempre-destacado aj-painel--medio aj-lado-a-lado__lista\">\\s*<h2>Revisor</h2>")
            .And.Contain(">Selecione…</option>")
            .And.MatchRegex($"<option value=\"{_admin}\" selected[^>]*>Diana Diretora</option>");
        html.Should().NotContain("aj-opcao__caixa").And.NotContain("Direcionamento de")
            .And.NotContain("Manter / deduzir").And.NotContain("Salvar responsáveis");

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-de-responsaveis\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Trocar revisor</button>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().Contain("<span class=\"aj-ajuda\">Recurso ordinário</span>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">.*?Salvar</button>");
    }

    [Fact]
    public async Task Atividade_concluida_avisa_que_nao_permite_mais_e_nao_mostra_formulario()
    {
        var html = await PaginaAsync($"/atividades/{_revisao}/responsaveis");

        html.Should().Contain("Esta atividade não permite mais essa ação.")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Responsáveis</h1>")
            .And.MatchRegex($"(?s)<a href=\"/atividades/{_revisao}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Revisão de contrato de locação</a>");
        html.Should().NotContain("formulario-de-responsaveis").And.NotContain("Salvar responsáveis")
            .And.NotContain(Ajuda);
    }

    [Fact]
    public async Task Atividade_que_nao_existe_mostra_nao_encontrada_com_o_voltar_para_a_lista()
    {
        var html = await PaginaAsync($"/atividades/{Guid.NewGuid()}/responsaveis");

        html.Should().Contain("Atividade não encontrada.")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">")
            .And.Contain("aria-label=\"Voltar para Atividades\"");
        html.Should().NotContain("formulario-de-responsaveis");
    }

    [Fact]
    public async Task Producao_ve_acesso_negado_sem_consultar_a_atividade()
    {
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync($"/atividades/{_minuta}/responsaveis"));

        html.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Responsáveis</h1>");
        html.Should().NotContain("Minuta de acordo trabalhista").And.NotContain("formulario-de-responsaveis");
        // A tela confere o perfil antes de consultar: a atividade não foi lida pelo Pedro.
        (await _app.EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .CountAsync(r => r.UsuarioId == _pedro && r.TipoRecurso == "Atividade" && r.RecursoId == _minuta)))
            .Should().Be(0);
    }
}
