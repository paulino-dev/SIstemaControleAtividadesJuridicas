using System.Net;
using AtividadesJuridicas.Application.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ClienteFormularioWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _litoral;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var litoral = await _app.ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null, new DadosDoClienteEntrada("Transportes Litoral Ltda.", null, null, new EnderecoEntrada(Uf: "AL"), null),
            "11.222.333/0001-81", "Litoral Transportes", null, null, null));
        litoral.Sucesso.Should().BeTrue(litoral.Mensagem);
        _litoral = litoral.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Novo_cliente_comeca_em_pessoa_fisica_com_os_paineis_e_as_acoes_de_cada_visao()
    {
        var html = await PaginaAsync("/clientes/novo");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Novo cliente</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Só o nome e o CPF ou CNPJ são obrigatórios.</p>")
            .And.MatchRegex("<form[^>]*id=\"formulario-do-cliente\"[^>]*class=\"aj-lado-a-lado aj-lado-a-lado--formulario\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-lado-a-lado__lista\">\\s*<div class=\"aj-painel__cabecalho\">\\s*<h2>Identificação e contato</h2>")
            .And.MatchRegex("<div class=\"aj-segmentado\" role=\"group\" aria-label=\"Tipo de cliente\">\\s*<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"true\"[^>]*>Pessoa física</button>")
            .And.MatchRegex("<label class=\"aj-rotulo aj-grade-campos__inteiro\">\\s*Nome completo")
            .And.Contain("placeholder=\"000.000.000-00\"").And.Contain("Data de nascimento")
            .And.Contain("<div class=\"aj-grade-endereco\">")
            .And.MatchRegex("<label class=\"aj-rotulo aj-grade-endereco__uf\">\\s*UF<input[^>]*maxlength=\"2\"[^>]*class=\"aj-campo aj-campo--curto[ \"]")
            .And.MatchRegex("<span class=\"aj-so-leitor\">Observações</span>");
        html.Should().NotContain("Razão social").And.NotContain("Inscrição estadual");

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/clientes\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-cliente\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Salvar cliente</button>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should()
            .MatchRegex("<div class=\"aj-segmentado aj-segmentado--recuado aj-segmentado--largo\" role=\"group\" aria-label=\"Tipo de cliente\">")
            .And.Contain("<p class=\"aj-ajuda\">Só o nome e o CPF ou CNPJ são obrigatórios.</p>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">.*?Salvar</button>")
            .And.NotContain("Salvar cliente");
    }

    [Fact]
    public async Task Edicao_traz_o_tipo_travado_os_campos_preenchidos_e_o_voltar_para_a_ficha()
    {
        var html = await PaginaAsync($"/clientes/{_litoral}/editar");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Editar cliente</h1>")
            .And.MatchRegex($"<a href=\"/clientes/{_litoral}\" class=\"aj-voltar\">")
            .And.MatchRegex("<input[^>]*value=\"Transportes Litoral Ltda.\"").And.MatchRegex("<input[^>]*value=\"11.222.333/0001-81\"")
            .And.MatchRegex("<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\" disabled[^>]*>Pessoa física</button>")
            .And.Contain("Razão social").And.Contain("Inscrição municipal");
    }

    [Fact]
    public async Task Edicao_de_cliente_que_nao_existe_mostra_nao_encontrado_sem_formulario()
    {
        var html = await PaginaAsync($"/clientes/{Guid.NewGuid()}/editar");

        html.Should().Contain("Cliente não encontrado.").And.MatchRegex("<a href=\"/clientes\" class=\"aj-voltar\">");
        html.Should().NotContain("Identificação e contato").And.NotContain("Salvar cliente");
    }

    [Fact]
    public async Task Producao_nao_abre_o_formulario()
    {
        await _app.CriarPedroAsync();
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync($"/clientes/{_litoral}/editar"));

        html.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Editar cliente</h1>");
        html.Should().NotContain("Identificação e contato").And.NotContain("Transportes Litoral Ltda.");
    }
}
