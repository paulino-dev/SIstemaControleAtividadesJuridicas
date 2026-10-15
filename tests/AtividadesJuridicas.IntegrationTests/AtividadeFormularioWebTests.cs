using System.Net;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadeFormularioWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _litoral, _minuta;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = await _app.ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        area.Sucesso.Should().BeTrue(area.Mensagem);
        var litoral = await _app.ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null, new DadosDoClienteEntrada("Transportes Litoral Ltda.", null, null, new EnderecoEntrada(Uf: "AL"), null),
            "11.222.333/0001-81", "Litoral Transportes", null, null, null));
        litoral.Sucesso.Should().BeTrue(litoral.Mensagem);
        _litoral = litoral.Valor;
        var processo = await _app.ExecutarAsync(_admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(null, "Litoral — reclamação trabalhista", area.Valor, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(_litoral, PoloProcessual.Ativo)]));
        processo.Sucesso.Should().BeTrue(processo.Mensagem);
        var minuta = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Minuta de acordo trabalhista", "Redigir a minuta.", new DateOnly(2026, 10, 9), _litoral, processo.Valor, null, 2));
        minuta.Sucesso.Should().BeTrue(minuta.Mensagem);
        _minuta = minuta.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Nova_atividade_traz_os_dois_paineis_a_complexidade_sem_escolha_e_as_acoes_de_cada_visao()
    {
        var html = await PaginaAsync("/atividades/nova");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Nova atividade</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Diga o que precisa ser feito e a que processo ou cliente se refere.</p>")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">\\s*<svg[^>]*>.*?</svg>\\s*Atividades</a>")
            .And.MatchRegex("<form[^>]*id=\"formulario-da-atividade\"[^>]*class=\"aj-lado-a-lado aj-lado-a-lado--formulario\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-lado-a-lado__lista\">\\s*<h2>Atividade</h2>\\s*<div class=\"aj-grade-campos\">")
            .And.MatchRegex("<label class=\"aj-rotulo aj-grade-campos__inteiro\">\\s*Título\\s*<input[^>]*maxlength=\"200\"")
            .And.MatchRegex("<label class=\"aj-rotulo\">Data de entrega<input[^>]*type=\"date\"")
            .And.MatchRegex(
                "<div class=\"aj-rotulo\" role=\"group\" aria-label=\"Complexidade\">Complexidade<div class=\"aj-segmentado\">\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>1</button>\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>2</button>\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>3</button>\\s*</div>")
            .And.MatchRegex("<label class=\"aj-rotulo aj-grade-campos__inteiro\">\\s*Descrição\\s*<textarea[^>]*rows=\"4\"")
            .And.MatchRegex("<textarea[^>]*maxlength=\"10000\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-lado-a-lado__painel\">\\s*<h2>Vínculos</h2>\\s*<label class=\"aj-rotulo\">Área de atuação<select class=\"aj-campo\"[^>]*>\\s*<option[^>]*>Nenhuma</option>\\s*<option[^>]*>Trabalhista</option>")
            .And.MatchRegex("<span class=\"aj-so-leitor\">Escolher processo \\(título, parte contrária ou número CNJ\\)</span>\\s*<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-processo\" placeholder=\"Escolher processo: título, parte contrária ou número CNJ\"")
            .And.MatchRegex("<span class=\"aj-so-leitor\">Escolher cliente \\(nome, CPF ou CNPJ\\)</span>\\s*<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-cliente\" placeholder=\"Escolher cliente: nome, CPF ou CNPJ\"")
            .And.Contain("<form id=\"busca-de-processo\" hidden").And.Contain("<form id=\"busca-de-cliente\" hidden");
        // Sem cliente escolhido, nada de lembrete do portal nem de processo escolhido.
        html.Should().NotContain("O título aparece para o cliente no portal.").And.NotContain("aria-label=\"Sem processo\"");

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/atividades\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*" +
            "<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-da-atividade\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Salvar atividade</button>");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/atividades\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>\\s*<button[^>]*form=\"formulario-da-atividade\"[^>]*>.*?Salvar atividade</button>");
        var mobile = BlocoDeVisao.De(html, "aj-so-mobile");
        mobile.Should().MatchRegex("(?s)<a href=\"/atividades\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>\\s*<button[^>]*form=\"formulario-da-atividade\"[^>]*>.*?Salvar</button>");
        mobile.Should().NotContain("Salvar atividade");
        html.Should().MatchRegex("(?s)<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"/atividades\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para Atividades\">.*?<h1>Nova atividade</h1>");
    }

    [Fact]
    public async Task Edicao_traz_o_processo_escolhido_o_cliente_do_processo_e_a_complexidade_marcada()
    {
        var html = await PaginaAsync($"/atividades/{_minuta}/editar");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Editar atividade</h1>")
            .And.MatchRegex($"<a href=\"/atividades/{_minuta}\" class=\"aj-voltar\">\\s*<svg[^>]*>.*?</svg>\\s*Minuta de acordo trabalhista</a>")
            .And.MatchRegex($"(?s)<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"/atividades/{_minuta}\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para a atividade\">")
            .And.MatchRegex("<input[^>]*value=\"Minuta de acordo trabalhista\"")
            .And.MatchRegex("<input[^>]*value=\"2026-10-09\"")
            .And.MatchRegex("aria-pressed=\"false\"[^>]*>1</button>\\s*<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"true\"[^>]*>2</button>\\s*<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>3</button>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-vinculo__escolhido aj-vinculo__escolhido--detalhado\">\\s*<span class=\"aj-vinculo__texto\">\\s*" +
                "<span class=\"aj-ajuda\">Processo</span>\\s*<span class=\"aj-vinculo__titulo\">Litoral — reclamação trabalhista</span>\\s*" +
                "<span class=\"aj-ajuda\">Área Trabalhista, definida pelo processo</span>\\s*</span>\\s*" +
                "<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Sem processo\"")
            .And.MatchRegex($"(?s)<label class=\"aj-rotulo\">Cliente do processo<select class=\"aj-campo\"[^>]*>.*?<option value=\"{_litoral}\" selected[^>]*>Transportes Litoral Ltda.</option>")
            .And.Contain("<span class=\"aj-ajuda\">O título aparece para o cliente no portal.</span>")
            // Com processo escolhido, a busca é para trocá-lo.
            .And.Contain("form=\"busca-de-processo\" placeholder=\"Trocar o processo: título, parte contrária ou número CNJ\"");
        // Com processo, a área é a dele e o cliente é um dos dele: sem o campo de área nem a busca de cliente.
        html.Should().NotContain("Área de atuação").And.NotContain("Escolher cliente (nome, CPF ou CNPJ)")
            .And.NotContain("Escolher processo: título");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex($"<a href=\"/atividades/{_minuta}\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>");
    }

    [Fact]
    public async Task Edicao_de_atividade_que_nao_existe_mostra_nao_encontrada_sem_formulario()
    {
        var html = await PaginaAsync($"/atividades/{Guid.NewGuid()}/editar");

        html.Should().Contain("Atividade não encontrada.")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">\\s*<svg[^>]*>.*?</svg>\\s*Atividades</a>");
        html.Should().NotContain("<h2>Vínculos</h2>").And.NotContain("Salvar atividade");
    }

    [Fact]
    public async Task Producao_ve_acesso_negado_sem_o_formulario_e_sem_consultar_a_atividade()
    {
        var pedro = await _app.CriarPedroAsync();
        // O Pedro é responsável pela atividade: se a tela a consultasse, a consulta daria certo e registraria "Visualizou".
        var direcionada = await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            _minuta, [new ResponsavelEntrada(pedro, "Redigir a minuta")], _admin));
        direcionada.Sucesso.Should().BeTrue(direcionada.Mensagem);
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var nova = WebUtility.HtmlDecode(await doPedro.GetStringAsync("/atividades/nova"));
        var edicao = WebUtility.HtmlDecode(await doPedro.GetStringAsync($"/atividades/{_minuta}/editar"));
        await PaginaAsync($"/atividades/{_minuta}/editar");   // a Diretoria abre a mesma edição

        nova.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Nova atividade</h1>");
        nova.Should().NotContain("<h2>Vínculos</h2>").And.NotContain("Salvar atividade");
        edicao.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Editar atividade</h1>");
        edicao.Should().NotContain("<h2>Atividade</h2>").And.NotContain("Minuta de acordo trabalhista");
        // A tela confere o perfil antes de consultar: a leitura da atividade só fica no registro da Diretoria.
        var leituras = await _app.EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(a => a.TipoRecurso == "Atividade" && a.Acao == AcaoDeAcesso.Visualizou && a.RecursoId == _minuta).ToListAsync());
        leituras.Should().Contain(a => a.UsuarioId == _admin).And.NotContain(a => a.UsuarioId == pedro);
    }
}
