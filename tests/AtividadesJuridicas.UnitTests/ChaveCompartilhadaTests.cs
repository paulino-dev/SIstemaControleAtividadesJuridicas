using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// O índice de <c>ChaveArmazenamento</c> deixou de ser único: um arquivo pode servir a mais de um documento. A garantia que
/// ele dava agora é destes dois fatos de código, fixados aqui: só um caminho cria documento com a chave de outro, e todo
/// outro envio grava um arquivo novo, com chave nova (spec das solicitações do cliente, seção 3).
/// </summary>
public partial class ChaveCompartilhadaTests
{
    private const string OQueFazer =
        "Um caminho novo que compartilha a chave de armazenamento precisa antes ser registrado na spec das solicitações " +
        "do cliente (seção 3, regras que substituem o índice único), com a regra de remoção do arquivo físico que ele " +
        "exige. Só depois acrescente o arquivo à lista deste teste.";

    [GeneratedRegex(@"\.LevarParaAtividade\s*\(")]
    private static partial Regex LevaDocumentoParaAtividade();

    [GeneratedRegex(@"\bnew\s+ArquivoDoDocumento\s*\(")]
    private static partial Regex MontaArquivoDoDocumento();

    [Fact]
    public void So_CriarAtividadeDaSolicitacao_leva_documento_para_atividade()
    {
        // Documento.cs declara o método e não o chama; fica de fora da conta.
        var encontrados = ArquivosDeSrcQueContem(LevaDocumentoParaAtividade(), exceto: "Documento.cs");

        encontrados.Should().BeEquivalentTo(["CriarAtividadeDaSolicitacao.cs"], OQueFazer);
    }

    [Fact]
    public void Arquivo_de_documento_so_e_montado_com_chave_nova_ou_pelo_proprio_documento()
    {
        // ArquivoEnviado monta o arquivo recém-gravado (chave nova); Documento monta o do documento que se leva.
        var encontrados = ArquivosDeSrcQueContem(MontaArquivoDoDocumento());

        encontrados.Should().BeEquivalentTo(["ArquivoEnviado.cs", "Documento.cs"], OQueFazer);
    }

    private static List<string> ArquivosDeSrcQueContem(Regex padrao, string? exceto = null)
    {
        var separadores = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var src = Path.Combine(GravacoesPorForaTests.RaizDoRepositorio(), "src");

        return [.. Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories)
            .Where(caminho => Path.GetExtension(caminho) is ".cs" or ".razor")
            .Where(caminho =>
            {
                var partes = caminho.Split(separadores);
                return !partes.Contains("obj") && !partes.Contains("bin") && !partes.Contains("Migrations");
            })
            .Where(caminho => Path.GetFileName(caminho) != exceto)
            .Where(caminho => padrao.IsMatch(File.ReadAllText(caminho)))
            .Select(caminho => Path.GetFileName(caminho))
            .Distinct()
            .Order()];
    }
}
