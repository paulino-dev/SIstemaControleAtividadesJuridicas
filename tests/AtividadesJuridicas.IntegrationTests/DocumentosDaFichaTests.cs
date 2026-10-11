using System.Net;
using System.Text;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Documentos;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A lista de documentos da ficha do cliente, que só abre por clique e ainda tem a marcação antiga. Fixa o que
/// ela desenha, para a lógica poder mudar de arquivo sem mudar a tela.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public class DocumentosDaFichaTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _maria, _documento;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        (_, _maria) = await _app.CriarCarlaAsync();
        var enviado = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoDoCliente, _maria, null, "procuracao.pdf", "application/pdf",
            new MemoryStream(Encoding.UTF8.GetBytes("conteúdo"))));
        enviado.Sucesso.Should().BeTrue(enviado.Mensagem);
        _documento = enviado.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private static MemoryStream Conteudo() => new(Encoding.UTF8.GetBytes("conteúdo"));

    private async Task<string> FichaAsync(bool podeGerenciar,
        CategoriaDocumento categoria = CategoriaDocumento.DocumentoDoCliente, string titulo = "Documentos do cliente") =>
        WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<DocumentosDoCliente>(_app, _admin, new()
        {
            ["ClienteId"] = _maria, ["Categoria"] = categoria, ["Titulo"] = titulo, ["PodeGerenciar"] = podeGerenciar,
        }));

    [Fact]
    public async Task Ficha_lista_o_documento_com_o_autor_as_acoes_e_o_envio()
    {
        var html = await FichaAsync(podeGerenciar: true);

        html.Should().Contain("<h3>Documentos do cliente</h3>")
            .And.Contain($"href=\"/documentos/{_documento}/baixar\"").And.Contain("procuracao.pdf")
            .And.Contain("<th>Por</th>").And.Contain("<td>Diana Diretora</td>")
            .And.Contain(">Renomear</button>").And.Contain(">Excluir</button>")
            .And.Contain("Enviar documento").And.Contain("Arquivo (até 50 MB)").And.Contain("Nome (opcional)");
        // Documento do cliente não pede confirmação de envio nem tem editor de acesso. O editor é dos Documentos
        // Gerais: o teste de lá é o controle positivo.
        html.Should().NotContain("Confirmar envio").And.NotContain(">Acesso</button>").And.NotContain("Carregando");
    }

    [Fact]
    public async Task Ficha_de_documentos_gerais_com_gestao_tem_o_editor_de_acesso()
    {
        var geral = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, _maria, null, "parecer.pdf", "application/pdf", Conteudo()));
        geral.Sucesso.Should().BeTrue(geral.Mensagem);

        var html = await FichaAsync(podeGerenciar: true, CategoriaDocumento.DocumentoGeral, "Documentos Gerais");

        html.Should().Contain("<h3>Documentos Gerais</h3>")
            .And.Contain($"href=\"/documentos/{geral.Valor}/baixar\"").And.Contain("parecer.pdf")
            .And.Contain(">Renomear</button>").And.Contain(">Acesso</button>").And.Contain(">Excluir</button>");
    }

    [Fact]
    public async Task Ficha_de_documentos_de_atividades_tem_a_coluna_da_atividade_e_nao_tem_envio()
    {
        var atividade = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Contestação trabalhista", "Prazo de 15 dias.", new DateOnly(2090, 6, 15), _maria, null, null, 1));
        atividade.Sucesso.Should().BeTrue(atividade.Mensagem);
        var anexado = await _app.ExecutarAsync(_admin, new EnviarDocumentoDaAtividade(
            atividade.Valor, null, "peticao.pdf", "application/pdf", Conteudo()));
        anexado.Sucesso.Should().BeTrue(anexado.Mensagem);

        var html = await FichaAsync(podeGerenciar: true, CategoriaDocumento.DocumentoAtividade, "Documentos de Atividades");

        html.Should().Contain("<h3>Documentos de Atividades</h3>")
            .And.Contain($"href=\"/documentos/{anexado.Valor}/baixar\"").And.Contain("peticao.pdf")
            .And.Contain("<th>Atividade</th>").And.Contain("<td>Contestação trabalhista</td>")
            .And.Contain(">Renomear</button>").And.Contain(">Excluir</button>");
        // Documento de atividade só nasce na ficha da atividade: mesmo com gestão, aqui não há envio.
        html.Should().NotContain("Enviar documento").And.NotContain("Arquivo (até");
    }

    [Fact]
    public async Task Ficha_sem_gestao_so_lista()
    {
        var html = await FichaAsync(podeGerenciar: false);

        html.Should().Contain("procuracao.pdf").And.Contain($"href=\"/documentos/{_documento}/baixar\"");
        html.Should().NotContain("Renomear").And.NotContain("Excluir").And.NotContain("Enviar documento");
    }
}
