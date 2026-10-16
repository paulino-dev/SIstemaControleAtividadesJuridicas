using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Agenda;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PaginaDaAgenda = AtividadesJuridicas.Web.Components.Pages.Agenda;

namespace AtividadesJuridicas.IntegrationTests;

public class AgendaInterativaTests : TelaInterativa
{
    private static readonly FusoDoEscritorio Fuso = new("America/Sao_Paulo");

    private static readonly UsuarioResumo Diana = new(
        AtorDaDiretoria.Id, "Diana Diretora", "diana@x.com", AtorDaDiretoria.PerfilId, "Diretoria", TipoPerfil.Diretoria, null, true, null);

    private static readonly UsuarioResumo Pedro = new(
        AtorDaProducao.Id, "Pedro Produção", "pedro@x.com", AtorDaProducao.PerfilId, "Produção", TipoPerfil.Producao, null, true, null);

    // Onde ficam os controles de cada largura.
    private const string Desktop = ".aj-barra-filtros--dividida";
    private const string Tablet = ".aj-barra-filtros--plana";
    private const string Mobile = ".aj-barra-superior__setas";
    private const string VisoesDoDesktop = ".aj-barra-filtros--dividida [aria-label='Visualização da agenda']";
    private const string VisoesDoTablet = ".aj-barra-filtros--plana [aria-label='Visualização']";
    private const string VisoesDoMobile = ".aj-barra-superior .aj-segmentado--claro";

    private static DateTime Local(int ano, int mes, int dia, int hora, int minuto = 0) =>
        Fuso.ParaUtc(new DateTime(ano, mes, dia, hora, minuto, 0));

    private static EventoResumo Evento(string titulo, DateTime inicio, int minutos = 60, TipoEvento tipo = TipoEvento.Reuniao) =>
        new(Guid.CreateVersion7(), titulo, tipo, inicio, inicio.AddMinutes(minutos), null, null, null, ["Diana Diretora"]);

    private static Resultado<IReadOnlyList<EventoResumo>> Eventos(params EventoResumo[] eventos) =>
        Resultado<IReadOnlyList<EventoResumo>>.Ok(eventos);

    private static DateOnly Hoje() => DateOnly.FromDateTime(Fuso.ParaLocal(DateTime.UtcNow));

    private NavigationManager Navegacao => Services.GetRequiredService<NavigationManager>();

    private string Endereco => Navegacao.Uri;

    private List<ListarEventos> Consultas => [.. Despachante.Recebidas.OfType<ListarEventos>()];

    private IRenderedComponent<ComBarraSuperior<PaginaDaAgenda>> Agenda(
        string endereco, UsuarioAutenticado? ator = null, params EventoResumo[] eventos)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(ator ?? AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(Eventos(eventos));
        // A agenda lê visao e data do endereço: a navegação de teste começa nele.
        Navegacao.NavigateTo(endereco);
        return Desenhar<ComBarraSuperior<PaginaDaAgenda>>();
    }

    private static string Texto(IElement elemento) => Regex.Replace(elemento.TextContent, "\\s+", " ").Trim();

    private static IElement Botao<T>(IRenderedComponent<T> tela, string onde, string rotulo) where T : IComponent =>
        tela.Find($"{onde} button[aria-label='{rotulo}']");

    private static IElement Opcao<T>(IRenderedComponent<T> tela, string grupo, string texto) where T : IComponent =>
        tela.FindAll($"{grupo} .aj-segmentado__opcao").Single(b => b.TextContent == texto);

    private static string Pressionada<T>(IRenderedComponent<T> tela, string grupo) where T : IComponent =>
        tela.Find($"{grupo} .aj-segmentado__opcao[aria-pressed=true]").TextContent;

    private static string Subtitulo<T>(IRenderedComponent<T> tela) where T : IComponent =>
        Texto(tela.Find("header.aj-cabecalho p.aj-subtitulo"));

    // Um campo de filtro pelo nome ("Tipo", "Participante"), na barra do desktop ou no painel.
    private static IElement Campo<T>(IRenderedComponent<T> tela, string onde, string nome) where T : IComponent =>
        tela.Find(onde).QuerySelectorAll("label.aj-campo-filtro")
            .Single(l => l.QuerySelector("span")!.TextContent == nome).QuerySelector("select")!;

    private static IElement CampoDaBarra<T>(IRenderedComponent<T> tela, string nome) where T : IComponent =>
        Campo(tela, "form.aj-barra-filtros__grupo", nome);

    private static IElement CampoDoPainel<T>(IRenderedComponent<T> tela, string nome) where T : IComponent =>
        Campo(tela, "form#filtros-da-agenda", nome);

    // O seletor do título do Dia que a agenda pede ao módulo de foco (o do desktop e o da seção do tablet: o módulo foca
    // o que está à vista). Os pedidos se leem pelo SeletoresFocados da TelaInterativa, que já monta o módulo de foco.
    private const string TituloDoDia = ".aj-dia__cabeca, .aj-secao-dia__cabeca";

    // Cinco eventos em 14/10: a célula do Mês mostra três e o "+2".
    private static EventoResumo[] CincoEm14() =>
    [
        Evento("Primeiro", Local(2026, 10, 14, 9)), Evento("Segundo", Local(2026, 10, 14, 10)),
        Evento("Terceiro", Local(2026, 10, 14, 11)), Evento("Quarto", Local(2026, 10, 14, 12)),
        Evento("Quinto", Local(2026, 10, 14, 13)),
    ];

    // Review Focus 1: sem endereço ou com endereço estranho, hoje no padrão de cada largura, com uma consulta só.
    [Theory]
    [InlineData("/agenda")]
    [InlineData("/agenda?visao=xyz")]
    [InlineData("/agenda?data=2026-13-40")]
    [InlineData("/agenda?data=1999-01-01")]
    [InlineData("/agenda?visao=xyz&data=08-10-2026")]
    public void Sem_endereco_ou_com_endereco_estranho_abre_hoje_no_padrao_com_uma_consulta_so(string endereco)
    {
        var tela = Agenda(endereco);
        var hoje = Hoje();
        var segunda = RotulosDeAgenda.SegundaDe(hoje);

        var consulta = Consultas.Should().ContainSingle().Which;
        consulta.DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(segunda));
        consulta.AteUtc.Should().Be(Fuso.InicioDoDiaEmUtc(segunda.AddDays(7)));
        Subtitulo(tela).Should().Be(RotulosDeAgenda.SubtituloDaSemana(segunda));
        Pressionada(tela, VisoesDoDesktop).Should().Be("Semana");
        Pressionada(tela, VisoesDoTablet).Should().Be("Semana");
        Pressionada(tela, VisoesDoMobile).Should().Be("Dia");
        // A Semana no desktop e no tablet, o Dia no mobile, sobre os mesmos eventos.
        tela.FindAll("div.aj-desktop-tablet .aj-semana").Should().ContainSingle();
        tela.FindAll("div.aj-so-mobile .aj-dia-escolhido").Should().ContainSingle();
        tela.FindAll(".aj-barra-superior .aj-faixa-dias").Should().ContainSingle();
        Texto(tela.Find(".aj-barra-superior__periodo strong")).Should().Be(RotulosDeAgenda.MesDoMobile(hoje));
        tela.Markup.Should().NotContain("aj-mes__grade").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public void Visao_mes_consulta_as_semanas_inteiras_do_mes_e_mostra_a_grade_compacta_no_mobile()
    {
        var tela = Agenda("/agenda?visao=mes&data=2026-10-15");

        var consulta = Consultas.Should().ContainSingle().Which;
        consulta.DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 9, 28)));
        consulta.AteUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 11, 2)));
        Subtitulo(tela).Should().Be("Outubro de 2026");
        Texto(tela.Find(".aj-barra-superior__periodo strong")).Should().Be("Outubro 2026");
        Pressionada(tela, VisoesDoDesktop).Should().Be("Mês");
        Pressionada(tela, VisoesDoTablet).Should().Be("Mês");
        Pressionada(tela, VisoesDoMobile).Should().Be("Mês");
        tela.FindAll(".aj-mes").Should().ContainSingle();
        // A grade compacta fica no topo do bloco do mobile, antes da lista do dia, e rola com ela; a barra fica com o
        // segmentado e a linha do mês.
        tela.FindAll("div.aj-so-mobile > .aj-faixa-dias--mes").Should().ContainSingle()
            .Which.NextElementSibling!.ClassName.Should().Be("aj-dia-escolhido");
        tela.FindAll(".aj-barra-superior .aj-faixa-dias").Should().BeEmpty();
        tela.Find("header.aj-barra-superior").Children.Select(c => c.ClassName).Should().Equal(
            "aj-barra-superior__linha", "aj-segmentado aj-segmentado--recuado aj-segmentado--claro", "aj-barra-superior__periodo");
        tela.FindAll(".aj-semana").Should().BeEmpty();
        tela.FindAll($"{Mobile} button").Select(b => b.GetAttribute("aria-label")).Should().Equal("Mês anterior", "Próximo mês");
        tela.Find("header.aj-cabecalho a.aj-botao--primario").GetAttribute("href").Should().Be("/agenda/novo?visao=mes&data=2026-10-15");
    }

    [Theory]
    // Desktop: uma semana (Semana e sem visão), um dia (Dia) ou um mês (Mês); o dia que não existe vira o último do mês.
    [InlineData("/agenda?data=2026-10-08", Desktop, "Período anterior", "/agenda?data=2026-10-01")]
    [InlineData("/agenda?data=2026-10-08", Desktop, "Próximo período", "/agenda?data=2026-10-15")]
    [InlineData("/agenda?visao=semana&data=2026-10-08", Desktop, "Próximo período", "/agenda?visao=semana&data=2026-10-15")]
    [InlineData("/agenda?visao=dia&data=2026-10-08", Desktop, "Período anterior", "/agenda?visao=dia&data=2026-10-07")]
    [InlineData("/agenda?visao=mes&data=2026-10-31", Desktop, "Próximo período", "/agenda?visao=mes&data=2026-11-30")]
    [InlineData("/agenda?visao=mes&data=2026-01-31", Desktop, "Próximo período", "/agenda?visao=mes&data=2026-02-28")]
    // Tablet: os rótulos de cada visão.
    [InlineData("/agenda?data=2026-10-08", Tablet, "Semana anterior", "/agenda?data=2026-10-01")]
    [InlineData("/agenda?visao=dia&data=2026-10-08", Tablet, "Próximo dia", "/agenda?visao=dia&data=2026-10-09")]
    [InlineData("/agenda?visao=mes&data=2026-10-08", Tablet, "Mês anterior", "/agenda?visao=mes&data=2026-09-08")]
    // Mobile: no Dia e na Semana as setas andam a faixa de semana em semana; no Mês, de mês em mês.
    [InlineData("/agenda?data=2026-10-08", Mobile, "Próxima semana", "/agenda?data=2026-10-15")]
    [InlineData("/agenda?visao=dia&data=2026-10-08", Mobile, "Semana anterior", "/agenda?visao=dia&data=2026-10-01")]
    [InlineData("/agenda?visao=semana&data=2026-10-08", Mobile, "Próxima semana", "/agenda?visao=semana&data=2026-10-15")]
    [InlineData("/agenda?visao=mes&data=2026-10-08", Mobile, "Próximo mês", "/agenda?visao=mes&data=2026-11-08")]
    public void Setas_de_cada_largura_levam_ao_periodo_vizinho(string endereco, string onde, string rotulo, string destino)
    {
        var tela = Agenda(endereco);

        Botao(tela, onde, rotulo).Click();

        Endereco.Should().EndWith(destino);
    }

    [Fact]
    public void Hoje_leva_a_data_de_hoje_mantendo_a_visao_e_o_mobile_nao_tem_hoje()
    {
        var tela = Agenda("/agenda?visao=dia&data=2020-01-06");
        var hoje = Hoje().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        tela.Find($"{Desktop} .aj-segmentado__hoje").Click();

        Endereco.Should().EndWith($"/agenda?visao=dia&data={hoje}");

        Navegacao.NavigateTo("/agenda?data=2020-01-06");
        tela.Find($"{Tablet} .aj-segmentado__hoje").Click();

        Endereco.Should().EndWith($"/agenda?data={hoje}");
        // O mobile tem as duas setas e nenhum "Hoje" (o quadro).
        tela.FindAll($"{Mobile} button").Should().HaveCount(2);
        tela.FindAll(".aj-barra-superior button").Where(b => Texto(b) == "Hoje").Should().BeEmpty();
    }

    [Fact]
    public void Visoes_trocam_a_visao_com_a_mesma_data()
    {
        var tela = Agenda("/agenda?data=2026-10-08");

        Opcao(tela, VisoesDoDesktop, "Mês").Click();

        Endereco.Should().EndWith("/agenda?visao=mes&data=2026-10-08");
        Pressionada(tela, VisoesDoDesktop).Should().Be("Mês");

        Opcao(tela, VisoesDoMobile, "Semana").Click();
        Endereco.Should().EndWith("/agenda?visao=semana&data=2026-10-08");

        Opcao(tela, VisoesDoTablet, "Dia").Click();
        Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-08");

        // Sem data no endereço, a visão vai sozinha (a data continua sendo hoje).
        Navegacao.NavigateTo("/agenda");
        Opcao(tela, VisoesDoMobile, "Semana").Click();
        Endereco.Should().EndWith("/agenda?visao=semana");
    }

    [Fact]
    public void Faixa_de_dias_troca_a_data_e_na_semana_leva_ao_dia()
    {
        var tela = Agenda("/agenda?data=2026-10-08");

        tela.Find(".aj-faixa-dias button[aria-label^='Segunda, 05/10']").Click();

        Endereco.Should().EndWith("/agenda?data=2026-10-05");

        Navegacao.NavigateTo("/agenda?visao=semana&data=2026-10-08");
        tela.Find(".aj-faixa-dias button[aria-label^='Sexta, 09/10']").Click();

        Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-09");
        Consultas.Should().ContainSingle();   // sempre a mesma semana
    }

    [Fact]
    public void Grade_compacta_do_mes_troca_a_data_e_fica_no_mes()
    {
        var tela = Agenda("/agenda?visao=mes&data=2026-10-08");

        tela.FindAll("div.aj-so-mobile > .aj-faixa-dias--mes .aj-faixa-dias__dia")
            .Single(d => !d.ClassList.Contains("aj-faixa-dias__dia--fora") && Texto(d.QuerySelector(".aj-faixa-dias__numero")!) == "20")
            .Click();

        Endereco.Should().EndWith("/agenda?visao=mes&data=2026-10-20");
        Pressionada(tela, VisoesDoMobile).Should().Be("Mês");
        Consultas.Should().ContainSingle();   // o mesmo mês: a mesma consulta
    }

    [Fact]
    public void Mais_eventos_da_celula_do_mes_leva_ao_dia_daquela_data()
    {
        var tela = Agenda("/agenda?visao=mes&data=2026-10-08", AtorDaDiretoria,
            Evento("Primeiro", Local(2026, 10, 14, 9)), Evento("Segundo", Local(2026, 10, 14, 10)),
            Evento("Terceiro", Local(2026, 10, 14, 11)), Evento("Quarto", Local(2026, 10, 14, 12)),
            Evento("Quinto", Local(2026, 10, 14, 13)));

        tela.FindAll("button[aria-label='Mais 2 eventos em 14/10']").Should().ContainSingle().Which.Click();

        Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-14");
        Pressionada(tela, VisoesDoDesktop).Should().Be("Dia");
    }

    [Fact]
    public void Trocar_de_periodo_dentro_do_mesmo_intervalo_nao_repete_a_consulta()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        Consultas.Should().ContainSingle();

        Opcao(tela, VisoesDoDesktop, "Dia").Click();

        Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-08");
        Pressionada(tela, VisoesDoDesktop).Should().Be("Dia");
        Subtitulo(tela).Should().Be("Quinta-feira, 08/10/2026");
        Consultas.Should().ContainSingle();   // o Dia consulta a mesma semana

        Botao(tela, Desktop, "Próximo período").Click();
        Subtitulo(tela).Should().Be("Sexta-feira, 09/10/2026");
        Botao(tela, Desktop, "Próximo período").Click();
        Botao(tela, Desktop, "Próximo período").Click();
        Subtitulo(tela).Should().Be("Domingo, 11/10/2026");
        Consultas.Should().ContainSingle();

        Botao(tela, Desktop, "Próximo período").Click();   // segunda, 12/10: outra semana

        Consultas.Should().HaveCount(2);
        Consultas[1].DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 12)));
        Consultas[1].AteUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 19)));
    }

    [Fact]
    public void Filtro_fica_ao_trocar_de_periodo_e_filtrar_sem_mudanca_nao_consulta()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        CampoDaBarra(tela, "Tipo").Change(nameof(TipoEvento.Prazo));
        CampoDaBarra(tela, "Participante").Change(Pedro.Id.ToString());

        tela.Find("form.aj-barra-filtros__grupo").Submit();

        Consultas.Should().HaveCount(2);
        Consultas[1].Tipo.Should().Be(TipoEvento.Prazo);
        Consultas[1].ParticipanteId.Should().Be(Pedro.Id);
        Consultas[1].DeUtc.Should().Be(Consultas[0].DeUtc);

        tela.Find("form.aj-barra-filtros__grupo").Submit();
        Consultas.Should().HaveCount(2);   // o mesmo período e o mesmo filtro

        Botao(tela, Desktop, "Próximo período").Click();

        Consultas.Should().HaveCount(3);
        Consultas[2].DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 12)));
        Consultas[2].Tipo.Should().Be(TipoEvento.Prazo);
        Consultas[2].ParticipanteId.Should().Be(Pedro.Id);
        CampoDaBarra(tela, "Tipo").QuerySelector("option[selected]")!.TextContent.Should().Be("Prazo");
        CampoDaBarra(tela, "Participante").QuerySelector("option[selected]")!.TextContent.Should().Be("Pedro Produção");
    }

    [Fact]
    public void Painel_de_filtros_do_tablet_abre_com_o_foco_no_tipo_filtrar_aplica_e_fecha_e_limpar_volta_a_todos()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        var botao = tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]");
        Texto(botao).Should().Be("Filtros");
        botao.GetAttribute("aria-expanded").Should().Be("false");
        // Fechado, o painel fica no HTML, escondido: o aria-controls dos dois botões aponta para um form que existe.
        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeTrue();
        SeletoresFocados.Should().BeEmpty();

        botao.Click();

        tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]").GetAttribute("aria-expanded").Should().Be("true");
        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeFalse();
        // O foco vai ao primeiro campo do painel: o pedido ao módulo e, nesse seletor, o primeiro é o Tipo.
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-agenda"));
        tela.Find(CamposDoPainel("filtros-da-agenda")).ParentElement!.QuerySelector("span")!.TextContent.Should().Be("Tipo");
        CampoDoPainel(tela, "Tipo").QuerySelectorAll("option").Select(o => o.TextContent)
            .Should().Equal("Todos", "Audiência", "Reunião", "Prazo", "Outro");
        CampoDoPainel(tela, "Participante").QuerySelectorAll("option").Select(o => o.TextContent)
            .Should().Equal("Todos", "Diana Diretora", "Pedro Produção");
        Texto(tela.Find("form#filtros-da-agenda .aj-painel__acoes")).Should().Be("Limpar Filtrar");

        CampoDoPainel(tela, "Tipo").Change(nameof(TipoEvento.Audiencia));
        CampoDoPainel(tela, "Participante").Change(Pedro.Id.ToString());
        tela.Find("form#filtros-da-agenda").Submit();

        Consultas.Should().HaveCount(2);
        Consultas[1].Tipo.Should().Be(TipoEvento.Audiencia);
        Consultas[1].ParticipanteId.Should().Be(Pedro.Id);
        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeTrue();
        tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]").GetAttribute("aria-expanded").Should().Be("false");
        // "Filtrar" devolve o foco ao botão que abre o painel. São dois (o "Filtrar agenda" do mobile e o "Filtros" do
        // tablet), cada um numa visão: o navegador foca o que está à vista.
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-agenda"), BotaoDoPainel("filtros-da-agenda"));
        tela.FindAll(BotaoDoPainel("filtros-da-agenda")).Select(b => b.GetAttribute("aria-label") ?? Texto(b))
            .Should().Equal("Filtrar agenda", "Filtros");

        tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]").Click();
        CampoDoPainel(tela, "Tipo").QuerySelector("option[selected]")!.TextContent.Should().Be("Audiência");
        CampoDoPainel(tela, "Participante").QuerySelector("option[selected]")!.TextContent.Should().Be("Pedro Produção");
        tela.Find("form#filtros-da-agenda").QuerySelectorAll("button").Single(b => b.TextContent == "Limpar").Click();

        Consultas.Should().HaveCount(2);   // limpar não consulta
        // Com o valor do @bind nulo, o bUnit não marca opção nenhuma (antes do Limpar as marcadas eram "Audiência" e
        // "Pedro Produção"); no navegador, o Blazor escreve "" no campo, e ele mostra a opção de valor vazio, "Todos".
        CampoDoPainel(tela, "Tipo").QuerySelector("option[selected]").Should().BeNull();
        CampoDoPainel(tela, "Tipo").QuerySelector("option[value='']")!.TextContent.Should().Be("Todos");
        CampoDoPainel(tela, "Participante").QuerySelector("option[selected]").Should().BeNull();
        CampoDoPainel(tela, "Participante").QuerySelector("option[value='']")!.TextContent.Should().Be("Todos");
        tela.Find("form#filtros-da-agenda").Submit();
        Consultas.Should().HaveCount(3);
        Consultas[2].Tipo.Should().BeNull();
        Consultas[2].ParticipanteId.Should().BeNull();
    }

    // A consulta lê o filtro aplicado (o do último "Filtrar"): o que se escolhe no painel e não se aplica não vale na
    // troca de período, e fechar o painel sem filtrar volta os campos ao aplicado.
    [Fact]
    public void Filtro_escolhido_no_painel_e_nao_aplicado_nao_vale_na_troca_de_periodo_e_fechar_volta_ao_aplicado()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        var filtros = $"{Tablet} button[aria-controls=filtros-da-agenda]";
        var filtrarAgenda = ".aj-barra-superior button[aria-label='Filtrar agenda']";

        tela.Find(filtros).Click();
        CampoDoPainel(tela, "Tipo").Change(nameof(TipoEvento.Prazo));
        tela.Find(filtros).Click();   // fecha sem filtrar
        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeTrue();
        // Fechar pelo próprio botão não pede foco: ele já está no botão.
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-agenda"));

        Botao(tela, Tablet, "Próxima semana").Click();

        Consultas.Should().HaveCount(2);
        Consultas[1].DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 12)));
        Consultas[1].Tipo.Should().BeNull();

        // Reaberto, o painel mostra o filtro aplicado (nenhum: nenhuma opção marcada, como depois do "Limpar"), e não o
        // que ficou escolhido.
        tela.Find(filtros).Click();
        CampoDoPainel(tela, "Tipo").QuerySelector("option[selected]").Should().BeNull();

        CampoDoPainel(tela, "Tipo").Change(nameof(TipoEvento.Prazo));
        tela.Find("form#filtros-da-agenda").Submit();

        Consultas.Should().HaveCount(3);
        Consultas[2].DeUtc.Should().Be(Consultas[1].DeUtc);
        Consultas[2].Tipo.Should().Be(TipoEvento.Prazo);

        // Com um filtro aplicado, o mesmo pelo "Filtrar agenda" do mobile: escolher outros, fechar sem filtrar e trocar
        // de semana consulta com o aplicado, e o painel reaberto o mostra.
        tela.Find(filtrarAgenda).Click();
        CampoDoPainel(tela, "Tipo").Change(nameof(TipoEvento.Reuniao));
        CampoDoPainel(tela, "Participante").Change(Pedro.Id.ToString());
        tela.Find(filtrarAgenda).Click();

        Botao(tela, Mobile, "Semana anterior").Click();

        Consultas.Should().HaveCount(4);
        Consultas[3].DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 5)));
        Consultas[3].Tipo.Should().Be(TipoEvento.Prazo);
        Consultas[3].ParticipanteId.Should().BeNull();
        tela.Find(filtrarAgenda).Click();
        CampoDoPainel(tela, "Tipo").QuerySelector("option[selected]")!.TextContent.Should().Be("Prazo");
        CampoDoPainel(tela, "Participante").QuerySelector("option[selected]").Should().BeNull();
    }

    // Esc com o foco num campo do painel fecha como o botão: o que se escolheu e não se aplicou sai, nada é consultado,
    // e o foco volta ao botão que abre o painel. Outra tecla não fecha.
    [Fact]
    public void Esc_no_painel_fecha_sem_consultar_descarta_o_que_nao_foi_aplicado_e_devolve_o_foco_ao_botao()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        var filtros = $"{Tablet} button[aria-controls=filtros-da-agenda]";
        tela.Find(filtros).Click();
        CampoDoPainel(tela, "Tipo").Change(nameof(TipoEvento.Prazo));

        CampoDoPainel(tela, "Tipo").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeTrue();
        tela.Find(filtros).GetAttribute("aria-expanded").Should().Be("false");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-agenda"), BotaoDoPainel("filtros-da-agenda"));
        Consultas.Should().ContainSingle();   // fechar não consulta
        // Os campos voltaram ao aplicado (nenhum): nenhuma opção marcada, e o navegador mostra "Todos".
        CampoDoPainel(tela, "Tipo").QuerySelector("option[selected]").Should().BeNull();
        CampoDoPainel(tela, "Tipo").QuerySelector("option[value='']")!.TextContent.Should().Be("Todos");

        tela.Find(filtros).Click();
        CampoDoPainel(tela, "Tipo").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeFalse();
        tela.Find(filtros).GetAttribute("aria-expanded").Should().Be("true");
    }

    [Fact]
    public void Campos_da_barra_do_desktop_trocados_sem_filtrar_nao_valem_na_troca_de_visao_nem_de_periodo()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        CampoDaBarra(tela, "Tipo").Change(nameof(TipoEvento.Prazo));
        CampoDaBarra(tela, "Participante").Change(Pedro.Id.ToString());

        // A mesma semana em outra visão: o pedido é o mesmo, sem consulta nova.
        Opcao(tela, VisoesDoDesktop, "Dia").Click();
        Consultas.Should().ContainSingle();

        Opcao(tela, VisoesDoDesktop, "Semana").Click();
        Botao(tela, Desktop, "Próximo período").Click();

        Consultas.Should().HaveCount(2);
        Consultas[1].DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 12)));
        Consultas[1].Tipo.Should().BeNull();
        Consultas[1].ParticipanteId.Should().BeNull();

        tela.Find("form.aj-barra-filtros__grupo").Submit();

        Consultas.Should().HaveCount(3);
        Consultas[2].DeUtc.Should().Be(Consultas[1].DeUtc);
        Consultas[2].Tipo.Should().Be(TipoEvento.Prazo);
        Consultas[2].ParticipanteId.Should().Be(Pedro.Id);
    }

    [Fact]
    public void Filtrar_agenda_do_mobile_abre_o_mesmo_painel_e_a_producao_so_tem_o_tipo()
    {
        var tela = Agenda("/agenda?data=2026-10-08", AtorDaProducao);
        var botao = tela.Find(".aj-barra-superior button[aria-label='Filtrar agenda']");
        botao.GetAttribute("aria-controls").Should().Be("filtros-da-agenda");

        botao.Click();

        tela.Find(".aj-barra-superior button[aria-label='Filtrar agenda']").GetAttribute("aria-expanded").Should().Be("true");
        tela.Find("form#filtros-da-agenda").QuerySelectorAll("label.aj-campo-filtro span").Select(s => s.TextContent)
            .Should().Equal("Tipo");
        tela.Find("form.aj-barra-filtros__grupo").QuerySelectorAll("label.aj-campo-filtro span").Select(s => s.TextContent)
            .Should().Equal("Tipo");
        Despachante.Recebidas.OfType<ListarUsuarios>().Should().BeEmpty();
        Consultas.Should().OnlyContain(c => c.ParticipanteId == null);
    }

    [Fact]
    public void Recado_de_evento_salvo_com_sobreposicao_aparece_nas_tres_redacoes_e_sai_ao_trocar_de_periodo()
    {
        var recados = Services.GetRequiredService<Recados>();
        recados.Deixar("Evento salvo.", TipoDeAviso.Sucesso,
            [new SobreposicaoDeAgenda(Guid.NewGuid(), "Rafael Teles", Local(2026, 10, 7, 10), Local(2026, 10, 7, 11, 30))]);

        var tela = Agenda("/agenda?visao=semana&data=2026-10-07");

        tela.FindAll("p.aj-aviso--frase").Select(Texto).Should().Equal(
            "Evento salvo. Atenção: Rafael Teles já tem evento das 10:00 às 11:30 (07/10).",
            "Evento salvo. Rafael Teles já tem evento das 10:00 às 11:30 (07/10).",
            "Rafael Teles já tem evento das 10:00 às 11:30 em 07/10.");
        // No topo do conteúdo, antes dos dias.
        tela.Markup.IndexOf("aj-aviso--frase", StringComparison.Ordinal)
            .Should().BeLessThan(tela.Markup.IndexOf("aj-semana", StringComparison.Ordinal));
        recados.Retirar().Should().BeNull();

        Botao(tela, Desktop, "Próximo período").Click();

        tela.FindAll("p.aj-aviso--frase").Should().BeEmpty();
        tela.FindAll(".aj-semana").Should().ContainSingle();
    }

    [Fact]
    public void Recado_de_evento_excluido_e_um_aviso_de_sucesso()
    {
        Services.GetRequiredService<Recados>().Deixar("Evento excluído.");

        var tela = Agenda("/agenda?data=2026-10-08");

        Texto(tela.Find("p.aj-aviso.aj-aviso--sucesso")).Should().Be("Evento excluído.");
        tela.FindAll("p.aj-aviso--frase").Should().BeEmpty();
    }

    [Fact]
    public void Falha_da_consulta_mostra_o_erro_sem_dias_e_tentar_de_novo_consulta_o_mesmo_periodo()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(
                Resultado<IReadOnlyList<EventoResumo>>.Falha(TipoErro.Validacao, "Falha de teste."));
        Navegacao.NavigateTo("/agenda?data=2026-10-08");
        var tela = Desenhar<ComBarraSuperior<PaginaDaAgenda>>();

        tela.Markup.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        // Falha não é semana vazia: nenhum dia aparece; a barra de período continua.
        tela.FindAll(".aj-semana, .aj-secao-dia, .aj-dia-escolhido").Should().BeEmpty();
        tela.Markup.Should().NotContain("Sem eventos");
        tela.FindAll(Desktop).Should().ContainSingle();
        // O erro com "Tentar de novo" é o das listas (EstadoDaLista), não uma cópia da marcação dele.
        tela.FindComponents<EstadoDaLista>().Should().ContainSingle().Which.Instance.Erro.Should().Be("Falha de teste.");

        Despachante.Responde<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(
            Eventos(Evento("Protocolo de documentos", Local(2026, 10, 8, 9, 30), 60, TipoEvento.Outro)));
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.FindAll(".aj-semana").Should().ContainSingle());
        tela.Markup.Should().Contain("Protocolo de documentos").And.NotContain("Falha de teste.");
        Consultas.Should().HaveCount(2);
        Consultas[1].DeUtc.Should().Be(Consultas[0].DeUtc);
    }

    [Fact]
    public void Acesso_negado_na_consulta_encerra_a_tela()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(
                Resultado<IReadOnlyList<EventoResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado."));
        Navegacao.NavigateTo("/agenda?data=2026-10-08");

        var tela = Desenhar<ComBarraSuperior<PaginaDaAgenda>>();

        tela.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente.");
        tela.FindAll(".aj-barra-filtros, .aj-barra-superior__periodo, .aj-fab").Should().BeEmpty();
    }

    [Fact]
    public void Resposta_de_um_periodo_que_ja_ficou_para_tras_e_descartada()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        var velha = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();
        var nova = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();

        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(velha.Task);
        Botao(tela, Desktop, "Próximo período").Click();   // 12/10: a consulta fica esperando
        tela.Markup.Should().Contain("Carregando…");
        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(nova.Task);
        Botao(tela, Desktop, "Próximo período").Click();   // 19/10

        nova.SetResult(Eventos(Evento("Reunião da semana nova", Local(2026, 10, 20, 9))));
        velha.SetResult(Eventos(Evento("Reunião da semana velha", Local(2026, 10, 13, 9))));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Reunião da semana nova"));
        tela.Markup.Should().NotContain("Reunião da semana velha").And.NotContain("Carregando…");
        Subtitulo(tela).Should().Be("Semana de 19/10 a 25/10/2026");
        Consultas.Should().HaveCount(3);
    }

    // F13: as setas e o "Hoje" de cada largura num grupo com nome, como o segmentado das visões.
    [Fact]
    public void Setas_e_hoje_de_cada_largura_ficam_num_grupo_chamado_periodo()
    {
        var tela = Agenda("/agenda?data=2026-10-08");

        static string[] Botoes(IElement grupo) =>
            [.. grupo.QuerySelectorAll("button").Select(b => b.GetAttribute("aria-label") ?? Texto(b))];

        tela.FindAll("[role=group][aria-label='Período']").Should().HaveCount(3);
        Botoes(tela.Find($"{Desktop} [role=group][aria-label='Período']")).Should().Equal("Período anterior", "Hoje", "Próximo período");
        Botoes(tela.Find($"{Tablet} [role=group][aria-label='Período']")).Should().Equal("Semana anterior", "Hoje", "Próxima semana");
        var mobile = tela.Find(Mobile);
        mobile.GetAttribute("role").Should().Be("group");
        mobile.GetAttribute("aria-label").Should().Be("Período");
        Botoes(mobile).Should().Equal("Semana anterior", "Próxima semana");
    }

    // F8: a troca de período deixa a seta no lugar (o foco fica nela) e anuncia o período novo numa região só para
    // leitor de cada largura, com o texto do subtítulo do período daquela largura; o anúncio muda já na carga.
    [Fact]
    public void Troca_de_periodo_anuncia_o_periodo_novo_e_a_seta_continua_na_tela_durante_a_carga()
    {
        const string Anuncio = "div.aj-desktop-tablet > p.aj-so-leitor[role=status]";
        const string AnuncioDoMobile = "div.aj-so-mobile > p.aj-so-leitor[role=status]";
        var tela = Agenda("/agenda?data=2026-10-08");
        Texto(tela.Find(Anuncio)).Should().Be("Semana de 05/10 a 11/10/2026");
        Texto(tela.Find(AnuncioDoMobile)).Should().Be("Quinta-feira, 08/10/2026");

        var semana = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();
        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(semana.Task);
        Botao(tela, Desktop, "Próximo período").Click();

        tela.Markup.Should().Contain("Carregando…");
        Texto(tela.Find(Anuncio)).Should().Be("Semana de 12/10 a 18/10/2026");
        Texto(tela.Find(AnuncioDoMobile)).Should().Be("Quinta-feira, 15/10/2026");
        Subtitulo(tela).Should().Be("Semana de 12/10 a 18/10/2026");
        tela.FindAll($"{Desktop} button[aria-label='Próximo período']").Should().ContainSingle();

        semana.SetResult(Eventos());

        tela.WaitForAssertion(() => tela.Markup.Should().NotContain("Carregando…"));
        Texto(tela.Find(Anuncio)).Should().Be("Semana de 12/10 a 18/10/2026");
        tela.FindAll(Anuncio).Should().ContainSingle();
        tela.FindAll(AnuncioDoMobile).Should().ContainSingle();
    }

    // F9: tocar num dia de fora do mês na grade compacta leva ao outro mês, e a grade fica à vista durante a carga, já
    // com as semanas do mês novo e o dia tocado escolhido: o botão tocado continua na tela. A lista do dia espera.
    [Fact]
    public void Grade_compacta_do_mes_fica_a_vista_durante_a_carga_do_mes_novo()
    {
        const string Grade = "div.aj-so-mobile > .aj-faixa-dias--mes";
        var tela = Agenda("/agenda?visao=mes&data=2026-10-08");
        var novembro = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();
        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(novembro.Task);

        // 01/11 é o último dia (de fora) da grade de outubro.
        tela.Find($"{Grade} button[aria-label^='Domingo, 01/11']").Click();

        Endereco.Should().EndWith("/agenda?visao=mes&data=2026-11-01");
        Consultas.Should().HaveCount(2);
        // A grade de novembro de 2026 vai de 26/10 a 06/12: seis semanas.
        tela.FindAll($"{Grade} button.aj-faixa-dias__dia").Should().HaveCount(42);
        tela.Find($"{Grade} button[aria-pressed=true]").GetAttribute("aria-label").Should().StartWith("Domingo, 01/11");
        Texto(tela.Find("div.aj-so-mobile > p.aj-subtitulo[role=status]")).Should().Be("Carregando…");
        Texto(tela.Find("div.aj-desktop-tablet > p.aj-subtitulo[role=status]")).Should().Be("Carregando…");
        tela.FindAll(".aj-dia-escolhido").Should().BeEmpty();

        novembro.SetResult(Eventos(Evento("Reunião de novembro", Local(2026, 11, 1, 9))));

        tela.WaitForAssertion(() => tela.FindAll("div.aj-so-mobile > .aj-dia-escolhido").Should().ContainSingle());
        tela.Markup.Should().Contain("Reunião de novembro");
        tela.FindAll("div.aj-so-mobile > p.aj-subtitulo[role=status]").Should().BeEmpty();
        tela.FindAll($"{Grade} button.aj-faixa-dias__dia").Should().HaveCount(42);
    }

    // F11: o "+N" do Mês leva ao Dia e, quando o Dia aparece (depois da carga da semana dele), o foco ao título do Dia à
    // vista: o "+N" saiu da tela com a grade. Enquanto a semana carrega, nada é pedido ao módulo de foco.
    [Fact]
    public void Mais_eventos_do_mes_leva_o_foco_ao_titulo_do_dia_quando_ele_aparece()
    {
        var eventos = CincoEm14();
        var tela = Agenda("/agenda?visao=mes&data=2026-10-08", AtorDaDiretoria, eventos);
        var semana = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();
        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(semana.Task);

        tela.Find("button[aria-label='Mais 2 eventos em 14/10']").Click();

        Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-14");
        tela.Markup.Should().Contain("Carregando…");
        SeletoresFocados.Should().BeEmpty();

        semana.SetResult(Eventos(eventos));

        tela.WaitForAssertion(() => SeletoresFocados.Should().Equal(TituloDoDia));
        tela.Find("h2.aj-dia__cabeca").GetAttribute("tabindex").Should().Be("-1");
        tela.Find("section.aj-secao-dia > h2.aj-secao-dia__cabeca").GetAttribute("tabindex").Should().Be("-1");
    }

    // F11: uma seta tocada antes de o Dia aparecer cancela o foco pedido pelo "+N": quem tocou a seta fica com o foco.
    [Fact]
    public void Seta_antes_de_o_dia_aparecer_cancela_o_foco_no_titulo_do_dia()
    {
        var tela = Agenda("/agenda?visao=mes&data=2026-10-08", AtorDaDiretoria, CincoEm14());
        var semana = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();
        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(semana.Task);
        tela.Find("button[aria-label='Mais 2 eventos em 14/10']").Click();

        // 15/10, a mesma semana: a consulta é a que já espera.
        Botao(tela, Desktop, "Próximo período").Click();
        semana.SetResult(Eventos());

        tela.WaitForAssertion(() => tela.FindAll("h2.aj-dia__cabeca").Should().ContainSingle());
        Subtitulo(tela).Should().Be("Quinta-feira, 15/10/2026");
        SeletoresFocados.Should().BeEmpty();
    }
}
