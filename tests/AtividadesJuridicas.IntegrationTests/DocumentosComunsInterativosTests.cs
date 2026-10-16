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

        tela.Find("form.aj-painel h2").TextContent.Should().Be("Enviar documento");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(tela.Find("input[type=file]"));
    }

    [Fact]
    public void Enviar_pede_a_confirmacao_e_esconde_o_painel_e_cancelar_o_traz_de_volta_com_o_arquivo()
    {
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar envio");
        tela.Find("[role=alertdialog] strong").TextContent.Should().Be("Tabela de honorários.pdf · 9 B");
        tela.Find("form.aj-painel").HasAttribute("hidden").Should().BeTrue();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        tela.Find("form.aj-painel").HasAttribute("hidden").Should().BeFalse();
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Confirmar_envio_manda_com_a_visibilidade_confirmada_fecha_o_painel_e_avisa()
    {
        Despachante.Responde<EnviarDocumentoComum, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar envio").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento enviado."));
        Despachante.Recebidas.OfType<EnviarDocumentoComum>().Should().ContainSingle()
            .Which.Should().Match<EnviarDocumentoComum>(e => e.ConfirmouVisibilidade && e.NomeArquivo == "Tabela de honorários.pdf");
        tela.FindAll("form.aj-painel").Should().BeEmpty();
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
    }

    [Fact]
    public void Uma_coisa_aberta_por_vez_entre_envio_renomear_e_exclusao()
    {
        var tela = Tela();

        AbrirEnvio(tela);
        ClicarNaLinha(tela, "Renomear");
        tela.FindAll("form.aj-painel h2").Select(h => h.TextContent).Should().Equal("Renomear \"Modelo de contrato.pdf\"");
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2)[1].Arguments[0]
            .ShouldBeElementReferenceTo(tela.Find("form.aj-painel input.aj-campo"));

        ClicarNaLinha(tela, "Excluir", linha: 1);
        tela.FindAll("form.aj-painel").Should().BeEmpty();
        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        tela.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a exclusão de Modelo de procuração.docx?");

        AbrirEnvio(tela);
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        tela.Find("form.aj-painel h2").TextContent.Should().Be("Enviar documento");
    }

    [Fact]
    public void Cancelar_o_envio_e_reabrir_comeca_sem_o_arquivo()
    {
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Cancelar").Click();
        AbrirEnvio(tela);

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeTrue();
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
        tela.FindAll("form.aj-painel").Should().BeEmpty();

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

        tela.Find("form.aj-painel input.aj-campo").Change("Contrato padrão.pdf");
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Nome alterado."));
        Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle()
            .Which.Should().Be(new RenomearDocumento(Contrato, "Contrato padrão.pdf"));
        tela.FindAll("form.aj-painel").Should().BeEmpty();
    }

    [Fact]
    public void Renomear_documento_que_sumiu_fecha_o_painel_rele_a_lista_e_mantem_a_mensagem()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado."));
        var tela = Tela();
        ClicarNaLinha(tela, "Renomear");

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento não encontrado."));
        tela.FindAll("form.aj-painel").Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarDocumentosComuns>().Should().HaveCount(2);
        tela.FindAll("table.aj-tabela tbody tr").Should().HaveCount(2);
    }

    [Fact]
    public void Excluir_documento_que_sumiu_rele_a_lista_e_mantem_a_mensagem()
    {
        Despachante.Responde<ExcluirDocumento, Resultado>(Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado."));
        var tela = Tela();
        ClicarNaLinha(tela, "Excluir");

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar exclusão").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento não encontrado."));
        Despachante.Recebidas.OfType<ListarDocumentosComuns>().Should().HaveCount(2);
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        tela.FindAll("table.aj-tabela tbody tr").Should().HaveCount(2);
    }

    // O "Enviar documento" desta tela: o do cabeçalho (desktop e tablet) ou o flutuante (mobile).
    private const string EnviarDocumento = ".aj-cabecalho > .aj-botao--primario, .aj-fab[aria-label=\"Enviar documento\"]";

    // O botão de uma ação da linha que abriu o que foi cancelado: o da ação no desktop ou o "⋯" da linha no tablet e no mobile.
    private static string DesktopOuMenu(string acao, Guid documento) =>
        $"#documento-{acao}-{documento:N}, #documento-{documento:N} .aj-menu-acoes > .aj-botao";

    private static AngleSharp.Dom.IElement AvisoCom(IRenderedComponent<DocumentosComunsDaDiretoria> tela, string texto) =>
        tela.FindAll(".aj-aviso").Single(a => a.TextContent.Contains(texto));

    [Fact]
    public void Os_botoes_que_abrem_tem_o_id_que_o_foco_de_volta_procura()
    {
        var tela = Tela();

        tela.Find($"#documento-renomear-{Contrato:N}").TextContent.Should().Be("Renomear");
        tela.Find($"#documento-excluir-{Contrato:N}").TextContent.Should().Be("Excluir");
        tela.Find($"#documento-{Contrato:N} .aj-menu-acoes > .aj-botao").GetAttribute("aria-label")
            .Should().Be("Ações de Modelo de contrato.pdf");
        tela.Find(".aj-cabecalho > .aj-botao--primario").TextContent.Should().Contain("Enviar documento");
    }

    [Fact]
    public void Cancelar_o_painel_de_envio_devolve_o_foco_ao_enviar_documento()
    {
        var tela = Tela();
        AbrirEnvio(tela);

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("form.aj-painel").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be(EnviarDocumento);
    }

    [Fact]
    public void Cancelar_a_confirmacao_de_envio_devolve_o_foco_ao_enviar_do_painel()
    {
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        tela.Find("form#documentos-envio").HasAttribute("hidden").Should().BeFalse();
        tela.Find("#documentos-envio .aj-botao--primario").TextContent.Should().Be("Enviar");
        UltimoSeletorFocado().Should().Be("#documentos-envio .aj-botao--primario");
    }

    [Fact]
    public void Cancelar_o_renomear_devolve_o_foco_ao_renomear_da_linha()
    {
        var tela = Tela();
        ClicarNaLinha(tela, "Renomear");

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("form.aj-painel").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be(DesktopOuMenu("renomear", Contrato));
    }

    [Fact]
    public void Cancelar_a_exclusao_devolve_o_foco_ao_excluir_da_linha()
    {
        var tela = Tela();
        ClicarNaLinha(tela, "Excluir", linha: 1);

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be(DesktopOuMenu("excluir", Modelo));
    }

    [Fact]
    public void Salvar_nome_leva_o_foco_ao_aviso_de_resultado()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Ok());
        var tela = Tela();
        ClicarNaLinha(tela, "Renomear");

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Nome alterado."));
        var aviso = AvisoCom(tela, "Nome alterado.");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Confirmar_a_exclusao_leva_o_foco_ao_aviso_de_resultado()
    {
        Despachante.Responde<ExcluirDocumento, Resultado>(Resultado.Ok());
        var tela = Tela();
        ClicarNaLinha(tela, "Excluir");

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar exclusão").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento excluído."));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(AvisoCom(tela, "Documento excluído."));
    }

    [Fact]
    public void Confirmar_o_envio_leva_o_foco_ao_aviso_de_resultado()
    {
        Despachante.Responde<EnviarDocumentoComum, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar envio").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento enviado."));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(AvisoCom(tela, "Documento enviado."));
    }

    [Fact]
    public void Dois_avisos_seguidos_iguais_revelam_cada_um()
    {
        // O arquivo grande demais é recusado na hora, sem consulta: a mesma frase duas vezes, sem desenho no meio.
        var tela = Tela();
        AbrirEnvio(tela);
        tela.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[51 * 1024 * 1024], "grande.pdf"));

        tela.Find("form#documentos-envio").Submit();
        var antes = FocosDeElementoPedidos;
        tela.Find("form#documentos-envio").Submit();

        AvisoCom(tela, "O arquivo passa do limite de 50 MB.").GetAttribute("tabindex").Should().Be("-1");
        FocosDeElementoPedidos.Should().Be(antes + 1);
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(AvisoCom(tela, "O arquivo passa do limite de 50 MB."));
    }

    [Fact]
    public void Com_a_confirmacao_a_vista_enviar_documento_leva_o_foco_a_ela()
    {
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();
        var antes = SeletoresFocados.Count;
        var focosAntes = FocosDeElementoPedidos;

        AbrirEnvio(tela);

        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar envio");
        tela.Find("form#documentos-envio").HasAttribute("hidden").Should().BeTrue();
        SeletoresFocados.Count.Should().Be(antes + 1);
        UltimoSeletorFocado().Should().Be("[role=\"alertdialog\"][aria-label=\"Confirmar envio\"]");
        // O campo de arquivo, escondido com o painel, não recebe mais o foco.
        FocosDeElementoPedidos.Should().Be(focosAntes);
    }

    [Fact]
    public void Confirmar_envio_leva_o_foco_a_confirmacao_enquanto_o_envio_dura()
    {
        // O "Confirmar envio" se desabilita com o clique: o foco não pode ficar nele, e vai à confirmação, que continua.
        var envio = new TaskCompletionSource<Resultado<Guid>>();
        Despachante.RespondeDepois<EnviarDocumentoComum, Resultado<Guid>>(envio.Task);
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();
        var antes = SeletoresFocados.Count;

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar envio").Click();

        tela.Find("[role=alertdialog]").QuerySelectorAll("button").Should().HaveCount(2).And.OnlyContain(b => b.HasAttribute("disabled"));
        SeletoresFocados.Count.Should().Be(antes + 1);
        UltimoSeletorFocado().Should().Be("[role=\"alertdialog\"][aria-label=\"Confirmar envio\"]");

        envio.SetResult(Resultado<Guid>.Ok(Guid.NewGuid()));
        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento enviado."));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(AvisoCom(tela, "Documento enviado."));
    }

    [Fact]
    public void Salvar_nome_duas_vezes_seguidas_renomeia_uma_vez_so()
    {
        var renomeio = new TaskCompletionSource<Resultado>();
        Despachante.RespondeDepois<RenomearDocumento, Resultado>(renomeio.Task);
        var tela = Tela();
        ClicarNaLinha(tela, "Renomear");

        tela.Find("form#documentos-renomear").Submit();
        tela.Find("form#documentos-renomear").Submit();   // o segundo Enter, com a primeira gravação em curso

        Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle();
        tela.FindAll("form#documentos-renomear button").Single(b => b.TextContent == "Salvar nome")
            .HasAttribute("disabled").Should().BeTrue();

        renomeio.SetResult(Resultado.Ok());
        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Nome alterado."));
        Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle();
    }

    [Fact]
    public void Tentar_de_novo_mostra_carregando_ate_a_resposta()
    {
        Despachante.Responde<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(
            Resultado<Pagina<DocumentoDoPortal>>.Falha(TipoErro.Validacao, "Falha de teste."));
        var tela = Desenhar<DocumentosComunsDaDiretoria>();
        var nova = new TaskCompletionSource<Resultado<Pagina<DocumentoDoPortal>>>();
        Despachante.RespondeDepois<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(nova.Task);

        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.Markup.Should().Contain("Carregando…").And.NotContain("Falha de teste.");
        tela.FindAll("button").Should().NotContain(b => b.TextContent == "Tentar de novo");

        nova.SetResult(Lista());
        tela.WaitForAssertion(() => tela.FindAll("table.aj-tabela tbody tr").Should().HaveCount(2));
        tela.Markup.Should().NotContain("Carregando…");
        Despachante.Recebidas.OfType<ListarDocumentosComuns>().Should().HaveCount(2);
    }

    [Fact]
    public void Cada_painel_e_um_formulario_que_o_Enter_envia()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Ok());
        var tela = Tela();
        AbrirEnvio(tela);
        tela.Find("form#documentos-envio").QuerySelectorAll("button").Select(b => (b.TextContent, b.GetAttribute("type")))
            .Should().Equal(("Cancelar", "button"), ("Enviar", "submit"));
        Escolher(tela);

        tela.Find("form#documentos-envio").Submit();   // o Enter no campo do nome

        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar envio");
        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        ClicarNaLinha(tela, "Renomear");
        tela.Find("form#documentos-renomear").QuerySelectorAll("button").Select(b => (b.TextContent, b.GetAttribute("type")))
            .Should().Equal(("Cancelar", "button"), ("Salvar nome", "submit"));
        tela.Find("form#documentos-renomear input.aj-campo").Change("Contrato padrão.pdf");

        tela.Find("form#documentos-renomear").Submit();   // o Enter no campo do novo nome

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle()
            .Which.Should().Be(new RenomearDocumento(Contrato, "Contrato padrão.pdf")));
    }

    [Fact]
    public void Durante_o_envio_confirmado_a_confirmacao_fica_a_vista_e_o_campo_de_arquivo_desabilitado()
    {
        var envio = new TaskCompletionSource<Resultado<Guid>>();
        Despachante.RespondeDepois<EnviarDocumentoComum, Resultado<Guid>>(envio.Task);
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar envio").Click();

        var confirmacao = tela.Find("[role=alertdialog]");
        confirmacao.GetAttribute("aria-label").Should().Be("Confirmar envio");
        confirmacao.QuerySelectorAll("button").Should().HaveCount(2).And.OnlyContain(b => b.HasAttribute("disabled"));
        tela.Find("form#documentos-envio").HasAttribute("hidden").Should().BeTrue();
        tela.Find("input[type=file]").HasAttribute("disabled").Should().BeTrue();

        envio.SetResult(Resultado<Guid>.Ok(Guid.NewGuid()));
        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento enviado."));
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
    }

    [Fact]
    public void Acesso_encerrado_depois_de_salvar_o_nome_nao_deixa_aviso_ao_lado()
    {
        var encerrado = false;
        Despachante.Responde<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(Lista());
        var tela = Desenhar<DocumentosComunsDaDiretoria>(p => p.Add(d => d.OnAcessoEncerrado, () => encerrado = true));
        // O nome é salvo, e a recarga da lista que vem depois responde acesso negado.
        Despachante
            .Responde<RenomearDocumento, Resultado>(Resultado.Ok())
            .Responde<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(
                Resultado<Pagina<DocumentoDoPortal>>.Falha(TipoErro.AcessoNegado, "Acesso negado."));
        ClicarNaLinha(tela, "Renomear");

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        tela.WaitForAssertion(() => encerrado.Should().BeTrue());
        tela.Markup.Should().Contain("Nenhum documento.").And.NotContain("Nome alterado.");
        tela.FindAll(".aj-aviso").Should().BeEmpty();
    }
}
