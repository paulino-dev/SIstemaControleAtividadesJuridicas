using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Web.Components.Documentos;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O que a Diretoria faz na tela de Documentos comuns, clicando: o envio com confirmação, o renomear e a exclusão,
/// uma coisa aberta por vez, e o foco no que abre. É a rede da mudança na lógica de documentos (fase 9D).
/// </summary>
public class DocumentosComunsInterativosTests : TelaInterativa
{
    private static readonly Guid Contrato = Guid.Parse("0199a3f2-7c1e-7000-8000-000000000001");
    private static readonly Guid Modelo = Guid.Parse("0199a3f2-7c1e-7000-8000-000000000002");

    private static Resultado<Pagina<DocumentoDoPortal>> Lista() => Resultado<Pagina<DocumentoDoPortal>>.Ok(new([
        new DocumentoDoPortal(Contrato, "Modelo de contrato.pdf", 96 * 1024, new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc), "Diana Diretora"),
        new DocumentoDoPortal(Modelo, "Modelo de procuração.docx", 40 * 1024, new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc), "Diana Diretora"),
    ], 2, 1, 20));

    private IRenderedComponent<DocumentosComunsDaDiretoria> Tela()
    {
        Despachante.Responde<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(Lista());
        return Desenhar<DocumentosComunsDaDiretoria>();
    }

    private static void Escolher(IRenderedComponent<DocumentosComunsDaDiretoria> tela, string nome = "Tabela de honorários.pdf") =>
        tela.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", nome));

    // O botão do cabeçalho do desktop (o flutuante do mobile chama o mesmo método).
    private static void AbrirEnvio(IRenderedComponent<DocumentosComunsDaDiretoria> tela) =>
        tela.Find("header.aj-cabecalho button.aj-botao--primario").Click();

    private static void ClicarNaLinha(IRenderedComponent<DocumentosComunsDaDiretoria> tela, string texto, int linha = 0) =>
        tela.FindAll("table.aj-tabela tbody tr")[linha].QuerySelectorAll("button").Single(b => b.TextContent == texto).Click();

    [Fact]
    public void Enviar_documento_abre_o_painel_e_leva_o_foco_ao_campo_do_arquivo()
    {
        var tela = Tela();

        AbrirEnvio(tela);

        tela.Find("section.aj-painel h2").TextContent.Should().Be("Enviar documento");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(tela.Find("input[type=file]"));
    }

    [Fact]
    public void Enviar_pede_a_confirmacao_e_esconde_o_painel_e_cancelar_o_traz_de_volta_com_o_arquivo()
    {
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);

        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar envio");
        tela.Find("[role=alertdialog] strong").TextContent.Should().Be("Tabela de honorários.pdf · 9 B");
        tela.Find("section.aj-painel").HasAttribute("hidden").Should().BeTrue();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        tela.Find("section.aj-painel").HasAttribute("hidden").Should().BeFalse();
        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Confirmar_envio_manda_com_a_visibilidade_confirmada_fecha_o_painel_e_avisa()
    {
        Despachante.Responde<EnviarDocumentoComum, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar envio").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento enviado."));
        Despachante.Recebidas.OfType<EnviarDocumentoComum>().Should().ContainSingle()
            .Which.Should().Match<EnviarDocumentoComum>(e => e.ConfirmouVisibilidade && e.NomeArquivo == "Tabela de honorários.pdf");
        tela.FindAll("section.aj-painel:not(.aj-painel--principal)").Should().BeEmpty();
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
    }

    [Fact]
    public void Uma_coisa_aberta_por_vez_entre_envio_renomear_e_exclusao()
    {
        var tela = Tela();

        AbrirEnvio(tela);
        ClicarNaLinha(tela, "Renomear");
        tela.FindAll("section.aj-painel h2").Select(h => h.TextContent).Should().Equal("Renomear \"Modelo de contrato.pdf\"");
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2)[1].Arguments[0]
            .ShouldBeElementReferenceTo(tela.Find("section.aj-painel input.aj-campo"));

        ClicarNaLinha(tela, "Excluir", linha: 1);
        tela.FindAll("section.aj-painel:not(.aj-painel--principal)").Should().BeEmpty();
        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        tela.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a exclusão de Modelo de procuração.docx?");

        AbrirEnvio(tela);
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        tela.Find("section.aj-painel h2").TextContent.Should().Be("Enviar documento");
    }

    [Fact]
    public void Cancelar_o_envio_e_reabrir_comeca_sem_o_arquivo()
    {
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);

        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Cancelar").Click();
        AbrirEnvio(tela);

        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Com_uma_exclusao_em_curso_nada_se_abre_e_as_acoes_das_linhas_esperam()
    {
        var exclusao = new TaskCompletionSource<Resultado>();
        Despachante.RespondeDepois<ExcluirDocumento, Resultado>(exclusao.Task);
        var tela = Tela();
        ClicarNaLinha(tela, "Excluir");

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar exclusão").Click();

        tela.FindAll("table.aj-tabela tbody button").Should().OnlyContain(b => b.HasAttribute("disabled"));
        AbrirEnvio(tela);
        tela.FindAll("section.aj-painel:not(.aj-painel--principal)").Should().BeEmpty();

        exclusao.SetResult(Resultado.Ok());
        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento excluído."));
        tela.FindAll("table.aj-tabela tbody button").Should().OnlyContain(b => !b.HasAttribute("disabled"));
    }

    [Fact]
    public void Salvar_nome_renomeia_fecha_o_painel_e_avisa()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Ok());
        var tela = Tela();
        ClicarNaLinha(tela, "Renomear");

        tela.Find("section.aj-painel input.aj-campo").Change("Contrato padrão.pdf");
        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Nome alterado."));
        Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle()
            .Which.Should().Be(new RenomearDocumento(Contrato, "Contrato padrão.pdf"));
        tela.FindAll("section.aj-painel:not(.aj-painel--principal)").Should().BeEmpty();
    }
}
