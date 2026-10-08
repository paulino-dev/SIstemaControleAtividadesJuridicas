using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class EnderecoTests
{
    [Fact]
    public void Apara_textos_e_normaliza_uf_e_cep()
    {
        var endereco = Endereco.Criar(" Rua A ", " 10 ", "  ", "Centro", "Maceió", "al", "57.020-000");

        endereco.Logradouro.Should().Be("Rua A");
        endereco.Numero.Should().Be("10");
        endereco.Complemento.Should().BeNull();
        endereco.Uf.Should().Be("AL");
        endereco.Cep.Should().Be("57020000");
        endereco.EstaVazio.Should().BeFalse();
    }

    [Fact]
    public void Endereco_sem_nenhum_campo_e_vazio_e_igual_ao_vazio()
    {
        var endereco = Endereco.Criar(null, " ", null, null, "", null, null);

        endereco.EstaVazio.Should().BeTrue();
        endereco.Should().Be(Endereco.Vazio);
    }

    [Fact]
    public void Enderecos_com_os_mesmos_valores_sao_iguais()
    {
        Endereco.Criar("Rua A", "10", null, null, "Maceió", "AL", "57020-000")
            .Should().Be(Endereco.Criar("Rua A", "10", null, null, "Maceió", "al", "57020000"));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("ALA")]
    [InlineData("A1")]
    public void Recusa_uf_que_nao_tenha_duas_letras(string uf)
    {
        var acao = () => Endereco.Criar(null, null, null, null, null, uf, null);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData("5702000")]
    [InlineData("570200000")]
    [InlineData("57020-00a")]
    public void Recusa_cep_que_nao_tenha_oito_digitos(string cep)
    {
        var acao = () => Endereco.Criar(null, null, null, null, null, null, cep);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_logradouro_maior_que_200_caracteres()
    {
        var acao = () => Endereco.Criar(new string('a', 201), null, null, null, null, null, null);

        acao.Should().Throw<ExcecaoDeDominio>();
    }
}
