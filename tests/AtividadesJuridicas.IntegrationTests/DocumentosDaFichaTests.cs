using System.Net;
using System.Text;
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

    private async Task<string> FichaAsync(bool podeGerenciar) => WebUtility.HtmlDecode(
        await TelaDeTeste.RenderizarAsync<DocumentosDoCliente>(_app, _admin, new()
        {
            ["ClienteId"] = _maria, ["Categoria"] = CategoriaDocumento.DocumentoDoCliente,
            ["Titulo"] = "Documentos do cliente", ["PodeGerenciar"] = podeGerenciar,
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
        // Documento do cliente não pede confirmação de envio nem tem editor de acesso.
        html.Should().NotContain("Confirmar envio").And.NotContain(">Acesso</button>").And.NotContain("Carregando");
    }

    [Fact]
    public async Task Ficha_sem_gestao_so_lista()
    {
        var html = await FichaAsync(podeGerenciar: false);

        html.Should().Contain("procuracao.pdf").And.Contain($"href=\"/documentos/{_documento}/baixar\"");
        html.Should().NotContain("Renomear").And.NotContain("Excluir").And.NotContain("Enviar documento");
    }
}
