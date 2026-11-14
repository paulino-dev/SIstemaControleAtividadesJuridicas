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

    // As formas de montar o arquivo: `new ArquivoDoDocumento(...)` e o construtor por tipo-alvo, `ArquivoDoDocumento x = new(...)`
    // (também como membro de expressão, `ArquivoDoDocumento x => new(...)`, e anulável).
    [GeneratedRegex(@"\bnew\s+ArquivoDoDocumento\s*\(|\bArquivoDoDocumento\??\s+\w+\s*=>?\s*new\s*\(")]
    private static partial Regex MontaArquivoDoDocumento();

    // O arquivo do domínio, pelo caminho relativo a src: só ele fica de fora da primeira conta, e não qualquer outro Documento.cs.
    private static readonly string DocumentoDoDominio = Path.Combine("AtividadesJuridicas.Domain", "Documentos", "Documento.cs");

    [Fact]
    public void So_CriarAtividadeDaSolicitacao_leva_documento_para_atividade()
    {
        // O Documento.cs do domínio declara o método e não o chama; fica de fora da conta. Outro arquivo com o mesmo nome,
        // em outra pasta, não fica: vale o caminho, e não o nome.
        var encontrados = ArquivosDeSrcQueContem(LevaDocumentoParaAtividade(), exceto: DocumentoDoDominio);

        encontrados.Should().BeEquivalentTo(["CriarAtividadeDaSolicitacao.cs"], OQueFazer);
    }

    [Fact]
    public void Arquivo_de_documento_so_e_montado_com_chave_nova_ou_pelo_proprio_documento()
    {
        // ArquivoEnviado monta o arquivo recém-gravado (chave nova); Documento monta o do documento que se leva.
        var encontrados = ArquivosDeSrcQueContem(MontaArquivoDoDocumento());

        encontrados.Should().BeEquivalentTo(["ArquivoEnviado.cs", "Documento.cs"], OQueFazer);
    }

    [Theory]
    [InlineData("var arquivo = new ArquivoDoDocumento(nome, tipo, 1, hash, chave);")]
    [InlineData("return new  ArquivoDoDocumento (nome, tipo, 1, hash, chave);")]
    [InlineData("ArquivoDoDocumento arquivo = new(nome, tipo, 1, hash, chave);")]
    [InlineData("ArquivoDoDocumento arquivo=new (nome, tipo, 1, hash, chave);")]
    [InlineData("private ArquivoDoDocumento? _arquivo = new(nome, tipo, 1, hash, chave);")]
    [InlineData("ArquivoDoDocumento Arquivo => new(nome, tipo, 1, hash, chave);")]
    public void A_varredura_do_arquivo_pega_o_construtor_com_e_sem_o_nome_do_tipo(string linha) =>
        MontaArquivoDoDocumento().IsMatch(linha).Should().BeTrue();

    [Theory]
    [InlineData("public sealed record ArquivoDoDocumento(string Nome, string Tipo);")]
    [InlineData("Func<ArquivoDoDocumento, Task<Documento>> criar,")]
    [InlineData("var gravados = new List<ArquivoDoDocumento>(arquivos.Count);")]
    [InlineData("List<ArquivoDoDocumento> gravados = new();")]
    [InlineData("static Documento Criar(Guid id, ArquivoDoDocumento arquivo) => Montar(arquivo);")]
    public void A_varredura_do_arquivo_nao_pega_o_que_nao_monta_o_arquivo(string linha) =>
        MontaArquivoDoDocumento().IsMatch(linha).Should().BeFalse();

    /// <param name="exceto">Um arquivo de src, pelo caminho relativo a ela, que fica de fora da conta.</param>
    private static List<string> ArquivosDeSrcQueContem(Regex padrao, string? exceto = null)
    {
        var separadores = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var src = Path.Combine(GravacoesPorForaTests.RaizDoRepositorio(), "src");
        var excluido = exceto is null ? null : Path.Combine(src, exceto);

        return [.. Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories)
            .Where(caminho => Path.GetExtension(caminho) is ".cs" or ".razor")
            .Where(caminho =>
            {
                var partes = caminho.Split(separadores);
                return !partes.Contains("obj") && !partes.Contains("bin") && !partes.Contains("Migrations");
            })
            .Where(caminho => !string.Equals(caminho, excluido, StringComparison.OrdinalIgnoreCase))
            .Where(caminho => padrao.IsMatch(File.ReadAllText(caminho)))
            .Select(caminho => Path.GetFileName(caminho))
            .Distinct()
            .Order()];
    }
}
