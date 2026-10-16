using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Paginas = AtividadesJuridicas.Web.Components.Pages;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O módulo de foco do layout, pelo que o .NET pede a ele: a rede de segurança instalada uma vez, o título focado e a
/// rolagem zerada quando a página muda, e o serviço <c>Foco</c> que as telas usam. O que o JavaScript faz (o foco que
/// cai no body, a rolagem do main) só se confere no navegador.
/// </summary>
public class FocoNoTituloInterativoTests : TelaInterativa
{
    // A rota como o Router a entrega: o tipo da página e os valores do endereço. A consulta (?visao=, ?produtor=,
    // ?pagina=) não entra no RouteData: mudar só a consulta entrega outra instância com o mesmo tipo de página.
    private static RouteData Rota<TPagina>(params (string Nome, object? Valor)[] valores) =>
        new(typeof(TPagina), valores.ToDictionary(v => v.Nome, v => v.Valor));

    [Fact]
    public void Na_primeira_pagina_instala_a_rede_e_foca_o_titulo_sem_zerar_a_rolagem()
    {
        Desenhar<FocoNoTitulo>(p => p.Add(f => f.RouteData, Rota<Paginas.Home>()));

        ModuloDeFoco.VerifyInvoke("vigiarFoco");
        ModuloDeFoco.VerifyInvoke("focarTituloVisivel");
        ModuloDeFoco.VerifyNotInvoke("zerarRolagem");
    }

    [Fact]
    public void Pagina_nova_zera_a_rolagem_e_foca_o_titulo_sem_instalar_a_rede_de_novo()
    {
        var tela = Desenhar<FocoNoTitulo>(p => p.Add(f => f.RouteData, Rota<Paginas.Home>()));

        tela.Render(p => p.Add(f => f.RouteData, Rota<Paginas.Clientes>()));

        ModuloDeFoco.VerifyInvoke("zerarRolagem");
        ModuloDeFoco.VerifyInvoke("focarTituloVisivel", calledTimes: 2);
        ModuloDeFoco.VerifyInvoke("vigiarFoco", calledTimes: 1);
    }

    [Fact]
    public void Mesma_pagina_com_outra_consulta_ou_outro_endereco_nao_zera_a_rolagem_nem_foca_de_novo()
    {
        var tela = Desenhar<FocoNoTitulo>(p => p.Add(f => f.RouteData, Rota<Paginas.Agenda>()));

        // Troca de período da agenda (?visao=mes&data=…): a mesma página, outra consulta.
        tela.Render(p => p.Add(f => f.RouteData, Rota<Paginas.Agenda>()));
        // E o mesmo critério do título: a mesma ficha com outro id é a mesma página.
        tela.Render(p => p.Add(f => f.RouteData, Rota<Paginas.AtividadeFicha>(("Id", Guid.NewGuid()))));
        tela.Render(p => p.Add(f => f.RouteData, Rota<Paginas.AtividadeFicha>(("Id", Guid.NewGuid()))));

        // Da agenda para a ficha mudou a página: uma vez só, nas duas chamadas.
        ModuloDeFoco.VerifyInvoke("zerarRolagem", calledTimes: 1);
        ModuloDeFoco.VerifyInvoke("focarTituloVisivel", calledTimes: 2);
        ModuloDeFoco.VerifyInvoke("vigiarFoco", calledTimes: 1);
    }

    [Fact]
    public async Task Foco_pede_ao_modulo_o_primeiro_visivel_do_seletor()
    {
        var foco = Services.GetRequiredService<Foco>();

        await foco.FocarPrimeiroVisivelAsync("button[aria-controls=\"painel-de-filtros\"]");

        ModuloDeFoco.VerifyInvoke("focarPrimeiroVisivel").Arguments.Should().Equal("button[aria-controls=\"painel-de-filtros\"]");
        // Os auxiliares da TelaInterativa que as Tarefas 4 a 10 usam leem o mesmo registro.
        SeletoresFocados.Should().Equal("button[aria-controls=\"painel-de-filtros\"]");
        UltimoSeletorFocado().Should().Be("button[aria-controls=\"painel-de-filtros\"]");
        FocosDeElementoPedidos.Should().Be(0);
    }

    [Fact]
    public async Task Falha_do_modulo_nao_derruba_quem_pediu_o_foco()
    {
        ModuloDeFoco.SetupVoid("focarPrimeiroVisivel", "h1").SetException(new JSException("O módulo falhou."));
        var foco = Services.GetRequiredService<Foco>();

        await foco.Invoking(f => f.FocarPrimeiroVisivelAsync("h1")).Should().NotThrowAsync();
        ModuloDeFoco.VerifyInvoke("focarPrimeiroVisivel").Arguments.Should().Equal("h1");
    }
}
