using System.Globalization;
using System.Net;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class EventoFormularioWebTests(BancoFixture banco) : IAsyncLifetime
{
    // A agenda de onde se veio, como expressão regular (o "?" escapado; o "&" já decodificado).
    private const string Origem = "/agenda\\?visao=dia&data=2026-10-08";

    private readonly AplicacaoDeTeste _app = new(banco);
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");
    private Guid _admin, _pedro, _audiencia;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
        var area = await _app.ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        area.Sucesso.Should().BeTrue(area.Mensagem);
        var litoral = await _app.ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null, new DadosDoClienteEntrada("Transportes Litoral Ltda.", null, null, new EnderecoEntrada(Uf: "AL"), null),
            "11.222.333/0001-81", "Litoral Transportes", null, null, null));
        litoral.Sucesso.Should().BeTrue(litoral.Mensagem);
        var processo = await _app.ExecutarAsync(_admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(null, "Litoral — reclamação trabalhista", area.Valor, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(litoral.Valor, PoloProcessual.Ativo)]));
        processo.Sucesso.Should().BeTrue(processo.Mensagem);
        var minuta = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Minuta de acordo trabalhista", "Redigir a minuta.", new DateOnly(2026, 10, 9), litoral.Valor, processo.Valor, null, 2));
        minuta.Sucesso.Should().BeTrue(minuta.Mensagem);

        // A Diretoria cria; o Pedro só participa. 45 minutos: fora das quatro durações do tablet e do mobile.
        var audiencia = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Audiência de conciliação", "Levar os documentos.", "Fórum Cível, sala 4", TipoEvento.Audiencia,
            _fuso.ParaUtc(new DateTime(2026, 10, 7, 9, 0, 0)), 45, litoral.Valor, processo.Valor, minuta.Valor, [_pedro]));
        audiencia.Sucesso.Should().BeTrue(audiencia.Mensagem);
        _audiencia = audiencia.Valor!.Id;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Novo_evento_traz_os_controles_de_cada_visao_com_quem_cria_marcado_e_travado()
    {
        var html = await PaginaAsync("/agenda/novo?visao=dia&data=2026-10-08");
        var hoje = DateOnly.FromDateTime(_fuso.ParaLocal(DateTime.UtcNow)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var avatarDoPedro = $"aj-avatar {AvatarDePessoa.Classe("Pedro Produção")}";

        // Cabeçalho: o voltar leva à agenda de onde se veio; o subtítulo só no desktop; no mobile, o X "Fechar sem salvar".
        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Novo evento</h1>")
            .And.MatchRegex("<(div|span) class=\"aj-so-desktop\">\\s*<p class=\"aj-subtitulo\">Preencha os dados e escolha quem participa.</p>")
            .And.MatchRegex($"(?s)<a href=\"{Origem}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Agenda</a>")
            .And.MatchRegex(
                $"(?s)<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"{Origem}\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Fechar sem salvar\">" +
                "<svg[^>]*><path d=\"M6 6l12 12\"></path><path d=\"M18 6L6 18\"></path></svg></a>\\s*<h1>Novo evento</h1>")
            .And.MatchRegex("<form[^>]*id=\"formulario-do-evento\"[^>]*class=\"aj-lado-a-lado aj-lado-a-lado--painel-medio\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-painel--solto-no-mobile aj-lado-a-lado__lista\">\\s*<div class=\"aj-desktop-tablet\"><h2>Evento</h2></div>")
            .And.MatchRegex("<label class=\"aj-rotulo\">Local<input[^>]*maxlength=\"200\"")
            .And.Contain("<div class=\"aj-desktop-tablet\"><p class=\"aj-ajuda\">Quem cria o evento sempre participa.</p></div>")
            .And.Contain("<form id=\"busca-de-cliente\" hidden").And.Contain("<form id=\"busca-de-processo\" hidden")
            .And.Contain("<form id=\"busca-de-atividade\" hidden");

        // Vínculos (as três visões): o nome visível de cada um e a busca; o "(opcional)" do processo só no tablet e no mobile.
        html.Should().MatchRegex(
                "(?s)<div class=\"aj-desktop-tablet\"><h2>Vínculos</h2></div>\\s*<div class=\"aj-vinculo aj-vinculo--rotulado\" role=\"group\" aria-labelledby=\"rotulo-do-cliente\">\\s*" +
                "<span class=\"aj-vinculo__rotulo\" id=\"rotulo-do-cliente\">Cliente</span>\\s*<label class=\"aj-campo-icone\"><svg[^>]*>.*?</svg>" +
                "<span class=\"aj-so-leitor\">Escolher cliente \\(nome, CPF ou CNPJ\\)</span><input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-cliente\" placeholder=\"Escolher cliente: nome, CPF ou CNPJ\"")
            .And.Contain("<span class=\"aj-vinculo__rotulo\" id=\"rotulo-do-processo\">Processo <span class=\"aj-tablet-mobile\"><small>(opcional)</small></span></span>")
            .And.Contain("<span class=\"aj-vinculo__rotulo\" id=\"rotulo-da-atividade\">Atividade <small>(opcional)</small></span>")
            .And.MatchRegex("<span class=\"aj-so-leitor\">Escolher atividade \\(título\\)</span><input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-atividade\" placeholder=\"Buscar atividade pelo título\"");
        // Nada escolhido ainda.
        html.Should().NotContain("Remover cliente").And.NotContain("Remover processo").And.NotContain("Remover atividade");

        // Desktop: Título e Tipo (lista, Reunião escolhida) numa linha; Data (hoje), Hora (09:00) e Duração em minutos (60)
        // na outra; a ajuda do fuso; participantes em caixas, quem cria marcado e travado com a nota "(você)".
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex(
                "(?s)<div class=\"aj-linha-de-campos\">\\s*<label class=\"aj-rotulo aj-linha-de-campos__largo\">\\s*Título\\s*<input[^>]*maxlength=\"200\"[^>]*>\\s*</label>\\s*" +
                "<label class=\"aj-rotulo aj-linha-de-campos__medio\">Tipo<select[^>]*>\\s*<option value=\"Audiencia\">Audiência</option>\\s*" +
                "<option value=\"Reuniao\" selected[^>]*>Reunião</option>\\s*<option value=\"Prazo\">Prazo</option>\\s*<option value=\"Outro\">Outro</option>\\s*</select></label>\\s*</div>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-linha-de-campos\">\\s*<label class=\"aj-rotulo\">\\s*Data\\s*<input(?=[^>]*type=\"date\")(?=[^>]*min=\"2000-01-01\")(?=[^>]*max=\"2100-12-31\")" +
                $"(?=[^>]*value=\"{hoje}\")[^>]*>\\s*</label>\\s*" +
                "<label class=\"aj-rotulo aj-linha-de-campos__curto\">\\s*Hora\\s*<input(?=[^>]*type=\"time\")(?=[^>]*value=\"09:00(:00)?\")[^>]*>\\s*</label>\\s*" +
                "<label class=\"aj-rotulo\">\\s*Duração \\(minutos\\)\\s*<input(?=[^>]*type=\"number\")(?=[^>]*min=\"1\")(?=[^>]*max=\"10080\")(?=[^>]*value=\"60\")[^>]*>\\s*</label>\\s*</div>")
            .And.MatchRegex("<p class=\"aj-ajuda aj-ajuda--com-icone aj-ajuda--colada\"><svg[^>]*><circle cx=\"12\" cy=\"12\" r=\"8.5\"></circle><path d=\"M12 7.5V12l3 2\"></path></svg>Data e hora no horário do escritório.</p>")
            .And.MatchRegex("<label class=\"aj-rotulo\">\\s*Descrição\\s*<small>\\(opcional\\)</small>\\s*<textarea(?=[^>]*rows=\"5\")(?=[^>]*maxlength=\"4000\")(?=[^>]*class=\"aj-campo[ \"])[^>]*>")
            .And.Contain("form=\"busca-de-processo\" placeholder=\"Título, parte contrária ou número CNJ\"")
            .And.MatchRegex(
                "(?s)<div class=\"aj-opcoes\" role=\"group\" aria-labelledby=\"titulo-dos-participantes\">\\s*" +
                "<label class=\"aj-opcao\"><input type=\"checkbox\" class=\"aj-opcao__caixa\" checked disabled[^>]*>Diana Diretora<span class=\"aj-opcao__nota aj-opcao__nota--menor\">\\(você\\)</span></label>\\s*" +
                "<label class=\"aj-opcao\"><input type=\"checkbox\" class=\"aj-opcao__caixa\"(?![^>]*checked)[^>]*>Pedro Produção</label>\\s*</div>")
            .And.MatchRegex(
                $"(?s)<div class=\"aj-acoes aj-acoes--fixa\"><a href=\"{Origem}\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>" +
                "<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-evento\"[^>]*><svg[^>]*>.*?</svg>Salvar evento</button></div>");
        desktop.Should().NotContain("aj-escolha").And.NotContain("aj-chip--pessoa");

        // Tablet e mobile: o Título sozinho, o Tipo em ladrilhos (Reunião marcada), Data e Hora; sem o número de minutos
        // enquanto a duração é uma das quatro; a busca de processo com o exemplo curto.
        var tabletEMobile = BlocoDeVisao.De(html, "aj-tablet-mobile");
        tabletEMobile.Should().MatchRegex("<label class=\"aj-rotulo\">\\s*Título\\s*<input[^>]*maxlength=\"200\"")
            .And.MatchRegex(
                "(?s)<fieldset class=\"aj-conjunto\"><legend class=\"aj-rotulo\">Tipo</legend><div class=\"aj-escolha\">" +
                "<label class=\"aj-escolha__opcao\"><input type=\"radio\" class=\"aj-opcao__caixa\" name=\"tipo-do-evento\" value=\"Audiencia\"(?![^>]*checked)[^>]*>Audiência</label>" +
                "<label class=\"aj-escolha__opcao\"><input type=\"radio\" class=\"aj-opcao__caixa\" name=\"tipo-do-evento\" value=\"Reuniao\" checked[^>]*>Reunião</label>" +
                "<label class=\"aj-escolha__opcao\"><input[^>]*value=\"Prazo\"(?![^>]*checked)[^>]*>Prazo</label>" +
                "<label class=\"aj-escolha__opcao\"><input[^>]*value=\"Outro\"(?![^>]*checked)[^>]*>Outro</label></div></fieldset>")
            .And.MatchRegex($"(?s)<label class=\"aj-rotulo\">\\s*Data\\s*<input(?=[^>]*type=\"date\")(?=[^>]*value=\"{hoje}\")[^>]*>")
            .And.MatchRegex("(?s)<label class=\"aj-rotulo\">\\s*Hora\\s*<input(?=[^>]*type=\"time\")[^>]*>")
            .And.Contain("form=\"busca-de-processo\" placeholder=\"Título, parte ou número CNJ\"");
        tabletEMobile.Should().NotContain("Duração (minutos)").And.NotContain("Data e hora no horário do escritório.");

        // Tablet: Duração em lista ("1h30"); a descrição de 3 linhas no painel; chips com avatar e o nome inteiro (o "você"
        // dourado e travado); Cancelar secundário.
        var tablet = BlocoDeVisao.De(html, "aj-so-tablet");
        tablet.Should().MatchRegex(
                "(?s)<label class=\"aj-rotulo\">Duração<select class=\"aj-campo\"[^>]*>\\s*<option value=\"30\">30 minutos</option>\\s*<option value=\"60\" selected[^>]*>1 hora</option>\\s*" +
                "<option value=\"90\">1h30</option>\\s*<option value=\"120\">2 horas</option>\\s*<option value=\"outra\">Outra duração</option>\\s*</select></label>")
            .And.MatchRegex("<label class=\"aj-rotulo\">\\s*Descrição\\s*<small>\\(opcional\\)</small>\\s*<textarea(?=[^>]*rows=\"3\")[^>]*>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-opcoes\" role=\"group\" aria-labelledby=\"titulo-dos-participantes\">\\s*" +
                "<label class=\"aj-chip aj-chip--pessoa\"><input type=\"checkbox\" class=\"aj-opcao__caixa\" checked disabled[^>]*><span class=\"aj-avatar\" aria-hidden=\"true\">DD</span>Diana Diretora \\(você\\)</label>\\s*" +
                $"<label class=\"aj-chip aj-chip--pessoa\"><input type=\"checkbox\" class=\"aj-opcao__caixa\"(?![^>]*checked)[^>]*><span class=\"{avatarDoPedro}\" aria-hidden=\"true\">PP</span>Pedro Produção</label>\\s*</div>")
            .And.MatchRegex(
                $"(?s)<div class=\"aj-acoes aj-acoes--fixa\"><a href=\"{Origem}\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>" +
                "<button type=\"submit\"[^>]*form=\"formulario-do-evento\"[^>]*><svg[^>]*>.*?</svg>Salvar evento</button></div>");

        // Mobile: Duração em lista na linha inteira ("1 hora e 30 minutos"); participantes em chips com o primeiro nome
        // ("Você" para quem cria); a descrição depois deles, como último campo; só "Salvar evento" na barra.
        var mobile = BlocoDeVisao.De(html, "aj-so-mobile");
        mobile.Should().MatchRegex(
            "(?s)<label class=\"aj-rotulo aj-linha-de-campos__inteiro-no-mobile\">Duração<select class=\"aj-campo\"[^>]*>.*?<option value=\"60\" selected[^>]*>1 hora</option>\\s*" +
            "<option value=\"90\">1 hora e 30 minutos</option>.*?</select></label>" +
            ".*?<fieldset class=\"aj-conjunto\"><legend class=\"aj-rotulo\">Participantes</legend><div class=\"aj-opcoes\">\\s*" +
            "<label class=\"aj-chip aj-chip--pessoa\"><input type=\"checkbox\" class=\"aj-opcao__caixa\" checked disabled[^>]*><span class=\"aj-avatar\" aria-hidden=\"true\">DD</span>Você</label>\\s*" +
            $"<label class=\"aj-chip aj-chip--pessoa\"><input[^>]*><span class=\"{avatarDoPedro}\" aria-hidden=\"true\">PP</span>Pedro</label>\\s*</div></fieldset>" +
            ".*?<label class=\"aj-rotulo\">\\s*Descrição\\s*<small>\\(opcional\\)</small>\\s*<textarea(?=[^>]*rows=\"3\")[^>]*>" +
            ".*?<div class=\"aj-acoes aj-acoes--fixa\"><button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-evento\"[^>]*><svg[^>]*>.*?</svg>Salvar evento</button></div>");
        mobile.Should().NotContain("Cancelar").And.NotContain("Quem cria o evento sempre participa.");
    }

    [Fact]
    public async Task Edicao_vem_preenchida_com_os_vinculos_escolhidos_e_a_duracao_fora_das_opcoes_em_outra_duracao()
    {
        var html = await PaginaAsync($"/agenda/{_audiencia}/editar?visao=semana&data=2026-10-05");
        var ficha = $"/agenda/{_audiencia}\\?visao=semana&data=2026-10-05";

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Editar evento</h1>")
            .And.MatchRegex($"(?s)<a href=\"{ficha}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Audiência de conciliação</a>")
            .And.MatchRegex($"<a href=\"{ficha}\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Fechar sem salvar\">")
            .And.MatchRegex("<input[^>]*value=\"Audiência de conciliação\"")
            .And.MatchRegex("<input(?=[^>]*type=\"date\")(?=[^>]*value=\"2026-10-07\")[^>]*>")
            .And.MatchRegex("<input(?=[^>]*type=\"time\")(?=[^>]*value=\"09:00(:00)?\")[^>]*>")
            .And.MatchRegex("<input[^>]*value=\"Fórum Cível, sala 4\"")
            .And.Contain("Levar os documentos.")
            .And.MatchRegex(
                "(?s)<div class=\"aj-vinculo__escolhido aj-vinculo__escolhido--com-avatar\"><span class=\"aj-vinculo__avatar\" aria-hidden=\"true\">TL</span>" +
                "<strong class=\"aj-vinculo__nome\">Transportes Litoral Ltda.</strong><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Remover cliente\"[^>]*><svg")
            .And.MatchRegex("<div class=\"aj-vinculo__escolhido aj-vinculo__escolhido--com-avatar\"><strong class=\"aj-vinculo__nome\">Litoral — reclamação trabalhista</strong><button[^>]*aria-label=\"Remover processo\"")
            .And.MatchRegex("<div class=\"aj-vinculo__escolhido aj-vinculo__escolhido--com-avatar\"><strong class=\"aj-vinculo__nome\">Minuta de acordo trabalhista</strong><button[^>]*aria-label=\"Remover atividade\"");
        // Escolhidos, o processo e a atividade não têm busca; com cliente, nada de "Escolher cliente"; na edição, sem a
        // ajuda de quem cria (ele pode sair do evento).
        html.Should().NotContain("Escolher processo (título").And.NotContain("Escolher atividade (título)")
            .And.NotContain("Escolher cliente (nome, CPF ou CNPJ)").And.NotContain("Quem cria o evento sempre participa.");

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("<option value=\"Audiencia\" selected[^>]*>Audiência</option>")
            .And.MatchRegex("Duração \\(minutos\\)\\s*<input(?=[^>]*type=\"number\")(?=[^>]*value=\"45\")[^>]*>")
            .And.Contain("<span class=\"aj-so-leitor\">Trocar cliente</span><input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-cliente\" placeholder=\"Trocar cliente: nome, CPF ou CNPJ\"")
            // Na edição ninguém fica travado: quem criou aparece marcado e pode ser desmarcado.
            .And.MatchRegex("<input type=\"checkbox\" class=\"aj-opcao__caixa\" checked(?![^>]*disabled)[^>]*>Diana Diretora<span")
            .And.MatchRegex("<input type=\"checkbox\" class=\"aj-opcao__caixa\" checked(?![^>]*disabled)[^>]*>Pedro Produção</label>")
            .And.MatchRegex($"<a href=\"{ficha}\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>");
        var tabletEMobile = BlocoDeVisao.De(html, "aj-tablet-mobile");
        tabletEMobile.Should().MatchRegex("value=\"Audiencia\" checked[^>]*>Audiência</label>")
            .And.MatchRegex("<label class=\"aj-rotulo aj-linha-de-campos__inteiro-no-mobile\">\\s*Duração \\(minutos\\)\\s*<input(?=[^>]*type=\"number\")(?=[^>]*value=\"45\")[^>]*>");
        tabletEMobile.Should().NotContain("Trocar cliente");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex("<option value=\"outra\" selected[^>]*>Outra duração</option>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex("<option value=\"outra\" selected[^>]*>Outra duração</option>");
    }

    [Fact]
    public async Task Edicao_de_evento_que_nao_existe_mostra_nao_encontrado_sem_formulario()
    {
        var id = Guid.NewGuid();

        var html = await PaginaAsync($"/agenda/{id}/editar");

        html.Should().Contain("Evento não encontrado.")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Editar evento</h1>")
            .And.MatchRegex($"(?s)<a href=\"/agenda/{id}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Evento</a>");
        html.Should().NotContain("formulario-do-evento").And.NotContain("Salvar evento")
            .And.NotContain("Preencha os dados e escolha quem participa.");
    }

    [Fact]
    public async Task Producao_que_so_participa_ve_o_aviso_sem_formulario_e_a_que_criou_ve_o_formulario()
    {
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);
        var proprio = await _app.ExecutarAsync(_pedro, new SalvarEvento(
            null, "Reunião com o cliente", null, null, TipoEvento.Reuniao, _fuso.ParaUtc(new DateTime(2026, 10, 8, 15, 0, 0)), 60,
            null, null, null, [_pedro]));
        proprio.Sucesso.Should().BeTrue(proprio.Mensagem);

        var soParticipa = WebUtility.HtmlDecode(await doPedro.GetStringAsync($"/agenda/{_audiencia}/editar"));
        var criou = WebUtility.HtmlDecode(await doPedro.GetStringAsync($"/agenda/{proprio.Valor!.Id}/editar"));

        // Review Focus 4: o terceiro estado, com o voltar para a ficha (o título do evento, que ele vê).
        soParticipa.Should().MatchRegex("(?s)<p class=\"aj-aviso aj-aviso--atencao\" role=\"status\"><svg[^>]*>.*?</svg><span>Só a Diretoria e quem criou o evento podem editá-lo.</span></p>")
            .And.MatchRegex($"(?s)<a href=\"/agenda/{_audiencia}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Audiência de conciliação</a>")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Editar evento</h1>");
        soParticipa.Should().NotContain("formulario-do-evento").And.NotContain("Salvar evento")
            .And.NotContain("Preencha os dados e escolha quem participa.");
        criou.Should().Contain("formulario-do-evento").And.Contain("Salvar evento").And.MatchRegex("<input[^>]*value=\"Reunião com o cliente\"");
        criou.Should().NotContain("Só a Diretoria e quem criou o evento podem editá-lo.");
    }
}
