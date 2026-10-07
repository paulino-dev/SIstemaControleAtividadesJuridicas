using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Domain;

public class EmailTests
{
    [Fact]
    public void Normaliza_espacos_e_maiusculas()
    {
        Email.Criar("  Ana@Escritorio.COM ").Valor.Should().Be("ana@escritorio.com");
    }

    [Fact]
    public void Emails_que_diferem_so_em_caixa_sao_iguais()
    {
        Email.Criar("Ana@X.com").Should().Be(Email.Criar("ana@x.com"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("semarroba.com")]
    [InlineData("@x.com")]
    [InlineData("a@")]
    [InlineData("a@b")]
    [InlineData("a@@b.com")]
    [InlineData("a b@c.com")]
    [InlineData("a@.com")]
    [InlineData("a@b.")]
    public void Recusa_formato_invalido(string? texto)
    {
        Email.TentarCriar(texto, out var email).Should().BeFalse();
        email.Should().BeNull();

        var acao = () => Email.Criar(texto);
        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_email_maior_que_254_caracteres()
    {
        var longo = new string('a', 250) + "@x.com";

        Email.TentarCriar(longo, out _).Should().BeFalse();
    }
}
