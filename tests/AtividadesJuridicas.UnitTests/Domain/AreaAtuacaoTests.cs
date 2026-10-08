using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class AreaAtuacaoTests
{
    [Fact]
    public void Nasce_ativa_com_nome_aparado()
    {
        var area = AreaAtuacao.Criar("  Trabalhista ");

        area.Nome.Should().Be("Trabalhista");
        area.Ativo.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_nome_vazio_ao_criar_e_ao_renomear(string nome)
    {
        var criar = () => AreaAtuacao.Criar(nome);
        var renomear = () => AreaAtuacao.Criar("Cível").Renomear(nome);

        criar.Should().Throw<ExcecaoDeDominio>();
        renomear.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_nome_maior_que_100_caracteres()
    {
        var acao = () => AreaAtuacao.Criar(new string('a', 101));

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Renomear_e_alternar_o_estado()
    {
        var area = AreaAtuacao.Criar("Civel");

        area.Renomear(" Cível ");
        area.Inativar();

        area.Nome.Should().Be("Cível");
        area.Ativo.Should().BeFalse();

        area.Ativar();
        area.Ativo.Should().BeTrue();
    }
}
