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

    [Fact]
    public void Renomear_troca_o_nome_aparado_e_mantem_tipo_e_situacao()
    {
        var perfil = Perfil.Criar("Estagiário", TipoPerfil.Producao);
        perfil.Inativar();

        perfil.Renomear("  Estagiário de Direito ");

        perfil.Nome.Should().Be("Estagiário de Direito");
        perfil.Tipo.Should().Be(TipoPerfil.Producao);
        perfil.Ativo.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Renomear_recusa_nome_vazio(string nome)
    {
        var perfil = Perfil.Criar("Estagiário", TipoPerfil.Producao);

        var acao = () => perfil.Renomear(nome);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O nome do perfil é obrigatório.");
        perfil.Nome.Should().Be("Estagiário");
    }

    [Fact]
    public void Renomear_respeita_o_tamanho_maximo()
    {
        var perfil = Perfil.Criar("Estagiário", TipoPerfil.Producao);

        var acao = () => perfil.Renomear(new string('a', Perfil.TamanhoMaximoNome + 1));

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O nome do perfil deve ter até 100 caracteres.");
    }
}
