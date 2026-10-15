using System.Net;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ClientesWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public async Task InitializeAsync()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var criado = await _app.ExecutarAsync(adminId, new SalvarClientePessoaFisica(
            null,
            new DadosDoClienteEntrada("Maria Silva", "maria@x.com", null, new EnderecoEntrada(Cidade: "Maceió"), null),
            "529.982.247-25", null, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_a_lista_com_a_contagem_a_busca_e_a_acao_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/clientes"));

        html.Should().Contain("<p class=\"aj-subtitulo\">1 cliente</p>")
            .And.MatchRegex("<a href=\"/clientes/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\">\\s*<svg[^>]*>.*?</svg>\\s*Novo cliente</a>")
            .And.Contain("aria-label=\"Novo cliente\"");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("<form class=\"aj-barra-filtros\" role=\"search\"")
            .And.Contain("placeholder=\"Buscar por nome, CPF ou CNPJ\"")
            .And.MatchRegex("<label class=\"aj-opcao aj-opcao--filtro\">\\s*<input type=\"checkbox\" class=\"aj-opcao__caixa\"[^>]*>\\s*Incluir inativos</label>")
            .And.MatchRegex("(?s)<tr>\\s*<td class=\"aj-tabela__principal\">Maria Silva</td>\\s*<td class=\"aj-tabela__data\">529.982.247-25</td>\\s*<td class=\"aj-tabela__secundaria\">maria@x.com</td>\\s*<td class=\"aj-tabela__data\">—</td>\\s*<td>Maceió</td>\\s*<td>Ativo</td>")
            .And.MatchRegex("<a href=\"/clientes/[0-9a-f-]{36}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">\\s*Abrir\\s*<svg")
            .And.Contain("Página 1 de 1 · 1 cliente");
        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().MatchRegex(
            "(?s)<a href=\"/clientes/[0-9a-f-]{36}\" class=\"aj-cartao\">\\s*<span class=\"aj-cartao__titulo\">Maria Silva</span>\\s*<span class=\"aj-cartao__meta\">CPF 529.982.247-25<br\\s*/?>Maceió</span>\\s*<span class=\"aj-cartao__rodape\">\\s*<span class=\"aj-cartao__resumo\">maria@x.com</span>\\s*<span class=\"aj-cartao__situacao\">Ativo</span>");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex(
            "<button type=\"button\" class=\"aj-chip aj-chip--campo\" aria-pressed=\"false\"[^>]*>Incluir inativos</button>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex(
            "<div class=\"aj-chips\" role=\"group\" aria-label=\"Filtros\">\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"");
        html.Should().MatchRegex("<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-clientes\"")
            .And.Contain("<form id=\"busca-de-clientes\" hidden");
    }

    [Fact]
    public async Task Producao_ve_a_lista_e_abre_a_ficha_mas_nao_tem_acao_de_cadastro()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/clientes"));

        html.Should().Contain("Maria Silva").And.MatchRegex("<a href=\"/clientes/[0-9a-f-]{36}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">");
        html.Should().NotContain("Novo cliente").And.NotContain("/clientes/novo")
            .And.NotContain("Inativar").And.NotContain("Acesso negado").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Cliente_inativo_aparece_apagado_na_tabela_e_no_cartao_e_conta_no_subtitulo()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var maria = (await _app.ExecutarAsync(admin, new ListarClientes())).Valor!.Itens.Single();
        (await _app.ExecutarAsync(admin, new DefinirAtivoCliente(maria.Id, false))).Sucesso.Should().BeTrue();
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(await _app.ExecutarAsync(admin, new ObterMeuAcesso()))
            .Responde<ListarClientes, Resultado<PaginaDeClientes>>(await _app.ExecutarAsync(admin, new ListarClientes(IncluirInativos: true)));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Clientes>(despachante, new()));

        html.Should().Contain("1 cliente · 1 inativo")
            .And.MatchRegex("(?s)<tr class=\"aj-tabela__linha--inativa\">\\s*<td class=\"aj-tabela__principal\">Maria Silva</td>\\s*<td class=\"aj-tabela__data\">529.982.247-25</td>\\s*<td>maria@x.com</td>")
            .And.MatchRegex("<a href=\"/clientes/[0-9a-f-]{36}\" class=\"aj-cartao aj-cartao--inativo\">")
            .And.Contain("<span class=\"aj-cartao__situacao\">Inativo</span>");
    }

    [Fact]
    public async Task Falha_da_consulta_mostra_o_erro_sem_subtitulo_e_nao_a_lista_vazia()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(await _app.ExecutarAsync(admin, new ObterMeuAcesso()))
            .Responde<ListarClientes, Resultado<PaginaDeClientes>>(Resultado<PaginaDeClientes>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Clientes>(despachante, new()));

        html.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        html.Should().NotContain("Nenhum cliente.").And.NotContain("aj-subtitulo\">0").And.NotContain("Página 1");
    }

    [Fact]
    public async Task Recado_de_outra_tela_aparece_uma_vez_no_topo()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var recados = new Recados();
        recados.Deixar("Cliente salvo.");

        var html = await TelaDeTeste.RenderizarAsync<Clientes>(_app, admin, new(), recados);

        html.Should().MatchRegex("<p class=\"aj-aviso aj-aviso--sucesso\"[^>]*>.*Cliente salvo.");
        recados.Retirar().Should().BeNull();
    }

    [Fact]
    public async Task Menu_mostra_clientes_para_todos_os_perfis()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/clientes\"");
        (await doPedro.GetStringAsync("/")).Should().Contain("href=\"/clientes\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/clientes");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
