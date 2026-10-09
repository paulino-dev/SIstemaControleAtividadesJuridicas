using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// ExecuteUpdate, ExecuteDelete e SQL bruto não passam pelos interceptores: não carimbam, não fazem
/// exclusão lógica e não geram registro de auditoria. Cada uso precisa ser uma decisão consciente.
/// </summary>
public partial class GravacoesPorForaTests
{
    // Arquivo → motivo pelo qual a gravação por fora é aceita.
    private static readonly Dictionary<string, string> Permitidos = new()
    {
        ["UsuarioRepositorio.cs"] = "estado de login (falhas, bloqueio, último acesso): não é alteração de negócio",
        ["AcessoInicialDePerfil.cs"] = "perfil novo incluído em todos os Documentos Gerais num só comando: " +
                                      "registrado pelo evento perfil_incluido_nos_documentos_gerais",
    };

    [GeneratedRegex(@"\b(ExecuteUpdate|ExecuteDelete|ExecuteSql|SqlQuery|FromSql)\w*\s*[<(]")]
    private static partial Regex GravacaoPorFora();

    [Fact]
    public void So_os_arquivos_permitidos_gravam_sem_passar_pelos_interceptores()
    {
        var raiz = RaizDoRepositorio();
        var separadores = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        var encontrados = Directory.EnumerateFiles(Path.Combine(raiz, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(caminho =>
            {
                var partes = caminho.Split(separadores);
                return !partes.Contains("obj") && !partes.Contains("bin") && !partes.Contains("Migrations");
            })
            .Where(caminho => GravacaoPorFora().IsMatch(File.ReadAllText(caminho)))
            .Select(Path.GetFileName)
            .Order()
            .ToList();

        encontrados.Should().BeEquivalentTo(Permitidos.Keys,
            "um arquivo novo grava no banco sem passar pelos interceptores de carimbos e de auditoria. " +
            "Se isso for intencional, acrescente o arquivo e o motivo a Permitidos e registre o fato por um " +
            "evento de auditoria; caso contrário, grave pelo DbContext");
    }

    internal static string RaizDoRepositorio()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "Directory.Build.props")))
            pasta = pasta.Parent;
        return pasta?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
