using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class ClienteFichaInterativaTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1");

    private void Combinar(bool diretoria = true, bool ativo = true, int usuariosAtivos = 2)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretoria ? AtorDaDiretoria : AtorDaProducao)
            .Responde<ObterCliente, Resultado<ClienteDetalhe>>(Resultado<ClienteDetalhe>.Ok(new(Id, TipoCliente.PessoaJuridica,
                "Transportes Litoral Ltda.", null, null, null, null, null, null, null, null, null, null, ativo,
                null, null, null, "11.222.333/0001-81", null, null, null, null)))
            .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(Resultado<PaginaDeProcessos>.Ok(new([], 0, 1, 20, 0)))
            .Responde<ListarUsuariosDoCliente, Resultado<IReadOnlyList<UsuarioDoCliente>>>(Resultado<IReadOnlyList<UsuarioDoCliente>>.Ok(
                [.. Enumerable.Range(1, usuariosAtivos).Select(i => new UsuarioDoCliente(Guid.NewGuid(), $"Usuário {i}", $"u{i}@x.com", true, null))]))
            .Responde<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(Resultado<PaginaDeDocumentos>.Ok(new([], 0, 1, 20,
                new Dictionary<CategoriaDocumento, int> { [CategoriaDocumento.DocumentoGeral] = 3, [CategoriaDocumento.DocumentoAtividade] = 4 })));
    }

    private IRenderedComponent<ClienteFicha> Ficha() => Desenhar<ClienteFicha>(p => p.Add(f => f.Id, Id));

    [Fact]
    public void Inativar_pede_a_confirmacao_com_os_usuarios_e_confirmar_deixa_a_ficha_inativa()
    {
        Combinar(usuariosAtivos: 2);
        Despachante.Responde<DefinirAtivoCliente, Resultado>(Resultado.Ok());
        var ficha = Ficha();

        ficha.FindAll(".aj-painel__botoes button").Single(b => b.TextContent == "Inativar").Click();

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar inativação");
        ficha.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a inativação de Transportes Litoral Ltda.?");
        ficha.Find("[role=alertdialog]").TextContent.Should().Contain("2 usuários do portal ficam sem acesso até o cliente ser reativado.");

        ficha.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar inativação").Click();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Cliente inativado. 2 usuários do portal ficaram sem acesso."));
        Despachante.Recebidas.OfType<DefinirAtivoCliente>().Should().ContainSingle().Which.Should().Be(new DefinirAtivoCliente(Id, false));
        ficha.Markup.Should().Contain("· Inativo</p>").And.Contain(">Ativar</button>")
            .And.Contain("Cliente inativo: estes usuários estão sem acesso ao portal até o cliente ser reativado.");
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
    }

    [Fact]
    public void Cancelar_a_inativacao_nao_muda_nada()
    {
        Combinar();
        var ficha = Ficha();
        ficha.FindAll(".aj-painel__botoes button").Single(b => b.TextContent == "Inativar").Click();

        ficha.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        Despachante.Recebidas.OfType<DefinirAtivoCliente>().Should().BeEmpty();
    }

    [Fact]
    public void Tocar_numa_aba_mostra_os_paineis_dela_e_esconde_os_outros_no_mobile()
    {
        Combinar();
        var ficha = Ficha();

        ficha.FindAll(".aj-abas button").Single(b => b.TextContent.StartsWith("Documentos")).Click();

        ficha.Find(".aj-abas button[aria-selected=true]").TextContent.Should().Be("Documentos7");   // "Documentos" e o contador 3 + 4
        ficha.Markup.Should().MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\" id=\"dados-do-cliente\">\\s*<section class=\"aj-painel\">\\s*<h2>Dados</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel\" id=\"documentos-do-cliente\">\\s*(<[^>]+>\\s*)*?<section class=\"aj-painel\">\\s*<div class=\"aj-painel__cabecalho\">\\s*<h2>Documentos</h2>");
    }

    [Fact]
    public void Cada_aba_aponta_para_os_paineis_dela()
    {
        Combinar();

        var ficha = Ficha();

        AbasInterativasTests.CadaAbaApontaParaOsPaineisDela(ficha,
            "dados-do-cliente", "processos-do-cliente", "documentos-do-cliente", "usuarios-do-cliente");
    }

    [Fact]
    public void Seta_para_a_direita_escolhe_a_aba_seguinte_mostra_o_painel_dela_e_leva_o_foco()
    {
        Combinar();
        var ficha = Ficha();

        ficha.Find(".aj-abas button[aria-selected=true]").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        var escolhida = ficha.Find(".aj-abas button[aria-selected=true]");
        escolhida.TextContent.Should().Be("Processos0");
        // O tabindex móvel acompanha a escolha: a nova entra no Tab, a de antes sai.
        escolhida.GetAttribute("tabindex").Should().Be("0");
        ficha.Find(".aj-abas button[aria-controls=dados-do-cliente]").GetAttribute("tabindex").Should().Be("-1");
        ficha.Find("#processos-do-cliente").ClassList.Should().NotContain("aj-aba-painel--oculta").And.Contain("aj-aba-painel");
        ficha.Find("#dados-do-cliente").ClassList.Should().Contain("aj-aba-painel--oculta");
        FocosDeElementoPedidos.Should().Be(1);
    }

    [Fact]
    public void As_setas_levam_o_foco_a_aba_que_ficou_escolhida_e_nao_a_outra()
    {
        Combinar();
        var ficha = Ficha();
        // A ficha desenha mais de uma vez antes do teste: os botões das abas (têm @key) já foram atualizados no lugar e o
        // blazor:elementReference deles ficou vazio, então o elemento não serve de referência (a prova do elemento certo
        // está em AbasInterativasTests, que desenha as abas uma vez só). Aqui vale que o mesmo botão recebe sempre a mesma
        // referência: ida, volta e ida de novo pedem a mesma aba duas vezes e a outra no meio.
        void Teclar(string tecla) =>
            ficha.Find(".aj-abas button[aria-selected=true]").KeyDown(new KeyboardEventArgs { Key = tecla });

        Teclar("ArrowRight");
        var processos = ((ElementReference)UltimoFocoPedido().Arguments[0]!).Id;
        Teclar("ArrowLeft");
        var dados = ((ElementReference)UltimoFocoPedido().Arguments[0]!).Id;
        Teclar("ArrowRight");
        var denovo = ((ElementReference)UltimoFocoPedido().Arguments[0]!).Id;

        FocosDeElementoPedidos.Should().Be(3);
        processos.Should().NotBeNullOrEmpty().And.NotBe(dados);
        denovo.Should().Be(processos);
        ficha.Find(".aj-abas button[aria-selected=true]").TextContent.Should().Be("Processos0");
    }

    [Fact]
    public void Producao_nao_pede_os_usuarios_nem_as_areas_que_nao_le()
    {
        Combinar(diretoria: false);

        var ficha = Ficha();

        Despachante.Recebidas.OfType<ListarUsuariosDoCliente>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarDocumentosDoCliente>().Should()
            .OnlyContain(r => r.Categoria == CategoriaDocumento.DocumentoGeral || r.Categoria == CategoriaDocumento.DocumentoAtividade);
        ficha.FindAll(".aj-abas button").Select(b => b.TextContent).Should().Equal("Dados", "Processos0", "Documentos7");
    }

    [Fact]
    public void Consulta_que_falha_nao_mostra_zero_nos_contadores_das_abas_de_processos_e_usuarios()
    {
        Combinar();
        Despachante
            .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(Resultado<PaginaDeProcessos>.Falha(TipoErro.Validacao, "Falha."))
            .Responde<ListarUsuariosDoCliente, Resultado<IReadOnlyList<UsuarioDoCliente>>>(
                Resultado<IReadOnlyList<UsuarioDoCliente>>.Falha(TipoErro.Validacao, "Falha."));

        var ficha = Ficha();

        ficha.FindAll(".aj-abas button").Select(b => b.TextContent).Should().Equal("Dados", "Processos", "Documentos7", "Usuários");
    }

    [Fact]
    public void Ativar_some_com_o_recado_que_veio_de_outra_tela()
    {
        Combinar(ativo: false);
        Despachante.Responde<DefinirAtivoCliente, Resultado>(Resultado.Ok());
        Services.GetRequiredService<Recados>().Deixar("Cliente salvo.");
        var ficha = Ficha();
        ficha.Markup.Should().Contain("Cliente salvo.");

        ficha.FindAll(".aj-painel__botoes button").Single(b => b.TextContent == "Ativar").Click();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Cliente ativado."));
        ficha.Markup.Should().NotContain("Cliente salvo.");
    }
}
