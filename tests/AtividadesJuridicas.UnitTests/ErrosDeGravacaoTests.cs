using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// O erro de gravação dos formulários se revela (foco e rolagem até ele): num formulário mais alto que a tela do celular,
/// o aviso do topo ficaria fora da vista. O <c>Aviso</c> só se revela ao aparecer; por isso cada mensagem nova passa pelo
/// <c>MostrarErro</c>, que troca a chave do aviso, e o mesmo erro duas vezes seguidas se revela das duas.
/// </summary>
public class ErrosDeGravacaoTests
{
    // Caminhos a partir de src/AtividadesJuridicas.Web.
    private static readonly string[] Formularios =
    [
        "Components/Pages/UsuarioFormulario.razor",
        "Components/Pages/ClienteFormulario.razor",
        "Components/Pages/ProcessoFormulario.razor",
        "Components/Pages/AtividadeFormulario.razor",
        "Components/Pages/EventoFormulario.razor",
        "Components/Pages/AtividadeResponsaveis.razor",
        "Components/Atividades/MotivoComResponsaveis.razor",
        "Components/Pages/MinhaSenha.razor",
    ];

    public static TheoryData<string> Arquivos => new(Formularios);

    [Theory]
    [MemberData(nameof(Arquivos))]
    public void O_erro_de_gravacao_se_revela_e_troca_de_chave_a_cada_mensagem(string arquivo)
    {
        var codigo = File.ReadAllText(Path.Combine(
            [GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web", .. arquivo.Split('/')]));

        codigo.Should().Contain("<Aviso @key=\"_versaoDoErro\" Tipo=\"TipoDeAviso.Erro\" Revelar=\"true\">@_mensagem</Aviso>");
        // Uma só atribuição de texto ao _mensagem, a do MostrarErro, que também troca a chave; as outras são "= null".
        Regex.Matches(codigo, @"_mensagem\s*=(?!=)(?!\s*null\s*;)").Should().ContainSingle();
        codigo.Should().Contain("_mensagem = mensagem;").And.Contain("_versaoDoErro++;");
    }
}
