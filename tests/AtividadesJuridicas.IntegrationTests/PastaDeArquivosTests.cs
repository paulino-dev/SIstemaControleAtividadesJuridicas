using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

public class PastaDeArquivosTests
{
    private static readonly string Raiz = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "raiz-do-conteudo"));
    private static readonly string Absoluta = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "arquivos-do-escritorio"));

    [Fact]
    public void Desenvolvimento_com_caminho_relativo_fica_sob_a_raiz_do_conteudo()
    {
        PastaDeArquivos.Resolver("dados/arquivos", desenvolvimento: true, Raiz)
            .Should().Be(Path.GetFullPath(Path.Combine(Raiz, "dados", "arquivos")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Desenvolvimento_sem_valor_e_recusado(string? configurado)
    {
        var resolver = () => PastaDeArquivos.Resolver(configurado, desenvolvimento: true, Raiz);

        resolver.Should().Throw<InvalidOperationException>().WithMessage("*Armazenamento:Pasta*");
    }

    [Fact]
    public void Fora_do_desenvolvimento_caminho_relativo_e_recusado()
    {
        var resolver = () => PastaDeArquivos.Resolver("dados/arquivos", desenvolvimento: false, Raiz);

        resolver.Should().Throw<InvalidOperationException>().WithMessage("*Armazenamento:Pasta*caminho absoluto*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Fora_do_desenvolvimento_sem_valor_e_recusado(string? configurado)
    {
        var resolver = () => PastaDeArquivos.Resolver(configurado, desenvolvimento: false, Raiz);

        resolver.Should().Throw<InvalidOperationException>().WithMessage("*Armazenamento:Pasta*");
    }

    [Fact]
    public void Fora_do_desenvolvimento_caminho_absoluto_e_usado_como_esta()
    {
        PastaDeArquivos.Resolver(Absoluta, desenvolvimento: false, Raiz).Should().Be(Absoluta);
    }
}
