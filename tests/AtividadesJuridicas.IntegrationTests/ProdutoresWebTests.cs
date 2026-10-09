using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Components.Produtores;
using AtividadesJuridicas.Web.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

        html.Should().Contain("Produtores").And.Contain("Acompanhar produtividade").And.NotContain("Acesso negado");
        // Só as células de dados (a das ações tem classe): nome, perfil, OAB e situação de cada linha, em ordem.
        // A diretora não está entre elas, embora o nome dela apareça no cabeçalho da página.
        Textos(html, @"<td>\s*([^<]*?)\s*</td>").Should().Equal(
            "Iara Inativa", "Produção", "", "Inativo",
            "Pedro Produção", "Produção", "", "Ativo");
        Regex.Matches(html, "<tr class=\"inativo\"").Count.Should().Be(1);
        // O painel só abre por clique.
        html.Should().NotContain("Produtividade —");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_a_lista()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/produtores"));

        html.Should().Contain("Acesso negado").And.NotContain("Acompanhar produtividade");
        (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().BeEmpty(
            "a página confere o perfil antes de pedir a lista de usuários");
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
        // Sem conclusão mais antiga, o seletor traz só o mês corrente, e ele está escolhido.
        Meses(html).Should().Equal(mesCorrente);
        Textos(html, @"<option[^>]*selected[^>]*>\s*([^<]*?)\s*</option>").Should().Equal(mesCorrente);

        // Totais, na ordem das colunas: complexidade 1, 2, 3 e o total. Só a linha de totais tem células numéricas.
        Textos(html, @"<th>\s*(Complexidade \d|Total)\s*</th>")
            .Should().Equal("Complexidade 1", "Complexidade 2", "Complexidade 3", "Total");
        Textos(html, @"<td>\s*(\d+)\s*</td>").Should().Equal("1", "0", "2", "3");

        Textos(html, @"<h3>\s*([^<]*?)\s*</h3>")
            .Should().Equal("Complexidade 1 (1)", "Complexidade 2 (0)", "Complexidade 3 (2)");
        html.Should().MatchRegex(@"<td>\s*Petição simples\s*</td>");
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

        Textos(html, @"<td>\s*(\d+)\s*</td>").Should().Equal("0", "0", "0", "0");
        Regex.Matches(html, "Nenhuma atividade\\.").Count.Should().Be(3);
        Meses(html).Should().Equal(RotulosDeProdutividade.Mes(RotulosDeProdutividade.MesDe(Hoje())));
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
        Meses(html).Should().Equal(esperados.Select(RotulosDeProdutividade.Mes));
        // O painel abre no mês corrente, e a atividade antiga não é dele.
        Textos(html, @"<option[^>]*selected[^>]*>\s*([^<]*?)\s*</option>")
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

        html.Should().Contain("Produtor não encontrado.").And.Contain("Tentar de novo");
        html.Should().NotContain("Complexidade 1").And.NotContain("Nenhuma atividade.");
    }
}
