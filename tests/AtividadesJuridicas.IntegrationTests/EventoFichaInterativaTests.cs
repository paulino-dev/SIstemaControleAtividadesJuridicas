using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class EventoFichaInterativaTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000e7");
    private static readonly Guid Beatriz = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");
    private static readonly Guid Otavio = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b2");

    private static readonly AcoesDoEvento Gerencia = new(true, true);
    private static readonly AcoesDoEvento SoVe = new(false, false);

    /// <summary>
    /// O evento do quadro: Audiência em 07/10/2026, das 09:00 às 10:30 no fuso do escritório, com cliente e processo, sem
    /// atividade, criada pela Diretoria, versão 7. Participam a Beatriz e a Diretoria (na ordem do servidor, por nome).
    /// </summary>
    private static EventoDetalhe Detalhe(AcoesDoEvento? acoes = null, IReadOnlyList<ParticipanteDetalhe>? participantes = null) => new(
        Id, "Audiência de instrução — Litoral", "Audiência de instrução.\nLevar a proposta de acordo.", "Fórum Trabalhista, sala 4",
        TipoEvento.Audiencia, new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc), 90, new DateTime(2026, 10, 7, 13, 30, 0, DateTimeKind.Utc),
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1"), "Transportes Litoral Ltda.",
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1"), "Litoral — reclamação trabalhista",
        null, null, AtorDaDiretoria.Id, AtorDaDiretoria.Nome,
        participantes ?? new ParticipanteDetalhe[] { new(Beatriz, "Beatriz Lins", true), new(AtorDaDiretoria.Id, AtorDaDiretoria.Nome, true) },
        acoes ?? Gerencia, 7);

    /// <summary>O perfil de quem vê e o evento.</summary>
    private void Combinar(EventoDetalhe? detalhe = null, UsuarioAutenticado? ator = null) =>
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(ator ?? AtorDaDiretoria)
            .Responde<ObterEvento, Resultado<EventoDetalhe>>(Resultado<EventoDetalhe>.Ok(detalhe ?? Detalhe()));

    private IRenderedComponent<EventoFicha> Ficha() => Desenhar<EventoFicha>(p => p.Add(f => f.Id, Id));

    /// <summary>A ficha aberta a partir de um período da agenda: a navegação de teste começa no endereço com a origem.</summary>
    private IRenderedComponent<EventoFicha> FichaVindaDe(string consulta)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/agenda/{Id}{consulta}");
        return Ficha();
    }

    private string Endereco => Services.GetRequiredService<NavigationManager>().Uri;

    private const string NoCabecalho = ".aj-painel__botoes button, .aj-painel__botoes a";
    private const string NaBarra = ".aj-acoes--fixa button, .aj-acoes--fixa a";
    private const string ExcluirNoCabecalho = ".aj-painel__botoes button";
    private const string ExcluirNaBarra = ".aj-acoes--fixa button";
    private const string NaConfirmacao = "[role=alertdialog] button";

    private static void Clicar(IRenderedComponent<EventoFicha> tela, string seletor, string texto) =>
        tela.FindAll(seletor).First(e => e.TextContent == texto).Click();

    private static List<string> Textos(IEnumerable<IElement> elementos) =>
        [.. elementos.Select(e => Regex.Replace(e.TextContent, "\\s+", " ").Trim()).Where(t => t.Length > 0)];

    [Theory]
    // Quem vê, e o que o servidor permite: a Diretoria, a Produção que criou (permite) e a Produção que só participa.
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void As_acoes_seguem_o_que_o_servidor_permite_e_nao_o_perfil(bool diretoria, bool gerencia)
    {
        var ator = diretoria ? AtorDaDiretoria : AtorDaProducao;
        Combinar(Detalhe(gerencia ? Gerencia : SoVe, [new(ator.Id, ator.Nome, true)]), ator);

        var ficha = Ficha();

        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Audiência de instrução — Litoral");
        string[] esperadas = gerencia ? ["Excluir", "Editar"] : [];
        Textos(ficha.FindAll(NoCabecalho)).Should().Equal(esperadas);
        Textos(ficha.FindAll(NaBarra)).Should().Equal(esperadas);
        ficha.FindAll(".aj-painel__botoes").Should().HaveCount(gerencia ? 1 : 0);
        ficha.FindAll(".aj-acoes--fixa").Should().HaveCount(gerencia ? 1 : 0);
        ficha.FindAll($"a[href='/agenda/{Id}/editar']").Should().HaveCount(gerencia ? 2 : 0);
        ficha.FindAll(".aj-botao--destrutivo").Should().HaveCount(gerencia ? 1 : 0);
    }

    [Fact]
    public void Participantes_trazem_voce_primeiro_e_dourado_os_inativos_e_os_sem_nome()
    {
        Combinar(Detalhe(participantes:
        [
            new(Beatriz, "Beatriz Lins", true),
            new(AtorDaDiretoria.Id, AtorDaDiretoria.Nome, true),
            new(Otavio, "Otávio Brandão", false),
            new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b3"), null, false),
        ]));

        var ficha = Ficha();

        var linhas = ficha.FindAll(".aj-participantes > .aj-participante");
        linhas.Select(l => l.QuerySelector(".aj-participante__nome")!.TextContent).Should()
            .Equal("Diana Diretora (você)", "Beatriz Lins", "Otávio Brandão (inativo)", "Usuário não encontrado");
        linhas.Select(l => l.QuerySelector(".aj-avatar")!.TextContent).Should().Equal("DD", "BL", "OB", "?");
        linhas.Select(l => l.QuerySelector(".aj-avatar")!.ClassName).Should().Equal(
            "aj-avatar",
            $"aj-avatar {AvatarDePessoa.Classe("Beatriz Lins")}",
            $"aj-avatar {AvatarDePessoa.Classe("Otávio Brandão")}",
            $"aj-avatar {AvatarDePessoa.Classe("")}");
        linhas.Should().OnlyContain(l => l.QuerySelector(".aj-avatar")!.GetAttribute("aria-hidden") == "true");
        // Um painel só, para as três visões.
        ficha.FindAll(".aj-participantes").Should().ContainSingle();
    }

    [Fact]
    public void Detalhes_trazem_as_nove_linhas_do_quadro_no_desktop_e_no_tablet_e_mobile()
    {
        Combinar();

        var ficha = Ficha();

        string[] termos = ["Tipo", "Início", "Fim", "Duração", "Local", "Cliente", "Processo", "Atividade", "Criado por"];
        ficha.FindAll(".aj-so-desktop .aj-detalhes dt").Select(d => d.TextContent).Should().Equal(termos);
        ficha.FindAll(".aj-tablet-mobile .aj-detalhes dt").Select(d => d.TextContent).Should().Equal(termos);
        ficha.FindAll(".aj-so-desktop .aj-detalhes dd").Select(d => d.TextContent).Should().Equal(
            "Audiência", "07/10/2026 09:00", "07/10/2026 10:30", "90 minutos", "Fórum Trabalhista, sala 4",
            "Transportes Litoral Ltda.", "Litoral — reclamação trabalhista", "—", "Diana Diretora");
        ficha.FindAll("p.aj-texto-livre").Should().ContainSingle()
            .Which.TextContent.Should().Be("Audiência de instrução.\nLevar a proposta de acordo.");
        ficha.Find("p.aj-subtitulo").TextContent.Should().Be("Audiência · 07/10/2026, 09:00 – 10:30 · Fórum Trabalhista, sala 4");
        ficha.Find(".aj-titulo-da-ficha .aj-ajuda").TextContent.Should().Be("Audiência · 07/10/2026, 09:00 – 10:30");
    }

    [Theory]
    [InlineData("Minuta de acordo", false, "Minuta de acordo")]
    [InlineData(null, false, "Atividade vinculada (sem acesso)")]
    [InlineData(null, true, "Atividade vinculada (excluída)")]
    public void Vinculos_indisponiveis_e_criador_sumido_tem_textos_proprios(string? tituloDaAtividade, bool excluida, string atividade)
    {
        Combinar(Detalhe() with
        {
            ClienteNome = null,
            ProcessoTitulo = null,
            AtividadeId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a1"),
            AtividadeTitulo = tituloDaAtividade,
            AtividadeExcluida = excluida,
            CriadorNome = null,
        });

        var ficha = Ficha();

        var pares = ficha.FindAll(".aj-so-desktop .aj-detalhes > div")
            .ToDictionary(d => d.QuerySelector("dt")!.TextContent, d => d.QuerySelector("dd")!.TextContent);
        pares["Cliente"].Should().Be("Cliente indisponível");
        pares["Processo"].Should().Be("Processo indisponível");
        pares["Atividade"].Should().Be(atividade);
        pares["Criado por"].Should().Be("Usuário não encontrado");
    }

    [Fact]
    public void Evento_que_atravessa_a_meia_noite_mostra_a_data_do_fim_no_subtitulo_e_no_resumo()
    {
        // 02:30 UTC de 08/10 é 23:30 de 07/10 no escritório; o fim, 00:30 de 08/10.
        Combinar(Detalhe() with
        {
            InicioEm = new DateTime(2026, 10, 8, 2, 30, 0, DateTimeKind.Utc),
            DuracaoEstimadaMinutos = 60,
            FimEm = new DateTime(2026, 10, 8, 3, 30, 0, DateTimeKind.Utc),
        });

        var ficha = Ficha();

        ficha.Find("p.aj-subtitulo").TextContent.Should().Be("Audiência · 07/10/2026, 23:30 – 08/10 00:30 · Fórum Trabalhista, sala 4");
        ficha.Find(".aj-titulo-da-ficha .aj-ajuda").TextContent.Should().Be("Audiência · 07/10/2026, 23:30 – 08/10 00:30");
        ficha.FindAll(".aj-so-desktop .aj-detalhes dd")[2].TextContent.Should().Be("08/10/2026 00:30");
    }

    [Fact]
    public void Recado_de_evento_salvo_com_sobreposicao_mostra_as_tres_redacoes_uma_vez()
    {
        Combinar();
        // 13:00 a 14:30 UTC é 10:00 a 11:30 no escritório.
        Services.GetRequiredService<Recados>().Deixar("Evento salvo.", TipoDeAviso.Sucesso,
            [new SobreposicaoDeAgenda(Beatriz, "Beatriz Lins",
                new DateTime(2026, 10, 7, 13, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 7, 14, 30, 0, DateTimeKind.Utc))]);

        var ficha = Ficha();

        Textos(ficha.FindAll(".aj-so-desktop p.aj-aviso--atencao")).Should()
            .Equal("Evento salvo. Atenção: Beatriz Lins já tem evento das 10:00 às 11:30 (07/10).");
        Textos(ficha.FindAll(".aj-so-tablet p.aj-aviso--atencao")).Should()
            .Equal("Evento salvo. Beatriz Lins já tem evento das 10:00 às 11:30 (07/10).");
        Textos(ficha.FindAll(".aj-so-mobile p.aj-aviso--atencao")).Should()
            .Equal("Beatriz Lins já tem evento das 10:00 às 11:30 em 07/10.");
        ficha.FindAll("p.aj-aviso--sucesso").Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Recado_sem_sobreposicao_e_um_aviso_de_sucesso()
    {
        Combinar();
        Services.GetRequiredService<Recados>().Deixar("Evento salvo.");

        var ficha = Ficha();

        ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Evento salvo.");
        ficha.FindAll("p.aj-aviso--atencao").Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Excluir_pede_a_confirmacao_e_confirmar_envia_com_a_versao_e_volta_a_agenda_de_origem_com_o_recado()
    {
        Combinar();
        Despachante.Responde<ExcluirEvento, Resultado>(Resultado.Ok());
        var ficha = FichaVindaDe("?visao=dia&data=2026-10-07");

        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        var confirmacao = ficha.Find("[role=alertdialog]");
        confirmacao.GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        confirmacao.ClassList.Should().Contain("aj-aviso--atencao");
        ficha.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a exclusão de Audiência de instrução — Litoral?");
        ficha.Find("[role=alertdialog] .aj-aviso__texto").TextContent.Should().Contain("O evento sai da agenda de todos os participantes.");
        Textos(ficha.FindAll(NaConfirmacao)).Should().Equal("Cancelar", "Confirmar exclusão");
        ficha.Find("[role=alertdialog] .aj-botao--primario").TextContent.Should().Be("Confirmar exclusão");
        Despachante.Recebidas.OfType<ExcluirEvento>().Should().BeEmpty();

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-07"));
        Despachante.Recebidas.OfType<ExcluirEvento>().Should().ContainSingle().Which.Should().Be(new ExcluirEvento(Id, 7));
        var recado = Services.GetRequiredService<Recados>().Retirar()!;
        recado.Texto.Should().Be("Evento excluído.");
        recado.Tipo.Should().Be(TipoDeAviso.Sucesso);
        recado.Sobreposicoes.Should().BeNull();
    }

    [Fact]
    public void Excluir_pela_barra_fixa_abre_a_mesma_confirmacao_tira_o_recado_e_sem_origem_volta_a_agenda()
    {
        Combinar();
        Despachante.Responde<ExcluirEvento, Resultado>(Resultado.Ok());
        Services.GetRequiredService<Recados>().Deixar("Evento salvo.");
        var ficha = Ficha();
        ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Evento salvo.");

        Clicar(ficha, ExcluirNaBarra, "Excluir");

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        ficha.FindAll("p.aj-aviso--sucesso").Should().BeEmpty();

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => Endereco.Should().EndWith("/agenda"));
        Despachante.Recebidas.OfType<ExcluirEvento>().Should().ContainSingle();
    }

    [Fact]
    public void Cancelar_fecha_a_confirmacao_e_nao_envia_nada()
    {
        Combinar();
        var ficha = Ficha();
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");
        ficha.FindAll("[role=alertdialog]").Should().ContainSingle();

        Clicar(ficha, NaConfirmacao, "Cancelar");

        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        Textos(ficha.FindAll(NoCabecalho)).Should().Equal("Excluir", "Editar");
        Despachante.Recebidas.OfType<ExcluirEvento>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ObterEvento>().Should().ContainSingle();
    }

    [Fact]
    public void Clique_duplo_em_confirmar_exclusao_envia_uma_vez()
    {
        var resposta = new TaskCompletionSource<Resultado>();
        Combinar();
        Despachante.RespondeDepois<ExcluirEvento, Resultado>(resposta.Task);
        var ficha = FichaVindaDe("?visao=semana&data=2026-10-05");
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");
        ficha.FindAll(NaConfirmacao).Should().NotBeEmpty().And.OnlyContain(b => b.HasAttribute("disabled"));
        ficha.FindAll(ExcluirNoCabecalho).Should().NotBeEmpty().And.OnlyContain(b => b.HasAttribute("disabled"));
        ficha.FindAll(ExcluirNaBarra).Should().NotBeEmpty().And.OnlyContain(b => b.HasAttribute("disabled"));
        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");
        Clicar(ficha, ExcluirNaBarra, "Excluir");
        resposta.SetResult(Resultado.Ok());

        ficha.WaitForAssertion(() => Endereco.Should().EndWith("/agenda?visao=semana&data=2026-10-05"));
        Despachante.Recebidas.OfType<ExcluirEvento>().Should().ContainSingle().Which.Should().Be(new ExcluirEvento(Id, 7));
    }

    [Fact]
    public void Depois_de_excluir_com_sucesso_o_botao_fica_travado_ate_a_navegacao()
    {
        Combinar();
        Despachante.Responde<ExcluirEvento, Resultado>(Resultado.Ok());
        var ficha = Ficha();
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");
        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");
        ficha.WaitForAssertion(() => Endereco.Should().EndWith("/agenda"));

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        Despachante.Recebidas.OfType<ExcluirEvento>().Should().ContainSingle();
        ficha.FindAll(NaConfirmacao).Should().NotBeEmpty().And.OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Exclusao_recusada_fecha_a_confirmacao_recarrega_e_mostra_a_mensagem_do_servidor()
    {
        var leituras = 0;
        Combinar();
        Despachante
            .Responde<ObterEvento, Resultado<EventoDetalhe>>(_ => Resultado<EventoDetalhe>.Ok(++leituras == 1
                ? Detalhe()
                : Detalhe() with { Titulo = "Audiência de instrução — Litoral (remarcada)", Versao = 8 }))
            .Responde<ExcluirEvento, Resultado>(Resultado.Falha(TipoErro.Conflito,
                "O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        var ficha = FichaVindaDe("?visao=dia&data=2026-10-07");
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => ficha.Find("[role=alert]").TextContent.Should()
            .Contain("O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        Despachante.Recebidas.OfType<ObterEvento>().Should().HaveCount(2);
        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Audiência de instrução — Litoral (remarcada)");
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        ficha.FindAll(ExcluirNoCabecalho).Should().NotBeEmpty().And.OnlyContain(b => !b.HasAttribute("disabled"));
        Endereco.Should().EndWith($"/agenda/{Id}?visao=dia&data=2026-10-07");
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();

        // A nova confirmação leva a versão relida, não a da primeira carga.
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");
        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => Despachante.Recebidas.OfType<ExcluirEvento>().Should().HaveCount(2));
        Despachante.Recebidas.OfType<ExcluirEvento>().Select(e => e.VersaoEsperada).Should().Equal(new uint?[] { 7, 8 });
    }

    [Fact]
    public void Exclusao_de_evento_que_sumiu_mostra_so_o_nao_encontrado()
    {
        var leituras = 0;
        Combinar();
        Despachante
            .Responde<ObterEvento, Resultado<EventoDetalhe>>(_ => ++leituras == 1
                ? Resultado<EventoDetalhe>.Ok(Detalhe())
                : Resultado<EventoDetalhe>.Falha(TipoErro.NaoEncontrado, "Evento não encontrado."))
            .Responde<ExcluirEvento, Resultado>(Resultado.Falha(TipoErro.NaoEncontrado, "Evento não encontrado."));
        var ficha = Ficha();
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => ficha.FindAll(".aj-detalhes").Should().BeEmpty());
        Regex.Matches(ficha.Markup, "Evento não encontrado\\.").Count.Should().Be(1);
        ficha.Find("a.aj-voltar").GetAttribute("href").Should().Be("/agenda");
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Acesso_negado_ao_excluir_encerra_a_tela()
    {
        Combinar();
        Despachante.Responde<ExcluirEvento, Resultado>(Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado."));
        var ficha = Ficha();
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente."));
        ficha.Markup.Should().NotContain("Audiência de instrução — Litoral").And.NotContain("Acesso negado.");
        Despachante.Recebidas.OfType<ObterEvento>().Should().ContainSingle();
        Endereco.Should().NotEndWith("/agenda");
    }

    [Fact]
    public void Nao_encontrado_mostra_o_voltar_com_a_origem_e_nada_mais()
    {
        Combinar();
        Despachante.Responde<ObterEvento, Resultado<EventoDetalhe>>(
            Resultado<EventoDetalhe>.Falha(TipoErro.NaoEncontrado, "Evento não encontrado."));

        var ficha = FichaVindaDe("?visao=mes&data=2026-10-07");

        ficha.Find("[role=alert]").TextContent.Should().Be("Evento não encontrado.");
        ficha.Find("a.aj-voltar").GetAttribute("href").Should().Be("/agenda?visao=mes&data=2026-10-07");
        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Evento");
        ficha.FindAll(".aj-detalhes").Should().BeEmpty();
        ficha.FindAll(".aj-acoes--fixa").Should().BeEmpty();
    }

    [Fact]
    public void Falha_na_consulta_mostra_o_erro_e_tentar_de_novo_traz_a_ficha()
    {
        var tentativas = 0;
        Combinar();
        Despachante.Responde<ObterEvento, Resultado<EventoDetalhe>>(_ => ++tentativas == 1
            ? Resultado<EventoDetalhe>.Falha(TipoErro.Validacao, "Falha de teste.")
            : Resultado<EventoDetalhe>.Ok(Detalhe()));
        var ficha = Ficha();
        ficha.Markup.Should().Contain("Falha de teste.");
        ficha.FindAll(".aj-detalhes").Should().BeEmpty();

        ficha.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        ficha.WaitForAssertion(() => ficha.Find("h1.aj-titulo").TextContent.Should().Be("Audiência de instrução — Litoral"));
        ficha.Markup.Should().NotContain("Falha de teste.");
    }

    [Fact]
    public void Acesso_encerrado_antes_da_carga_nao_consulta_o_evento()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>((UsuarioAutenticado?)null);

        var ficha = Ficha();

        ficha.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente.");
        ficha.FindAll(".aj-detalhes").Should().BeEmpty();
        Despachante.Recebidas.OfType<ObterEvento>().Should().BeEmpty();
    }

    [Fact]
    public void Acesso_negado_na_leitura_do_evento_encerra_a_tela()
    {
        Combinar();
        Despachante.Responde<ObterEvento, Resultado<EventoDetalhe>>(
            Resultado<EventoDetalhe>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var ficha = Ficha();

        ficha.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente.").And.NotContain("Acesso negado.");
        ficha.FindAll(".aj-detalhes").Should().BeEmpty();
    }

    [Fact]
    public void Trocar_de_evento_na_mesma_tela_recarrega_tira_o_recado_e_nao_retira_outro()
    {
        var outro = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000e8");
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterEvento, Resultado<EventoDetalhe>>(r => Resultado<EventoDetalhe>.Ok(r.Id == Id
                ? Detalhe()
                : Detalhe() with { Id = outro, Titulo = "Reunião de alinhamento", Tipo = TipoEvento.Reuniao }));
        var recados = Services.GetRequiredService<Recados>();
        recados.Deixar("Evento salvo.");
        var ficha = Ficha();
        ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Evento salvo.");
        recados.Deixar("Outro recado.");

        ficha.Render(p => p.Add(f => f.Id, outro));

        ficha.WaitForAssertion(() => ficha.Find("h1.aj-titulo").TextContent.Should().Be("Reunião de alinhamento"));
        Despachante.Recebidas.OfType<ObterEvento>().Select(r => r.Id).Should().Equal(Id, outro);
        ficha.FindAll("p.aj-aviso--sucesso").Should().BeEmpty();
        // O recado é retirado só na primeira carga: o deixado depois continua esperando a sua tela.
        recados.Retirar()!.Texto.Should().Be("Outro recado.");

        // O mesmo id não consulta de novo.
        ficha.Render(p => p.Add(f => f.Id, outro));

        Despachante.Recebidas.OfType<ObterEvento>().Should().HaveCount(2);
    }

    [Fact]
    public void Cancelar_a_exclusao_devolve_o_foco_ao_excluir()
    {
        Combinar();
        var ficha = Ficha();
        ficha.Find("#excluir-evento-desktop").TextContent.Should().Be("Excluir");
        ficha.Find("#excluir-evento-barra").TextContent.Should().Be("Excluir");
        Clicar(ficha, ExcluirNaBarra, "Excluir");

        Clicar(ficha, NaConfirmacao, "Cancelar");

        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be("#excluir-evento-desktop, #excluir-evento-barra");
    }

    [Fact]
    public void Exclusao_recusada_leva_o_foco_ao_aviso_de_erro()
    {
        Combinar();
        Despachante.Responde<ExcluirEvento, Resultado>(Resultado.Falha(TipoErro.Conflito,
            "O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        var ficha = Ficha();
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => ficha.Find("[role=alert]").TextContent.Should()
            .Contain("O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        var aviso = ficha.Find("[role=alert]");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }
}
