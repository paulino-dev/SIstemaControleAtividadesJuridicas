using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// Tela migrada para o design system não volta atrás. A trava recusa estilo embutido (atributo <c>style</c> e bloco
/// <c>&lt;style&gt;</c>), arquivo <c>.razor.css</c> ao lado da tela, <c>button</c>, <c>table</c>, <c>input</c>,
/// <c>select</c> e <c>textarea</c> sem classe (o <c>app.css</c> os vestiria) e classe fora do prefixo <c>aj-</c>,
/// em <c>class=</c> e em <c>Classe=</c> de componente, separada por qualquer espaço em branco. Cada fase da
/// migração acrescenta os seus arquivos a <see cref="Migradas"/>.
/// Pontos cegos, conferidos na revisão: classe montada por expressão Razor (com <c>@</c>) e marcação dentro de
/// comentário <c>@* *@</c>.
/// </summary>
public partial class TelasNoDesignSystemTests
{
    // Caminhos a partir de src/AtividadesJuridicas.Web.
    private static readonly string[] Migradas =
    [
        "Components/Documentos/DocumentosComunsDaDiretoria.razor",
        "Components/Documentos/DocumentosParaBaixar.razor",
        "Components/Layout/Abas.razor",
        "Components/Layout/Aviso.razor",
        "Components/Layout/BarraDeVoltar.razor",
        "Components/Layout/CabecalhoDeDetalhe.razor",
        "Components/Layout/CabecalhoDeFicha.razor",
        "Components/Layout/CabecalhoDePagina.razor",
        "Components/Layout/CartaoAtalho.razor",
        "Components/Layout/EstadoDaLista.razor",
        "Components/Layout/Icone.razor",
        "Components/Layout/MainLayout.razor",
        "Components/Layout/MenuDeAcoes.razor",
        "Components/Layout/Paginacao.razor",
        "Components/Layout/RotuloDeCampo.razor",
        "Components/Pages/Andamento.razor",
        "Components/Pages/Areas.razor",
        "Components/Pages/Auditoria.razor",
        "Components/Pages/Clientes.razor",
        "Components/Pages/Conta/Entrar.razor",
        "Components/Pages/DocumentosComuns.razor",
        "Components/Pages/Erro.razor",
        "Components/Pages/Home.razor",
        "Components/Pages/MeusDocumentos.razor",
        "Components/Pages/MinhaSenha.razor",
        "Components/Pages/NaoEncontrada.razor",
        "Components/Pages/Perfis.razor",
        "Components/Pages/Produtores.razor",
        "Components/Pages/UsuarioFormulario.razor",
        "Components/Pages/Usuarios.razor",
        "Components/Produtores/PainelDeProdutividade.razor",
        "Components/Usuarios/ConfirmacaoDeInativacao.razor",
    ];

    [GeneratedRegex("\\sstyle\\s*=\\s*[\"']")]
    private static partial Regex EstiloEmbutido();

    [GeneratedRegex("<style\\b", RegexOptions.IgnoreCase)]
    private static partial Regex FolhaEmbutida();

    // Só o valor fixo: classe montada por expressão Razor (com @) é conferida na revisão.
    [GeneratedRegex("\\s(?:class|Classe)\\s*=\\s*(?:\"([^\"@]*)\"|'([^'@]*)')")]
    private static partial Regex ClasseFixa();

    // A marca inteira de um elemento que o app.css veste quando está sem classe. Os valores entre aspas
    // são consumidos inteiros, para o "=>" de uma expressão não encerrar a marca.
    [GeneratedRegex("<(button|table|input|select|textarea)\\b((?:[^>\"']|\"[^\"]*\"|'[^']*')*)>")]
    private static partial Regex ElementoQueOAppCssVeste();

    [GeneratedRegex("\\sclass\\s*=")]
    private static partial Regex TemClasse();

    /// <summary>O que a marcação tem de fora do design system.</summary>
    private static List<string> Violacoes(string marcacao)
    {
        var violacoes = new List<string>();
        if (EstiloEmbutido().IsMatch(marcacao)) violacoes.Add("style");
        if (FolhaEmbutida().IsMatch(marcacao)) violacoes.Add("<style>");

        violacoes.AddRange(ElementoQueOAppCssVeste().Matches(marcacao)
            .Where(m => !TemClasse().IsMatch(m.Groups[2].Value))
            .Select(m => $"<{m.Groups[1].Value}> sem classe"));

        violacoes.AddRange(ClasseFixa().Matches(marcacao)
            .SelectMany(m => (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Where(classe => !classe.StartsWith("aj-", StringComparison.Ordinal)));
        return violacoes;
    }

    public static TheoryData<string> Arquivos => new(Migradas);

    [Theory]
    [MemberData(nameof(Arquivos))]
    public void Tela_migrada_nao_tem_estilo_embutido_nem_classe_fora_do_design_system(string arquivo)
    {
        var caminho = Path.Combine(
            [GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web", .. arquivo.Split('/')]);

        Violacoes(File.ReadAllText(caminho)).Should().BeEmpty(
            "tela migrada não tem estilo embutido nem classe fora do design system: o padrão vira classe aj- lá");
        File.Exists(caminho + ".css").Should().BeFalse("tela migrada não tem CSS próprio: o estilo vem do design system");
    }

    // A própria trava: o que ela tem de reprovar e o que tem de deixar passar.
    [Theory]
    [InlineData("<p style=\"margin:0\">x</p>", "style")]
    [InlineData("<p style='margin:0'>x</p>", "style")]
    [InlineData("<p class=\"erro\">x</p>", "erro")]
    [InlineData("<p class='aj-aviso erro'>x</p>", "erro")]
    [InlineData("<div class=\"aj-painel formulario\"></div>", "formulario")]
    public void A_trava_reprova_estilo_embutido_e_classe_de_fora(string marcacao, string violacao)
    {
        Violacoes(marcacao).Should().Equal(violacao);
    }

    [Theory]
    [InlineData("<style>p{margin:0}</style>", "<style>")]
    [InlineData("<button type=\"button\">x</button>", "<button> sem classe")]
    [InlineData("<button type=\"button\" @onclick=\"() => Abrir(x)\">x</button>", "<button> sem classe")]
    [InlineData("<table><tr><td>x</td></tr></table>", "<table> sem classe")]
    [InlineData("<input type=\"text\" @bind=\"_nome\" />", "<input> sem classe")]
    [InlineData("<select @bind=\"_x\"></select>", "<select> sem classe")]
    [InlineData("<textarea rows=\"3\"></textarea>", "<textarea> sem classe")]
    [InlineData("<Icone Nome=\"avancar\" Classe=\"seta\" />", "seta")]
    [InlineData("<p class=\"aj-aviso\terro\">x</p>", "erro")]
    [InlineData("<p class=\"aj-aviso\nerro\">x</p>", "erro")]
    public void A_trava_reprova_o_que_volta_a_marcacao_antiga(string marcacao, string violacao)
    {
        Violacoes(marcacao).Should().Equal(violacao);
    }

    [Theory]
    [InlineData("<p class=\"aj-aviso aj-aviso--erro\">x</p>")]
    [InlineData("<label class=\"@(erro ? \"aj-rotulo aj-rotulo--erro\" : \"aj-rotulo\")\">x</label>")]
    [InlineData("<Icone Nome=\"voltar\" Classe=\"aj-cartao__seta\" />")]
    public void A_trava_aceita_marcacao_do_design_system(string marcacao)
    {
        Violacoes(marcacao).Should().BeEmpty();
    }

    [Theory]
    [InlineData("<button type=\"button\" class=\"aj-botao\" @onclick=\"() => Abrir(x)\">x</button>")]
    [InlineData("<button type=\"button\" @onclick=\"() => Abrir(x)\" class=\"aj-botao\">x</button>")]
    [InlineData("<table class=\"aj-tabela\"><tr><td>x</td></tr></table>")]
    [InlineData("<input type=\"text\" class=\"aj-campo\" @bind=\"_nome\" />")]
    [InlineData("<InputText @bind-Value=\"_x\" class=\"aj-campo\" />")]
    [InlineData("<InputFile OnChange=\"Escolher\" class=\"aj-campo\" />")]
    [InlineData("<button type=\"button\" class=\"@Classe\">x</button>")]
    public void A_trava_aceita_elemento_com_classe_do_design_system(string marcacao)
    {
        Violacoes(marcacao).Should().BeEmpty();
    }
}
