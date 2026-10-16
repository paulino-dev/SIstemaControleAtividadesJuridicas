using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// Tela migrada para o design system não volta atrás. A trava recusa estilo embutido (atributo <c>style</c> e bloco
/// <c>&lt;style&gt;</c>), arquivo <c>.razor.css</c> ao lado da tela, <c>button</c>, <c>table</c>, <c>input</c>,
/// <c>select</c> e <c>textarea</c> sem classe (o navegador os desenharia com o estilo padrão) e classe fora do prefixo
/// <c>aj-</c>, em <c>class=</c> e em <c>Classe=</c> de componente, separada por qualquer espaço em branco. Todo
/// <c>.razor</c> da Web, em qualquer pasta, está em <see cref="Migradas"/> ou em <see cref="Infraestrutura"/>; só a
/// infraestrutura usa as classes que o framework exige (<see cref="ClassesDoFramework"/>) e só o aviso de reconexão tem
/// <c>.razor.css</c>.
/// Pontos cegos, conferidos na revisão: classe montada por expressão Razor (com <c>@</c>) e marcação dentro de
/// comentário <c>@* *@</c>.
/// </summary>
public partial class TelasNoDesignSystemTests
{
    // Caminhos a partir de src/AtividadesJuridicas.Web.
    private static readonly string[] Migradas =
    [
        "Components/Agenda/AvisoDeEventoSalvo.razor",
        "Components/Agenda/CartaoDeEvento.razor",
        "Components/Agenda/DiaDaAgenda.razor",
        "Components/Agenda/FaixaDeDias.razor",
        "Components/Agenda/GradeCompactaDoMes.razor",
        "Components/Agenda/ListaDoDia.razor",
        "Components/Agenda/MesDaAgenda.razor",
        "Components/Agenda/PontosDoDia.razor",
        "Components/Agenda/SecaoDoDia.razor",
        "Components/Agenda/SemanaDaAgenda.razor",
        "Components/Atividades/CabecalhoDaAtividade.razor",
        "Components/Atividades/ComentariosDaAtividade.razor",
        "Components/Atividades/DocumentosDaAtividade.razor",
        "Components/Atividades/EtiquetaDeSituacao.razor",
        "Components/Atividades/MotivoComResponsaveis.razor",
        "Components/Documentos/DocumentosComunsDaDiretoria.razor",
        "Components/Documentos/DocumentosDaFicha.razor",
        "Components/Documentos/DocumentosParaBaixar.razor",
        "Components/Layout/Abas.razor",
        "Components/Layout/Aviso.razor",
        "Components/Layout/BarraDeVoltar.razor",
        "Components/Layout/CabecalhoDeDetalhe.razor",
        "Components/Layout/CabecalhoDeFicha.razor",
        "Components/Layout/CabecalhoDePagina.razor",
        "Components/Layout/CargaDoFormulario.razor",
        "Components/Layout/CartaoAtalho.razor",
        "Components/Layout/EstadoDaLista.razor",
        "Components/Layout/FocoNoTitulo.razor",
        "Components/Layout/Icone.razor",
        "Components/Layout/LayoutVazio.razor",
        "Components/Layout/MainLayout.razor",
        "Components/Layout/MenuDeAcoes.razor",
        "Components/Layout/Paginacao.razor",
        "Components/Layout/PainelDeFiltros.razor",
        "Components/Layout/RotuloDeCampo.razor",
        "Components/Pages/Agenda.razor",
        "Components/Pages/Andamento.razor",
        "Components/Pages/Areas.razor",
        "Components/Pages/Atividades.razor",
        "Components/Pages/AtividadeFicha.razor",
        "Components/Pages/AtividadeFormulario.razor",
        "Components/Pages/AtividadeDevolver.razor",
        "Components/Pages/AtividadeReabrir.razor",
        "Components/Pages/AtividadeResponsaveis.razor",
        "Components/Pages/Auditoria.razor",
        "Components/Pages/Clientes.razor",
        "Components/Pages/ClienteFicha.razor",
        "Components/Pages/ClienteFormulario.razor",
        "Components/Pages/Conta/Entrar.razor",
        "Components/Pages/DocumentosComuns.razor",
        "Components/Pages/Erro.razor",
        "Components/Pages/EventoFicha.razor",
        "Components/Pages/EventoFormulario.razor",
        "Components/Pages/Home.razor",
        "Components/Pages/MeusDocumentos.razor",
        "Components/Pages/MinhaSenha.razor",
        "Components/Pages/NaoEncontrada.razor",
        "Components/Pages/Perfis.razor",
        "Components/Pages/ProcessoFicha.razor",
        "Components/Pages/ProcessoFormulario.razor",
        "Components/Pages/Processos.razor",
        "Components/Pages/Produtores.razor",
        "Components/Pages/UsuarioFormulario.razor",
        "Components/Pages/Usuarios.razor",
        "Components/Produtores/PainelDeProdutividade.razor",
        "Components/Routes.razor",
        "Components/Usuarios/ConfirmacaoDeInativacao.razor",
        "Components/_Imports.razor",
    ];

    // O documento e o aviso de reconexão usam classes que o Blazor exige: o blazor.web.js procura .reload e .dismiss na
    // faixa de erro, e o modelo de reconexão mostra cada mensagem pela sua classe components-…. O aviso de reconexão é o
    // único .razor com CSS próprio (exceção da spec).
    private static readonly string[] Infraestrutura =
    [
        "Components/App.razor",
        "Components/Layout/ReconnectModal.razor",
    ];

    // As classes fora do aj- que o framework exige, uma a uma (lista fechada: um nome novo é decisão consciente).
    private static readonly HashSet<string> ClassesDoFramework =
    [
        "reload",
        "dismiss",
        "components-reconnect-container",
        "components-reconnect-first-attempt-visible",
        "components-reconnect-repeated-attempt-visible",
        "components-reconnect-failed-visible",
        "components-pause-visible",
        "components-resume-failed-visible",
    ];

    [GeneratedRegex("\\sstyle\\s*=\\s*[\"']")]
    private static partial Regex EstiloEmbutido();

    [GeneratedRegex("<style\\b", RegexOptions.IgnoreCase)]
    private static partial Regex FolhaEmbutida();

    // Só o valor fixo: classe montada por expressão Razor (com @) é conferida na revisão.
    [GeneratedRegex("\\s(?:class|Classe)\\s*=\\s*(?:\"([^\"@]*)\"|'([^'@]*)')")]
    private static partial Regex ClasseFixa();

    // A marca inteira de um elemento que, sem classe, o navegador desenharia com o estilo padrão. Os valores entre aspas
    // são consumidos inteiros, para o "=>" de uma expressão não encerrar a marca.
    [GeneratedRegex("<(button|table|input|select|textarea)\\b((?:[^>\"']|\"[^\"]*\"|'[^']*')*)>")]
    private static partial Regex ElementoQueExigeClasse();

    [GeneratedRegex("\\sclass\\s*=")]
    private static partial Regex TemClasse();

    /// <summary>
    /// O que a marcação tem de fora do design system. <paramref name="aceitas"/>: as classes fora do <c>aj-</c> que o
    /// arquivo pode usar (só a infraestrutura passa as do framework; as telas, nenhuma).
    /// </summary>
    private static List<string> Violacoes(string marcacao, IReadOnlySet<string>? aceitas = null)
    {
        var violacoes = new List<string>();
        if (EstiloEmbutido().IsMatch(marcacao)) violacoes.Add("style");
        if (FolhaEmbutida().IsMatch(marcacao)) violacoes.Add("<style>");

        violacoes.AddRange(ElementoQueExigeClasse().Matches(marcacao)
            .Where(m => !TemClasse().IsMatch(m.Groups[2].Value))
            .Select(m => $"<{m.Groups[1].Value}> sem classe"));

        violacoes.AddRange(ClasseFixa().Matches(marcacao)
            .SelectMany(m => (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Where(classe => !classe.StartsWith("aj-", StringComparison.Ordinal) && aceitas?.Contains(classe) != true));
        return violacoes;
    }

    /// <summary>Os <c>.razor</c> que não estão em nenhuma das duas listas.</summary>
    private static List<string> ForaDaTrava(IEnumerable<string> arquivos) =>
        arquivos.Except(Migradas).Except(Infraestrutura).ToList();

    private static string Web() => Path.Combine(GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web");

    private static string Caminho(string arquivo) => Path.Combine([Web(), .. arquivo.Split('/')]);

    // Os arquivos de Components com a extensão pedida, em qualquer pasta, a partir de src/AtividadesJuridicas.Web.
    private static List<string> ArquivosDeComponents(string padrao)
    {
        var web = Web();
        return Directory.GetFiles(Path.Combine(web, "Components"), padrao, SearchOption.AllDirectories)
            .Select(caminho => Path.GetRelativePath(web, caminho).Replace('\\', '/'))
            .ToList();
    }

    public static TheoryData<string> Arquivos => new(Migradas);

    public static TheoryData<string> ArquivosDeInfraestrutura => new(Infraestrutura);

    [Theory]
    [MemberData(nameof(Arquivos))]
    public void Tela_migrada_nao_tem_estilo_embutido_nem_classe_fora_do_design_system(string arquivo)
    {
        var caminho = Caminho(arquivo);

        Violacoes(File.ReadAllText(caminho)).Should().BeEmpty(
            "tela migrada não tem estilo embutido nem classe fora do design system: o padrão vira classe aj- lá");
        File.Exists(caminho + ".css").Should().BeFalse("tela migrada não tem CSS próprio: o estilo vem do design system");
    }

    [Theory]
    [MemberData(nameof(ArquivosDeInfraestrutura))]
    public void Infraestrutura_so_usa_fora_do_design_system_as_classes_que_o_framework_exige(string arquivo)
    {
        var caminho = Caminho(arquivo);

        Violacoes(File.ReadAllText(caminho), ClassesDoFramework).Should().BeEmpty(
            "a infraestrutura não tem estilo embutido e só usa, fora das classes aj-, as que o Blazor exige");
        File.Exists(caminho + ".css").Should().Be(arquivo == "Components/Layout/ReconnectModal.razor",
            "só o aviso de reconexão tem CSS próprio (exceção da spec)");
    }

    // Todo .razor da Web, em qualquer pasta, fica na trava. Um componente novo (ou um antigo que sobrou) fora das listas
    // faz este teste falhar: entra em Migradas (marcação só do design system) ou, se o framework exigir, em
    // Infraestrutura. Substitui as travas por pasta das atividades e da agenda, e mantém as asserções delas.
    [Fact]
    public void Todo_razor_da_web_esta_na_trava()
    {
        var arquivos = ArquivosDeComponents("*.razor");

        arquivos.Should().Contain("Components/Pages/Atividades.razor").And.Contain("Components/Atividades/EtiquetaDeSituacao.razor")
            .And.Contain("Components/Pages/Agenda.razor").And.Contain("Components/Pages/EventoFicha.razor")
            .And.Contain("Components/Pages/EventoFormulario.razor").And.Contain("Components/Agenda/SemanaDaAgenda.razor")
            .And.Contain("Components/Pages/Conta/Entrar.razor").And.Contain("Components/App.razor");
        ForaDaTrava(arquivos).Should().BeEmpty(
            "todo .razor da Web está em Migradas ou em Infraestrutura; fora delas, pode voltar à marcação antiga");
        Migradas.Intersect(Infraestrutura).Should().BeEmpty("cada arquivo está numa lista só");
    }

    [Fact]
    public void So_o_aviso_de_reconexao_tem_css_proprio()
    {
        ArquivosDeComponents("*.razor.css").Should().Equal(new[] { "Components/Layout/ReconnectModal.razor.css" },
            "o estilo das telas vem do design system; o aviso de reconexão é a exceção da spec");
    }

    // A própria trava recursiva: um .razor fora das duas listas reprova, em qualquer pasta; os das listas passam.
    [Fact]
    public void A_trava_reprova_razor_fora_das_duas_listas()
    {
        ForaDaTrava(["Components/Pages/Home.razor", "Components/App.razor", "Components/Agenda/Detalhes/NovoCartao.razor"])
            .Should().Equal("Components/Agenda/Detalhes/NovoCartao.razor");
    }

    [Theory]
    [InlineData("<a href=\".\" class=\"reload\">Recarregar</a>", "reload")]
    [InlineData("<button type=\"button\" class=\"dismiss aj-botao\">x</button>", "dismiss")]
    [InlineData("<p class=\"components-pause-visible\">x</p>", "components-pause-visible")]
    public void A_trava_aceita_classe_do_framework_so_na_infraestrutura(string marcacao, string violacao)
    {
        Violacoes(marcacao).Should().Equal(violacao);
        Violacoes(marcacao, ClassesDoFramework).Should().BeEmpty();
    }

    [Theory]
    [InlineData("<p class=\"components-pause-visible erro\">x</p>", "erro")]
    [InlineData("<p class=\"components-reconnect-qualquer\">x</p>", "components-reconnect-qualquer")]
    [InlineData("<dialog style=\"width:20rem\"></dialog>", "style")]
    public void A_trava_da_infraestrutura_reprova_o_que_o_framework_nao_exige(string marcacao, string violacao)
    {
        Violacoes(marcacao, ClassesDoFramework).Should().Equal(violacao);
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
