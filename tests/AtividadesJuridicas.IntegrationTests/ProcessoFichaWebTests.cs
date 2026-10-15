using System.Net;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ProcessoFichaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _maria, _processo;

    public async Task InitializeAsync()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = await _app.ExecutarAsync(admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        var maria = await _app.ExecutarAsync(admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null));
        _maria = maria.Valor;
        var criado = await _app.ExecutarAsync(admin, new SalvarProcesso(
            null,
            new DadosDoProcessoEntrada("1234567-85.2024.8.02.0001", "Reclamação trabalhista", area.Valor, "2ª Vara do Trabalho de Maceió",
                "Maceió", "Empresa X", new DateOnly(2026, 3, 12), 84000m, "Audiência de instrução realizada."),
            [new ClienteDoProcessoEntrada(_maria, PoloProcessual.Passivo)]));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        _processo = criado.Valor;
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
    public async Task Diretoria_ve_o_cabecalho_os_clientes_os_dados_e_a_situacao()
    {
        var html = await PaginaAsync($"/processos/{_processo}");

        html.Should().Contain("<h1 class=\"aj-titulo\">Reclamação trabalhista</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">1234567-85.2024.8.02.0001 · Trabalhista · Ativo</p>")
            .And.Contain("<span class=\"aj-ajuda\">1234567-85.2024.8.02.0001 · Ativo</span>")
            .And.Contain("<h1>Processo</h1>");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("(?s)<div class=\"aj-lado-a-lado aj-lado-a-lado--ficha\">\\s*<div class=\"aj-lado-a-lado__lista aj-pilha\">\\s*<section class=\"aj-painel\">\\s*<h2>Clientes do processo</h2>")
            .And.MatchRegex($"(?s)<td class=\"aj-tabela__principal\">Maria Silva</td>\\s*<td class=\"aj-tabela__data\">529.982.247-25</td>\\s*<td>Passivo</td>\\s*<td class=\"aj-tabela__acao\"><a href=\"/clientes/{_maria}\"")
            .And.Contain("<div><dt>Valor da causa</dt><dd>R$ 84.000,00</dd></div>")
            .And.MatchRegex("(?s)<h2>Situação</h2>\\s*<span class=\"aj-ajuda\">Situação atual: <strong>Ativo</strong></span>\\s*<form class=\"aj-campo-com-botao\"")
            .And.MatchRegex("<a href=\"/processos/[0-9a-f-]{36}/editar\" class=\"aj-botao aj-botao--secundario\">Editar</a>");
        var tabletEMobile = BlocoDeVisao.De(html, "aj-tablet-mobile");
        tabletEMobile.Should().MatchRegex($"<a href=\"/clientes/{_maria}\" class=\"aj-linha aj-linha--link\">")
            .And.Contain("<span class=\"aj-ajuda\">CPF 529.982.247-25 · polo passivo</span>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should()
            .MatchRegex("(?s)aria-label=\"Assuntos do processo\".*?>Dados</button>.*?>Clientes<span class=\"aj-abas__contador\">1</span></button>.*?>Situação</button>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario\"[^>]*>Alterar situação</button>");
    }

    [Fact]
    public async Task Producao_ve_a_ficha_sem_editar_nem_situacao()
    {
        await _app.CriarPedroAsync();

        var html = await PaginaAsync($"/processos/{_processo}", comoPedro: true);

        html.Should().Contain("<h2>Clientes do processo</h2>").And.Contain("<h2>Dados</h2>");
        html.Should().NotContain("/editar").And.NotContain("<h2>Situação</h2>").And.NotContain("Alterar situação")
            .And.NotContain("aj-acoes--fixa").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Processo_que_nao_existe_mostra_nao_encontrado_com_o_voltar_e_nada_mais()
    {
        var html = await PaginaAsync($"/processos/{Guid.NewGuid()}");

        html.Should().Contain("Processo não encontrado.").And.Contain("aria-label=\"Voltar para Processos\"");
        html.Should().NotContain("<h2>Dados</h2>").And.NotContain("aj-abas").And.NotContain("/editar");
    }
}
