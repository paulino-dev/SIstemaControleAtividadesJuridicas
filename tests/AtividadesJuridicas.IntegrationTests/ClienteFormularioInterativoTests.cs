using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class ClienteFormularioInterativoTests : TelaInterativa
{
    [Fact]
    public void Trocar_para_pessoa_juridica_mostra_os_campos_dela_e_salvar_envia_e_volta_com_o_recado()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<SalvarClientePessoaJuridica, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var tela = Desenhar<ClienteFormulario>();

        tela.FindAll("button.aj-segmentado__opcao").First(b => b.TextContent == "Pessoa jurídica").Click();
        tela.Markup.Should().Contain("Razão social").And.NotContain("Data de nascimento");
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("Razão social")).QuerySelector("input")!.Change("Cooperativa Boa Safra");
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("CNPJ")).QuerySelector("input")!.Change("11.222.333/0001-81");
        tela.Find("form#formulario-do-cliente").Submit();

        tela.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/clientes"));
        Despachante.Recebidas.OfType<SalvarClientePessoaJuridica>().Should().ContainSingle()
            .Which.Dados.Nome.Should().Be("Cooperativa Boa Safra");
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Cliente salvo.");
    }

    [Fact]
    public void Salvar_sem_nome_nem_documento_mostra_o_erro_nos_campos_e_nao_envia()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria);
        var tela = Desenhar<ClienteFormulario>();

        tela.Find("form#formulario-do-cliente").Submit();

        tela.Markup.Should().Contain("Informe o nome.").And.Contain("Informe o documento.").And.Contain("aj-rotulo--erro");
        Despachante.Recebidas.OfType<SalvarClientePessoaFisica>().Should().BeEmpty();
    }

    [Fact]
    public void Falha_ao_salvar_mostra_o_aviso_e_mantem_o_que_foi_digitado()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<SalvarClientePessoaFisica, Resultado<Guid>>(Resultado<Guid>.Falha(TipoErro.Validacao, "CPF já cadastrado."));
        var tela = Desenhar<ClienteFormulario>();
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("Nome completo")).QuerySelector("input")!.Change("Helena Barros Lima");
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("CPF")).QuerySelector("input")!.Change("529.982.247-25");

        tela.Find("form#formulario-do-cliente").Submit();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("CPF já cadastrado."));
        tela.Markup.Should().MatchRegex("<input[^>]*value=\"Helena Barros Lima\"");
        Services.GetRequiredService<NavigationManager>().Uri.Should().NotEndWith("/clientes");
    }

    [Fact]
    public void Depois_de_salvar_com_sucesso_um_novo_envio_nao_dispara_outro_salvamento()
    {
        var resposta = new TaskCompletionSource<Resultado<Guid>>();
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .RespondeDepois<SalvarClientePessoaFisica, Resultado<Guid>>(resposta.Task);
        var tela = Desenhar<ClienteFormulario>();
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("Nome completo")).QuerySelector("input")!.Change("Helena Barros Lima");
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("CPF")).QuerySelector("input")!.Change("529.982.247-25");

        tela.Find("form#formulario-do-cliente").Submit();
        resposta.SetResult(Resultado<Guid>.Ok(Guid.NewGuid()));
        tela.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/clientes"));
        tela.Find("form#formulario-do-cliente").Submit();

        Despachante.Recebidas.OfType<SalvarClientePessoaFisica>().Should().ContainSingle();
    }
}
