using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Atividades;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Os documentos da ficha da atividade, clicando: as linhas com o ícone do tipo e o link que baixa, as ações por visão
/// (botões no desktop, "⋯" no tablet e no mobile), o envio sem confirmação, renomear e excluir, uma coisa aberta por vez,
/// e o "não encontrado" do documento (recarrega) separado do da atividade (avisa a ficha).
/// </summary>
public class DocumentosDaAtividadeInterativosTests : TelaInterativa
{
    private static readonly Guid Atividade = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1");
    private static readonly Guid Minuta = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");
    private static readonly Guid Calculo = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b2");

    // 19:40 em UTC são 16:40 no horário do escritório. O cálculo foi renomeado sem a extensão: o ícone sai do arquivo
    // enviado, não do nome.
    private static readonly DocumentoResumo DaMinuta = new(Minuta, "minuta-acordo-v2.docx", "minuta-acordo-v2.docx", 84 * 1024,
        CategoriaDocumento.DocumentoAtividade, new DateTime(2026, 10, 7, 19, 40, 0, DateTimeKind.Utc), "Beatriz Lins",
        Atividade, "Minuta de acordo trabalhista");
    private static readonly DocumentoResumo DoCalculo = new(Calculo, "Cálculo das verbas", "calculo-verbas.xlsx", 31 * 1024,
        CategoriaDocumento.DocumentoAtividade, new DateTime(2026, 10, 8, 12, 10, 0, DateTimeKind.Utc), "Otávio Brandão",
        Atividade, "Minuta de acordo trabalhista");

    private int _total = 2;

    public DocumentosDaAtividadeInterativosTests() =>
        Despachante.Responde<ListarDocumentosDaAtividade, Resultado<Pagina<DocumentoResumo>>>(pedido =>
            Resultado<Pagina<DocumentoResumo>>.Ok(new Pagina<DocumentoResumo>([DaMinuta, DoCalculo], _total, pedido.Pagina, 20)));

    private IRenderedComponent<DocumentosDaAtividade> Painel(
        bool podeGerenciar = true, Action<int>? aoContar = null, Action? naoEncontrada = null) =>
        Desenhar<DocumentosDaAtividade>(p => p
            .Add(d => d.AtividadeId, Atividade)
            .Add(d => d.PodeGerenciar, podeGerenciar)
            .Add(d => d.AoContar, (int total) => aoContar?.Invoke(total))
            .Add(d => d.OnAtividadeNaoEncontrada, () => naoEncontrada?.Invoke()));

    private static void AbrirEnvio(IRenderedComponent<DocumentosDaAtividade> painel) =>
        painel.Find(".aj-painel__cabecalho button").Click();

    private static void Escolher(IRenderedComponent<DocumentosDaAtividade> painel, string nome) =>
        painel.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", nome));

    // Os botões da linha no desktop.
    private static void ClicarNaLinha(IRenderedComponent<DocumentosDaAtividade> painel, string texto, int linha = 0) =>
        painel.FindAll("div.aj-so-desktop li.aj-documento")[linha].QuerySelectorAll("button").Single(b => b.TextContent == texto).Click();

    // Os itens do "⋯" no tablet e no mobile.
    private static void ClicarNoMenu(IRenderedComponent<DocumentosDaAtividade> painel, string texto, int linha = 0) =>
        painel.FindAll("div.aj-tablet-mobile li.aj-documento")[linha].QuerySelectorAll(".aj-menu-acoes__item").Single(b => b.TextContent == texto).Click();

    private static IEnumerable<string> Paineis(IRenderedComponent<DocumentosDaAtividade> painel) =>
        painel.FindAll("section.aj-painel h2").Select(h => h.TextContent);

    [Fact]
    public void Linhas_trazem_o_icone_do_tipo_o_link_que_baixa_e_a_meta_de_cada_visao()
    {
        var painel = Painel();

        painel.Find("section").ClassName.Should().Be("aj-painel aj-painel--ficha aj-painel--documentos");
        painel.Find(".aj-painel__cabecalho h2").TextContent.Should().Be("Documentos");
        var enviar = painel.Find(".aj-painel__cabecalho button");
        enviar.ClassName.Should().Be("aj-botao aj-botao--secundario aj-botao--com-icone");
        enviar.TextContent.Should().Be("Enviar documento");

        var desktop = painel.FindAll("div.aj-so-desktop ul.aj-documentos > li.aj-documento");
        desktop.Should().HaveCount(2);
        desktop[0].QuerySelector("span.aj-documento__icone")!.ClassName.Should().Be("aj-documento__icone aj-documento__icone--texto");
        desktop[1].QuerySelector("span.aj-documento__icone")!.ClassName.Should().Be("aj-documento__icone aj-documento__icone--planilha");
        var link = desktop[0].QuerySelector("div.aj-documento__texto a.aj-documento__nome")!;
        link.TextContent.Should().Be("minuta-acordo-v2.docx");
        link.GetAttribute("href").Should().Be($"/documentos/{Minuta}/baixar");
        link.HasAttribute("download").Should().BeTrue();
        link.GetAttribute("data-enhance-nav").Should().Be("false");
        desktop[0].QuerySelector(".aj-documento__meta")!.TextContent.Should().Be("84 KB · Beatriz Lins · 07/10/2026 16:40");
        desktop[1].QuerySelector(".aj-documento__meta")!.TextContent.Should().Be("31 KB · Otávio Brandão · 08/10/2026 09:10");
        desktop[0].QuerySelectorAll(".aj-documento__acoes > button").Select(b => b.ClassName).Should()
            .Equal("aj-botao aj-botao--fantasma", "aj-botao aj-botao--perigo");
        desktop[0].QuerySelectorAll(".aj-documento__acoes > button").Select(b => b.TextContent).Should().Equal("Renomear", "Excluir");

        var linhas = painel.FindAll("div.aj-tablet-mobile ul.aj-documentos > li.aj-documento");
        linhas.Should().HaveCount(2);
        linhas[1].QuerySelector("span.aj-documento__icone")!.ClassName.Should().Be("aj-documento__icone aj-documento__icone--planilha");
        var linkDoTablet = linhas[1].QuerySelector("a.aj-documento__nome")!;
        linkDoTablet.TextContent.Should().Be("Cálculo das verbas");
        linkDoTablet.GetAttribute("href").Should().Be($"/documentos/{Calculo}/baixar");
        linkDoTablet.HasAttribute("download").Should().BeTrue();
        linkDoTablet.GetAttribute("data-enhance-nav").Should().Be("false");
        linhas[0].QuerySelector(".aj-documento__meta")!.TextContent.Should().Be("84 KB · Beatriz Lins");
        linhas[0].QuerySelector(".aj-menu-acoes > button")!.GetAttribute("aria-label").Should().Be("Ações de minuta-acordo-v2.docx");
        linhas[0].QuerySelectorAll(".aj-menu-acoes__item").Select(b => b.TextContent).Should().Equal("Renomear", "Excluir");
        linhas[0].QuerySelector(".aj-menu-acoes__item--perigo")!.TextContent.Should().Be("Excluir");
        linhas[0].QuerySelectorAll(".aj-documento__acoes").Should().BeEmpty();

        painel.FindAll(".aj-paginacao").Should().BeEmpty();   // uma página só
    }

    [Fact]
    public void Sem_gestao_envia_mas_nao_renomeia_nem_exclui()
    {
        var painel = Painel(podeGerenciar: false);

        painel.FindAll("div.aj-so-desktop li.aj-documento").Should().HaveCount(2);
        painel.FindAll("div.aj-tablet-mobile li.aj-documento").Should().HaveCount(2);
        painel.Find(".aj-painel__cabecalho button").TextContent.Should().Be("Enviar documento");
        painel.FindAll(".aj-documento__acoes").Should().BeEmpty();
        painel.FindAll(".aj-menu-acoes").Should().BeEmpty();
        painel.FindAll("button").Select(b => b.TextContent).Should().NotContain("Renomear").And.NotContain("Excluir");
    }

    [Fact]
    public void Enviar_abre_o_painel_manda_pela_atividade_sem_confirmacao_recarrega_e_avisa()
    {
        Despachante.Responde<EnviarDocumentoDaAtividade, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var painel = Painel();

        AbrirEnvio(painel);

        Paineis(painel).Should().Equal("Documentos", "Enviar documento");
        painel.Markup.Should().Contain("Arquivo (até 50 MB)").And.Contain("Nome (opcional)");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(painel.Find("input[type=file]"));
        painel.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeTrue();

        Escolher(painel, "minuta-final.docx");
        painel.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("Nome (opcional)")).QuerySelector("input")!.Change("Minuta final");
        painel.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Documento enviado."));
        Despachante.Recebidas.OfType<EnviarDocumentoDaAtividade>().Should().ContainSingle()
            .Which.Should().Match<EnviarDocumentoDaAtividade>(e =>
                e.AtividadeId == Atividade && e.Nome == "Minuta final" && e.NomeArquivo == "minuta-final.docx");
        Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Select(l => l.Pagina).Should().Equal(1, 1);
        Paineis(painel).Should().Equal("Documentos");
        painel.FindAll("[role=alertdialog]").Should().BeEmpty();   // o envio da atividade não pede confirmação
    }

    [Fact]
    public void Renomear_leva_o_foco_ao_nome_salva_fecha_o_painel_e_avisa()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Ok());
        var painel = Painel();

        ClicarNaLinha(painel, "Renomear");

        Paineis(painel).Should().Equal("Documentos", "Renomear \"minuta-acordo-v2.docx\"");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(painel.Find("section.aj-painel input.aj-campo"));

        painel.Find("section.aj-painel input.aj-campo").Change("Minuta assinada.docx");
        painel.FindAll("section.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Nome alterado."));
        Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle()
            .Which.Should().Be(new RenomearDocumento(Minuta, "Minuta assinada.docx"));
        Paineis(painel).Should().Equal("Documentos");
    }

    [Fact]
    public void Renomear_documento_que_sumiu_fecha_o_painel_e_recarrega_sem_avisar_a_ficha()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado."));
        var avisada = false;
        var painel = Painel(naoEncontrada: () => avisada = true);
        ClicarNaLinha(painel, "Renomear");

        painel.FindAll("section.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Documento não encontrado."));
        Paineis(painel).Should().Equal("Documentos");
        Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Should().HaveCount(2);
        painel.FindAll("div.aj-so-desktop li.aj-documento").Should().HaveCount(2);
        avisada.Should().BeFalse();
    }

    [Fact]
    public void Excluir_pede_confirmacao_no_aviso_e_so_entao_exclui()
    {
        Despachante.Responde<ExcluirDocumento, Resultado>(Resultado.Ok());
        var painel = Painel();

        ClicarNaLinha(painel, "Excluir");

        var confirmacao = painel.Find("[role=alertdialog]");
        confirmacao.GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        confirmacao.QuerySelector("strong")!.TextContent.Should().Be("Confirmar a exclusão de minuta-acordo-v2.docx?");
        confirmacao.QuerySelector(".aj-aviso__texto")!.TextContent.Should().Contain("O documento é excluído da atividade.");
        confirmacao.QuerySelectorAll("button").Select(b => b.TextContent).Should().Equal("Cancelar", "Confirmar exclusão");
        Despachante.Recebidas.OfType<ExcluirDocumento>().Should().BeEmpty();

        painel.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar exclusão").Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Documento excluído."));
        Despachante.Recebidas.OfType<ExcluirDocumento>().Should().ContainSingle().Which.Should().Be(new ExcluirDocumento(Minuta));
        painel.FindAll("[role=alertdialog]").Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Should().HaveCount(2);
    }

    [Fact]
    public void Excluir_documento_que_sumiu_recarrega_a_lista_sem_avisar_a_ficha()
    {
        Despachante.Responde<ExcluirDocumento, Resultado>(Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado."));
        var avisada = false;
        var painel = Painel(naoEncontrada: () => avisada = true);
        ClicarNoMenu(painel, "Excluir", linha: 1);

        painel.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar exclusão").Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Documento não encontrado."));
        Despachante.Recebidas.OfType<ExcluirDocumento>().Should().ContainSingle().Which.Should().Be(new ExcluirDocumento(Calculo));
        Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Should().HaveCount(2);
        painel.FindAll("div.aj-so-desktop li.aj-documento").Should().HaveCount(2);
        avisada.Should().BeFalse();
    }

    [Fact]
    public void Uma_coisa_aberta_por_vez_entre_envio_renomear_e_exclusao()
    {
        var painel = Painel();

        AbrirEnvio(painel);
        Escolher(painel, "rascunho.docx");
        ClicarNoMenu(painel, "Renomear", linha: 1);

        Paineis(painel).Should().Equal("Documentos", "Renomear \"Cálculo das verbas\"");
        painel.FindAll("input[type=file]").Should().BeEmpty();

        ClicarNaLinha(painel, "Excluir");

        Paineis(painel).Should().Equal("Documentos");
        painel.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a exclusão de minuta-acordo-v2.docx?");

        AbrirEnvio(painel);

        painel.FindAll("[role=alertdialog]").Should().BeEmpty();
        Paineis(painel).Should().Equal("Documentos", "Enviar documento");
        // O arquivo escolhido antes não volta: o envio reaberto começa do zero.
        painel.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void AoContar_avisa_o_total_e_a_paginacao_aparece_com_mais_de_uma_pagina()
    {
        _total = 25;
        var contagens = new List<int>();

        var painel = Painel(aoContar: contagens.Add);

        contagens.Should().Equal(25);
        painel.Find(".aj-paginacao .aj-subtitulo").TextContent.Should().Be("Página 1 de 2 · 25 documentos");

        painel.FindAll(".aj-paginacao button").Single(b => b.TextContent == "Próxima").Click();

        painel.WaitForAssertion(() => Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Select(l => l.Pagina)
            .Should().Equal(1, 2));
        contagens.Should().Equal(25, 25);
    }

    [Fact]
    public void Atividade_que_sumiu_avisa_a_ficha_e_troca_o_painel_pela_mensagem()
    {
        Despachante.Responde<ListarDocumentosDaAtividade, Resultado<Pagina<DocumentoResumo>>>(
            Resultado<Pagina<DocumentoResumo>>.Falha(TipoErro.NaoEncontrado, "Atividade não encontrada."));
        var avisada = false;
        var contagens = new List<int>();

        var painel = Painel(aoContar: contagens.Add, naoEncontrada: () => avisada = true);

        avisada.Should().BeTrue();
        painel.Find(".aj-painel__cabecalho h2").TextContent.Should().Be("Documentos");
        painel.Find("section .aj-estado-lista").TextContent.Should().Be("Atividade não encontrada.");
        painel.FindAll(".aj-painel__cabecalho button").Should().BeEmpty();
        painel.FindAll("li.aj-documento").Should().BeEmpty();
        painel.Markup.Should().NotContain("Tentar de novo").And.NotContain("Nenhum documento.");
        contagens.Should().BeEmpty();
    }
}
