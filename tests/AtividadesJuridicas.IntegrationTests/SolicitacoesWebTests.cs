using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Solicitacoes;
using AtividadesJuridicas.Domain.Solicitacoes;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>As telas das solicitações do cliente, no HTML pré-renderizado: quem as abre, o menu e o início.</summary>
[Collection(ColecaoBanco.Nome)]
public class SolicitacoesWebTests(BancoFixture banco) : IAsyncLifetime
{
    private const string AvisoDas24Horas = "A equipe verifica sua solicitação em até 24 horas úteis.";

    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _carla, _solicitacao;

    public async Task InitializeAsync()
    {
        (_carla, _) = await _app.CriarCarlaAsync();
        await _app.CriarPedroAsync();

        var aberta = await _app.ExecutarAsync(_carla, new AbrirSolicitacao(
            "Preciso de uma certidão atualizada do processo.", UrgenciaDaSolicitacao.Alta,
            [new ArquivoParaEnviar("rg.pdf", "application/pdf", new MemoryStream(Encoding.UTF8.GetBytes("conteúdo")))]));
        aberta.Sucesso.Should().BeTrue(aberta.Mensagem);
        _solicitacao = aberta.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private static string Decodificado(string html) => WebUtility.HtmlDecode(html);

    private Task<HttpClient> DaCarlaAsync() =>
        _app.ClienteAutenticadoAsync(UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);

    private Task<HttpClient> DoPedroAsync() =>
        _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

    private Task<int> ConsultasDeSolicitacaoAsync() => _app.EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().CountAsync(r => r.TipoRecurso == "Solicitacao"));

    // A página não encontrada responde 404 quando é o roteador que a dá: lê o corpo sem exigir sucesso.
    private static async Task<string> PaginaAsync(HttpClient cliente, string endereco) =>
        Decodificado(await (await cliente.GetAsync(endereco)).Content.ReadAsStringAsync());

    private static string Corpo(string html) =>
        Regex.Match(html, "<main[^>]*>.*?</main>", RegexOptions.Singleline).Value;

    private static string[] Destinos(string trecho) =>
        Regex.Matches(trecho, "href=\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToArray();

    // O nav do menu lateral é o primeiro da página (o logo do topo do menu fica fora dele).
    private static string MenuLateral(string html) =>
        Regex.Match(html, "<nav aria-label=\"Principal\">.*?</nav>", RegexOptions.Singleline).Value;

    [Fact]
    public async Task Equipe_em_rota_do_portal_ve_nao_encontrada()
    {
        var admin = await _app.ClienteAutenticadoAsync();
        var pedro = await DoPedroAsync();
        var negadosAntes = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Count;
        var consultasAntes = await ConsultasDeSolicitacaoAsync();

        foreach (var equipe in new[] { admin, pedro })
        foreach (var endereco in new[]
                 {
                     "/minhas-solicitacoes", "/minhas-solicitacoes/nova", $"/minhas-solicitacoes/{_solicitacao}",
                     $"/minhas-solicitacoes/{_solicitacao}?enviada=1", $"/minhas-solicitacoes/{_solicitacao}/editar",
                 })
        {
            var html = await PaginaAsync(equipe, endereco);

            html.Should().Contain("Página não encontrada", endereco).And.NotContain("certidão atualizada", endereco)
                .And.NotContain("Carregando", endereco).And.NotContain(AvisoDas24Horas, endereco);
        }

        (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().HaveCount(negadosAntes);
        (await ConsultasDeSolicitacaoAsync()).Should().Be(consultasAntes);
    }

    [Fact]
    public async Task Cliente_em_solicitacoes_da_diretoria_ve_nao_encontrada()
    {
        var carla = await DaCarlaAsync();
        var negadosAntes = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Count;
        var consultasAntes = await ConsultasDeSolicitacaoAsync();

        foreach (var endereco in new[] { "/solicitacoes", $"/solicitacoes/{_solicitacao}" })
        {
            var html = await PaginaAsync(carla, endereco);

            html.Should().Contain("Página não encontrada", endereco).And.NotContain("certidão atualizada", endereco)
                .And.NotContain("Carregando", endereco);
        }

        (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().HaveCount(negadosAntes);
        (await ConsultasDeSolicitacaoAsync()).Should().Be(consultasAntes);
    }

    [Fact]
    public async Task Producao_em_solicitacoes_ve_nao_encontrada()
    {
        var pedro = await DoPedroAsync();
        var negadosAntes = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Count;
        var consultasAntes = await ConsultasDeSolicitacaoAsync();

        foreach (var endereco in new[] { "/solicitacoes", $"/solicitacoes/{_solicitacao}" })
        {
            var html = await PaginaAsync(pedro, endereco);

            html.Should().Contain("Página não encontrada", endereco).And.NotContain("certidão atualizada", endereco)
                .And.NotContain("Carregando", endereco).And.NotContain("Maria Silva", endereco);
        }

        // A própria página conhece o perfil antes de consultar: nem negação registrada, nem consulta.
        (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().HaveCount(negadosAntes);
        (await ConsultasDeSolicitacaoAsync()).Should().Be(consultasAntes);
    }

    [Fact]
    public async Task Menu_do_cliente_e_da_diretoria_tem_solicitacoes()
    {
        var carla = Decodificado(await (await DaCarlaAsync()).GetStringAsync("/"));
        var admin = Decodificado(await (await _app.ClienteAutenticadoAsync()).GetStringAsync("/"));

        // Cliente: no menu lateral e no trilho, depois de Andamento; na barra inferior fica no "Mais".
        Destinos(MenuLateral(carla)).Should().Equal(
            "/", "/andamento", "/minhas-solicitacoes", "/meus-documentos", "/documentos-comuns", "/minha-senha");
        MenuLateral(carla).Should().MatchRegex("<svg[^>]*>.*?</svg>\\s*Solicitações</a>");
        Destinos(LayoutWebTests.Navegacao(carla, "aj-trilho")).Should().Equal(
            "/", "/andamento", "/minhas-solicitacoes", "/meus-documentos", "/documentos-comuns", "/minha-senha");
        Destinos(LayoutWebTests.Navegacao(carla, "aj-nav-inferior")).Should().Equal("/", "/andamento", "/meus-documentos");

        // Diretoria: só no menu lateral, depois de Atividades.
        var menuDaDiretoria = Destinos(MenuLateral(admin));
        menuDaDiretoria.Should().ContainInOrder("/", "/atividades", "/solicitacoes", "/agenda");
        Array.IndexOf(menuDaDiretoria, "/solicitacoes").Should().Be(Array.IndexOf(menuDaDiretoria, "/atividades") + 1);
        Destinos(LayoutWebTests.Navegacao(admin, "aj-trilho")).Should().NotContain("/solicitacoes");
        Destinos(LayoutWebTests.Navegacao(admin, "aj-nav-inferior")).Should().NotContain("/solicitacoes");
        admin.Should().NotContain("href=\"/minhas-solicitacoes");
    }

    [Fact]
    public async Task Menu_da_producao_nao_tem()
    {
        var html = Decodificado(await (await DoPedroAsync()).GetStringAsync("/"));

        html.Should().Contain("href=\"/atividades\"").And.NotContain("href=\"/solicitacoes\"")
            .And.NotContain("href=\"/minhas-solicitacoes");
    }

    [Theory]
    [InlineData("/minhas-solicitacoes", "mais")]
    [InlineData("/minhas-solicitacoes/nova", "mais")]
    public async Task Cliente_em_solicitacoes_tem_o_destino_no_trilho_e_mais_na_barra(string rota, string naBarra)
    {
        var html = await PaginaAsync(await DaCarlaAsync(), rota);

        LayoutWebTests.Acesos(LayoutWebTests.Navegacao(html, "aj-trilho")).Should().Equal("/minhas-solicitacoes");
        LayoutWebTests.Acesos(LayoutWebTests.Navegacao(html, "aj-nav-inferior")).Should().Equal(naBarra);
    }

    [Fact]
    public async Task Diretoria_em_solicitacoes_tem_mais_aceso_no_trilho_e_na_barra()
    {
        var html = await PaginaAsync(await _app.ClienteAutenticadoAsync(), "/solicitacoes");

        LayoutWebTests.Acesos(LayoutWebTests.Navegacao(html, "aj-trilho")).Should().Equal("mais");
        LayoutWebTests.Acesos(LayoutWebTests.Navegacao(html, "aj-nav-inferior")).Should().Equal("mais");
    }

    [Fact]
    public async Task Inicio_do_cliente_tem_o_atalho_nova_solicitacao()
    {
        var corpo = Corpo(Decodificado(await (await DaCarlaAsync()).GetStringAsync("/")));

        Destinos(corpo).Should().Equal("/andamento", "/meus-documentos", "/documentos-comuns", "/minhas-solicitacoes/nova");
        corpo.Should().MatchRegex(
            "(?s)<a href=\"/minhas-solicitacoes/nova\" class=\"aj-cartao aj-cartao--atalho\">.*?" +
            "<span class=\"aj-cartao__titulo\">Nova solicitação</span>" +
            "<span class=\"aj-cartao__meta\">Peça algo ao escritório e anexe documentos\\.</span>");
    }

    [Fact]
    public async Task Formulario_novo_mostra_o_aviso_das_24_horas_uteis()
    {
        var html = await PaginaAsync(await DaCarlaAsync(), "/minhas-solicitacoes/nova");

        html.Should().Contain("<h1 class=\"aj-titulo\">Nova solicitação</h1>").And.Contain(AvisoDas24Horas)
            .And.Contain(">Enviar solicitação</button>").And.Contain(">Baixa</button>").And.Contain(">Média</button>")
            .And.Contain(">Alta</button>").And.MatchRegex("<input(?=[^>]*\\stype=\"file\")(?=[^>]*\\smultiple)[^>]*>")
            .And.NotContain("Página não encontrada").And.NotContain("Carregando");
        // Nenhuma urgência vem marcada.
        html.Should().NotContain("aria-pressed=\"true\"");
    }

    [Fact]
    public async Task Cliente_ve_a_lista_e_a_ficha_da_solicitacao()
    {
        var carla = await DaCarlaAsync();

        var lista = await PaginaAsync(carla, "/minhas-solicitacoes");
        lista.Should().Contain("<h1 class=\"aj-titulo\">Minhas solicitações</h1>")
            .And.Contain("Preciso de uma certidão atualizada do processo.")
            .And.Contain("Aguardando análise").And.Contain("Alta").And.Contain("Carla Cliente")
            .And.Contain($"href=\"/minhas-solicitacoes/{_solicitacao}\"")
            .And.Contain("href=\"/minhas-solicitacoes/nova\"")
            .And.NotContain("Nenhuma solicitação.").And.NotContain("Carregando");
        lista.Should().MatchRegex("\\d{2}/\\d{2}/\\d{4}");

        var ficha = await PaginaAsync(carla, $"/minhas-solicitacoes/{_solicitacao}?enviada=1");
        ficha.Should().Contain("Solicitação enviada. A equipe verifica sua solicitação em até 24 horas úteis.")
            .And.Contain("Preciso de uma certidão atualizada do processo.")
            .And.MatchRegex("<a href=\"/documentos/[0-9a-f-]{36}/baixar\"[^>]*download[^>]*>rg\\.pdf</a>")
            .And.Contain($"href=\"/minhas-solicitacoes/{_solicitacao}/editar\"").And.Contain(">Cancelar solicitação</button>")
            .And.NotContain("Carregando").And.NotContain("Página não encontrada");
    }

    [Fact]
    public async Task Diretoria_ve_a_lista_e_a_ficha_da_solicitacao()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var lista = await PaginaAsync(admin, "/solicitacoes");
        lista.Should().Contain("<h1 class=\"aj-titulo\">Solicitações</h1>").And.Contain("Maria Silva")
            .And.Contain("Carla Cliente").And.Contain("Preciso de uma certidão atualizada do processo.")
            .And.Contain($"href=\"/solicitacoes/{_solicitacao}\"").And.NotContain("Carregando");
        lista.Should().MatchRegex("<button type=\"button\" class=\"aj-chip\" aria-pressed=\"true\"[^>]*>Aguardando</button>");

        var ficha = await PaginaAsync(admin, $"/solicitacoes/{_solicitacao}");
        ficha.Should().Contain("Preciso de uma certidão atualizada do processo.").And.Contain("Carla Cliente")
            .And.MatchRegex("<a href=\"/clientes/[0-9a-f-]{36}\"[^>]*>Maria Silva</a>")
            .And.MatchRegex("<a href=\"/documentos/[0-9a-f-]{36}/baixar\"[^>]*download[^>]*>rg\\.pdf</a>")
            .And.NotContain("Ver atividade").And.NotContain("Cancelar solicitação").And.NotContain("Carregando");
    }
}
