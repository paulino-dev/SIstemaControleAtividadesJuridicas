using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Documentos;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;

namespace AtividadesJuridicas.IntegrationTests;

public class DocumentosDaFichaInterativosTests : TelaInterativa
{
    private static readonly Guid Maria = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a1");
    private static readonly Guid Procuracao = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");

    private static readonly Dictionary<CategoriaDocumento, int> Contagem = new()
    {
        [CategoriaDocumento.DocumentoGeral] = 3, [CategoriaDocumento.ContratoHonorarios] = 1,
        [CategoriaDocumento.DocumentoAtividade] = 4, [CategoriaDocumento.DocumentoDoCliente] = 2,
    };

    private static Resultado<PaginaDeDocumentos> Pagina(ListarDocumentosDoCliente pedido) => Resultado<PaginaDeDocumentos>.Ok(new([
        new DocumentoResumo(Procuracao, "Procuração.pdf", "procuracao.pdf", 412 * 1024, pedido.Categoria,
            new DateTime(2026, 9, 2, 13, 14, 0, DateTimeKind.Utc), "Marina Couto", null,
            pedido.Categoria == CategoriaDocumento.DocumentoAtividade ? "Contestação — cobrança de ISS" : null),
    ], 1, 1, 20, Contagem));

    private IRenderedComponent<DocumentosDaFicha> Ficha(bool podeGerenciar = true, Action<int>? aoContar = null)
    {
        Despachante.Responde<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(Pagina);
        return Desenhar<DocumentosDaFicha>(p => p
            .Add(d => d.ClienteId, Maria).Add(d => d.PodeGerenciar, podeGerenciar)
            .Add(d => d.AoContar, (int total) => aoContar?.Invoke(total)));
    }

    private static void Chip(IRenderedComponent<DocumentosDaFicha> ficha, string comeco) =>
        ficha.FindAll(".aj-chips button").Single(b => b.TextContent.StartsWith(comeco)).Click();

    [Fact]
    public void Trocar_de_chip_le_a_area_nova_e_conta_a_soma()
    {
        var total = 0;
        var ficha = Ficha(aoContar: t => total = t);

        Chip(ficha, "De atividades");

        Despachante.Recebidas.OfType<ListarDocumentosDoCliente>().Select(r => r.Categoria).Should()
            .Equal(CategoriaDocumento.DocumentoGeral, CategoriaDocumento.DocumentoAtividade);
        ficha.Find(".aj-chips button[aria-pressed=true]").TextContent.Should().Be("De atividades · 4");
        BlocoDeVisao.De(ficha.Markup, "aj-so-desktop").Should().Contain("<th scope=\"col\">Atividade</th>")
            .And.Contain("Contestação — cobrança de ISS");
        ficha.FindAll(".aj-painel__cabecalho button").Should().BeEmpty();   // De atividades não tem envio
        // Com gestão a linha renomeia e exclui, mas não tem editor de acesso (só os Gerais têm).
        var botoes = ficha.FindAll("table.aj-tabela tbody button").Select(b => b.TextContent).ToList();
        botoes.Should().Contain("Renomear").And.Contain("Excluir").And.NotContain("Acesso");
        var menu = ficha.FindAll(".aj-menu-acoes__item").Select(b => b.TextContent).ToList();
        menu.Should().Contain("Renomear").And.Contain("Excluir").And.NotContain("Acesso");
        total.Should().Be(10);
    }

    [Fact]
    public void Trocar_de_chip_com_o_envio_aberto_descarta_o_arquivo_e_o_envio_vai_para_a_area_do_painel()
    {
        Despachante.Responde<EnviarDocumento, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var ficha = Ficha();
        ficha.Find(".aj-painel__cabecalho button").Click();
        ficha.Markup.Should().Contain("Arquivo (até 50 MB)").And.Contain("Nome (opcional)");
        ficha.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", "contrato.pdf"));
        ficha.Markup.Should().Contain("Vai para Gerais.");

        Chip(ficha, "Do cliente");

        ficha.FindAll("input[type=file]").Should().BeEmpty();
        // Documento do cliente não pede confirmação de envio nem tem editor de acesso.
        ficha.Markup.Should().NotContain("Confirmar envio");
        ficha.FindAll("button").Should().NotContain(b => b.TextContent == "Acesso");
        ficha.Find(".aj-painel__cabecalho button").Click();
        ficha.Markup.Should().Contain("Vai para Do cliente.");
        ficha.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeTrue();

        ficha.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", "comprovante.pdf"));
        ficha.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        ficha.WaitForAssertion(() => Despachante.Recebidas.OfType<EnviarDocumento>().Should().ContainSingle()
            .Which.Should().Match<EnviarDocumento>(e => e.Categoria == CategoriaDocumento.DocumentoDoCliente && e.NomeArquivo == "comprovante.pdf"));
    }

    [Fact]
    public void Acesso_fecha_o_envio_e_a_exclusao_e_leva_o_foco_a_primeira_opcao()
    {
        Despachante
            .Responde<ObterAcessoDoDocumento, Resultado<AcessoDoDocumento>>(Resultado<AcessoDoDocumento>.Ok(
                new AcessoDoDocumento(Procuracao, "Procuração.pdf", CategoriaDocumento.DocumentoGeral, [], [])))
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(
                Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([
                    new(Guid.NewGuid(), "Produção", TipoPerfil.Producao, true, 2)]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
                Resultado<IReadOnlyList<UsuarioResumo>>.Ok([]));
        var ficha = Ficha();
        ficha.Find(".aj-painel__cabecalho button").Click();

        ficha.FindAll("table.aj-tabela tbody button").Single(b => b.TextContent == "Acesso").Click();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Quem acessa \"Procuração.pdf\""));
        ficha.FindAll("input[type=file]").Should().BeEmpty();
        ficha.Find("div.aj-opcoes label.aj-opcao").TextContent.Should().Contain("Produção");
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2)[1].Arguments[0]
            .ShouldBeElementReferenceTo(ficha.Find("div.aj-opcoes input.aj-opcao__caixa"));
    }

    [Fact]
    public void Producao_so_pede_as_areas_que_le()
    {
        var ficha = Ficha(podeGerenciar: false);

        Chip(ficha, "De atividades");
        Chip(ficha, "Gerais");

        Despachante.Recebidas.OfType<ListarDocumentosDoCliente>().Select(r => r.Categoria).Should()
            .OnlyContain(c => c == CategoriaDocumento.DocumentoGeral || c == CategoriaDocumento.DocumentoAtividade);
        ficha.FindAll(".aj-chips button").Select(b => b.TextContent).Should().Equal("Gerais · 3", "De atividades · 4");
    }

    private void CombinarAcesso() => Despachante
        .Responde<ObterAcessoDoDocumento, Resultado<AcessoDoDocumento>>(Resultado<AcessoDoDocumento>.Ok(
            new AcessoDoDocumento(Procuracao, "Procuração.pdf", CategoriaDocumento.DocumentoGeral, [], [])))
        .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(
            Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([new(Guid.NewGuid(), "Produção", TipoPerfil.Producao, true, 2)]))
        .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([]))
        .Responde<DefinirAcessoDoDocumento, Resultado>(Resultado.Ok());

    [Fact]
    public void Os_botoes_que_abrem_tem_o_id_que_o_foco_de_volta_procura()
    {
        var ficha = Ficha();

        ficha.Find("#documentos-enviar").TextContent.Should().Contain("Enviar documento");
        ficha.Find($"#documento-acesso-{Procuracao:N}").TextContent.Should().Be("Acesso");
        ficha.Find($"#documento-renomear-{Procuracao:N}").TextContent.Should().Be("Renomear");
        ficha.Find($"#documento-excluir-{Procuracao:N}").TextContent.Should().Be("Excluir");
        ficha.Find($"#documento-{Procuracao:N} .aj-menu-acoes > .aj-botao").GetAttribute("aria-label")
            .Should().Be("Ações de Procuração.pdf");
    }

    [Fact]
    public void Cancelar_o_envio_devolve_o_foco_ao_enviar_documento_do_painel()
    {
        var ficha = Ficha();
        ficha.Find("#documentos-enviar").Click();

        ficha.FindAll("form.aj-painel button").Single(b => b.TextContent == "Cancelar").Click();

        ficha.FindAll("form.aj-painel").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be("#documentos-enviar");
    }

    [Fact]
    public void Cancelar_o_acesso_devolve_o_foco_ao_acesso_da_linha()
    {
        CombinarAcesso();
        var ficha = Ficha();
        ficha.FindAll("table.aj-tabela tbody button").Single(b => b.TextContent == "Acesso").Click();
        ficha.WaitForAssertion(() => ficha.FindAll("form#documentos-acesso").Should().ContainSingle());

        ficha.FindAll("form#documentos-acesso button").Single(b => b.TextContent == "Cancelar").Click();

        ficha.FindAll("form#documentos-acesso").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be($"#documento-acesso-{Procuracao:N}, #documento-{Procuracao:N} .aj-menu-acoes > .aj-botao");
    }

    [Fact]
    public void Salvar_o_acesso_pelo_Enter_salva_e_leva_o_foco_ao_aviso_de_resultado()
    {
        CombinarAcesso();
        var ficha = Ficha();
        ficha.FindAll("table.aj-tabela tbody button").Single(b => b.TextContent == "Acesso").Click();
        ficha.WaitForAssertion(() => ficha.FindAll("form#documentos-acesso").Should().ContainSingle());
        ficha.Find("form#documentos-acesso").QuerySelectorAll("button").Select(b => (b.TextContent, b.GetAttribute("type")))
            .Should().Equal(("Cancelar", "button"), ("Salvar acesso", "submit"));

        ficha.Find("form#documentos-acesso").Submit();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Acesso atualizado."));
        Despachante.Recebidas.OfType<DefinirAcessoDoDocumento>().Should().ContainSingle();
        var aviso = ficha.FindAll(".aj-aviso").Single(a => a.TextContent.Contains("Acesso atualizado."));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Envio_e_renomear_sao_formularios_que_o_Enter_envia()
    {
        Despachante
            .Responde<EnviarDocumento, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()))
            .Responde<RenomearDocumento, Resultado>(Resultado.Ok());
        var ficha = Ficha();
        ficha.Find("#documentos-enviar").Click();
        ficha.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", "contrato.pdf"));

        ficha.Find("form#documentos-envio").Submit();

        ficha.WaitForAssertion(() => Despachante.Recebidas.OfType<EnviarDocumento>().Should().ContainSingle());
        ficha.FindAll("table.aj-tabela tbody button").Single(b => b.TextContent == "Renomear").Click();
        ficha.Find("form#documentos-renomear input.aj-campo").Change("Procuração assinada.pdf");

        ficha.Find("form#documentos-renomear").Submit();

        ficha.WaitForAssertion(() => Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle()
            .Which.Should().Be(new RenomearDocumento(Procuracao, "Procuração assinada.pdf")));
    }

    [Fact]
    public async Task Resposta_de_uma_area_que_ja_ficou_para_tras_e_descartada()
    {
        var ficha = Ficha();   // Gerais, respondida na hora
        var velha = new TaskCompletionSource<Resultado<PaginaDeDocumentos>>();
        Despachante.RespondeDepois<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(velha.Task);
        Chip(ficha, "De atividades");   // a consulta fica esperando
        Despachante.Responde<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(Pagina);
        Chip(ficha, "Do cliente");      // responde na hora

        velha.SetResult(Resultado<PaginaDeDocumentos>.Ok(new([
            new DocumentoResumo(Guid.NewGuid(), "Contestação.pdf", "contestacao.pdf", 1024, CategoriaDocumento.DocumentoAtividade,
                new DateTime(2026, 9, 3, 13, 0, 0, DateTimeKind.Utc), "Marina Couto", null, "Contestação — cobrança de ISS"),
        ], 1, 1, 20, Contagem)));
        // A continuação da resposta velha entra na fila do renderizador antes desta chamada vazia: quando ela volta,
        // a resposta velha já foi tratada (as esperas seguintes da cadeia continuam no mesmo contexto, sem voltar à fila).
        await ficha.InvokeAsync(() => { });

        ficha.Find(".aj-chips button[aria-pressed=true]").TextContent.Should().Be("Do cliente · 2");
        ficha.Markup.Should().Contain("Procuração.pdf").And.NotContain("Contestação.pdf");
    }

    [Fact]
    public void Tentar_de_novo_mostra_carregando_ate_a_resposta()
    {
        Despachante.Responde<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(
            Resultado<PaginaDeDocumentos>.Falha(TipoErro.Validacao, "Falha de teste."));
        var ficha = Desenhar<DocumentosDaFicha>(p => p.Add(d => d.ClienteId, Maria).Add(d => d.PodeGerenciar, true));
        var nova = new TaskCompletionSource<Resultado<PaginaDeDocumentos>>();
        Despachante.RespondeDepois<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(nova.Task);

        ficha.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        ficha.Markup.Should().Contain("Carregando…").And.NotContain("Falha de teste.");
        nova.SetResult(Pagina(new ListarDocumentosDoCliente(Maria, CategoriaDocumento.DocumentoGeral)));
        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Procuração.pdf"));
        ficha.Markup.Should().NotContain("Carregando…");
    }
}
