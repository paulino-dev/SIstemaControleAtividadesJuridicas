using AngleSharp.Dom;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using AuditoriaPagina = AtividadesJuridicas.Web.Components.Pages.Auditoria;

namespace AtividadesJuridicas.IntegrationTests;

public class AuditoriaInterativaTests : TelaInterativa
{
    private const string Painel = "form#filtros-da-auditoria";
    private const string BotaoDoTablet = ".aj-so-tablet button[aria-controls=filtros-da-auditoria]";

    private static readonly FusoDoEscritorio Fuso = new("America/Sao_Paulo");

    private static readonly UsuarioResumo Diana = new(
        AtorDaDiretoria.Id, "Diana Diretora", "diana@x.com", AtorDaDiretoria.PerfilId, "Diretoria", TipoPerfil.Diretoria, null, true, null);

    private static readonly UsuarioResumo Pedro = new(
        AtorDaProducao.Id, "Pedro Produção", "pedro@x.com", AtorDaProducao.PerfilId, "Produção", TipoPerfil.Producao, null, true, null);

    // 120 alterações: três páginas de 50, com o número que a consulta pediu (as linhas não importam aqui).
    private static Resultado<Pagina<RegistroAuditoriaResumo>> Alteracoes(int pagina) =>
        Resultado<Pagina<RegistroAuditoriaResumo>>.Ok(new Pagina<RegistroAuditoriaResumo>([], 120, pagina, ConsultaDeAuditoriaOpcoes.TamanhoDaPagina));

    private static Resultado<Pagina<RegistroAcessoResumo>> Acessos(int total) =>
        Resultado<Pagina<RegistroAcessoResumo>>.Ok(new Pagina<RegistroAcessoResumo>([], total, 1, ConsultaDeAuditoriaOpcoes.TamanhoDaPagina));

    private IRenderedComponent<AuditoriaPagina> Tela()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ConsultarAuditoria, Resultado<Pagina<RegistroAuditoriaResumo>>>(c => Alteracoes(c.Pagina))
            .Responde<ConsultarAcessos, Resultado<Pagina<RegistroAcessoResumo>>>(Acessos(7));
        return Desenhar<AuditoriaPagina>();
    }

    private List<ConsultarAuditoria> ConsultasDeAlteracoes => [.. Despachante.Recebidas.OfType<ConsultarAuditoria>()];

    private List<ConsultarAcessos> ConsultasDeAcessos => [.. Despachante.Recebidas.OfType<ConsultarAcessos>()];

    // Um campo do painel pelo nome ("Usuário", "Entidade", "Gravação"…): o select ou o input do rótulo.
    private static IElement CampoDoPainel(IRenderedComponent<AuditoriaPagina> tela, string nome) =>
        tela.Find(Painel).QuerySelectorAll("label.aj-campo-filtro")
            .Single(l => l.QuerySelector("span")!.TextContent == nome).QuerySelector("select, input")!;

    // O usuário que o campo do painel mostra (o value da opção marcada); nenhuma marcada é "Todos", o vazio.
    private static string UsuarioDoPainel(IRenderedComponent<AuditoriaPagina> tela) =>
        CampoDoPainel(tela, "Usuário").QuerySelector("option[selected]")?.GetAttribute("value") ?? string.Empty;

    private static IElement Botao(IRenderedComponent<AuditoriaPagina> tela, string onde, string texto) =>
        tela.FindAll($"{onde} button").Single(b => b.TextContent == texto);

    [Fact]
    public void Painel_fica_no_html_fechado_abre_com_o_foco_no_primeiro_campo_e_filtrar_fecha_e_devolve_o_foco()
    {
        var tela = Tela();
        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();
        // Os dois botões (o do tablet e o do mobile) apontam para o painel, que existe mesmo fechado.
        tela.FindAll(BotaoDoPainel("filtros-da-auditoria")).Should().HaveCount(2);
        SeletoresFocados.Should().BeEmpty();

        tela.Find(BotaoDoTablet).Click();

        tela.Find(Painel).HasAttribute("hidden").Should().BeFalse();
        tela.Find(BotaoDoTablet).GetAttribute("aria-expanded").Should().Be("true");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-auditoria"));
        tela.Find(CamposDoPainel("filtros-da-auditoria")).ParentElement!.QuerySelector("span")!.TextContent.Should().Be("Usuário");

        CampoDoPainel(tela, "Usuário").Change(Pedro.Id.ToString());
        tela.Find(Painel).Submit();

        ConsultasDeAlteracoes.Last().Filtro.UsuarioId.Should().Be(Pedro.Id);
        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();
        tela.Find(BotaoDoTablet).GetAttribute("aria-expanded").Should().Be("false");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-auditoria"), BotaoDoPainel("filtros-da-auditoria"));
    }

    [Fact]
    public void Esc_no_painel_fecha_sem_consultar_volta_os_campos_ao_aplicado_e_devolve_o_foco_ao_botao()
    {
        var tela = Tela();
        tela.Find(BotaoDoTablet).Click();
        CampoDoPainel(tela, "Usuário").Change(Pedro.Id.ToString());
        var consultas = ConsultasDeAlteracoes.Count;

        CampoDoPainel(tela, "Usuário").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();
        tela.Find(BotaoDoTablet).GetAttribute("aria-expanded").Should().Be("false");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-auditoria"), BotaoDoPainel("filtros-da-auditoria"));
        ConsultasDeAlteracoes.Should().HaveCount(consultas);   // fechar não consulta
        // De volta ao aplicado (nenhum usuário): nenhuma opção marcada, e o navegador mostra "Todos".
        CampoDoPainel(tela, "Usuário").QuerySelector("option[selected]").Should().BeNull();
        CampoDoPainel(tela, "Usuário").QuerySelector("option[value='']")!.TextContent.Should().Be("Todos");
    }

    // Um "Filtrar" que não fecha o painel (identificador inválido) não leva o foco embora: ele fica no painel, com o
    // campo a corrigir. Corrigido e aplicado, o painel fecha e devolve o foco.
    [Fact]
    public void Filtrar_com_identificador_invalido_deixa_o_painel_aberto_sem_tirar_o_foco_dele()
    {
        var tela = Tela();
        tela.Find(BotaoDoTablet).Click();
        CampoDoPainel(tela, "Gravação").Change("lixo");

        tela.Find(Painel).Submit();

        tela.Markup.Should().Contain("Identificador inválido.");
        tela.Find("p.aj-aviso--erro").TextContent.Trim().Should().Be("Identificador inválido.");
        // Sem consulta não há páginas: a paginação da consulta anterior não fica sob o aviso.
        tela.FindAll(".aj-paginacao").Should().BeEmpty();
        tela.Find(Painel).HasAttribute("hidden").Should().BeFalse();
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-auditoria"));   // só o da abertura

        CampoDoPainel(tela, "Gravação").Change("");
        tela.Find(Painel).Submit();

        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();
        tela.Markup.Should().NotContain("Identificador inválido.").And.Contain("Página 1 de 3");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-auditoria"), BotaoDoPainel("filtros-da-auditoria"));
    }

    // Review Focus 5: o que se escolhe no painel e não se aplica não vale na troca de página nem de aba, e o painel
    // reaberto mostra o filtro aplicado.
    [Fact]
    public void Filtro_escolhido_no_painel_e_nao_aplicado_nao_vale_na_troca_de_pagina_nem_de_aba_e_reabrir_mostra_o_aplicado()
    {
        var tela = Tela();
        tela.Find(BotaoDoTablet).Click();
        CampoDoPainel(tela, "Usuário").Change(Pedro.Id.ToString());
        CampoDoPainel(tela, "Entidade").Change("Perfil");
        tela.Find(BotaoDoTablet).Click();   // fecha sem filtrar

        Botao(tela, ".aj-paginacao", "Próxima").Click();

        var consulta = ConsultasDeAlteracoes.Last();
        consulta.Pagina.Should().Be(2);
        consulta.Filtro.UsuarioId.Should().BeNull();
        consulta.Filtro.Entidade.Should().BeNull();

        // Com o painel aberto e um usuário escolhido sem filtrar, a troca de aba também consulta com o aplicado.
        tela.Find(BotaoDoTablet).Click();
        CampoDoPainel(tela, "Usuário").Change(Pedro.Id.ToString());
        Botao(tela, ".aj-segmentado", "Consultas").Click();

        ConsultasDeAcessos.Should().ContainSingle().Which.Filtro.UsuarioId.Should().BeNull();

        // Fechado e reaberto, o painel mostra o aplicado (nenhum usuário), e não o que ficou escolhido.
        tela.Find(BotaoDoTablet).Click();
        tela.Find(BotaoDoTablet).Click();
        CampoDoPainel(tela, "Usuário").QuerySelector("option[selected]").Should().BeNull();
        CampoDoPainel(tela, "Usuário").QuerySelector("option[value='']")!.TextContent.Should().Be("Todos");
    }

    // Review Focus 5 com um filtro já aplicado: o que se escolhe no painel e se descarta (pelo botão ou pelo Esc) dá
    // lugar ao aplicado, e não ao filtro vazio; reaberto, o painel mostra o aplicado, e a troca de página consulta com ele.
    [Fact]
    public void Com_um_usuario_aplicado_fechar_o_painel_sem_filtrar_volta_a_ele_e_a_proxima_pagina_o_mantem()
    {
        var tela = Tela();
        tela.Find(BotaoDoTablet).Click();
        CampoDoPainel(tela, "Usuário").Change(Pedro.Id.ToString());
        tela.Find(Painel).Submit();
        ConsultasDeAlteracoes.Last().Filtro.UsuarioId.Should().Be(Pedro.Id);

        // Escolhe outro usuário e fecha pelo botão.
        tela.Find(BotaoDoTablet).Click();
        UsuarioDoPainel(tela).Should().Be(Pedro.Id.ToString());
        CampoDoPainel(tela, "Usuário").Change(Diana.Id.ToString());
        tela.Find(BotaoDoTablet).Click();

        // Fechado (o painel fica no HTML, escondido), o campo já voltou ao aplicado; reaberto, continua nele.
        UsuarioDoPainel(tela).Should().Be(Pedro.Id.ToString());
        tela.Find(BotaoDoTablet).Click();
        UsuarioDoPainel(tela).Should().Be(Pedro.Id.ToString());

        // Escolhe outro usuário e fecha pelo Esc.
        CampoDoPainel(tela, "Usuário").Change(Diana.Id.ToString());
        CampoDoPainel(tela, "Usuário").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();
        UsuarioDoPainel(tela).Should().Be(Pedro.Id.ToString());

        tela.Find(BotaoDoTablet).Click();
        UsuarioDoPainel(tela).Should().Be(Pedro.Id.ToString());
        tela.Find(BotaoDoTablet).Click();

        Botao(tela, ".aj-paginacao", "Próxima").Click();

        var consulta = ConsultasDeAlteracoes.Last();
        consulta.Pagina.Should().Be(2);
        consulta.Filtro.UsuarioId.Should().Be(Pedro.Id);
    }

    // O período, na linha do tablet e do mobile, também só vale depois do "Filtrar" (o Enter da linha).
    [Fact]
    public void Periodo_trocado_na_linha_sem_filtrar_nao_vale_na_troca_de_pagina()
    {
        var tela = Tela();
        tela.Find("form.aj-linha-filtros input[type=date]").Change("2026-10-01");

        Botao(tela, ".aj-paginacao", "Próxima").Click();

        ConsultasDeAlteracoes.Last().Pagina.Should().Be(2);
        ConsultasDeAlteracoes.Last().Filtro.DeUtc.Should().BeNull();

        tela.Find("form.aj-linha-filtros").Submit();

        ConsultasDeAlteracoes.Last().Pagina.Should().Be(1);
        ConsultasDeAlteracoes.Last().Filtro.DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 1)));
    }

    // C3: a lista de usuários do filtro faz parte da carga inicial; se é ela que falha, "Tentar de novo" a refaz (e
    // depois consulta a primeira página), em vez de consultar só os registros.
    [Fact]
    public void Falha_ao_carregar_os_usuarios_e_tentar_de_novo_refaz_a_carga_inicial()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
                Resultado<IReadOnlyList<UsuarioResumo>>.Falha(TipoErro.Validacao, "Falha de teste."))
            .Responde<ConsultarAuditoria, Resultado<Pagina<RegistroAuditoriaResumo>>>(c => Alteracoes(c.Pagina));
        var tela = Desenhar<AuditoriaPagina>();

        tela.Markup.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        ConsultasDeAlteracoes.Should().BeEmpty();

        Despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]));
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().NotContain("Falha de teste.").And.Contain("Página 1 de 3"));
        Despachante.Recebidas.OfType<ListarUsuarios>().Should().HaveCount(2);
        ConsultasDeAlteracoes.Should().ContainSingle().Which.Pagina.Should().Be(1);
        // Os usuários chegaram ao filtro.
        CampoDoPainel(tela, "Usuário").QuerySelectorAll("option").Select(o => o.TextContent)
            .Should().Equal("Todos", "Diana Diretora", "Pedro Produção");
    }

    // C4: enquanto a aba nova carrega, a paginação sai (não mostra os números de outra consulta) e volta com os da nova.
    [Fact]
    public void Enquanto_a_aba_nova_carrega_a_paginacao_sai_e_volta_com_os_numeros_dela()
    {
        var tela = Tela();
        tela.Find(".aj-paginacao .aj-subtitulo").TextContent.Should().Be("Página 1 de 3 · 120 registros");
        var acessos = new TaskCompletionSource<Resultado<Pagina<RegistroAcessoResumo>>>();
        Despachante.RespondeDepois<ConsultarAcessos, Resultado<Pagina<RegistroAcessoResumo>>>(acessos.Task);

        Botao(tela, ".aj-segmentado", "Consultas").Click();

        tela.Markup.Should().Contain("Carregando…");
        tela.FindAll(".aj-paginacao").Should().BeEmpty();

        acessos.SetResult(Acessos(7));

        tela.WaitForAssertion(() => tela.Find(".aj-paginacao .aj-subtitulo").TextContent.Should().Be("Página 1 de 1 · 7 registros"));
        tela.Markup.Should().NotContain("Carregando…");
    }
}
