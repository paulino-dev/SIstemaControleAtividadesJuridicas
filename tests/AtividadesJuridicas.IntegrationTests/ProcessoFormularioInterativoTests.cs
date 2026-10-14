using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class ProcessoFormularioInterativoTests : TelaInterativa
{
    private static readonly Guid Area = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a9");
    private static readonly ClienteResumo Cooperativa = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c9"),
        TipoCliente.PessoaJuridica, "Cooperativa Agrícola Boa Safra", "11.222.333/0001-81", null, null, null, true);

    private IRenderedComponent<ProcessoFormulario> Formulario()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(
                Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Ok([new AreaAtuacaoResumo(Area, "Cível", true)]))
            .Responde<ListarClientes, Resultado<PaginaDeClientes>>(Resultado<PaginaDeClientes>.Ok(new([Cooperativa], 1, 1, 20, 1, 0)))
            .Responde<SalvarProcesso, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        return Desenhar<ProcessoFormulario>();
    }

    private static void Digitar(IRenderedComponent<ProcessoFormulario> tela, string rotulo, string valor) =>
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.TrimStart().StartsWith(rotulo)).QuerySelector("input, textarea")!.Change(valor);

    [Fact]
    public void Enter_na_busca_mostra_o_candidato_e_adicionar_vincula_no_polo_ativo()
    {
        var tela = Formulario();

        tela.Find("input[form=busca-de-cliente]").Input("coop");
        tela.Find("form#busca-de-cliente").Submit();

        tela.Find("div.aj-candidato .aj-candidato__titulo").TextContent.Should().Be("Cooperativa Agrícola Boa Safra");
        tela.Find("div.aj-candidato .aj-ajuda").TextContent.Should().Be("CNPJ 11.222.333/0001-81");
        tela.Find("div.aj-candidato button").Click();

        tela.Find("div.aj-vinculo__escolhido .aj-vinculo__nome").TextContent.Should().Be("Cooperativa Agrícola Boa Safra");
        tela.Find("div.aj-vinculo__escolhido select option[selected]").TextContent.Should().Be("Ativo");
        tela.FindAll("div.aj-candidato").Should().BeEmpty();
        Despachante.Recebidas.OfType<SalvarProcesso>().Should().BeEmpty();   // o Enter da busca não envia o processo
    }

    [Fact]
    public void Valor_da_causa_fora_do_formato_mostra_o_erro_no_campo_e_nao_grava()
    {
        var tela = Formulario();
        Digitar(tela, "Título", "Litoral × Seguradora — indenização");
        Digitar(tela, "Valor da causa", "84000.50");

        tela.Find("form#formulario-do-processo").Submit();

        tela.Markup.Should().Contain("Informe o valor como 84.000,00.");
        Despachante.Recebidas.OfType<SalvarProcesso>().Should().BeEmpty();
    }

    [Fact]
    public void Valor_no_formato_brasileiro_grava_o_numero_e_volta_com_o_recado()
    {
        var tela = Formulario();
        Digitar(tela, "Título", "Litoral × Seguradora — indenização");
        Digitar(tela, "Valor da causa", "84.000,00");
        tela.Find("select.aj-campo").Change(Area.ToString());

        tela.Find("form#formulario-do-processo").Submit();

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<SalvarProcesso>().Should().ContainSingle()
            .Which.Dados.ValorCausa.Should().Be(84000m));
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/processos");
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Processo salvo.");
    }

    [Fact]
    public void Depois_de_salvar_com_sucesso_um_novo_envio_nao_dispara_outro_salvamento()
    {
        var resposta = new TaskCompletionSource<Resultado<Guid>>();
        var tela = Formulario();
        Despachante.RespondeDepois<SalvarProcesso, Resultado<Guid>>(resposta.Task);
        Digitar(tela, "Título", "Litoral × Seguradora — indenização");
        tela.Find("select.aj-campo").Change(Area.ToString());

        tela.Find("form#formulario-do-processo").Submit();
        resposta.SetResult(Resultado<Guid>.Ok(Guid.NewGuid()));
        tela.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/processos"));
        tela.Find("form#formulario-do-processo").Submit();

        Despachante.Recebidas.OfType<SalvarProcesso>().Should().ContainSingle();
    }
}
