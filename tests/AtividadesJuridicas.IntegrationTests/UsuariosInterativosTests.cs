using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PaginaDeUsuarios = AtividadesJuridicas.Web.Components.Pages.Usuarios;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>A lista de usuários, clicando: a confirmação de inativação, o foco ao fechá-la e o aviso de resultado.</summary>
public class UsuariosInterativosTests : TelaInterativa
{
    private static readonly Guid Pedro = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d102");
    private static readonly Guid Producao = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e102");

    private static readonly UsuarioResumo DoPedro =
        new(Pedro, "Pedro Produção", "pedro@x.com", Producao, "Produção", TipoPerfil.Producao, null, true, null);

    private IRenderedComponent<PaginaDeUsuarios> Tela()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok(
                [new PerfilDetalhe(Producao, "Produção", TipoPerfil.Producao, true, 1)]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([DoPedro]))
            .Responde<ObterPendenciasDoUsuario, Resultado<PendenciasDoUsuario>>(
                Resultado<PendenciasDoUsuario>.Ok(new PendenciasDoUsuario(0, 0, 0)));
        return Desenhar<PaginaDeUsuarios>();
    }

    [Fact]
    public void Cancelar_a_inativacao_devolve_o_foco_ao_inativar_da_linha()
    {
        var tela = Tela();
        var inativar = tela.Find($"#ativar-ou-inativar-{Pedro:N}");
        inativar.TextContent.Should().Be("Inativar");
        inativar.Click();
        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar inativação");

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be($"#ativar-ou-inativar-{Pedro:N}");
    }

    [Fact]
    public void Confirmar_a_inativacao_leva_o_foco_ao_aviso_de_resultado()
    {
        var tela = Tela();
        Despachante.Responde<DefinirAtivoUsuario, Resultado>(Resultado.Ok());
        tela.Find($"#ativar-ou-inativar-{Pedro:N}").Click();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar inativação").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Usuário inativado."));
        var aviso = tela.Find("p.aj-aviso--sucesso");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Recado_de_outra_tela_aparece_sem_levar_o_foco()
    {
        // Trava: o recado chega com a navegação, e quem leva o foco ao título é o FocoNoTitulo, não o aviso.
        Services.GetRequiredService<Recados>().Deixar("Usuário salvo.");

        var tela = Tela();

        tela.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Usuário salvo.");
        tela.Find("p.aj-aviso--sucesso").HasAttribute("tabindex").Should().BeFalse();
        FocosDeElementoPedidos.Should().Be(0);
    }
}
