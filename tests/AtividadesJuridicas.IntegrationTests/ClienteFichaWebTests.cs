using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ClienteFichaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _litoral, _processo;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = await _app.ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        var litoral = await _app.ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null,
            new DadosDoClienteEntrada("Transportes Litoral Ltda.", "juridico@litoral.example", "(82) 3000-0002",
                new EnderecoEntrada("Av. do Porto", "1200", null, "Jaraguá", "Maceió", "AL"), "Faturamento no dia 10."),
            "11.222.333/0001-81", "Litoral Transportes", "Paulo Nogueira", null, null));
        litoral.Sucesso.Should().BeTrue(litoral.Mensagem);
        _litoral = litoral.Valor;
        var processo = await _app.ExecutarAsync(_admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(null, "Litoral — reclamação trabalhista", area.Valor, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(_litoral, PoloProcessual.Passivo)]));
        processo.Sucesso.Should().BeTrue(processo.Mensagem);
        _processo = processo.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota, bool comoPedro = false)
    {
        var cliente = comoPedro
            ? await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro)
            : await _app.ClienteAutenticadoAsync();
        return WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));
    }

    [Fact]
    public async Task Diretoria_ve_o_cabecalho_os_dados_os_processos_e_as_acoes()
    {
        var html = await PaginaAsync($"/clientes/{_litoral}");

        html.Should().Contain("<h1 class=\"aj-titulo\">Transportes Litoral Ltda.</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Pessoa jurídica · CNPJ 11.222.333/0001-81 · Ativo</p>")
            .And.Contain("<span class=\"aj-ajuda\">Pessoa jurídica · Ativo</span>")
            .And.Contain("<h1>Cliente</h1>")
            .And.MatchRegex("<div class=\"aj-lado-a-lado aj-lado-a-lado--painel-primeiro aj-lado-a-lado--ficha\">\\s*<div class=\"aj-lado-a-lado__painel aj-pilha\">")
            .And.MatchRegex("(?s)<h2>Dados</h2>\\s*<dl class=\"aj-detalhes\">\\s*<div><dt>CNPJ</dt><dd>11.222.333/0001-81</dd></div>\\s*<div><dt>Nome fantasia</dt><dd>Litoral Transportes</dd></div>\\s*<div><dt>Responsável</dt><dd>Paulo Nogueira</dd></div>\\s*<div><dt>Inscrição estadual</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Endereço</dt><dd>Av. do Porto, 1200, Jaraguá, Maceió/AL</dd></div>")
            .And.MatchRegex("(?s)<h2>Observações</h2>\\s*<p>Faturamento no dia 10.</p>");

        BlocoDeVisao.De(html, "aj-so-desktop").Should()
            .MatchRegex("(?s)<div class=\"aj-painel__botoes\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--fantasma\"[^>]*>Inativar</button>\\s*<a href=\"/clientes/[0-9a-f-]{36}/editar\" class=\"aj-botao aj-botao--secundario\">Editar</a>")
            .And.MatchRegex($"(?s)<td class=\"aj-tabela__principal\">Litoral — reclamação trabalhista</td>\\s*<td class=\"aj-tabela__secundaria\">Sem número</td>\\s*<td>Ativo</td>\\s*<td class=\"aj-tabela__acao\"><a href=\"/processos/{_processo}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">");
        var tabletEMobile = BlocoDeVisao.De(html, "aj-tablet-mobile");
        tabletEMobile.Should().MatchRegex($"(?s)<a href=\"/processos/{_processo}\" class=\"aj-linha aj-linha--link\">.*?<span class=\"aj-ajuda\">Sem número · Ativo</span>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario\"[^>]*>Inativar</button>\\s*<a href=\"/clientes/[0-9a-f-]{36}/editar\" class=\"aj-botao aj-botao--primario\">Editar</a>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex(
            "(?s)<div class=\"aj-abas\" role=\"tablist\" aria-label=\"Assuntos do cliente\">.*?aria-selected=\"true\"[^>]*>Dados</button>.*?>Processos<span class=\"aj-abas__contador\">1</span></button>.*?>Usuários<span class=\"aj-abas__contador\">0</span></button>");
        html.Should().Contain("Nenhum usuário. Crie o acesso do cliente em Usuários.").And.Contain("<h2>Documentos</h2>");
        // Na aba Dados, os outros painéis ficam ocultos no mobile.
        html.Should().MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\">\\s*<section class=\"aj-painel\">\\s*<h2>Processos do cliente</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel\">\\s*<section class=\"aj-painel\">\\s*<h2>Dados</h2>");
    }

    [Fact]
    public async Task Producao_ve_a_ficha_sem_acoes_sem_usuarios_e_so_com_as_areas_dela()
    {
        await _app.CriarPedroAsync();

        var html = await PaginaAsync($"/clientes/{_litoral}", comoPedro: true);

        html.Should().Contain("<h1 class=\"aj-titulo\">Transportes Litoral Ltda.</h1>").And.Contain("<h2>Dados</h2>")
            .And.Contain("Litoral — reclamação trabalhista").And.Contain(">Gerais · 0</button>");
        html.Should().NotContain("Inativar").And.NotContain("/editar").And.NotContain("aj-acoes--fixa")
            .And.NotContain("Usuários do cliente").And.NotContain("Contratos de honorários").And.NotContain("Enviar documento")
            .And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Cliente_que_nao_existe_mostra_nao_encontrado_com_o_voltar_e_nada_mais()
    {
        var html = await PaginaAsync($"/clientes/{Guid.NewGuid()}");

        html.Should().Contain("Cliente não encontrado.").And.Contain("aria-label=\"Voltar para Clientes\"")
            .And.MatchRegex("<a href=\"/clientes\" class=\"aj-voltar\">");
        html.Should().NotContain("<h2>Dados</h2>").And.NotContain("<h2>Documentos</h2>").And.NotContain("Inativar")
            .And.NotContain("aj-abas");
    }

    [Fact]
    public async Task Cliente_inativo_oferece_ativar_e_avisa_dos_usuarios_sem_acesso()
    {
        (await _app.ExecutarAsync(_admin, new DefinirAtivoCliente(_litoral, false))).Sucesso.Should().BeTrue();

        var html = await PaginaAsync($"/clientes/{_litoral}");

        html.Should().Contain("· Inativo</p>").And.Contain(">Ativar</button>").And.NotContain(">Inativar</button>");
    }
}
