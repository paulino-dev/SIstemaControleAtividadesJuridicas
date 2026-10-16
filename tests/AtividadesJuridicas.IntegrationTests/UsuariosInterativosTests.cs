using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PaginaDeUsuarios = AtividadesJuridicas.Web.Components.Pages.Usuarios;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A lista de usuários, clicando: a confirmação de inativação, o foco ao fechá-la e o aviso de resultado. Enquanto a
/// inativação do primeiro envia, os botões das linhas seguem ativos e a confirmação de outro usuário pode abrir no lugar:
/// o resultado do primeiro (sucesso ou falha) não aparece no topo ao lado dela.
/// </summary>
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

    private static readonly Guid Perfil = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a1");

    private static readonly UsuarioResumo Rafael = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a2"),
        "Rafael Teles", "rafael@x.com", Perfil, "Produção", TipoPerfil.Producao, null, true, null);
    private static readonly UsuarioResumo Beatriz = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a3"),
        "Beatriz Lins", "beatriz@x.com", Perfil, "Produção", TipoPerfil.Producao, null, true, null);
    private static readonly UsuarioResumo Camila = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a4"),
        "Camila Prado", "camila@x.com", Perfil, "Produção", TipoPerfil.Producao, null, true, null);

    // A lista com o Rafael e a Beatriz, ativos e sem pendências; a inativação responde quando o teste quiser.
    private IRenderedComponent<PaginaDeUsuarios> Lista(Task<Resultado> inativacao)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Rafael, Beatriz]))
            .Responde<ObterPendenciasDoUsuario, Resultado<PendenciasDoUsuario>>(
                Resultado<PendenciasDoUsuario>.Ok(new PendenciasDoUsuario(0, 0, 0)))
            .RespondeDepois<DefinirAtivoUsuario, Resultado>(inativacao);
        return Desenhar<PaginaDeUsuarios>();
    }

    // O "Inativar" da linha da tabela do desktop.
    private static void Inativar(IRenderedComponent<PaginaDeUsuarios> tela, string nome) =>
        tela.FindAll("div.aj-so-desktop tbody tr")
            .Single(linha => linha.QuerySelector("td.aj-tabela__principal")!.TextContent == nome)
            .QuerySelectorAll("button").Single(b => b.TextContent == "Inativar").Click();

    private static void Confirmar(IRenderedComponent<PaginaDeUsuarios> tela) =>
        tela.Find("[role=alertdialog]").QuerySelectorAll("button").Single(b => b.TextContent == "Confirmar inativação").Click();

    // A linha do usuário na tabela do desktop.
    private static AngleSharp.Dom.IElement Linha(IRenderedComponent<PaginaDeUsuarios> tela, string nome) =>
        tela.FindAll("div.aj-so-desktop tbody tr").Single(linha => linha.QuerySelector("td.aj-tabela__principal")!.TextContent == nome);

    // C8: a inativação do Rafael fica esperando, e a confirmação da Beatriz abre no lugar da dele. A falha do Rafael
    // chega: a mensagem não vai ao topo, ao lado da confirmação da Beatriz, como se fosse dela.
    [Fact]
    public void Falha_da_inativacao_do_primeiro_nao_aparece_ao_lado_da_confirmacao_do_segundo()
    {
        var inativacao = new TaskCompletionSource<Resultado>();
        var tela = Lista(inativacao.Task);
        Inativar(tela, "Rafael Teles");
        Confirmar(tela);
        Inativar(tela, "Beatriz Lins");
        tela.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a inativação de Beatriz Lins?");

        // A recarga que segue a resposta traz a Camila: é o sinal de que a resposta do Rafael foi tratada.
        Despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
            Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Rafael, Beatriz, Camila]));
        inativacao.SetResult(Resultado.Falha(TipoErro.Validacao, "Falha de teste."));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Camila Prado"));
        tela.FindAll("[role=alertdialog]").Should().ContainSingle()
            .Which.QuerySelector("strong")!.TextContent.Should().Be("Confirmar a inativação de Beatriz Lins?");
        tela.Markup.Should().NotContain("Falha de teste.");
    }

    // C8: o mesmo com o sucesso: "Usuário inativado." não diz o nome e, ao lado da confirmação da Beatriz, pareceria
    // falar dela. A lista recarregada mostra o Rafael inativo.
    [Fact]
    public void Sucesso_da_inativacao_do_primeiro_nao_aparece_ao_lado_da_confirmacao_do_segundo()
    {
        var inativacao = new TaskCompletionSource<Resultado>();
        var tela = Lista(inativacao.Task);
        Inativar(tela, "Rafael Teles");
        Confirmar(tela);
        Inativar(tela, "Beatriz Lins");

        Despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
            Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Rafael with { Ativo = false }, Beatriz, Camila]));
        inativacao.SetResult(Resultado.Ok());

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Camila Prado"));
        Linha(tela, "Rafael Teles").QuerySelectorAll("button").Select(b => b.TextContent).Should().Equal("Ativar");
        tela.FindAll("[role=alertdialog]").Should().ContainSingle()
            .Which.QuerySelector("strong")!.TextContent.Should().Be("Confirmar a inativação de Beatriz Lins?");
        tela.Markup.Should().NotContain("Usuário inativado.");
    }

    // C8: abrir outra confirmação limpa a mensagem de resultado que já estava no topo, de sucesso ou de falha.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Abrir_outra_confirmacao_limpa_a_mensagem_de_resultado_anterior(bool sucesso)
    {
        var inativacao = new TaskCompletionSource<Resultado>();
        var tela = Lista(inativacao.Task);
        Inativar(tela, "Rafael Teles");
        Confirmar(tela);
        var mensagem = sucesso ? "Usuário inativado." : "Falha de teste.";
        if (sucesso)
        {
            Despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
                Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Rafael with { Ativo = false }, Beatriz]));
        }

        inativacao.SetResult(sucesso ? Resultado.Ok() : Resultado.Falha(TipoErro.Validacao, "Falha de teste."));
        tela.WaitForAssertion(() => tela.Markup.Should().Contain(mensagem));

        Inativar(tela, "Beatriz Lins");

        tela.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a inativação de Beatriz Lins?");
        tela.Markup.Should().NotContain(mensagem);
    }

    // Sem outra confirmação aberta, a falha vai ao topo, como hoje.
    [Fact]
    public void Falha_da_inativacao_sem_outra_confirmacao_aberta_aparece_no_topo()
    {
        var inativacao = new TaskCompletionSource<Resultado>();
        var tela = Lista(inativacao.Task);
        Inativar(tela, "Rafael Teles");
        Confirmar(tela);

        inativacao.SetResult(Resultado.Falha(TipoErro.Validacao, "Falha de teste."));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Falha de teste."));
        tela.Find("p.aj-aviso.aj-aviso--erro").TextContent.Should().Contain("Falha de teste.");
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
    }
}
