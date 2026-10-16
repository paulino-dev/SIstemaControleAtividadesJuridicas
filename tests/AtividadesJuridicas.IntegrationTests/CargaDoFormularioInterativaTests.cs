using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O estado de carregamento dos formulários em tela própria: um estado por vez, com a marcação que os formulários já
/// emitiam; o formulário só quando a carga ficou pronta; e a nova tentativa, que repete a carga inteira.
/// </summary>
public class CargaDoFormularioInterativaTests : TelaInterativa
{
    private const string Formulario = "<form id=\"formulario-de-teste\" class=\"aj-painel\">o formulário</form>";
    private const string Erro = "Não foi possível consultar o cliente.";
    private static readonly Guid Area = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a9");

    private static readonly string[] Textos =
        ["Carregando…", "Acesso negado.", "Cliente não encontrado.", Erro, "Tentar de novo", "o formulário"];

    private int _tentativas;

    private IRenderedComponent<CargaDoFormulario> Carga(EstadoDeCarga estado) =>
        Desenhar<CargaDoFormulario>(p => p
            .Add(c => c.Estado, estado)
            .Add(c => c.Erro, Erro)
            .Add(c => c.TextoNaoEncontrado, "Cliente não encontrado.")
            .Add(c => c.TentarDeNovo, () => _tentativas++)
            .AddChildContent(Formulario));

    [Theory]
    [InlineData(EstadoDeCarga.Carregando, "Carregando…")]
    [InlineData(EstadoDeCarga.AcessoNegado, "Acesso negado.")]
    [InlineData(EstadoDeCarga.NaoEncontrado, "Cliente não encontrado.")]
    [InlineData(EstadoDeCarga.Pronto, "o formulário")]
    public void Mostra_um_estado_por_vez(EstadoDeCarga estado, string texto)
    {
        var carga = Carga(estado);

        carga.Markup.Should().Contain(texto);
        foreach (var outro in Textos.Where(t => t != texto))
            carga.Markup.Should().NotContain(outro);
    }

    [Fact]
    public void Emite_a_marcacao_que_os_formularios_ja_emitiam()
    {
        Carga(EstadoDeCarga.Carregando).Markup.Should().Contain("<p class=\"aj-subtitulo\" role=\"status\">Carregando…</p>");
        Carga(EstadoDeCarga.AcessoNegado).Markup.Should().MatchRegex(
            "(?s)<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">\\s*<svg.*?</svg>\\s*<span>Acesso negado.</span>\\s*</p>");
        Carga(EstadoDeCarga.NaoEncontrado).Markup.Should().MatchRegex(
            "(?s)<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">\\s*<svg.*?</svg>\\s*<span>Cliente não encontrado.</span>\\s*</p>");
        Carga(EstadoDeCarga.Pronto).Markup.Should().Contain(Formulario);
    }

    [Fact]
    public void Falha_mostra_o_erro_com_tentar_de_novo_sem_o_formulario_e_o_clique_avisa()
    {
        var carga = Carga(EstadoDeCarga.Falhou);

        var aviso = carga.Find("div.aj-aviso.aj-aviso--erro.aj-aviso--com-acoes[role=alert]");
        aviso.TextContent.Should().Contain(Erro);
        carga.FindAll("#formulario-de-teste").Should().BeEmpty();
        carga.Markup.Should().NotContain("Carregando…").And.NotContain("Acesso negado.");

        aviso.QuerySelector("button.aj-botao.aj-botao--secundario")!.Click();

        _tentativas.Should().Be(1);
    }

    [Fact]
    public void Formulario_de_processo_com_falha_na_carga_tenta_de_novo_e_mostra_o_formulario()
    {
        var tentativas = 0;
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(_ => ++tentativas == 1
                ? Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Falha(TipoErro.Validacao, "Não foi possível consultar as áreas de atuação.")
                : Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Ok([new AreaAtuacaoResumo(Area, "Cível", true)]));
        var tela = Desenhar<ProcessoFormulario>();

        tela.Find("div.aj-aviso--erro").TextContent.Should().Contain("Não foi possível consultar as áreas de atuação.");
        tela.FindAll("form#formulario-do-processo").Should().BeEmpty();

        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.FindAll("form#formulario-do-processo").Should().ContainSingle());
        tela.Find("select.aj-campo").TextContent.Should().Contain("Cível");
        tela.FindAll("button").Where(b => b.TextContent == "Tentar de novo").Should().BeEmpty();
        // A carga inteira, inclusive a do perfil de quem está na tela.
        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().HaveCount(2);
        Despachante.Recebidas.OfType<ListarAreasAtuacao>().Should().HaveCount(2);
    }

    [Fact]
    public void Formulario_de_usuario_repete_a_carga_dos_perfis_e_mostra_o_formulario()
    {
        var tentativas = 0;
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(_ => ++tentativas == 1
                ? Resultado<IReadOnlyList<PerfilDetalhe>>.Falha(TipoErro.Validacao, "Não foi possível consultar os perfis.")
                : Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([new PerfilDetalhe(Guid.NewGuid(), "Produção", TipoPerfil.Producao, true, 0)]));
        var tela = Desenhar<UsuarioFormulario>();

        tela.Find("div.aj-aviso--erro").TextContent.Should().Contain("Não foi possível consultar os perfis.");
        tela.FindAll("form#formulario-do-usuario").Should().BeEmpty();

        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.FindAll("form#formulario-do-usuario").Should().ContainSingle());
        tela.Markup.Should().Contain("Escolha o perfil. Para um usuário de cliente, indique o cliente.");
        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().HaveCount(2);
        Despachante.Recebidas.OfType<ListarPerfis>().Should().HaveCount(2);
    }

    [Fact]
    public void Formulario_de_cliente_com_falha_na_consulta_mostra_tentar_de_novo_e_consulta_de_novo()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterCliente, Resultado<ClienteDetalhe>>(Resultado<ClienteDetalhe>.Falha(TipoErro.Validacao, Erro));
        var tela = Desenhar<ClienteFormulario>(p => p.Add(c => c.Id, (Guid?)Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1")));

        tela.Find("div.aj-aviso--erro").TextContent.Should().Contain(Erro);
        tela.FindAll("form#formulario-do-cliente").Should().BeEmpty();
        // O subtítulo só acompanha o formulário.
        tela.Markup.Should().NotContain("Só o nome e o CPF ou CNPJ são obrigatórios.");

        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<ObterCliente>().Should().HaveCount(2));
        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().HaveCount(2);
        tela.Find("div.aj-aviso--erro").TextContent.Should().Contain(Erro);
    }

    [Fact]
    public void Tentar_de_novo_mostra_carregando_ate_a_tela_terminar_e_nao_repete_no_segundo_clique()
    {
        var carga = new TaskCompletionSource();
        var tentativas = 0;
        var tela = Desenhar<CargaDoFormulario>(p => p
            .Add(c => c.Estado, EstadoDeCarga.Falhou)
            .Add(c => c.Erro, Erro)
            .Add(c => c.TextoNaoEncontrado, "Cliente não encontrado.")
            .Add(c => c.TentarDeNovo, () =>
            {
                tentativas++;
                return carga.Task;
            })
            .AddChildContent(Formulario));

        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        // A tela ainda diz "Falhou" (a carga dela não terminou): o componente mostra o carregando e some com o botão.
        tela.Markup.Should().Contain("Carregando…").And.NotContain(Erro);
        tela.FindAll("button").Should().NotContain(b => b.TextContent == "Tentar de novo");
        tentativas.Should().Be(1);

        carga.SetResult();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain(Erro));
        tela.Markup.Should().NotContain("Carregando…");
        tentativas.Should().Be(1);
    }
}
