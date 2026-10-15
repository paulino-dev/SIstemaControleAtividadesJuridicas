using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Usuarios;
using AtividadesJuridicas.Web.Infra;
using UsuarioFormularioPagina = AtividadesJuridicas.Web.Components.Pages.UsuarioFormulario;
using UsuariosPagina = AtividadesJuridicas.Web.Components.Pages.Usuarios;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class UsuariosWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _carla;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
        (_carla, _) = await _app.CriarCarlaAsync();
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
        // As barras de ação ficam fora do painel (no desktop, sobre o fundo da página, como no quadro) e enviam o
        // formulário do usuário pelo atributo form.
        var formulario = Regex.Match(html, "(?s)<form[^>]*id=\"formulario-do-usuario\".*?</form>").Value;
        formulario.Should().Contain("<h2>Usuário</h2>").And.NotContain("aj-acoes");
        Regex.Matches(html, "<button type=\"submit\"[^>]*form=\"formulario-do-usuario\"").Count.Should().Be(3);
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
        situacao.Should().Contain("<h2>Situação</h2>");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().Contain("Salvar usuário");
        // O painel "Situação" existe uma vez só na página, e é o do tablet e do mobile.
        Regex.Matches(html, "<h2>Situação</h2>").Count.Should().Be(1);
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
        html.Should().NotContain("<h2>Usuário</h2>").And.NotContain("Salvar usuário").And.NotContain("Escolha o perfil.");
    }

    [Fact]
    public async Task Producao_nao_abre_o_formulario()
    {
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync("/usuarios/novo"));

        html.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Novo usuário</h1>");
        html.Should().NotContain("<h2>Usuário</h2>").And.NotContain("diana@x.com").And.NotContain("Escolha o perfil.");
    }

    [Fact]
    public async Task Producao_nao_abre_a_edicao()
    {
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync($"/usuarios/{_carla}/editar"));

        html.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Editar usuário</h1>")
            .And.MatchRegex("<a href=\"/usuarios\" class=\"aj-voltar\">");
        html.Should().NotContain("<h2>Usuário</h2>").And.NotContain("Salvar usuário").And.NotContain("Carla Cliente");
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

    [Fact]
    public async Task Falha_na_consulta_da_lista_na_edicao_mostra_o_erro_e_nao_diz_que_o_usuario_nao_existe()
    {
        var diretora = new UsuarioAutenticado(Guid.NewGuid(), "Diana Diretora", TipoPerfil.Diretoria, Guid.NewGuid(), null);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretora)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
                Resultado<IReadOnlyList<UsuarioResumo>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<UsuarioFormularioPagina>(
            despachante, new() { ["Id"] = Guid.NewGuid() }));

        html.Should().MatchRegex("(?s)class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\".*?Falha de teste\\..*?>Tentar de novo</button>")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Editar usuário</h1>")
            .And.MatchRegex("<a href=\"/usuarios\" class=\"aj-voltar\">");
        html.Should().NotContain("Usuário não encontrado.").And.NotContain("<h2>Usuário</h2>").And.NotContain("Salvar usuário")
            .And.NotContain("Escolha o perfil.");
    }

    private static readonly UsuarioAutenticado Diretora = new(Guid.NewGuid(), "Diana Diretora", TipoPerfil.Diretoria, Guid.NewGuid(), null);
    private static readonly PerfilDetalhe PerfilProducao = new(Guid.NewGuid(), "Produção", TipoPerfil.Producao, true, 0);
    private static readonly PerfilDetalhe PerfilCliente = new(Guid.NewGuid(), "Cliente", TipoPerfil.Cliente, true, 0);

    private static DespachanteCombinado DespachanteDoFormulario() => new DespachanteCombinado()
        .Responde<ObterMeuAcesso, UsuarioAutenticado?>(Diretora)
        .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(
            Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([PerfilProducao, PerfilCliente]));

    [Fact]
    public async Task Salvar_com_acesso_negado_encerra_a_tela_em_vez_de_deixar_o_formulario()
    {
        var despachante = DespachanteDoFormulario()
            .Responde<CriarUsuario, Resultado<Guid>>(Resultado<Guid>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarEAgirAsync<UsuarioFormularioPagina>(
            despachante, new(), tela => TelaDeTeste.ChamarAsync(tela, "SalvarAsync")));

        html.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Novo usuário</h1>");
        html.Should().NotContain("<h2>Usuário</h2>").And.NotContain("Salvar usuário").And.NotContain("Escolha o perfil.");
    }

    [Fact]
    public async Task Falha_na_consulta_dos_perfis_mostra_o_erro_no_lugar_do_formulario()
    {
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(Diretora)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(
                Resultado<IReadOnlyList<PerfilDetalhe>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<UsuarioFormularioPagina>(despachante, new()));

        html.Should().MatchRegex("(?s)class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\".*?Falha de teste\\..*?>Tentar de novo</button>")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Novo usuário</h1>");
        html.Should().NotContain("Acesso negado").And.NotContain("<h2>Usuário</h2>").And.NotContain("Salvar usuário")
            .And.NotContain("Escolha o perfil.");
    }

    [Fact]
    public async Task Cliente_nao_informado_marca_a_busca_como_invalida_so_nesse_estado()
    {
        var despachante = DespachanteDoFormulario();

        var antes = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarEAgirAsync<UsuarioFormularioPagina>(
            despachante, new(), tela => TelaDeTeste.ChamarAsync(tela, "AoMudarPerfil", (Guid?)PerfilCliente.Id)));
        var depois = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarEAgirAsync<UsuarioFormularioPagina>(
            despachante, new(), async tela =>
            {
                await TelaDeTeste.ChamarAsync(tela, "AoMudarPerfil", (Guid?)PerfilCliente.Id);
                await TelaDeTeste.ChamarAsync(tela, "SalvarAsync");
            }));

        antes.Should().MatchRegex("<input[^>]*placeholder=\"Buscar cliente: nome, CPF ou CNPJ\"");
        antes.Should().NotContain("aria-invalid").And.NotContain("Informe o cliente do usuário.");
        depois.Should().Contain("Informe o cliente do usuário.")
            .And.MatchRegex("<input[^>]*placeholder=\"Buscar cliente: nome, CPF ou CNPJ\"[^>]*aria-invalid=\"true\"");
    }

    [Fact]
    public async Task Lista_traz_a_tabela_no_desktop_e_os_cartoes_no_tablet_e_no_mobile()
    {
        (await _app.ExecutarAsync(_admin, new DefinirAtivoUsuario(_pedro, false))).Sucesso.Should().BeTrue();

        var html = await PaginaAsync("/usuarios");

        html.Should().Contain("<p class=\"aj-subtitulo\">3 usuários · 1 inativo</p>")
            .And.MatchRegex("<a href=\"/usuarios/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"")
            .And.MatchRegex("<a href=\"/usuarios/novo\" class=\"aj-fab\" aria-label=\"Novo usuário\"");
        html.Should().NotContain("<h2>Novo usuário</h2>").And.NotContain("Senha");

        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Último acesso</th>")
            .And.MatchRegex("<tr class=\"aj-tabela__linha--inativa\">\\s*<td class=\"aj-tabela__principal\">Pedro Produção</td>\\s*<td class=\"aj-tabela__secundaria\">pedro@x.com</td>")
            .And.MatchRegex($"<td class=\"aj-tabela__acao aj-tabela__acao--varias\"><a href=\"/usuarios/{_pedro}/editar\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\">Editar</a><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Ativar</button></td>");

        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().MatchRegex($"<a href=\"/usuarios/{_pedro}/editar\" class=\"aj-cartao aj-cartao--inativo\">\\s*<span class=\"aj-cartao__titulo\">Pedro Produção</span>")
            .And.MatchRegex("<span class=\"aj-cartao__meta\">pedro@x.com<br\\s*/?>Produção \\(Produção\\)</span>")
            .And.Contain("<span class=\"aj-cartao__situacao\">Inativo</span>")
            .And.MatchRegex("<span class=\"aj-cartao__nota\">(Último acesso \\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}|Sem acesso registrado)</span>");
        cartoes.Should().NotContain(">Inativar</button>").And.NotContain(">Ativar</button>");
    }

    [Fact]
    public async Task Recado_do_formulario_aparece_uma_vez_na_lista()
    {
        var recados = new Recados();
        recados.Deixar("Usuário salvo.");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<UsuariosPagina>(_app, _admin, new(), recados));
        var depois = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<UsuariosPagina>(_app, _admin, new(), recados));

        html.Should().MatchRegex("(?s)<p class=\"aj-aviso aj-aviso--sucesso\" role=\"status\">.*?<span>Usuário salvo.</span>");
        // A segunda vez a lista vem (com a diretora) e o recado não: ele é entregue uma vez só.
        depois.Should().Contain("Diana Diretora").And.NotContain("Usuário salvo.");
    }

    [Fact]
    public async Task Falha_na_consulta_dos_usuarios_mostra_o_erro_e_nao_acesso_negado()
    {
        var diretora = new UsuarioAutenticado(Guid.NewGuid(), "Diana Diretora", TipoPerfil.Diretoria, Guid.NewGuid(), null);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretora)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
                Resultado<IReadOnlyList<UsuarioResumo>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<UsuariosPagina>(despachante, new()));

        html.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>").And.Contain("Novo usuário");
        html.Should().NotContain("Acesso negado").And.NotContain("Nenhum usuário.").And.NotContain("<table");
    }

    [Fact]
    public async Task Recarga_que_falha_depois_de_ativar_mantem_a_lista_e_avisa()
    {
        var diretora = new UsuarioAutenticado(Guid.NewGuid(), "Diana Diretora", TipoPerfil.Diretoria, Guid.NewGuid(), null);
        var inativo = new UsuarioResumo(Guid.NewGuid(), "Pedro Produção", "pedro@x.com", Guid.NewGuid(), "Produção",
            TipoPerfil.Producao, null, false, null);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretora)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([inativo]))
            .Responde<DefinirAtivoUsuario, Resultado>(Resultado.Ok());

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarEAgirAsync<UsuariosPagina>(despachante, new(), async tela =>
        {
            // A ativação dá certo, mas a consulta que a segue falha.
            despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
                Resultado<IReadOnlyList<UsuarioResumo>>.Falha(TipoErro.Validacao, "Falha de teste."));
            await TelaDeTeste.ChamarAsync(tela, "AlternarAtivoAsync", inativo);
        }));

        html.Should().MatchRegex("(?s)<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">.*?<span>Usuário ativado, mas não foi possível atualizar a lista.</span>")
            .And.Contain("<p class=\"aj-subtitulo\">1 usuário · 1 inativo</p>");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().Contain("Pedro Produção");
        html.Should().NotContain("Falha de teste.").And.NotContain(">Tentar de novo</button>");
    }
}
