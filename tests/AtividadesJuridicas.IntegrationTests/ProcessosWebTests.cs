using System.Net;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Pages;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ProcessosWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public async Task InitializeAsync()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = await _app.ExecutarAsync(admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        var maria = await _app.ExecutarAsync(admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null));
        var criado = await _app.ExecutarAsync(admin, new SalvarProcesso(
            null,
            new DadosDoProcessoEntrada("1234567-85.2024.8.02.0001", "Reclamação trabalhista", area.Valor, null, "Maceió",
                "Empresa X", null, null, null),
            [new ClienteDoProcessoEntrada(maria.Valor, PoloProcessual.Ativo)]));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_a_lista_com_a_contagem_os_filtros_e_a_acao_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/processos"));

        html.Should().Contain("<p class=\"aj-subtitulo\">1 processo</p>")
            .And.MatchRegex("<a href=\"/processos/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\">");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("<form class=\"aj-barra-filtros\" role=\"search\"")
            .And.Contain("placeholder=\"Título, parte contrária ou número CNJ completo\"")
            .And.MatchRegex("(?s)<label class=\"aj-campo-filtro\">\\s*<span>Situação</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>.*?Todas</option>.*?Encerrado</option>")
            .And.MatchRegex("(?s)<td class=\"aj-tabela__principal\">Reclamação trabalhista</td>\\s*<td class=\"aj-tabela__data\">1234567-85.2024.8.02.0001</td>\\s*<td>Trabalhista</td>\\s*<td>Maria Silva</td>\\s*<td class=\"aj-tabela__secundaria\">Empresa X</td>\\s*<td>Ativo</td>")
            .And.MatchRegex("<a href=\"/processos/[0-9a-f-]{36}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">")
            .And.Contain("Página 1 de 1 · 1 processo");
        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().MatchRegex("(?s)<div class=\"aj-chips\" role=\"group\" aria-label=\"Filtrar por situação\">\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"true\"[^>]*>Todas · 1</button>\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Ativos</button>")
            .And.MatchRegex("(?s)<a href=\"/processos/[0-9a-f-]{36}\" class=\"aj-cartao\">\\s*<span class=\"aj-cartao__titulo\">Reclamação trabalhista</span>\\s*<span class=\"aj-cartao__meta aj-cartao__meta--tabular\">1234567-85.2024.8.02.0001<br\\s*/?>Trabalhista · Maria Silva</span>\\s*<span class=\"aj-cartao__rodape\">\\s*<span class=\"aj-cartao__resumo\">Empresa X</span>\\s*<span class=\"aj-cartao__situacao\">Ativo</span>");
        html.Should().MatchRegex("<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-processos\"");
    }

    [Fact]
    public async Task Producao_ve_a_lista_e_abre_a_ficha_mas_nao_tem_acao_de_cadastro()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/processos"));

        html.Should().Contain("Reclamação trabalhista").And.MatchRegex("<a href=\"/processos/[0-9a-f-]{36}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">");
        html.Should().NotContain("Novo processo").And.NotContain("/processos/novo").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Processo_sem_numero_suspenso_e_sem_parte_contraria_aparece_como_no_quadro()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = (await _app.ExecutarAsync(admin, new ListarAreasAtuacao())).Valor!.Single();
        var maria = (await _app.ExecutarAsync(admin, new ListarClientes(null, false, 1))).Valor!.Itens.Single();
        var processo = await _app.ExecutarAsync(admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(null, "Figueiredo — inventário", area.Id, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(maria.Id, PoloProcessual.Ativo)]));
        processo.Sucesso.Should().BeTrue(processo.Mensagem);
        (await _app.ExecutarAsync(admin, new AlterarStatusProcesso(processo.Valor, StatusProcesso.Suspenso))).Sucesso.Should().BeTrue();

        var html = WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync("/processos"));

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<td class=\"aj-tabela__principal\">Figueiredo — inventário</td>\\s*<td class=\"aj-tabela__secundaria\">Sem número</td>\\s*<td>Trabalhista</td>\\s*<td>Maria Silva</td>\\s*<td class=\"aj-tabela__secundaria\">—</td>\\s*<td class=\"aj-tabela__apagada\">Suspenso</td>");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should()
            .Contain("Sem número<br").And.Contain("Trabalhista · Maria Silva")
            .And.Contain("<span class=\"aj-cartao__resumo\">Sem parte contrária</span>")
            .And.Contain("<span class=\"aj-cartao__situacao aj-cartao__situacao--apagada\">Suspenso</span>");
        html.Should().Contain("2 processos").And.Contain("Todas · 2");
    }

    // O domínio recusa processo sem cliente: o "—" e o "Sem cliente" vêm de uma página montada à mão.
    [Fact]
    public async Task Processo_sem_cliente_aparece_com_travessao_na_tabela_e_sem_cliente_no_cartao()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var semCliente = new ProcessoResumo(Guid.NewGuid(), null, "Processo órfão", "Trabalhista", StatusProcesso.Ativo, [], "  ");
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(await _app.ExecutarAsync(admin, new ObterMeuAcesso()))
            .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(Resultado<PaginaDeProcessos>.Ok(new([semCliente], 1, 1, 20, 1)));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Processos>(despachante, new()));

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<td>Trabalhista</td>\\s*<td>—</td>\\s*<td class=\"aj-tabela__secundaria\">—</td>");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should()
            .Contain("Trabalhista · Sem cliente").And.Contain("<span class=\"aj-cartao__resumo\">Sem parte contrária</span>");
    }

    [Fact]
    public async Task Falha_da_consulta_mostra_o_erro_sem_subtitulo_e_nao_a_lista_vazia()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(await _app.ExecutarAsync(admin, new ObterMeuAcesso()))
            .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(Resultado<PaginaDeProcessos>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Processos>(despachante, new()));

        html.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        html.Should().NotContain("Nenhum processo.").And.NotContain("Todas ·").And.NotContain("Página 1");
    }

    [Fact]
    public async Task Menu_mostra_processos_para_todos_os_perfis()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/processos\"");
        (await doPedro.GetStringAsync("/")).Should().Contain("href=\"/processos\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/processos");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
