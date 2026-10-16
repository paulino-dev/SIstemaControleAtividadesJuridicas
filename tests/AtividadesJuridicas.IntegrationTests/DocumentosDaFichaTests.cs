using System.Net;
using System.Text;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Documentos;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O painel "Documentos" da ficha do cliente, no primeiro desenho (a ficha abre em "Gerais"). O que o clique
/// faz (trocar de área, enviar, acesso, foco) está em <see cref="DocumentosDaFichaInterativosTests"/>.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public class DocumentosDaFichaTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _maria;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        (_, _maria) = await _app.CriarCarlaAsync();
        var enviado = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoDoCliente, _maria, null, "procuracao.pdf", "application/pdf",
            new MemoryStream(Encoding.UTF8.GetBytes("conteúdo"))));
        enviado.Sucesso.Should().BeTrue(enviado.Mensagem);
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private static MemoryStream Conteudo() => new(Encoding.UTF8.GetBytes("conteúdo"));

    private async Task<string> FichaAsync(Guid usuario, bool podeGerenciar) =>
        WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<DocumentosDaFicha>(_app, usuario, new()
        {
            ["ClienteId"] = _maria, ["PodeGerenciar"] = podeGerenciar,
        }));

    [Fact]
    public async Task Diretoria_ve_os_quatro_chips_com_a_contagem_e_o_envio()
    {
        // O documento do InitializeAsync é "Do cliente"; a ficha abre em "Gerais", vazia.
        var html = await FichaAsync(_admin, podeGerenciar: true);

        html.Should().MatchRegex("(?s)<section class=\"aj-painel\">\\s*<div class=\"aj-painel__cabecalho\">\\s*<h2>Documentos</h2>\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Enviar documento</button>")
            .And.MatchRegex("(?s)<div class=\"aj-chips\" role=\"group\" aria-label=\"Área de documentos\">\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"true\"[^>]*>Gerais · 0</button>\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Contratos de honorários · 0</button>\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>De atividades · 0</button>\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Do cliente · 1</button>")
            .And.Contain("Nenhum documento.");
    }

    [Fact]
    public async Task Diretoria_em_gerais_ve_a_tabela_com_as_acoes_o_acesso_e_a_ajuda()
    {
        var geral = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, _maria, null, "procuracao-geral.pdf", "application/pdf", Conteudo()));
        geral.Sucesso.Should().BeTrue(geral.Mensagem);

        var html = await FichaAsync(_admin, podeGerenciar: true);

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().Contain("<div class=\"aj-tabela-caixa aj-tabela-caixa--plana\">")
            .And.MatchRegex($"<td class=\"aj-tabela__principal\"><a href=\"/documentos/{geral.Valor}/baixar\" download data-enhance-nav=\"false\">procuracao-geral.pdf</a></td>")
            .And.Contain("<td>Diana Diretora</td>")
            .And.MatchRegex("<td class=\"aj-tabela__acao aj-tabela__acao--varias\"><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Renomear</button><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Acesso</button><button type=\"button\" class=\"aj-botao aj-botao--perigo aj-botao--menor\"[^>]*>Excluir</button></td>")
            .And.Contain("A Diretoria acessa sempre. Em “Acesso” você escolhe os perfis e os usuários que também veem cada documento geral.");
        var linhas = BlocoDeVisao.De(html, "aj-tablet-mobile");
        linhas.Should().MatchRegex($"(?s)<div class=\"aj-linhas\">\\s*<div class=\"aj-linha\" id=\"documento-{geral.Valor:N}\">\\s*<div class=\"aj-linha__texto\">\\s*<a href=\"/documentos/{geral.Valor}/baixar\" download data-enhance-nav=\"false\" class=\"aj-linha__titulo\">procuracao-geral.pdf</a>")
            .And.Contain("aria-label=\"Ações de procuracao-geral.pdf\"").And.Contain(">Acesso</button>");
    }

    [Fact]
    public async Task Producao_ve_so_gerais_e_de_atividades_sem_envio_nem_acoes()
    {
        var pedro = await _app.CriarPedroAsync();
        // Um documento geral nasce aberto a todos os perfis ativos (Documento.CriarGeral): a Produção o vê sem
        // DefinirAcessoDoDocumento.
        var geral = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, _maria, null, "geral-do-pedro.pdf", "application/pdf", Conteudo()));
        geral.Sucesso.Should().BeTrue(geral.Mensagem);

        var html = await FichaAsync(pedro, podeGerenciar: false);

        html.Should().Contain(">Gerais · 1</button>").And.Contain(">De atividades · 0</button>").And.Contain("geral-do-pedro.pdf");
        html.Should().NotContain("Contratos de honorários").And.NotContain("Do cliente ·").And.NotContain("Enviar documento")
            .And.NotContain(">Renomear</button>").And.NotContain(">Excluir</button>").And.NotContain("aj-menu-acoes")
            .And.NotContain("Seu acesso foi encerrado");
    }
}
