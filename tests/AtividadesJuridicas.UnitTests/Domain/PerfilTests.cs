using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Domain;

public class PerfilTests
{
    [Fact]
    public void Nasce_ativo_com_nome_aparado()
    {
        var perfil = Perfil.Criar("  Diretor ", TipoPerfil.Diretoria);

        perfil.Nome.Should().Be("Diretor");
        perfil.Tipo.Should().Be(TipoPerfil.Diretoria);
        perfil.Ativo.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_nome_vazio(string nome)
    {
        var acao = () => Perfil.Criar(nome, TipoPerfil.Producao);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_tipo_fora_do_enum()
    {
        var acao = () => Perfil.Criar("X", (TipoPerfil)99);

        acao.Should().Throw<ExcecaoDeDominio>();
    }
}
