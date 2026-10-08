using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class CnpjTests
{
    [Theory]
    [InlineData("11222333000181", "11222333000181", "11.222.333/0001-81")]
    [InlineData("11.222.333/0001-81", "11222333000181", "11.222.333/0001-81")]
    [InlineData("11.444.777/0001-61", "11444777000161", "11.444.777/0001-61")]
    public void Aceita_cnpj_numerico(string texto, string valor, string formatado)
    {
        var cnpj = Cnpj.Criar(texto);

        cnpj.Valor.Should().Be(valor);
        cnpj.Formatado.Should().Be(formatado);
    }

    [Theory]
    [InlineData("12ABC34501DE35")]
    [InlineData("12.ABC.345/01DE-35")]
    [InlineData("12.abc.345/01de-35")]
    public void Aceita_cnpj_alfanumerico_e_normaliza_para_maiusculas(string texto)
    {
        var cnpj = Cnpj.Criar(texto);

        cnpj.Valor.Should().Be("12ABC34501DE35");
        cnpj.Formatado.Should().Be("12.ABC.345/01DE-35");
    }

    [Fact]
    public void Mesmo_cnpj_com_outra_pontuacao_ou_caixa_e_igual()
    {
        Cnpj.Criar("12.abc.345/01de-35").Should().Be(Cnpj.Criar("12ABC34501DE35"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1122233300018")]
    [InlineData("112223330001811")]
    [InlineData("11222333000180")]
    [InlineData("00000000000000")]
    [InlineData("12ABC34501DE36")]
    [InlineData("12ABC34501DEA5")]
    [InlineData("12ÁBC34501DE35")]
    [InlineData("11.222.333-0001/81!")]
    public void Recusa_cnpj_invalido(string? texto)
    {
        Cnpj.TentarCriar(texto, out var cnpj).Should().BeFalse();
        cnpj.Should().BeNull();

        var acao = () => Cnpj.Criar(texto);
        acao.Should().Throw<ExcecaoDeDominio>();
    }
}
