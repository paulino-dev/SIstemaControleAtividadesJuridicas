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
}
