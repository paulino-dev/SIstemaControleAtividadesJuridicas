using System.Net;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ProcessoFormularioWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _processo;

    public async Task InitializeAsync()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = await _app.ExecutarAsync(admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        var maria = await _app.ExecutarAsync(admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null));
        var criado = await _app.ExecutarAsync(admin, new SalvarProcesso(
            null,
            new DadosDoProcessoEntrada("1234567-85.2024.8.02.0001", "Reclamação trabalhista", area.Valor, null, "Maceió",
                "Empresa X", null, 84000m, null),
            [new ClienteDoProcessoEntrada(maria.Valor, PoloProcessual.Passivo)]));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        _processo = criado.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Novo_processo_traz_os_dois_paineis_a_busca_sem_botao_e_as_acoes()
    {
        var html = await PaginaAsync("/processos/novo");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Novo processo</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Preencha os dados e vincule os clientes do processo.</p>")
            .And.MatchRegex("<form[^>]*id=\"formulario-do-processo\"[^>]*class=\"aj-lado-a-lado aj-lado-a-lado--formulario\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-lado-a-lado__lista\">\\s*<h2>Processo</h2>")
            .And.Contain("placeholder=\"0000000-00.0000.0.00.0000\"").And.Contain(">Selecione…</option>")
            .And.MatchRegex("<input[^>]*inputmode=\"decimal\"[^>]*placeholder=\"0,00\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-lado-a-lado__painel\">\\s*<h2>Clientes do processo</h2>")
            .And.Contain("<span class=\"aj-ajuda\">Nenhum cliente vinculado.</span>")
            .And.MatchRegex("<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-cliente\" placeholder=\"Adicionar cliente por nome, CPF ou CNPJ\"")
            .And.Contain("O cliente entra no polo ativo. Troque o polo ao lado do nome.")
            .And.Contain("<form id=\"busca-de-cliente\" hidden");
        html.Should().NotContain(">Procurar</button>").And.NotContain("Situação atual");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/processos\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-processo\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Salvar processo</button>");
    }

    [Fact]
    public async Task Edicao_traz_o_cliente_vinculado_com_o_polo_e_o_valor_no_formato_brasileiro()
    {
        var html = await PaginaAsync($"/processos/{_processo}/editar");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Editar processo</h1>")
            .And.MatchRegex($"<a href=\"/processos/{_processo}\" class=\"aj-voltar\">")
            .And.MatchRegex("<input[^>]*value=\"84.000,00\"")
            .And.MatchRegex("(?s)<div class=\"aj-vinculo__escolhido\">\\s*<span class=\"aj-vinculo__nome\">Maria Silva</span>\\s*<label class=\"aj-campo-filtro aj-campo-filtro--polo\">\\s*<span>Polo</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>.*?<option value=\"Passivo\" selected")
            .And.Contain("aria-label=\"Remover Maria Silva\"");
        html.Should().NotContain("Nenhum cliente vinculado.");
    }

    [Fact]
    public async Task Producao_nao_abre_o_formulario()
    {
        await _app.CriarPedroAsync();
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync("/processos/novo"));

        html.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Novo processo</h1>");
        html.Should().NotContain("<h2>Processo</h2>").And.NotContain("Trabalhista");
    }
}
