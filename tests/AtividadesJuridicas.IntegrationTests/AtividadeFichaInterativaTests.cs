using Microsoft.AspNetCore.Components;
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Atividades;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class AtividadeFichaInterativaTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a7");

    private static readonly AcoesDaAtividade SemAcoes = new(false, false, false, false, false, false, false);

    /// <summary>A atividade do quadro: Feita, com uma responsável que concluiu e um pendente, versão 7.</summary>
    private static AtividadeDetalhe Detalhe(
        StatusAtividade status = StatusAtividade.Feita, AcoesDaAtividade? acoes = null, IReadOnlyList<HistoricoDetalhe>? historico = null) => new(
        Id, "Minuta de acordo trabalhista", "Redigir a minuta de acordo.", new DateOnly(2026, 10, 9), status, false, null,
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1"), "Transportes Litoral Ltda.",
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1"), "Litoral — reclamação trabalhista",
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000e1"), "Trabalhista",
        TipoAutor.Usuario, "Marina Couto", AtorDaDiretoria.Id, AtorDaDiretoria.Nome,
        [
            new ResponsavelDetalhe(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1"), "Beatriz Lins", true, "Redigir a minuta.",
                new DateTime(2026, 10, 7, 19, 42, 0, DateTimeKind.Utc)),
            new ResponsavelDetalhe(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b2"), "Otávio Brandão", false, null, null),
        ],
        historico ?? new[] { new HistoricoDetalhe(null, StatusAtividade.Criada, "Marina Couto", new DateTime(2026, 10, 1, 13, 2, 0, DateTimeKind.Utc), null) },
        acoes ?? SemAcoes, 7, 2);

    /// <summary>O perfil, a atividade e as listas dos comentários e dos documentos (com quantos de cada).</summary>
    private void Combinar(AtividadeDetalhe? detalhe = null, UsuarioAutenticado? ator = null, int comentarios = 0, int documentos = 0)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(ator ?? AtorDaDiretoria)
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(Resultado<AtividadeDetalhe>.Ok(detalhe ?? Detalhe()))
            .Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(Resultado<IReadOnlyList<ComentarioDetalhe>>.Ok(
                [.. Enumerable.Range(1, comentarios).Select(i => new ComentarioDetalhe(
                    Guid.NewGuid(), Guid.NewGuid(), "Beatriz Lins", $"Comentário {i}", new DateTime(2026, 10, 7, 19, 45, 0, DateTimeKind.Utc)))]))
            .Responde<ListarDocumentosDaAtividade, Resultado<Pagina<DocumentoResumo>>>(Resultado<Pagina<DocumentoResumo>>.Ok(new(
                [.. Enumerable.Range(1, documentos).Select(i => new DocumentoResumo(
                    Guid.NewGuid(), $"documento-{i}.docx", $"documento-{i}.docx", 84 * 1024, CategoriaDocumento.DocumentoAtividade,
                    new DateTime(2026, 10, 7, 19, 40, 0, DateTimeKind.Utc), "Beatriz Lins", Id, "Minuta de acordo trabalhista"))],
                documentos, 1, 20)));
    }

    private IRenderedComponent<AtividadeFicha> Ficha() => Desenhar<AtividadeFicha>(p => p.Add(f => f.Id, Id));

    [Fact]
    public void Tocar_numa_aba_mostra_o_painel_dela_e_esconde_os_outros_no_mobile()
    {
        Combinar();
        var ficha = Ficha();
        ficha.Find(".aj-abas button[aria-selected=true]").TextContent.Should().Be("Detalhes");

        ficha.FindAll(".aj-abas button").Single(b => b.TextContent == "Histórico").Click();

        ficha.Find(".aj-abas button[aria-selected=true]").TextContent.Should().Be("Histórico");
        ficha.Markup.Should()
            .MatchRegex("<div class=\"aj-aba-painel\">\\s*<section class=\"aj-painel aj-painel--ficha aj-painel--historico\">\\s*<h2>Histórico</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\">\\s*<div class=\"aj-grade\">")
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\">\\s*<section class=\"aj-painel[^\"]*\">\\s*<h2>Comentários</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\">\\s*<section class=\"aj-painel[^\"]*\">\\s*<div class=\"aj-painel__cabecalho\">\\s*<h2>Documentos</h2>");
    }

    [Fact]
    public void Comentarios_e_documentos_aparecem_uma_vez_so_e_contam_nas_abas()
    {
        Combinar(comentarios: 2, documentos: 3);

        var ficha = Ficha();

        ficha.FindComponents<ComentariosDaAtividade>().Should().ContainSingle();
        ficha.FindComponents<DocumentosDaAtividade>().Should().ContainSingle();
        ficha.WaitForAssertion(() => ficha.FindAll(".aj-abas button").Select(b => b.TextContent).Should()
            .Equal("Detalhes", "Comentários2", "Documentos3", "Histórico"));
    }

    [Fact]
    public void Aba_fica_sem_contador_quando_a_lista_dela_falha()
    {
        Combinar(documentos: 3);
        Despachante.Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
            Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var ficha = Ficha();

        ficha.WaitForAssertion(() => ficha.FindAll(".aj-abas button").Select(b => b.TextContent).Should()
            .Equal("Detalhes", "Comentários", "Documentos3", "Histórico"));
    }

    [Fact]
    public void Historico_traz_a_mudanca_o_autor_o_motivo_e_a_cor_da_situacao_nova()
    {
        Combinar(Detalhe(StatusAtividade.ParaCorrecao, historico:
        [
            new(null, StatusAtividade.Criada, null, new DateTime(2026, 10, 1, 13, 2, 0, DateTimeKind.Utc), null),
            new(StatusAtividade.Criada, StatusAtividade.Direcionada, "Marina Couto", new DateTime(2026, 10, 1, 13, 20, 0, DateTimeKind.Utc), null),
            new(StatusAtividade.Direcionada, StatusAtividade.Feita, "Otávio Brandão", new DateTime(2026, 10, 8, 12, 15, 0, DateTimeKind.Utc), null),
            new(StatusAtividade.Feita, StatusAtividade.ParaCorrecao, "Marina Couto", new DateTime(2026, 10, 8, 14, 0, 0, DateTimeKind.Utc),
                "Conferir os valores da cláusula terceira."),
        ]));

        var ficha = Ficha();

        // O desktop vem antes na marcação; o tablet e o mobile repetem o mesmo painel.
        ficha.FindAll(".aj-so-desktop .aj-historico__mudanca").Select(e => e.TextContent).Should()
            .Equal("Criada", "Criada → Direcionada", "Direcionada → Feita", "Feita → Para correção");
        ficha.FindAll(".aj-so-desktop .aj-historico__autor").Select(e => e.TextContent).Should()
            .Equal("Sistema · 01/10/2026 10:02", "Marina Couto · 01/10/2026 10:20", "Otávio Brandão · 08/10/2026 09:15",
                "Marina Couto · 08/10/2026 11:00");
        var itens = ficha.FindAll(".aj-so-desktop .aj-historico__item");
        itens.Select(i => i.QuerySelector(".aj-historico__motivo")?.TextContent).Should()
            .Equal(null, null, null, "Conferir os valores da cláusula terceira.");
        ficha.FindAll(".aj-so-desktop .aj-historico__ponto").Select(p => p.ClassName).Should().Equal(
            "aj-historico__ponto",
            "aj-historico__ponto aj-historico__ponto--direcionada",
            "aj-historico__ponto aj-historico__ponto--feita",
            "aj-historico__ponto aj-historico__ponto--correcao aj-historico__ponto--atual");
        ficha.FindAll(".aj-so-desktop .aj-historico__linha").Should().HaveCount(3);
    }

    [Fact]
    public void Responsaveis_trazem_a_parte_concluida_com_as_duas_datas_e_o_pendente_sem_icone()
    {
        Combinar();

        var ficha = Ficha();

        var beatriz = ficha.FindAll(".aj-so-desktop .aj-responsavel")[0];
        beatriz.QuerySelector(".aj-avatar")!.TextContent.Should().Be("BL");
        beatriz.QuerySelector(".aj-avatar")!.ClassName.Should().Be($"aj-avatar {AvatarDePessoa.Classe("Beatriz Lins")}");
        beatriz.QuerySelector(".aj-responsavel__estado svg").Should().NotBeNull();
        beatriz.QuerySelector(".aj-responsavel__estado .aj-so-desktop")!.TextContent.Should().Be("Concluída em 07/10/2026 16:42");
        beatriz.QuerySelector(".aj-responsavel__estado .aj-tablet-mobile")!.TextContent.Should().Be("Concluída em 07/10 16:42");
        beatriz.QuerySelector(".aj-responsavel__direcionamento")!.TextContent.Should().Be("Redigir a minuta.");
        // O direcionamento de outra pessoa não chega do servidor: sem ele, a linha não aparece.
        var otavio = ficha.FindAll(".aj-so-desktop .aj-responsavel")[1];
        otavio.QuerySelector(".aj-responsavel__estado--pendente")!.TextContent.Should().Be("Pendente");
        otavio.QuerySelector(".aj-responsavel__estado svg").Should().BeNull();
        otavio.QuerySelector(".aj-responsavel__direcionamento").Should().BeNull();
    }

    [Fact]
    public void Recado_de_outra_tela_aparece_uma_vez_no_topo()
    {
        Combinar();
        Services.GetRequiredService<Recados>().Deixar("Responsáveis salvos.");

        var ficha = Ficha();

        ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Responsáveis salvos.");
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Nao_encontrada_mostra_o_voltar_sem_paineis_e_nao_consulta_comentarios_nem_documentos()
    {
        Combinar();
        Despachante.Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
            Resultado<AtividadeDetalhe>.Falha(TipoErro.NaoEncontrado, "Atividade não encontrada."));

        var ficha = Ficha();

        ficha.Markup.Should().Contain("Atividade não encontrada.").And.Contain("href=\"/atividades\"");
        ficha.FindAll(".aj-abas").Should().BeEmpty();
        ficha.FindComponents<ComentariosDaAtividade>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarComentariosDaAtividade>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Should().BeEmpty();
    }

    [Fact]
    public void Falha_na_consulta_mostra_o_erro_e_tentar_de_novo_traz_a_ficha()
    {
        var tentativas = 0;
        Combinar();
        Despachante.Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => ++tentativas == 1
            ? Resultado<AtividadeDetalhe>.Falha(TipoErro.Validacao, "Falha de teste.")
            : Resultado<AtividadeDetalhe>.Ok(Detalhe()));
        var ficha = Ficha();
        ficha.Markup.Should().Contain("Falha de teste.");
        ficha.FindAll(".aj-abas").Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarComentariosDaAtividade>().Should().BeEmpty();

        ficha.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        ficha.WaitForAssertion(() => ficha.Find("h1.aj-titulo").TextContent.Should().Be("Minuta de acordo trabalhista"));
        ficha.Markup.Should().NotContain("Falha de teste.");
    }

    [Fact]
    public void Comentarios_que_descobrem_a_atividade_sumida_levam_a_ficha_ao_nao_encontrada()
    {
        var leituras = 0;
        Combinar();
        Despachante
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => ++leituras == 1
                ? Resultado<AtividadeDetalhe>.Ok(Detalhe())
                : Resultado<AtividadeDetalhe>.Falha(TipoErro.NaoEncontrado, "Atividade não encontrada."))
            .Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
                Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.NaoEncontrado, "Atividade não encontrada."));

        var ficha = Ficha();

        ficha.WaitForAssertion(() => ficha.FindAll(".aj-abas").Should().BeEmpty());
        ficha.Markup.Should().Contain("Atividade não encontrada.");
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
    }

    [Fact]
    public void Acesso_negado_num_componente_encerra_a_tela()
    {
        Combinar();
        Despachante.Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
            Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var ficha = Ficha();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente."));
        ficha.Markup.Should().NotContain("Minuta de acordo trabalhista").And.NotContain("<h2>Comentários</h2>").And.NotContain("<h2>Documentos</h2>");
        ficha.FindAll(".aj-abas").Should().BeEmpty();
        ficha.FindComponents<ComentariosDaAtividade>().Should().BeEmpty();
        ficha.FindComponents<DocumentosDaAtividade>().Should().BeEmpty();
    }

    [Fact]
    public void Acesso_negado_nos_documentos_tambem_encerra_a_tela_inteira()
    {
        Combinar();
        Despachante.Responde<ListarDocumentosDaAtividade, Resultado<Pagina<DocumentoResumo>>>(
            Resultado<Pagina<DocumentoResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var ficha = Ficha();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente."));
        ficha.Markup.Should().NotContain("Minuta de acordo trabalhista").And.NotContain("<h2>Comentários</h2>").And.NotContain("<h2>Documentos</h2>");
        ficha.FindAll(".aj-abas").Should().BeEmpty();
    }

    // ---- Ações da ficha ----

    private const string NaFaixa = ".aj-painel__botoes--faixa button";
    private const string NaConfirmacao = "[role=alertdialog] button";

    private static AcoesDaAtividade Acoes(bool editar = false, bool responsaveis = false, bool trocarRevisor = false,
        bool concluir = false, bool aprovar = false, bool reabrir = false, bool excluir = false) =>
        new(editar, responsaveis, trocarRevisor, concluir, aprovar, reabrir, excluir);

    private static void Clicar<T>(IRenderedComponent<T> tela, string seletor, string texto) where T : IComponent =>
        tela.FindAll(seletor).First(e => e.TextContent == texto).Click();

    private static List<string> Textos(IEnumerable<IElement> elementos) =>
        [.. elementos.Select(e => e.TextContent.Trim()).Where(t => t.Length > 0)];

    private static List<string> Lista(string textos) => textos.Length == 0 ? [] : [.. textos.Split('|')];

    private IRenderedComponent<ComBarraSuperior<AtividadeFicha>> FichaComBarra() =>
        Desenhar<ComBarraSuperior<AtividadeFicha>>(p => p.AddUnmatched(nameof(AtividadeFicha.Id), Id));

    [Theory]
    // Permissões (editar, responsáveis, trocar revisor, concluir, aprovar/devolver, reabrir, excluir) → faixa, divisor,
    // "Mais ações" e barra fixa do mobile. Os casos A a F da tabela da tarefa e as bordas do divisor.
    [InlineData(false, false, false, false, false, false, false, "", false, "", "")]
    [InlineData(false, false, false, true, false, false, false, "Concluir minha parte", false, "", "Concluir minha parte")]
    [InlineData(false, false, false, false, true, false, false, "Aprovar|Devolver", false, "", "Devolver|Aprovar")]
    [InlineData(true, false, true, false, true, false, true, "Aprovar|Devolver|Editar|Trocar revisor|Excluir", true, "Editar|Trocar revisor|Excluir", "Devolver|Aprovar")]
    [InlineData(true, true, true, false, false, false, true, "Editar|Responsáveis|Excluir", false, "Editar|Responsáveis|Excluir", "")]
    [InlineData(true, true, true, true, false, false, true, "Concluir minha parte|Editar|Responsáveis|Excluir", true, "Editar|Responsáveis|Excluir", "Concluir minha parte")]
    [InlineData(true, false, false, false, false, true, true, "Reabrir|Editar|Excluir", true, "Editar|Excluir", "Reabrir")]
    [InlineData(true, false, true, false, false, false, true, "Editar|Trocar revisor|Excluir", false, "Editar|Trocar revisor|Excluir", "")]
    public void Cada_acao_aparece_so_com_a_sua_permissao(
        bool editar, bool responsaveis, bool trocarRevisor, bool concluir, bool aprovar, bool reabrir, bool excluir,
        string faixa, bool divisor, string maisAcoes, string barraFixa)
    {
        Combinar(Detalhe(acoes: new(editar, responsaveis, trocarRevisor, concluir, aprovar, reabrir, excluir)));

        var ficha = FichaComBarra();

        ficha.Find(".aj-barra-superior h1").TextContent.Should().Be("Atividade");
        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Minuta de acordo trabalhista");
        ficha.FindAll(".aj-painel__botoes--faixa").Should().HaveCount(faixa.Length == 0 ? 0 : 1);
        Textos(ficha.FindAll(".aj-painel__botoes--faixa a, .aj-painel__botoes--faixa button")).Should().Equal(Lista(faixa));
        ficha.FindAll(".aj-painel__divisor").Should().HaveCount(divisor ? 1 : 0);
        ficha.FindAll(".aj-painel__botoes--faixa button[aria-label='Excluir atividade']").Should().HaveCount(excluir ? 1 : 0);
        ficha.FindAll(".aj-barra-superior button[aria-label='Mais ações']").Should().HaveCount(maisAcoes.Length == 0 ? 0 : 1);
        Textos(ficha.FindAll(".aj-barra-superior .aj-menu-acoes__item")).Should().Equal(Lista(maisAcoes));
        ficha.FindAll(".aj-barra-superior .aj-menu-acoes__item--perigo").Should().HaveCount(excluir ? 1 : 0);
        Textos(ficha.FindAll(".aj-acoes--fixa a, .aj-acoes--fixa button")).Should().Equal(Lista(barraFixa));
    }

    [Fact]
    public void Concluir_minha_parte_pede_a_confirmacao_e_confirmar_envia_recarrega_e_avisa()
    {
        var concluida = false;
        Combinar(ator: AtorDaProducao);
        Despachante
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => Resultado<AtividadeDetalhe>.Ok(concluida
                ? Detalhe(StatusAtividade.Feita)
                : Detalhe(StatusAtividade.Direcionada, Acoes(concluir: true))))
            .Responde<ConcluirMinhaParte, Resultado>(_ =>
            {
                concluida = true;
                return Resultado.Ok();
            });
        var ficha = Ficha();

        Clicar(ficha, NaFaixa, "Concluir minha parte");

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar conclusão");
        ficha.Find("[role=alertdialog] strong").TextContent.Should().Be("Concluir a sua parte em Minuta de acordo trabalhista?");
        ficha.Find("[role=alertdialog] .aj-aviso__texto").TextContent.Should().Contain("A sua parte fica registrada como concluída.");
        Textos(ficha.FindAll(NaConfirmacao)).Should().Equal("Cancelar", "Concluir minha parte");

        Clicar(ficha, NaConfirmacao, "Concluir minha parte");

        ficha.WaitForAssertion(() => ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Sua parte foi concluída."));
        Despachante.Recebidas.OfType<ConcluirMinhaParte>().Should().ContainSingle().Which.Should().Be(new ConcluirMinhaParte(Id));
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        // A ficha recarregada é a de agora: Feita, e sem a ação que acabou de ser feita.
        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Minuta de acordo trabalhista");
        ficha.FindAll(".aj-painel__botoes--faixa").Should().BeEmpty();
        ficha.Markup.Should().MatchRegex("aj-etiqueta--feita[^\"]*\">Feita</span>");
    }

    [Fact]
    public void Aprovar_pede_a_confirmacao_e_confirmar_envia_recarrega_e_avisa()
    {
        var aprovada = false;
        Combinar();
        Despachante
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => Resultado<AtividadeDetalhe>.Ok(aprovada
                ? Detalhe(StatusAtividade.Concluida, Acoes(editar: true, reabrir: true, excluir: true))
                : Detalhe(acoes: Acoes(editar: true, trocarRevisor: true, aprovar: true, excluir: true))))
            .Responde<AprovarAtividade, Resultado>(_ =>
            {
                aprovada = true;
                return Resultado.Ok();
            });
        var ficha = Ficha();

        Clicar(ficha, NaFaixa, "Aprovar");

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar aprovação");
        ficha.Find("[role=alertdialog] strong").TextContent.Should().Be("Aprovar Minuta de acordo trabalhista?");
        ficha.Find("[role=alertdialog] .aj-aviso__texto").TextContent.Should().Contain("A atividade fica concluída.");
        Textos(ficha.FindAll(NaConfirmacao)).Should().Equal("Cancelar", "Aprovar");

        Clicar(ficha, NaConfirmacao, "Aprovar");

        ficha.WaitForAssertion(() => ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Atividade aprovada."));
        Despachante.Recebidas.OfType<AprovarAtividade>().Should().ContainSingle().Which.Should().Be(new AprovarAtividade(Id));
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        Textos(ficha.FindAll(".aj-painel__botoes--faixa a, .aj-painel__botoes--faixa button")).Should().Equal("Reabrir", "Editar", "Excluir");
    }

    [Fact]
    public void Excluir_pede_a_confirmacao_e_confirmar_envia_com_a_versao_e_volta_a_lista_com_o_recado()
    {
        Combinar(Detalhe(acoes: Acoes(editar: true, excluir: true)));
        Despachante.Responde<ExcluirAtividade, Resultado>(Resultado.Ok());
        var ficha = Ficha();

        Clicar(ficha, NaFaixa, "Excluir");

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        ficha.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a exclusão de Minuta de acordo trabalhista?");
        ficha.Find("[role=alertdialog] .aj-aviso__texto").TextContent.Should()
            .Contain("A atividade sai das listas com os responsáveis e o histórico.");
        Textos(ficha.FindAll(NaConfirmacao)).Should().Equal("Cancelar", "Confirmar exclusão");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/atividades"));
        Despachante.Recebidas.OfType<ExcluirAtividade>().Should().ContainSingle().Which.Should().Be(new ExcluirAtividade(Id, 7));
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade excluída.");
    }

    [Fact]
    public void Excluir_vindo_de_produtores_tambem_volta_a_lista_de_atividades_com_o_recado()
    {
        var produtor = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000d1");
        Combinar(Detalhe(acoes: Acoes(editar: true, excluir: true)));
        Despachante.Responde<ExcluirAtividade, Resultado>(Resultado.Ok());
        // A ficha lê "de" e "produtor" do endereço: a navegação de teste começa nele.
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/atividades/{Id}?de=produtores&produtor={produtor}");
        var ficha = Ficha();
        // O voltar é o de Produtores: o endereço chegou à ficha.
        ficha.Find("a.aj-voltar").GetAttribute("href").Should().Be($"/produtores?produtor={produtor}");

        Clicar(ficha, NaFaixa, "Excluir");
        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        // Produtores não lê recados: a exclusão volta sempre à lista de atividades, que mostra o recado.
        ficha.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/atividades"));
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade excluída.");
    }

    [Fact]
    public void Depois_de_excluir_com_sucesso_o_botao_fica_travado_ate_a_navegacao()
    {
        Combinar(Detalhe(acoes: Acoes(editar: true, excluir: true)));
        Despachante.Responde<ExcluirAtividade, Resultado>(Resultado.Ok());
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Excluir");
        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");
        ficha.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/atividades"));

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        Despachante.Recebidas.OfType<ExcluirAtividade>().Should().ContainSingle();
        ficha.FindAll(NaConfirmacao).Should().OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Excluir_pelo_mais_acoes_do_mobile_abre_a_mesma_confirmacao()
    {
        Combinar(Detalhe(acoes: Acoes(editar: true, excluir: true)));
        var ficha = FichaComBarra();

        ficha.Find(".aj-barra-superior button[aria-label='Mais ações']").Click();
        ficha.Find(".aj-barra-superior .aj-menu-acoes__item--perigo").Click();

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        Despachante.Recebidas.OfType<ExcluirAtividade>().Should().BeEmpty();
    }

    [Theory]
    [InlineData("Concluir minha parte", "Confirmar conclusão")]
    [InlineData("Aprovar", "Confirmar aprovação")]
    [InlineData("Excluir", "Confirmar exclusão")]
    public void Cancelar_fecha_a_confirmacao_e_nao_envia_nada(string acao, string rotulo)
    {
        var acoes = acao switch
        {
            "Concluir minha parte" => Acoes(concluir: true),
            "Aprovar" => Acoes(aprovar: true),
            _ => Acoes(editar: true, excluir: true),
        };
        Combinar(Detalhe(acoes: acoes));
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, acao);
        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be(rotulo);

        Clicar(ficha, NaConfirmacao, "Cancelar");

        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        ficha.FindAll(NaFaixa).Should().Contain(b => b.TextContent == acao);
        Despachante.Recebidas.OfType<ConcluirMinhaParte>().Should().BeEmpty();
        Despachante.Recebidas.OfType<AprovarAtividade>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ExcluirAtividade>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ObterAtividade>().Should().ContainSingle();
    }

    [Fact]
    public void Pedir_outra_confirmacao_troca_a_aberta_e_tira_o_recado()
    {
        Combinar(Detalhe(acoes: Acoes(editar: true, aprovar: true, excluir: true)));
        Services.GetRequiredService<Recados>().Deixar("Responsáveis salvos.");
        var ficha = Ficha();
        ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Responsáveis salvos.");

        Clicar(ficha, NaFaixa, "Aprovar");
        Clicar(ficha, NaFaixa, "Excluir");

        ficha.FindAll("[role=alertdialog]").Should().ContainSingle()
            .Which.GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        ficha.FindAll("p.aj-aviso--sucesso").Should().BeEmpty();
    }

    [Fact]
    public void Clique_duplo_em_concluir_minha_parte_envia_uma_vez()
    {
        var resposta = new TaskCompletionSource<Resultado>();
        Combinar(Detalhe(StatusAtividade.Direcionada, Acoes(concluir: true)), ator: AtorDaProducao);
        Despachante.RespondeDepois<ConcluirMinhaParte, Resultado>(resposta.Task);
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Concluir minha parte");

        Clicar(ficha, NaConfirmacao, "Concluir minha parte");
        ficha.FindAll(NaConfirmacao).Should().OnlyContain(b => b.HasAttribute("disabled"));
        ficha.FindAll(NaFaixa).Should().OnlyContain(b => b.HasAttribute("disabled"));
        Clicar(ficha, NaConfirmacao, "Concluir minha parte");
        Clicar(ficha, NaFaixa, "Concluir minha parte");
        resposta.SetResult(Resultado.Ok());

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Sua parte foi concluída."));
        Despachante.Recebidas.OfType<ConcluirMinhaParte>().Should().ContainSingle();
    }

    [Fact]
    public void Clique_duplo_em_aprovar_envia_uma_vez()
    {
        var resposta = new TaskCompletionSource<Resultado>();
        Combinar(Detalhe(acoes: Acoes(aprovar: true)));
        Despachante.RespondeDepois<AprovarAtividade, Resultado>(resposta.Task);
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Aprovar");

        Clicar(ficha, NaConfirmacao, "Aprovar");
        ficha.FindAll(NaConfirmacao).Should().OnlyContain(b => b.HasAttribute("disabled"));
        Clicar(ficha, NaConfirmacao, "Aprovar");
        Clicar(ficha, NaFaixa, "Aprovar");
        resposta.SetResult(Resultado.Ok());

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Atividade aprovada."));
        Despachante.Recebidas.OfType<AprovarAtividade>().Should().ContainSingle();
    }

    [Fact]
    public void Recusa_mostra_o_erro_e_traz_a_ficha_como_esta_agora()
    {
        var leituras = 0;
        Combinar();
        Despachante
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => Resultado<AtividadeDetalhe>.Ok(++leituras == 1
                ? Detalhe(acoes: Acoes(aprovar: true))
                : Detalhe(StatusAtividade.Concluida)))
            .Responde<AprovarAtividade, Resultado>(Resultado.Falha(TipoErro.Validacao,
                "A atividade só pode ser aprovada ou devolvida quando está feita ou corrigida."));
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Aprovar");

        Clicar(ficha, NaConfirmacao, "Aprovar");

        ficha.WaitForAssertion(() => ficha.Find("[role=alert]").TextContent.Should()
            .Contain("A atividade só pode ser aprovada ou devolvida quando está feita ou corrigida."));
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Minuta de acordo trabalhista");
        ficha.FindAll(".aj-painel__botoes--faixa").Should().BeEmpty();
    }

    [Fact]
    public void Exclusao_recusada_mostra_o_erro_recarrega_e_nao_sai_da_ficha()
    {
        Combinar(Detalhe(acoes: Acoes(editar: true, excluir: true)));
        Despachante.Responde<ExcluirAtividade, Resultado>(Resultado.Falha(TipoErro.Conflito,
            "A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => ficha.Find("[role=alert]").TextContent.Should()
            .Contain("A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        ficha.FindAll(NaFaixa).Should().OnlyContain(b => !b.HasAttribute("disabled"));
        Services.GetRequiredService<NavigationManager>().Uri.Should().NotEndWith("/atividades");
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Recarga_que_falha_depois_do_sucesso_avisa_que_a_ficha_nao_foi_atualizada()
    {
        var leituras = 0;
        Combinar();
        Despachante
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => ++leituras == 1
                ? Resultado<AtividadeDetalhe>.Ok(Detalhe(acoes: Acoes(aprovar: true)))
                : Resultado<AtividadeDetalhe>.Falha(TipoErro.Validacao, "Falha de teste."))
            .Responde<AprovarAtividade, Resultado>(Resultado.Ok());
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Aprovar");

        Clicar(ficha, NaConfirmacao, "Aprovar");

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Atividade aprovada, mas não foi possível atualizar a ficha."));
        // Dados antigos não ficam na tela: no lugar da ficha, o erro da consulta com "Tentar de novo".
        ficha.Markup.Should().Contain("Falha de teste.").And.Contain("Tentar de novo");
        ficha.FindAll(".aj-abas").Should().BeEmpty();
    }

    [Fact]
    public void Acesso_negado_numa_acao_encerra_a_tela()
    {
        Combinar(Detalhe(acoes: Acoes(aprovar: true)));
        Despachante.Responde<AprovarAtividade, Resultado>(Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado."));
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Aprovar");

        Clicar(ficha, NaConfirmacao, "Aprovar");

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente."));
        ficha.Markup.Should().NotContain("Minuta de acordo trabalhista").And.NotContain("Acesso negado.");
        Despachante.Recebidas.OfType<ObterAtividade>().Should().ContainSingle();
    }
}
