using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class CpfTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    [InlineData(" 529 982 247 25 ")]
    public void Aceita_e_normaliza_para_onze_digitos(string texto)
    {
        var cpf = Cpf.Criar(texto);

        cpf.Valor.Should().Be("52998224725");
        cpf.Formatado.Should().Be("529.982.247-25");
    }

    [Fact]
    public void Mesmo_cpf_com_outra_pontuacao_e_igual()
    {
        Cpf.Criar("529.982.247-25").Should().Be(Cpf.Criar("52998224725"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("5299822472")]
    [InlineData("529982247250")]
    [InlineData("52998224724")]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    [InlineData("529.982.247-2a")]
    [InlineData("529/982/247-25")]
    public void Recusa_cpf_invalido(string? texto)
    {
        Cpf.TentarCriar(texto, out var cpf).Should().BeFalse();
        cpf.Should().BeNull();

        var acao = () => Cpf.Criar(texto);
        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData("11144477735")]
    [InlineData("12345678909")]
    public void Aceita_outros_cpfs_validos(string texto)
    {
        Cpf.TentarCriar(texto, out _).Should().BeTrue();
    }
}
