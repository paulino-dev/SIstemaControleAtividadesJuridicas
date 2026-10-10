using AtividadesJuridicas.Web.Components.Layout;
using Microsoft.AspNetCore.Components;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Os blocos de tela do design system, desenhados sozinhos.</summary>
public class ComponentesDoDesignSystemTests
{
    [Theory]
    [InlineData("adicionar")]
    [InlineData("buscar")]
    [InlineData("filtros")]
    [InlineData("voltar")]
    [InlineData("avancar")]
    [InlineData("confirmar")]
    [InlineData("excluir")]
    [InlineData("devolver")]
    [InlineData("entrar")]
    [InlineData("baixar")]
    [InlineData("atencao")]
    [InlineData("erro")]
    public async Task Icone_do_design_system_tem_tracado(string nome)
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(new() { ["Nome"] = nome });

        html.Should().MatchRegex("<svg[^>]*aria-hidden=\"true\"[^>]*>\\s*<(path|circle|rect)");
    }

    [Fact]
    public async Task Icone_aceita_classe()
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(
            new() { ["Nome"] = "avancar", ["Classe"] = "aj-cartao__seta" });

        html.Should().Contain("class=\"aj-cartao__seta\"");
    }

    [Theory]
    [InlineData(TipoDeAviso.Sucesso, "aj-aviso aj-aviso--sucesso", "status", "M5 12.5l4.5 4.5L19 7.5")]
    [InlineData(TipoDeAviso.Atencao, "aj-aviso aj-aviso--atencao", "status", "M12 4l9 16H3z")]
    [InlineData(TipoDeAviso.Erro, "aj-aviso aj-aviso--erro", "alert", "M12 7.5v5.5")]
    public async Task Aviso_traz_a_classe_o_papel_e_o_icone_do_tipo(
        TipoDeAviso tipo, string classe, string papel, string tracado)
    {
        RenderFragment texto = construtor => construtor.AddContent(0, "Revisor alterado.");

        var html = await TelaDeTeste.RenderizarSemDadosAsync<Aviso>(
            new() { ["Tipo"] = tipo, ["ChildContent"] = texto });

        // O traçado é o do ícone do tipo: confirmar, atenção ou erro.
        html.Should().Contain($"class=\"{classe}\"").And.Contain($"role=\"{papel}\"")
            .And.Contain($"d=\"{tracado}\"").And.Contain("<span>Revisor alterado.</span>");
    }
}
