using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class NumeroCnjTests
{
    [Theory]
    [InlineData("1234567-85.2024.8.02.0001", "12345678520248020001")]
    [InlineData("12345678520248020001", "12345678520248020001")]
    [InlineData(" 0012345-22.2026.5.19.0005 ", "00123452220265190005")]
    [InlineData("7654321-31.2025.8.26.0100", "76543213120258260100")]
    public void Aceita_e_normaliza_para_vinte_digitos(string texto, string valor)
    {
        NumeroCnj.Criar(texto).Valor.Should().Be(valor);
    }

    [Fact]
    public void Formata_no_padrao_do_cnj()
    {
        NumeroCnj.Criar("12345678520248020001").Formatado.Should().Be("1234567-85.2024.8.02.0001");
    }

    [Fact]
    public void Mesmo_numero_com_outra_pontuacao_e_igual()
    {
        NumeroCnj.Criar("1234567-85.2024.8.02.0001").Should().Be(NumeroCnj.Criar("12345678520248020001"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567852024802000")]
    [InlineData("123456785202480200011")]
    [InlineData("1234567-86.2024.8.02.0001")]
    [InlineData("1234567-85.2024.8.02.0002")]
    [InlineData("1234567-85.2024.8.02.000a")]
    [InlineData("1234567/85.2024.8.02.0001")]
    public void Recusa_numero_invalido(string? texto)
    {
        NumeroCnj.TentarCriar(texto, out var numero).Should().BeFalse();
        numero.Should().BeNull();

        var acao = () => NumeroCnj.Criar(texto);
        acao.Should().Throw<ExcecaoDeDominio>();
    }
}
