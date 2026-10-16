using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>O formulário de usuário em edição, clicando: a confirmação de inativação e o erro de gravação.</summary>
public class UsuarioFormularioInterativoTests : TelaInterativa
{
    private static readonly Guid Pedro = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d102");
    private static readonly Guid Producao = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e102");

    private IRenderedComponent<UsuarioFormulario> Edicao()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok(
                [new PerfilDetalhe(Producao, "Produção", TipoPerfil.Producao, true, 1)]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok(
                [new UsuarioResumo(Pedro, "Pedro Produção", "pedro@x.com", Producao, "Produção", TipoPerfil.Producao, null, true, null)]))
            .Responde<ObterPendenciasDoUsuario, Resultado<PendenciasDoUsuario>>(
                Resultado<PendenciasDoUsuario>.Ok(new PendenciasDoUsuario(0, 0, 0)));
        return Desenhar<UsuarioFormulario>(p => p.Add(f => f.Id, (Guid?)Pedro));
    }

    [Fact]
    public void Cancelar_a_inativacao_devolve_o_foco_ao_inativar_usuario()
    {
        var tela = Edicao();
        var inativar = tela.Find("#ativar-ou-inativar-usuario");
        inativar.TextContent.Should().Be("Inativar usuário");
        inativar.Click();
        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar inativação");

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be("#ativar-ou-inativar-usuario");
    }

    [Fact]
    public void Falha_ao_salvar_leva_o_foco_ao_aviso_de_erro()
    {
        var tela = Edicao();
        Despachante.Responde<EditarUsuario, Resultado>(Resultado.Falha(TipoErro.Validacao, "E-mail já cadastrado."));

        tela.Find("form#formulario-do-usuario").Submit();

        tela.WaitForAssertion(() => tela.Find("p.aj-aviso--erro").TextContent.Should().Be("E-mail já cadastrado."));
        var aviso = tela.Find("p.aj-aviso--erro");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }
}
