using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Domain;

public class ClienteTests
{
    private static DadosDoCliente Dados(string nome = "Maria Silva", string? email = "maria@x.com") => new(
        nome, email is null ? null : Email.Criar(email), " (82) 99999-0000 ",
        Endereco.Criar("Rua A", "10", null, "Centro", "Maceió", "AL", "57020-000"), " Cliente antigo ");

    [Fact]
    public void Pessoa_fisica_nasce_ativa_com_o_tipo_fixado()
    {
        var cliente = ClientePessoaFisica.Criar(Dados(), Cpf.Criar("529.982.247-25"), " 1234567 ", new DateOnly(1980, 5, 17));

        cliente.Tipo.Should().Be(TipoCliente.PessoaFisica);
        cliente.Nome.Should().Be("Maria Silva");
        cliente.NomeCompleto.Should().Be("Maria Silva");
        cliente.Email!.Valor.Should().Be("maria@x.com");
        cliente.Telefone.Should().Be("(82) 99999-0000");
        cliente.Endereco.Cidade.Should().Be("Maceió");
        cliente.Observacoes.Should().Be("Cliente antigo");
        cliente.Cpf.Valor.Should().Be("52998224725");
        cliente.Rg.Should().Be("1234567");
        cliente.DataNascimento.Should().Be(new DateOnly(1980, 5, 17));
        cliente.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Pessoa_juridica_nasce_ativa_com_o_tipo_fixado()
    {
        var cliente = ClientePessoaJuridica.Criar(
            Dados("Acme Ltda", null), Cnpj.Criar("11.222.333/0001-81"), " Acme ", " João ", " 123 ", "  ");

        cliente.Tipo.Should().Be(TipoCliente.PessoaJuridica);
        cliente.Nome.Should().Be("Acme Ltda");
        cliente.RazaoSocial.Should().Be("Acme Ltda");
        cliente.Email.Should().BeNull();
        cliente.Cnpj.Valor.Should().Be("11222333000181");
        cliente.NomeFantasia.Should().Be("Acme");
        cliente.NomeResponsavel.Should().Be("João");
        cliente.InscricaoEstadual.Should().Be("123");
        cliente.InscricaoMunicipal.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_nome_vazio(string nome)
    {
        var acao = () => ClientePessoaFisica.Criar(Dados(nome), Cpf.Criar("52998224725"), null, null);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_textos_acima_do_limite()
    {
        var nomeLongo = () => ClientePessoaFisica.Criar(Dados(new string('a', 201)), Cpf.Criar("52998224725"), null, null);
        var rgLongo = () => ClientePessoaFisica.Criar(Dados(), Cpf.Criar("52998224725"), new string('1', 21), null);
        var fantasiaLonga = () => ClientePessoaJuridica.Criar(
            Dados("Acme"), Cnpj.Criar("11222333000181"), new string('a', 201), null, null, null);

        nomeLongo.Should().Throw<ExcecaoDeDominio>();
        rgLongo.Should().Throw<ExcecaoDeDominio>();
        fantasiaLonga.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Alterar_substitui_os_dados_sem_mexer_no_estado_ativo()
    {
        var cliente = ClientePessoaFisica.Criar(Dados(), Cpf.Criar("52998224725"), "1", null);
        cliente.Inativar();

        cliente.Alterar(Dados("Maria S. Santos", null) with { Endereco = Endereco.Vazio },
            Cpf.Criar("111.444.777-35"), null, new DateOnly(1981, 1, 1));

        cliente.Nome.Should().Be("Maria S. Santos");
        cliente.Email.Should().BeNull();
        cliente.Endereco.EstaVazio.Should().BeTrue();
        cliente.Cpf.Valor.Should().Be("11144477735");
        cliente.Rg.Should().BeNull();
        cliente.Ativo.Should().BeFalse();
        cliente.Tipo.Should().Be(TipoCliente.PessoaFisica);
    }

    [Fact]
    public void Alteracao_invalida_nao_deixa_o_cliente_pela_metade()
    {
        var cliente = ClientePessoaFisica.Criar(Dados(), Cpf.Criar("52998224725"), "1", null);

        var acao = () => cliente.Alterar(Dados("Outro Nome"), Cpf.Criar("11144477735"), new string('1', 21), null);

        acao.Should().Throw<ExcecaoDeDominio>();
        cliente.Nome.Should().Be("Maria Silva");
        cliente.Cpf.Valor.Should().Be("52998224725");
    }
}
