using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Web.Components.Usuarios;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class UsuariosWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _carla, _maria;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
        (_carla, _maria) = await _app.CriarCarlaAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Formulario_novo_traz_o_voltar_os_campos_da_equipe_e_as_acoes_de_cada_visao()
    {
        var html = await PaginaAsync("/usuarios/novo");

        var desktop = BlocoDeVisao.De(html, "aj-desktop-tablet");
        desktop.Should().MatchRegex("<a href=\"/usuarios\" class=\"aj-voltar\">")
            .And.Contain("<p class=\"aj-subtitulo\">Escolha o perfil. Para um usuário de cliente, indique o cliente.</p>")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Novo usuário</h1>");
        html.Should().Contain("aria-label=\"Voltar para Usuários\"")
            .And.MatchRegex("<form[^>]*class=\"aj-painel aj-painel--formulario\"")
            .And.Contain("<h2>Usuário</h2>").And.Contain("<div class=\"aj-grade-campos\">")
            .And.MatchRegex("<span>Número da OAB <small>\\(opcional\\)</small></span>")
            .And.MatchRegex("autocomplete=\"new-password\"");
        // Usuário novo começa em Produção: sem o bloco do cliente.
        html.Should().NotContain("aj-vinculo").And.NotContain("Nova senha");

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--formulario\">\\s*<a href=\"/usuarios\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Salvar usuário</button>");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex(
            "<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/usuarios\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">.*?Salvar</button>")
            .And.NotContain("Salvar usuário");
        // O perfil inativo não é opção para usuário novo, e o escolhido de início é de Produção.
        var select = Regex.Match(html, "<select[^>]*>.*?</select>", RegexOptions.Singleline).Value;
        select.Should().Contain(">Diretor (Diretoria)</option>").And.Contain(">Cliente (Cliente)</option>");
        Regex.Matches(select, "<option[^>]*selected[^>]*>[^<]*</option>").Select(m => m.Value).Should()
            .ContainSingle().Which.Should().Contain("Produção (Produção)");
    }

    [Fact]
    public async Task Edicao_de_usuario_de_cliente_traz_o_cliente_escolhido_e_a_situacao()
    {
        var html = await PaginaAsync($"/usuarios/{_carla}/editar");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Editar usuário</h1>")
            .And.MatchRegex("<input[^>]*value=\"Carla Cliente\"")
            .And.MatchRegex("<div class=\"aj-vinculo aj-grade-campos__inteiro\">")
            .And.MatchRegex("<div class=\"aj-vinculo__escolhido\">\\s*<span class=\"aj-vinculo__nome\">Maria Silva</span>\\s*<button type=\"button\" class=\"aj-botao aj-botao--fantasma\"[^>]*>Trocar</button>")
            .And.Contain("O usuário entra no portal e vê só o que é deste cliente.")
            .And.MatchRegex("<span>Nova senha <small>\\(deixe em branco para manter\\)</small></span>");
        html.Should().NotContain("Número da OAB");

        var situacao = BlocoDeVisao.De(html, "aj-tablet-mobile");
        situacao.Should().MatchRegex("(?s)<section class=\"aj-painel\">\\s*<h2>Situação</h2>.*?Ativo.*?>Inativar usuário</button>");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().Contain("Salvar usuário").And.NotContain("Situação");
    }

    [Fact]
    public async Task Edicao_de_usuario_da_equipe_traz_a_oab_e_nao_o_cliente()
    {
        var html = await PaginaAsync($"/usuarios/{_pedro}/editar");

        html.Should().MatchRegex("<input[^>]*value=\"Pedro Produção\"").And.Contain("Número da OAB");
        html.Should().NotContain("aj-vinculo");
    }

    [Fact]
    public async Task Edicao_de_usuario_que_nao_existe_mostra_nao_encontrado_com_o_voltar()
    {
        var html = await PaginaAsync($"/usuarios/{Guid.NewGuid()}/editar");

        html.Should().Contain("Usuário não encontrado.").And.MatchRegex("<a href=\"/usuarios\" class=\"aj-voltar\">")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Editar usuário</h1>");
        html.Should().NotContain("<h2>Usuário</h2>").And.NotContain("Salvar usuário");
    }

    [Fact]
    public async Task Producao_nao_abre_o_formulario()
    {
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync("/usuarios/novo"));

        html.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Novo usuário</h1>");
        html.Should().NotContain("<h2>Usuário</h2>").And.NotContain("diana@x.com");
    }

    [Fact]
    public async Task Confirmacao_de_inativacao_diz_as_pendencias_e_poe_cancelar_antes_do_verbo()
    {
        var despachante = new DespachanteCombinado().Responde<ObterPendenciasDoUsuario, Resultado<PendenciasDoUsuario>>(
            Resultado<PendenciasDoUsuario>.Ok(new PendenciasDoUsuario(2, 1, 3)));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<ConfirmacaoDeInativacao>(despachante, new()
        {
            ["UsuarioId"] = Guid.NewGuid(), ["Nome"] = "Rafael Teles",
        }));

        html.Should().MatchRegex("<div class=\"aj-aviso aj-aviso--atencao aj-aviso--com-acoes\"[^>]*role=\"alertdialog\"")
            .And.Contain("aria-label=\"Confirmar inativação\"")
            .And.Contain("<strong>Confirmar a inativação de Rafael Teles?</strong>")
            .And.Contain("Este usuário tem 2 partes pendentes, 1 revisão em aberto e 3 eventos futuros.")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--secundario\"[^>]*>Cancelar</button>\\s*<button type=\"button\" class=\"aj-botao aj-botao--primario\"[^>]*>Confirmar inativação</button>");
    }

    [Fact]
    public async Task Confirmacao_sem_as_pendencias_avisa_que_nao_verificou()
    {
        var despachante = new DespachanteCombinado().Responde<ObterPendenciasDoUsuario, Resultado<PendenciasDoUsuario>>(
            Resultado<PendenciasDoUsuario>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<ConfirmacaoDeInativacao>(despachante, new()
        {
            ["UsuarioId"] = Guid.NewGuid(), ["Nome"] = "Rafael Teles",
        }));

        html.Should().Contain("<strong>Confirmar a inativação de Rafael Teles?</strong>")
            .And.Contain("Não foi possível verificar as pendências deste usuário.").And.Contain(">Confirmar inativação</button>");
    }
}
