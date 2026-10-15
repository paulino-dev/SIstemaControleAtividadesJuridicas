using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Documentos;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace AtividadesJuridicas.IntegrationTests;

public class GanchosDaGestaoDeDocumentosTests : TelaInterativa
{
    [Fact]
    public void Lista_de_outra_origem_le_e_envia_pelos_ganchos_sem_cliente()
    {
        var tela = Desenhar<ListaPelosGanchos>();

        tela.Find("li").TextContent.Should().Be("minuta-acordo-v2.docx");
        tela.Instance.PaginasLidas.Should().Equal(1);

        tela.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", "calculo-verbas.xlsx"));
        tela.Find("button").Click();

        tela.WaitForAssertion(() => tela.Find("p").TextContent.Should().Be("Documento enviado."));
        tela.Instance.ArquivosEnviados.Should().Equal("calculo-verbas.xlsx");
        tela.Instance.PaginasLidas.Should().Equal(1, 1);   // a lista é relida depois do envio
        Despachante.Recebidas.Should().BeEmpty();          // nada pelos casos de uso de comuns ou de cliente
    }

    /// <summary>Uma lista de documentos de outra origem: a categoria de atividade, sem cliente, lida e enviada pelos ganchos.</summary>
    public sealed class ListaPelosGanchos : GestaoDeDocumentos
    {
        private static readonly Guid Minuta = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1");

        public List<int> PaginasLidas { get; } = [];
        public List<string> ArquivosEnviados { get; } = [];

        protected override CategoriaDocumento CategoriaDosDocumentos => CategoriaDocumento.DocumentoAtividade;
        protected override Guid? ClienteDosDocumentos => null;
        protected override ValueTask FocarAvisoDeEnvioAsync() => ValueTask.CompletedTask;

        protected override Task<Resultado<Pagina<DocumentoResumo>>> ListarPaginaAsync(int pagina)
        {
            PaginasLidas.Add(pagina);
            return Task.FromResult(Resultado<Pagina<DocumentoResumo>>.Ok(new Pagina<DocumentoResumo>(
                [new DocumentoResumo(Minuta, "minuta-acordo-v2.docx", "minuta-acordo-v2.docx", 84 * 1024,
                    CategoriaDocumento.DocumentoAtividade, new DateTime(2026, 10, 7, 19, 40, 0, DateTimeKind.Utc),
                    "Beatriz Lins", Guid.NewGuid(), "Minuta de acordo trabalhista")],
                1, pagina, 20)));
        }

        protected override Task<Resultado<Guid>> EnviarArquivoAsync(
            string? nome, string nomeArquivo, string? tipoConteudo, Stream conteudo)
        {
            ArquivosEnviados.Add(nomeArquivo);
            return Task.FromResult(Resultado<Guid>.Ok(Guid.NewGuid()));
        }

        protected override void BuildRenderTree(RenderTreeBuilder construtor)
        {
            construtor.OpenElement(0, "ul");
            foreach (var linha in _pagina.Itens)
            {
                construtor.OpenElement(1, "li");
                construtor.AddContent(2, linha.Nome);
                construtor.CloseElement();
            }
            construtor.CloseElement();
            construtor.OpenComponent<InputFile>(3);
            construtor.AddComponentParameter(4, nameof(InputFile.OnChange),
                EventCallback.Factory.Create<InputFileChangeEventArgs>(this, Escolher));
            construtor.CloseComponent();
            construtor.OpenElement(5, "button");
            construtor.AddAttribute(6, "type", "button");
            construtor.AddAttribute(7, "onclick", EventCallback.Factory.Create(this, EnviarAsync));
            construtor.AddContent(8, "Enviar");
            construtor.CloseElement();
            if (_mensagem is not null)
            {
                construtor.OpenElement(9, "p");
                construtor.AddContent(10, _mensagem);
                construtor.CloseElement();
            }
        }
    }
}
