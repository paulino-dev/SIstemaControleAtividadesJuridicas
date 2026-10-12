using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Components.Produtores;
using AtividadesJuridicas.Web.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProdutoresPagina = AtividadesJuridicas.Web.Components.Pages.Produtores;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Os meses que o painel oferece, o intervalo que cada um vira na consulta e os textos do painel.</summary>
public class RotulosDeProdutividadeTests
{
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    [Fact]
    public void Lista_vai_do_mes_corrente_ate_o_mais_antigo_atravessando_a_virada_do_ano()
    {
        RotulosDeProdutividade.Lista(new DateOnly(2027, 2, 14), new DateOnly(2026, 11, 30)).Should().Equal(
            new DateOnly(2027, 2, 1), new DateOnly(2027, 1, 1), new DateOnly(2026, 12, 1), new DateOnly(2026, 11, 1));
    }

    [Fact]
    public void Lista_sem_historico_ou_com_mais_antigo_no_futuro_traz_so_o_mes_corrente()
    {
        var hoje = new DateOnly(2026, 10, 8);

        RotulosDeProdutividade.Lista(hoje, hoje).Should().Equal(new DateOnly(2026, 10, 1));
        RotulosDeProdutividade.Lista(hoje, new DateOnly(2026, 12, 1)).Should().Equal(new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void Intervalo_e_o_mes_inteiro_no_fuso_do_escritorio_em_utc()
    {
        var (de, ate) = RotulosDeProdutividade.Intervalo(_fuso, new DateOnly(2026, 10, 17));

        de.Should().Be(new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc));
        ate.Should().Be(new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc));
        de.Kind.Should().Be(DateTimeKind.Utc);
        ate.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Conclusao_no_fim_da_noite_do_ultimo_dia_pertence_ao_mes_que_termina()
    {
        // 30/09 23:30 em São Paulo já é 01/10 02:30 em UTC.
        var conclusao = new DateTime(2026, 10, 1, 2, 30, 0, DateTimeKind.Utc);
        var (deSetembro, ateSetembro) = RotulosDeProdutividade.Intervalo(_fuso, new DateOnly(2026, 9, 1));
        var (deOutubro, _) = RotulosDeProdutividade.Intervalo(_fuso, new DateOnly(2026, 10, 1));

        RotulosDeProdutividade.MesDe(_fuso, conclusao).Should().Be(new DateOnly(2026, 9, 1));
        (conclusao >= deSetembro && conclusao < ateSetembro).Should().BeTrue();
        (conclusao >= deOutubro).Should().BeFalse();
    }

    [Fact]
    public void Mes_com_mudanca_de_horario_tem_o_intervalo_certo_e_cabe_no_limite_da_consulta()
    {
        var novaYork = new FusoDoEscritorio("America/New_York");

        // Em Nova York o relógio volta em 01/11/2026: novembro tem uma hora a mais.
        var (de, ate) = RotulosDeProdutividade.Intervalo(novaYork, new DateOnly(2026, 11, 1));

        de.Should().Be(new DateTime(2026, 11, 1, 4, 0, 0, DateTimeKind.Utc));
        ate.Should().Be(new DateTime(2026, 12, 1, 5, 0, 0, DateTimeKind.Utc));
        (ate - de).Should().BeLessThan(TimeSpan.FromDays(ProdutividadeOpcoes.PeriodoMaximoEmDias));
    }

    [Fact]
    public void Textos_nao_dependem_da_cultura_da_thread()
    {
        var antes = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");   // outros algarismos e outro calendário

            RotulosDeProdutividade.Mes(new DateOnly(2026, 10, 1)).Should().Be("outubro/2026");
            RotulosDeProdutividade.Chave(new DateOnly(2026, 3, 1)).Should().Be("2026-03");
            RotulosDeProdutividade.Coluna(2).Should().Be("Complexidade 2");
            RotulosDeProdutividade.Grupo(new GrupoDeComplexidade(3, [])).Should().Be("Complexidade 3 (0)");
        }
        finally
        {
            CultureInfo.CurrentCulture = antes;
        }
    }

    [Fact]
    public void Chave_vai_e_volta_e_texto_estranho_e_recusado()
    {
        RotulosDeProdutividade.TentarLer("2026-03", out var mes).Should().BeTrue();
        mes.Should().Be(new DateOnly(2026, 3, 1));

        RotulosDeProdutividade.TentarLer("março", out _).Should().BeFalse();
        RotulosDeProdutividade.TentarLer("2026-13", out _).Should().BeFalse();
        RotulosDeProdutividade.TentarLer(null, out _).Should().BeFalse();
    }

    [Fact]
    public void Titulo_do_painel_leva_o_nome_do_produtor()
    {
        RotulosDeProdutividade.Titulo("Pedro Produção").Should().Be("Produtividade — Pedro Produção");
    }
}

[Collection(ColecaoBanco.Nome)]
public class ProdutoresWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");
    private Guid _admin;
    private Guid _pedro;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private DateOnly Hoje() => DateOnly.FromDateTime(_fuso.ParaLocal(DateTime.UtcNow));

    /// <summary>Uma atividade do Pedro concluída agora (o relógio é o real): cai no mês corrente.</summary>
    private async Task<Guid> ConcluidaAgoraAsync(string titulo, int complexidade)
    {
        var criada = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, titulo, "Descrição.", new DateOnly(2090, 6, 15), null, null, null, complexidade));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        (await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            criada.Valor, [new ResponsavelEntrada(_pedro, null)], _admin))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(_pedro, new ConcluirMinhaParte(criada.Valor))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(_admin, new AprovarAtividade(criada.Valor))).Sucesso.Should().BeTrue();
        return criada.Valor;
    }

    private async Task<string> PainelAsync(Guid produtor, string nome) => WebUtility.HtmlDecode(
        await TelaDeTeste.RenderizarAsync<PainelDeProdutividade>(_app, _admin, new()
        {
            [nameof(PainelDeProdutividade.ProdutorId)] = produtor,
            [nameof(PainelDeProdutividade.ProdutorNome)] = nome,
        }));

    private static List<string> Textos(string html, string padrao) =>
        [.. Regex.Matches(html, padrao).Select(m => m.Groups[1].Value)];

    private static List<string> Meses(string html) => Textos(html, @"<option[^>]*>\s*([^<]*?)\s*</option>");

    // ---- Página ----

    [Fact]
    public async Task Diretor_ve_so_quem_e_de_producao_em_ordem_de_nome_com_os_inativos_marcados()
    {
        var producao = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
                .FirstAsync(p => p.Tipo == TipoPerfil.Producao && p.Ativo)).Id);
        var iara = await _app.ExecutarAsync(_admin, new CriarUsuario("Iara Inativa", "iara@x.com", "senha-da-iara", producao, null));
        iara.Sucesso.Should().BeTrue(iara.Mensagem);
        (await _app.ExecutarAsync(_admin, new DefinirAtivoUsuario(iara.Valor, false))).Sucesso.Should().BeTrue();
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/produtores"));

        html.Should().Contain("<p class=\"aj-subtitulo\">2 produtores</p>").And.NotContain("Acesso negado");
        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        // As linhas, em ordem de nome: nome, perfil, OAB e situação. A diretora não está entre elas.
        Regex.Matches(tabela, "<tr[^>]*>\\s*<td class=\"aj-tabela__principal\">([^<]+)</td>\\s*<td>([^<]+)</td>\\s*<td class=\"aj-tabela__data\">([^<]+)</td>\\s*<td>([^<]+)</td>")
            .Select(m => $"{m.Groups[1].Value}|{m.Groups[2].Value}|{m.Groups[3].Value}|{m.Groups[4].Value}")
            .Should().Equal("Iara Inativa|Produção|—|Inativo", "Pedro Produção|Produção|—|Ativo");
        Regex.Matches(tabela, "<tr class=\"aj-tabela__linha--inativa\"").Count.Should().Be(1);
        tabela.Should().MatchRegex($"<a href=\"/produtores\\?produtor={_pedro}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">Acompanhar\\s*<svg");
        // Sem produtor no endereço não há painel: a consulta de produtividade só acontece por escolha.
        html.Should().NotContain("Produtividade —").And.NotContain("Em acompanhamento");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should().MatchRegex("<select class=\"aj-campo-filtro__controle\"[^>]*>\\s*<option value(?:=\"\")?>Escolha o produtor</option>")
            .And.Contain(">Iara Inativa (inativo)</option>");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_a_lista()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/produtores"));

        html.Should().Contain("<h1 class=\"aj-titulo\">Produtores</h1>").And.Contain("Acesso negado").And.NotContain("Acompanhar");
        (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().BeEmpty(
            "a página confere o perfil antes de pedir a lista de usuários");
    }

    [Fact]
    public async Task Falha_na_consulta_dos_usuarios_mostra_o_erro_com_tentar_de_novo_e_nao_acesso_negado()
    {
        var diretora = new UsuarioAutenticado(Guid.NewGuid(), "Diana Diretora", TipoPerfil.Diretoria, Guid.NewGuid(), null);
        var pedro = new UsuarioResumo(Guid.NewGuid(), "Pedro Produção", "pedro@x.com", Guid.NewGuid(), "Produção",
            TipoPerfil.Producao, null, true, null);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretora)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
                Resultado<IReadOnlyList<UsuarioResumo>>.Falha(TipoErro.Validacao, "Falha de teste."));

        // O produtor do endereço (?produtor=) chega por um valor em cascata que o desenho estático não tem: o teste o põe
        // na propriedade e deixa a tela ler o endereço de novo, como ao mudar o parâmetro.
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarEAgirAsync<ProdutoresPagina>(despachante, [], tela =>
        {
            tela.ProdutorNoEndereco = pedro.Id.ToString();
            return TelaDeTeste.ChamarAsync(tela, "OnParametersSet");
        }));

        html.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>").And.Contain("<h1 class=\"aj-titulo\">Produtores</h1>");
        html.Should().NotContain("Acesso negado").And.NotContain("Nenhum produtor.").And.NotContain("<table")
            .And.NotContain("<select").And.NotContain("aj-subtitulo\">").And.NotContain("Produtividade —");

        // O "Tentar de novo" refaz a consulta da lista: com a resposta certa, a lista aparece no lugar do erro.
        var depois = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarEAgirAsync<ProdutoresPagina>(despachante, [], async tela =>
        {
            despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([pedro]));
            await TelaDeTeste.ChamarAsync(tela, "TentarDeNovoAsync");
        }));

        depois.Should().Contain("<p class=\"aj-subtitulo\">1 produtor</p>").And.Contain("Pedro Produção");
        depois.Should().NotContain("Falha de teste.").And.NotContain("Tentar de novo");
    }

    [Fact]
    public async Task Produtor_no_endereco_abre_o_painel_e_marca_a_linha()
    {
        await ConcluidaAgoraAsync("Petição simples", 1);
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync($"/produtores?produtor={_pedro}"));

        html.Should().MatchRegex("<div class=\"aj-lado-a-lado aj-lado-a-lado--painel-largo\">")
            .And.MatchRegex("<section class=\"aj-painel aj-painel--principal aj-lado-a-lado__painel\">")
            .And.Contain("<h2>Produtividade — Pedro Produção</h2>");
        BlocoDeVisao.De(html, "aj-so-desktop").Should()
            .MatchRegex("(?s)<tr class=\"aj-tabela__linha--selecionada\">.*?Pedro Produção.*?<span class=\"aj-tabela__marca\">Em acompanhamento</span>");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should()
            .MatchRegex($"<option value=\"{_pedro}\" selected[^>]*>Pedro Produção</option>")
            .And.NotContain("Escolha o produtor");
    }

    [Theory]
    [InlineData("lixo")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("admin")]
    public async Task Produtor_que_nao_vale_no_endereco_nao_abre_painel_nem_consulta(string valor)
    {
        var produtor = valor == "admin" ? _admin.ToString() : valor;
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync($"/produtores?produtor={produtor}"));

        html.Should().Contain("<p class=\"aj-subtitulo\">1 produtor</p>").And.Contain("Pedro Produção");
        html.Should().NotContain("Produtividade —").And.NotContain("Em acompanhamento");
        var acessos = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().CountAsync(a => a.TipoRecurso == "Produtividade"));
        acessos.Should().Be(0);
    }

    [Fact]
    public async Task Menu_mostra_produtores_so_para_a_diretoria()
    {
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/produtores\"");
        (await doPedro.GetStringAsync("/")).Should().NotContain("href=\"/produtores\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/produtores");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }

    // ---- Painel ----

    [Fact]
    public async Task Painel_abre_no_mes_corrente_com_os_totais_e_as_atividades_de_cada_complexidade()
    {
        await ConcluidaAgoraAsync("Petição simples", 1);
        await ConcluidaAgoraAsync("Parecer difícil", 3);
        await ConcluidaAgoraAsync("Recurso difícil", 3);
        var mesCorrente = RotulosDeProdutividade.Mes(RotulosDeProdutividade.MesDe(Hoje()));

        var html = await PainelAsync(_pedro, "Pedro Produção");

        html.Should().Contain("Produtividade — Pedro Produção");
        // Sem conclusão mais antiga, o seletor traz só o mês corrente, e ele está escolhido. O campo do desktop
        // e o do tablet e do mobile são o mesmo controle: trazem as mesmas opções.
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        Meses(desktop).Should().Equal(mesCorrente);
        Textos(desktop, @"<option[^>]*selected[^>]*>\s*([^<]*?)\s*</option>").Should().Equal(mesCorrente);
        Meses(BlocoDeVisao.De(html, "aj-tablet-mobile")).Should().Equal(Meses(desktop));

        // Totais, na ordem dos blocos: complexidade 1, 2, 3 e o total (em destaque).
        Textos(html, "<div class=\"aj-bloco[^\"]*\">\\s*<span class=\"aj-ajuda\">(Complexidade \\d|Total)</span>")
            .Should().Equal("Complexidade 1", "Complexidade 2", "Complexidade 3", "Total");
        Textos(html, "<span class=\"aj-ajuda\">(?:Complexidade \\d|Total)</span>\\s*<b>(\\d+)</b>")
            .Should().Equal("1", "0", "2", "3");
        html.Should().MatchRegex("<div class=\"aj-bloco aj-bloco--destaque\">\\s*<span class=\"aj-ajuda\">Total</span>\\s*<b>3</b>");

        Textos(html, @"<h3>\s*([^<]*?)\s*</h3>")
            .Should().Equal("Complexidade 1 (1)", "Complexidade 2 (0)", "Complexidade 3 (2)");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().Contain("<td class=\"aj-tabela__principal\">Petição simples</td>");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should().Contain("<span class=\"aj-linha__titulo\">Petição simples</span>");
        // Dentro do grupo, da conclusão mais antiga para a mais recente.
        html.IndexOf("Parecer difícil", StringComparison.Ordinal).Should().BePositive()
            .And.BeLessThan(html.IndexOf("Recurso difícil", StringComparison.Ordinal));
        // Só o grupo vazio diz que está vazio.
        Regex.Matches(html, "Nenhuma atividade\\.").Count.Should().Be(1);
        html.Should().Contain("Concluída em").And.Contain("Abrir");
        html.Should().NotContain("Tentar de novo").And.NotContain("Carregando");
    }

    [Fact]
    public async Task Painel_de_quem_nunca_concluiu_mostra_zeros_e_so_o_mes_corrente()
    {
        var html = await PainelAsync(_pedro, "Pedro Produção");

        Textos(html, "<span class=\"aj-ajuda\">(?:Complexidade \\d|Total)</span>\\s*<b>(\\d+)</b>")
            .Should().Equal("0", "0", "0", "0");
        Regex.Matches(html, "Nenhuma atividade\\.").Count.Should().Be(3);
        Meses(BlocoDeVisao.De(html, "aj-so-desktop")).Should().Equal(RotulosDeProdutividade.Mes(RotulosDeProdutividade.MesDe(Hoje())));
    }

    [Fact]
    public async Task Painel_oferece_os_meses_desde_a_primeira_conclusao()
    {
        var antiga = await ConcluidaAgoraAsync("De três meses atrás", 2);
        var haTresMeses = DateTime.UtcNow.AddMonths(-3);
        await _app.EmEscopoAsync(null, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var atividade = await db.Atividades.SingleAsync(a => a.Id == antiga);
            db.Entry(atividade).Property(a => a.ConcluidaEm).CurrentValue = haTresMeses;
            return await db.SaveChangesAsync();
        });
        var esperados = RotulosDeProdutividade.Lista(Hoje(), RotulosDeProdutividade.MesDe(_fuso, haTresMeses));

        var html = await PainelAsync(_pedro, "Pedro Produção");

        esperados.Count.Should().BeGreaterThan(1);
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        Meses(desktop).Should().Equal(esperados.Select(RotulosDeProdutividade.Mes));
        Meses(BlocoDeVisao.De(html, "aj-tablet-mobile")).Should().Equal(Meses(desktop));
        // O painel abre no mês corrente, e a atividade antiga não é dele.
        Textos(desktop, @"<option[^>]*selected[^>]*>\s*([^<]*?)\s*</option>")
            .Should().Equal(RotulosDeProdutividade.Mes(esperados[0]));
        html.Should().NotContain("De três meses atrás");
    }

    [Fact]
    public async Task Abrir_o_painel_fica_no_registro_de_consultas()
    {
        await PainelAsync(_pedro, "Pedro Produção");

        var acessos = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().ToListAsync());
        acessos.Should().Contain(a => a.TipoRecurso == "Produtividade" && a.Acao == AcaoDeAcesso.Visualizou
            && a.RecursoId == _pedro && a.UsuarioId == _admin);
    }

    [Fact]
    public async Task Painel_de_quem_nao_e_produtor_mostra_o_erro_no_lugar_dos_numeros()
    {
        var html = await PainelAsync(_admin, "Diana Diretora");

        html.Should().MatchRegex("(?s)<div class=\"aj-aviso aj-aviso--erro[^\"]*\"[^>]*>.*?Produtor não encontrado\\..*?Tentar de novo");
        html.Should().NotContain("aj-blocos").And.NotContain("Complexidade 1").And.NotContain("Nenhuma atividade.");
    }
}
