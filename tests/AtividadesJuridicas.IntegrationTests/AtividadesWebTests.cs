using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadesWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin;
    private Guid _pedro;
    private Guid _contestacao;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();

        var contestacao = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Elaborar contestação", "Prazo de 15 dias.", new DateOnly(2020, 1, 15), null, null, null, 2));
        contestacao.Sucesso.Should().BeTrue(contestacao.Mensagem);
        _contestacao = contestacao.Valor;
        var direcionada = await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            contestacao.Valor, [new ResponsavelEntrada(_pedro, "Redigir a peça")], _admin));
        direcionada.Sucesso.Should().BeTrue(direcionada.Mensagem);

        var interno = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Recurso interno", "Sem responsáveis.", null, null, null, null, 3));
        interno.Sucesso.Should().BeTrue(interno.Mensagem);
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    // A barra superior que a lista declara (a do layout é "aj-barra-superior aj-barra-superior--padrao").
    private static string BarraDoMobile(string html)
    {
        var barra = Regex.Match(html, "<header class=\"aj-barra-superior\">.*?</header>", RegexOptions.Singleline);
        barra.Success.Should().BeTrue("a lista declara a própria barra superior do mobile");
        return barra.Value;
    }

    [Fact]
    public async Task Diretor_ve_todas_as_atividades_e_as_acoes_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/atividades"));

        html.Should().Contain("Atividades").And.Contain("Elaborar contestação").And.Contain("Recurso interno")
            .And.Contain("Direcionada").And.Contain("Criada").And.Contain("Atrasada").And.Contain("15/01/2020")
            .And.Contain("Pedro Produção");
        // O cabeçalho conta o escopo da Diretoria (o escritório inteiro), com as atrasadas à parte, e traz a ação.
        html.Should().MatchRegex("<p class=\"aj-subtitulo\">2 atividades\\s*·\\s*<strong class=\"aj-subtitulo__alerta\">1 atrasada</strong></p>")
            .And.MatchRegex("<a href=\"/atividades/nova\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\">\\s*<svg[^>]*>.*?</svg>\\s*Nova atividade</a>")
            .And.Contain("<a href=\"/atividades/nova\" class=\"aj-fab\" aria-label=\"Nova atividade\">")
            .And.Contain("<form id=\"busca-de-atividades\" hidden");

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("<form class=\"aj-barra-filtros\" role=\"search\"")
            .And.MatchRegex("<label class=\"aj-campo-icone aj-campo-icone--filtro\">\\s*<svg[^>]*>.*?</svg>\\s*<span class=\"aj-so-leitor\">Busca</span>\\s*<input type=\"text\" class=\"aj-campo-icone__controle\" placeholder=\"Buscar pelo título da atividade\"")
            .And.MatchRegex("(?s)<label class=\"aj-campo-filtro\">\\s*<span>Situação</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>\\s*<option value[^>]*>Todas</option>.*?>Criada</option>.*?>Reaberta</option>\\s*</select>")
            .And.MatchRegex("<label class=\"aj-opcao aj-opcao--solta\">\\s*<input type=\"checkbox\" class=\"aj-opcao__caixa\"[^>]*>\\s*Só atrasadas</label>")
            .And.Contain("<button type=\"submit\" class=\"aj-botao aj-botao--secundario\">Buscar</button>")
            .And.Contain("<table class=\"aj-tabela aj-tabela--larga\">")
            .And.Contain("<thead><tr><th scope=\"col\">Título</th><th scope=\"col\">Situação</th><th scope=\"col\">Entrega</th><th scope=\"col\">Complexidade</th><th scope=\"col\">Cliente</th><th scope=\"col\">Processo</th><th scope=\"col\">Responsáveis</th><th scope=\"col\">Revisor</th><th scope=\"col\"><span class=\"aj-so-leitor\">Ações</span></th></tr></thead>")
            .And.MatchRegex(
                "(?s)<td class=\"aj-tabela__principal\">Elaborar contestação</td>\\s*" +
                "<td>\\s*<div class=\"aj-etiquetas\">\\s*<span class=\"aj-etiqueta aj-etiqueta--direcionada\">Direcionada</span>\\s*" +
                "<span class=\"aj-etiqueta aj-etiqueta--atrasada\"><svg[^>]*>.*?</svg>Atrasada</span>\\s*</div>\\s*</td>\\s*" +
                "<td class=\"aj-tabela__data aj-tabela__data--atrasada\">15/01/2020</td>\\s*<td>2</td>\\s*" +
                "<td class=\"aj-tabela__vazia\">—</td>\\s*<td class=\"aj-tabela__vazia\">—</td>\\s*" +
                "<td>Pedro Produção</td>\\s*<td>Diana Diretora</td>\\s*" +
                $"<td class=\"aj-tabela__acao\"><a href=\"/atividades/{_contestacao}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">\\s*Abrir\\s*<svg")
            .And.MatchRegex(
                "(?s)<td class=\"aj-tabela__principal\">Recurso interno</td>\\s*" +
                "<td>\\s*<div class=\"aj-etiquetas\">\\s*<span class=\"aj-etiqueta aj-etiqueta--criada\">Criada</span>\\s*</div>\\s*</td>\\s*" +
                "<td class=\"aj-tabela__vazia\">—</td>\\s*<td>3</td>\\s*" +
                "<td class=\"aj-tabela__vazia\">—</td>\\s*<td class=\"aj-tabela__vazia\">—</td>\\s*<td class=\"aj-tabela__vazia\">—</td>")
            .And.Contain("aj-paginacao--com-setas").And.Contain("Página 1 de 1 · 2 atividades");
        // Responsável: os não clientes; revisor: só a Diretoria.
        Regex.Match(desktop, "(?s)<span>Responsável</span>\\s*<select[^>]*>(.*?)</select>").Groups[1].Value
            .Should().Contain(">Todos</option>").And.Contain(">Diana Diretora</option>").And.Contain(">Pedro Produção</option>");
        Regex.Match(desktop, "(?s)<span>Revisor</span>\\s*<select[^>]*>(.*?)</select>").Groups[1].Value
            .Should().Contain(">Diana Diretora</option>").And.NotContain("Pedro Produção");

        var tablet = BlocoDeVisao.De(html, "aj-so-tablet");
        tablet.Should().MatchRegex("<form class=\"aj-linha-filtros aj-linha-filtros--busca\" role=\"search\"")
            .And.MatchRegex("<span class=\"aj-so-leitor\">Buscar atividade</span>\\s*<input type=\"search\" class=\"aj-campo-icone__controle\" placeholder=\"Buscar pelo título da atividade\"")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--com-icone\" aria-expanded=\"false\" aria-controls=\"filtros-de-atividades\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Responsável, revisor</button>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-chips\" role=\"group\" aria-label=\"Filtrar por situação\">\\s*" +
                "<button type=\"button\" class=\"aj-chip\" aria-pressed=\"true\"[^>]*>Todas · 2</button>\\s*" +
                "<button type=\"button\" class=\"aj-chip aj-chip--alerta\" aria-pressed=\"false\"[^>]*>Atrasadas · 1</button>\\s*" +
                "<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Direcionadas</button>\\s*" +
                "<button[^>]*>Feitas</button>\\s*<button[^>]*>Para correção</button>\\s*<button[^>]*>Corrigidas</button>\\s*<button[^>]*>Concluídas</button>\\s*</div>")
            .And.Contain("Página 1 de 1 · 2 atividades");

        // O painel de filtros fica no HTML, escondido, para o aria-controls dos dois botões (o do tablet e o "Mais
        // filtros" da barra do mobile) apontar para um form que existe.
        html.Should().MatchRegex(
            "<div class=\"aj-tablet-mobile\">\\s*<form id=\"filtros-de-atividades\" class=\"aj-painel aj-painel--filtros\" hidden[^>]*>\\s*" +
            "<label class=\"aj-campo-filtro\">\\s*<span>Situação</span>");

        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().MatchRegex(
            $"(?s)<a href=\"/atividades/{_contestacao}\" class=\"aj-cartao aj-cartao--atividade\">\\s*" +
            "<span class=\"aj-cartao__topo\">\\s*<span class=\"aj-cartao__etiquetas\">\\s*" +
            "<span class=\"aj-etiqueta aj-etiqueta--direcionada\">Direcionada</span>\\s*" +
            "<span class=\"aj-etiqueta aj-etiqueta--atrasada aj-etiqueta--com-ponto\">Atrasada</span>\\s*</span>\\s*" +
            "<span class=\"aj-so-mobile\"><svg class=\"aj-cartao__seta\"[^>]*>.*?</svg></span>\\s*</span>\\s*" +
            "<span class=\"aj-cartao__titulo\">Elaborar contestação</span>\\s*" +
            "<span class=\"aj-cartao__meta\">Sem cliente<br\\s*/?>Sem processo vinculado</span>\\s*" +
            "<span class=\"aj-cartao__rodape\">\\s*<span class=\"aj-cartao__data aj-cartao__data--atrasada\"><svg[^>]*>.*?</svg>15/01/2020</span>\\s*" +
            $"<span class=\"aj-avatares\">\\s*<span class=\"aj-avatar aj-avatar--p {AvatarDePessoa.Classe("Pedro Produção")}\" title=\"Pedro Produção\">PP</span>\\s*</span>")
            .And.MatchRegex(
                "(?s)<span class=\"aj-cartao__titulo\">Recurso interno</span>\\s*" +
                "<span class=\"aj-cartao__meta\">Sem cliente<br\\s*/?>Sem processo vinculado</span>\\s*" +
                "<span class=\"aj-cartao__rodape\">\\s*<span class=\"aj-cartao__data\"><svg[^>]*>.*?</svg>—</span>\\s*" +
                "<span class=\"aj-cartao__vazio\">Sem responsável</span>");

        // No mobile a paginação só aparece com mais de uma página; o bloco do mobile tem as setas dos cartões.
        BlocoDeVisao.De(html, "aj-so-mobile").Should().Contain("aj-cartao__seta").And.NotContain("Página 1");

        var barra = BarraDoMobile(html);
        barra.Should().MatchRegex("<span class=\"aj-subtitulo\">2 atividades\\s*·\\s*<strong class=\"aj-subtitulo__alerta\">1 atrasada</strong></span>")
            .And.MatchRegex("<div class=\"aj-barra-superior__busca\">\\s*<label class=\"aj-campo-icone\">")
            .And.Contain("<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-atividades\" placeholder=\"Buscar pelo título\"")
            .And.Contain("<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--icone\" aria-label=\"Mais filtros\" aria-expanded=\"false\" aria-controls=\"filtros-de-atividades\"")
            .And.MatchRegex("(?s)<div class=\"aj-chips aj-chips--barra\" role=\"group\" aria-label=\"Filtrar por situação\">.*?>Todas · 2</button>.*?>Atrasadas · 1</button>.*?>Concluídas</button>\\s*</div>");
        // O mobile não tem o chip "Corrigidas" (o quadro).
        barra.Should().NotContain("Corrigidas");
    }

    [Fact]
    public async Task Producao_ve_so_as_suas_e_nenhuma_acao_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/atividades"));

        html.Should().Contain("Elaborar contestação")
            .And.MatchRegex($"<a href=\"/atividades/{_contestacao}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">\\s*Abrir");
        html.Should().NotContain("Recurso interno").And.NotContain("Nova atividade").And.NotContain("/atividades/nova")
            .And.NotContain("Seu acesso foi encerrado");
        // Review Focus 1: os números contam só as atividades em que a Produção é responsável.
        html.Should().MatchRegex("<p class=\"aj-subtitulo\">1 atividade\\s*·\\s*<strong class=\"aj-subtitulo__alerta\">1 atrasada</strong></p>");

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().Contain("<span>Situação</span>").And.Contain("Só atrasadas");
        desktop.Should().NotContain("<span>Responsável</span>").And.NotContain("<span>Revisor</span>");

        var tablet = BlocoDeVisao.De(html, "aj-so-tablet");
        tablet.Should().MatchRegex("aria-controls=\"filtros-de-atividades\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Mais filtros</button>")
            .And.MatchRegex(">Todas · 1</button>").And.MatchRegex(">Atrasadas · 1</button>");
        tablet.Should().NotContain("Responsável, revisor").And.NotContain("Todas · 2");
    }

    [Fact]
    public async Task Menu_mostra_atividades_para_todos_os_perfis()
    {
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/atividades\"");
        (await doPedro.GetStringAsync("/")).Should().Contain("href=\"/atividades\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/atividades");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }

    [Fact]
    public async Task Listagem_fica_no_registro_de_consultas()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await cliente.GetAsync("/atividades")).EnsureSuccessStatusCode();

        var acessos = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().ToListAsync());
        acessos.Should().Contain(a =>
            a.TipoRecurso == "Atividade" && a.Acao == AcaoDeAcesso.Listou && a.UsuarioId == _pedro);
    }

    // ---- Complexidade ----

    /// <summary>Os níveis do controle segmentado de complexidade do formulário, cada um com se está marcado.</summary>
    private static List<(string Nivel, bool Marcado)> Complexidade(string html)
    {
        var grupo = Regex.Match(html,
            "<div class=\"aj-rotulo\" role=\"group\" aria-label=\"Complexidade\">\\s*Complexidade\\s*<div class=\"aj-segmentado\">(.*?)</div>",
            RegexOptions.Singleline);
        grupo.Success.Should().BeTrue("o formulário deve ter o campo Complexidade");
        return [.. Regex.Matches(grupo.Groups[1].Value,
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"(true|false)\"[^>]*>\\s*(\\d)\\s*</button>")
            .Select(m => (m.Groups[2].Value, m.Groups[1].Value == "true"))];
    }

    // O formulário em tela própria: nova (sem Id) ou a edição da atividade informada.
    private async Task<string> FormularioAsync(Guid? id) =>
        WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<AtividadeFormulario>(_app, _admin,
            id is { } atividade ? new() { [nameof(AtividadeFormulario.Id)] = atividade } : new()));

    [Fact]
    public async Task Lista_mostra_a_complexidade_de_cada_atividade()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/atividades"));

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        // A coluna vem logo depois de Entrega (spec 8).
        desktop.Should().Contain("<th scope=\"col\">Entrega</th><th scope=\"col\">Complexidade</th>");
        desktop.Should().MatchRegex("<td class=\"aj-tabela__data aj-tabela__data--atrasada\">15/01/2020</td>\\s*<td>2</td>")
            .And.MatchRegex("<td class=\"aj-tabela__vazia\">—</td>\\s*<td>3</td>");
    }

    [Fact]
    public async Task Recado_de_outra_tela_aparece_uma_vez_no_topo()
    {
        var recados = new Recados();
        recados.Deixar("Atividade excluída.");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<Atividades>(_app, _admin, new(), recados));

        html.Should().MatchRegex("<p class=\"aj-aviso aj-aviso--sucesso\"[^>]*>.*Atividade excluída.")
            .And.Contain("Elaborar contestação");
        recados.Retirar().Should().BeNull();
    }

    [Fact]
    public async Task Formulario_de_atividade_nova_pede_a_complexidade_e_comeca_sem_escolha()
    {
        var html = await FormularioAsync(null);

        Complexidade(html).Should().Equal(("1", false), ("2", false), ("3", false));
    }

    [Fact]
    public async Task Formulario_de_edicao_abre_com_a_complexidade_da_atividade()
    {
        var html = await FormularioAsync(_contestacao);

        Complexidade(html).Where(n => n.Marcado).Select(n => n.Nivel).Should().Equal("2");
    }

    [Fact]
    public async Task Formulario_de_atividade_lembra_que_o_titulo_aparece_para_o_cliente()
    {
        const string lembrete = "O título aparece para o cliente no portal.";
        var cliente = await _app.ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null));
        cliente.Sucesso.Should().BeTrue(cliente.Mensagem);
        var comCliente = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Petição da Maria", "Texto.", null, cliente.Valor, null, null, 1));
        comCliente.Sucesso.Should().BeTrue(comCliente.Mensagem);

        var daAtividadeComCliente = await FormularioAsync(comCliente.Valor);
        var daAtividadeNova = await FormularioAsync(null);
        var daAtividadeSemCliente = await FormularioAsync(_contestacao);

        daAtividadeComCliente.Should().Contain(lembrete);
        daAtividadeNova.Should().Contain("<h2>Vínculos</h2>").And.NotContain(lembrete);
        daAtividadeSemCliente.Should().Contain("<h2>Vínculos</h2>").And.NotContain(lembrete);
    }
}
