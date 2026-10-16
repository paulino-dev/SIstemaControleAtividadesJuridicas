using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O formulário de usuário em edição, clicando: a confirmação de inativação, o erro de gravação e a rota trocando de
/// usuário durante a carga (a resposta do usuário anterior chega atrasada e não toma o lugar do usuário da rota, nem o
/// estado da carga dele).
/// </summary>
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

    // Conferência da 9G (D6): o "Trocar" sai da tela com o cliente escolhido e a busca de cliente aparece no lugar dele;
    // o foco vai à busca, e não ao título da página, aonde a rede de segurança o levaria.
    [Fact]
    public void Trocar_o_cliente_leva_o_foco_a_busca_de_cliente()
    {
        var perfilDeCliente = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e103");
        var joana = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d103");
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok(
                [new PerfilDetalhe(perfilDeCliente, "Cliente", TipoPerfil.Cliente, true, 1)]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok(
                [new UsuarioResumo(joana, "Joana Figueiredo", "joana@x.com", perfilDeCliente, "Cliente", TipoPerfil.Cliente, null, true, null,
                    Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c3"), "Joana Figueiredo", true)]));
        var tela = Desenhar<UsuarioFormulario>(p => p.Add(f => f.Id, (Guid?)joana));
        tela.WaitForAssertion(() => tela.Find("div.aj-vinculo__escolhido .aj-vinculo__nome").TextContent.Should().Be("Joana Figueiredo"));
        const string Busca = "input[form=\"busca-de-cliente\"]";
        tela.FindAll(Busca).Should().BeEmpty();

        tela.FindAll("div.aj-vinculo__escolhido button").Single(b => b.TextContent == "Trocar").Click();

        UltimoSeletorFocado().Should().Be(Busca);
        tela.FindAll(Busca).Should().ContainSingle();
        tela.FindAll("div.aj-vinculo__escolhido").Should().BeEmpty();
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

    private static readonly PerfilDetalhe PerfilDaProducao = new(Producao, "Produção", TipoPerfil.Producao, true, 2);

    private static readonly Guid IdDaAna = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");
    private static readonly Guid IdDoBruno = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b2");

    // Não há consulta de um só usuário: o formulário o acha na lista.
    private static Resultado<IReadOnlyList<UsuarioResumo>> Lista() => Resultado<IReadOnlyList<UsuarioResumo>>.Ok(
    [
        new UsuarioResumo(IdDaAna, "Ana Antunes", "ana@x.com", Producao, "Produção", TipoPerfil.Producao, null, true, null),
        new UsuarioResumo(IdDoBruno, "Bruno Barros", "bruno@x.com", Producao, "Produção", TipoPerfil.Producao, null, true, null),
    ]);

    // A edição da Ana, com a consulta da lista esperando a resposta.
    private IRenderedComponent<UsuarioFormulario> EdicaoDaAnaEsperando(Task<Resultado<IReadOnlyList<UsuarioResumo>>> lista)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([PerfilDaProducao]))
            .RespondeDepois<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(lista);
        return Desenhar<UsuarioFormulario>(p => p.Add(c => c.Id, (Guid?)IdDaAna));
    }

    private static string? Email(IRenderedComponent<UsuarioFormulario> tela) =>
        tela.Find("form#formulario-do-usuario input[type=email]").GetAttribute("value");

    // C7: a rota passou ao Bruno com a consulta da Ana esperando; o Bruno chega na hora, e a resposta atrasada da Ana não
    // toma o lugar dele.
    [Fact]
    public void Resposta_atrasada_do_usuario_anterior_nao_sobrescreve_o_da_rota()
    {
        var daAna = new TaskCompletionSource<Resultado<IReadOnlyList<UsuarioResumo>>>();
        var tela = EdicaoDaAnaEsperando(daAna.Task);
        tela.Markup.Should().Contain("Carregando…");

        Despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Lista());
        tela.Render(p => p.Add(c => c.Id, (Guid?)IdDoBruno));
        tela.WaitForAssertion(() => Email(tela).Should().Be("bruno@x.com"));

        var desenhos = tela.RenderCount;
        daAna.SetResult(Lista());

        // A carga da Ana termina (a tela desenha de novo) sem mexer no formulário.
        tela.WaitForAssertion(() => tela.RenderCount.Should().BeGreaterThan(desenhos));
        Email(tela).Should().Be("bruno@x.com");
        tela.Find("form#formulario-do-usuario input.aj-campo").GetAttribute("value").Should().Be("Bruno Barros");
        tela.Markup.Should().NotContain("ana@x.com");
    }

    // C7: com as duas consultas esperando, a da Ana chega primeiro: a tela continua carregando o Bruno, sem mostrar o
    // formulário (nem vazio nem o da Ana); a do Bruno chega e o mostra.
    [Fact]
    public void Resposta_do_usuario_anterior_com_o_da_rota_ainda_esperando_nao_mostra_o_formulario()
    {
        var daAna = new TaskCompletionSource<Resultado<IReadOnlyList<UsuarioResumo>>>();
        var doBruno = new TaskCompletionSource<Resultado<IReadOnlyList<UsuarioResumo>>>();
        var tela = EdicaoDaAnaEsperando(daAna.Task);
        Despachante.RespondeDepois<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(doBruno.Task);
        tela.Render(p => p.Add(c => c.Id, (Guid?)IdDoBruno));

        var desenhos = tela.RenderCount;
        daAna.SetResult(Lista());

        tela.WaitForAssertion(() => tela.RenderCount.Should().BeGreaterThan(desenhos));
        tela.Markup.Should().Contain("Carregando…");
        tela.FindAll("form#formulario-do-usuario").Should().BeEmpty();

        doBruno.SetResult(Lista());

        tela.WaitForAssertion(() => Email(tela).Should().Be("bruno@x.com"));
        tela.Markup.Should().NotContain("Carregando…");
    }
}
