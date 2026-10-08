using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class ProcessoTests
{
    private static readonly Guid Area = Guid.CreateVersion7();
    private static readonly Guid Maria = Guid.CreateVersion7();
    private static readonly Guid Acme = Guid.CreateVersion7();
    private static readonly Guid Jose = Guid.CreateVersion7();

    private static DadosDoProcesso Dados(string titulo = " Reclamação trabalhista ", decimal? valor = 1500.50m) => new(
        NumeroCnj.Criar("1234567-85.2024.8.02.0001"), titulo, Area, " 2ª Vara do Trabalho ", " Maceió ",
        " Empresa X ", new DateOnly(2024, 3, 10), valor, "  ");

    private static ClienteDoProcesso[] Clientes(params (Guid Id, PoloProcessual Polo)[] itens) =>
        [.. itens.Select(i => new ClienteDoProcesso(i.Id, i.Polo))];

    private static Processo Novo() => Processo.Criar(Dados(), Clientes((Maria, PoloProcessual.Ativo)));

    [Fact]
    public void Nasce_ativo_com_dados_aparados_e_os_clientes_vinculados()
    {
        var processo = Processo.Criar(Dados(), Clientes((Maria, PoloProcessual.Ativo), (Acme, PoloProcessual.Terceiro)));

        processo.Status.Should().Be(StatusProcesso.Ativo);
        processo.NumeroCnj!.Valor.Should().Be("12345678520248020001");
        processo.Titulo.Should().Be("Reclamação trabalhista");
        processo.AreaAtuacaoId.Should().Be(Area);
        processo.Orgao.Should().Be("2ª Vara do Trabalho");
        processo.Comarca.Should().Be("Maceió");
        processo.ParteContraria.Should().Be("Empresa X");
        processo.DataDistribuicao.Should().Be(new DateOnly(2024, 3, 10));
        processo.ValorCausa.Should().Be(1500.50m);
        processo.Observacoes.Should().BeNull();

        processo.Clientes.Should().HaveCount(2);
        processo.Clientes.Should().OnlyContain(c => c.ProcessoId == processo.Id && c.Id != Guid.Empty);
        processo.Clientes.Select(c => (c.ClienteId, c.Polo))
            .Should().BeEquivalentTo([(Maria, PoloProcessual.Ativo), (Acme, PoloProcessual.Terceiro)]);
    }

    [Fact]
    public void Numero_cnj_e_opcional()
    {
        var processo = Processo.Criar(Dados() with { NumeroCnj = null }, Clientes((Maria, PoloProcessual.Ativo)));

        processo.NumeroCnj.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_titulo_vazio(string titulo)
    {
        var acao = () => Processo.Criar(Dados(titulo), Clientes((Maria, PoloProcessual.Ativo)));

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_area_vazia_valor_negativo_e_textos_acima_do_limite()
    {
        var semArea = () => Processo.Criar(Dados() with { AreaAtuacaoId = Guid.Empty }, Clientes((Maria, PoloProcessual.Ativo)));
        var valorNegativo = () => Processo.Criar(Dados(valor: -0.01m), Clientes((Maria, PoloProcessual.Ativo)));
        var tituloLongo = () => Processo.Criar(Dados(new string('a', 201)), Clientes((Maria, PoloProcessual.Ativo)));
        var comarcaLonga = () => Processo.Criar(Dados() with { Comarca = new string('a', 101) }, Clientes((Maria, PoloProcessual.Ativo)));

        semArea.Should().Throw<ExcecaoDeDominio>();
        valorNegativo.Should().Throw<ExcecaoDeDominio>();
        tituloLongo.Should().Throw<ExcecaoDeDominio>();
        comarcaLonga.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData("0.001", "O valor da causa deve ter no máximo duas casas decimais.")]
    [InlineData("1.005", "O valor da causa deve ter no máximo duas casas decimais.")]
    [InlineData("10000000000000000", "O valor da causa é alto demais.")]
    public void Recusa_valor_da_causa_com_mais_de_duas_casas_ou_grande_demais(string texto, string mensagem)
    {
        var valor = decimal.Parse(texto, System.Globalization.CultureInfo.InvariantCulture);

        var criar = () => Processo.Criar(Dados(valor: valor), Clientes((Maria, PoloProcessual.Ativo)));
        var alterar = () => Novo().Alterar(Dados(valor: valor));

        criar.Should().Throw<ExcecaoDeDominio>().WithMessage(mensagem);
        alterar.Should().Throw<ExcecaoDeDominio>().WithMessage(mensagem);
    }

    [Theory]
    [InlineData("1234.50")]
    [InlineData("1234.5000")]
    [InlineData("0")]
    [InlineData("9999999999999999.99")]
    [InlineData(null)]
    public void Aceita_valor_da_causa_que_cabe_em_numeric_18_2(string? texto)
    {
        decimal? valor = texto is null ? null : decimal.Parse(texto, System.Globalization.CultureInfo.InvariantCulture);

        var processo = Processo.Criar(Dados(valor: valor), Clientes((Maria, PoloProcessual.Ativo)));
        var outro = Novo();
        outro.Alterar(Dados(valor: valor));

        processo.ValorCausa.Should().Be(valor);
        outro.ValorCausa.Should().Be(valor);
    }

    [Fact]
    public void Recusa_processo_sem_cliente_e_cliente_repetido()
    {
        var semCliente = () => Processo.Criar(Dados(), []);
        var repetido = () => Processo.Criar(Dados(), Clientes((Maria, PoloProcessual.Ativo), (Maria, PoloProcessual.Passivo)));
        var clienteVazio = () => Processo.Criar(Dados(), Clientes((Guid.Empty, PoloProcessual.Ativo)));
        var poloInvalido = () => Processo.Criar(Dados(), Clientes((Maria, (PoloProcessual)9)));

        semCliente.Should().Throw<ExcecaoDeDominio>();
        repetido.Should().Throw<ExcecaoDeDominio>();
        clienteVazio.Should().Throw<ExcecaoDeDominio>();
        poloInvalido.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Alterar_substitui_os_dados_sem_mexer_em_status_nem_em_clientes()
    {
        var processo = Novo();
        processo.AlterarStatus(StatusProcesso.Suspenso);
        var outraArea = Guid.CreateVersion7();

        processo.Alterar(new DadosDoProcesso(null, "Ação de cobrança", outraArea, null, null, null, null, null, "Urgente"));

        processo.Titulo.Should().Be("Ação de cobrança");
        processo.NumeroCnj.Should().BeNull();
        processo.AreaAtuacaoId.Should().Be(outraArea);
        processo.Orgao.Should().BeNull();
        processo.ValorCausa.Should().BeNull();
        processo.Observacoes.Should().Be("Urgente");
        processo.Status.Should().Be(StatusProcesso.Suspenso);
        processo.Clientes.Should().ContainSingle();
    }

    [Fact]
    public void Alteracao_invalida_nao_deixa_o_processo_pela_metade()
    {
        var processo = Novo();

        var acao = () => processo.Alterar(Dados("Outro título", valor: -1m));

        acao.Should().Throw<ExcecaoDeDominio>();
        processo.Titulo.Should().Be("Reclamação trabalhista");
        processo.ValorCausa.Should().Be(1500.50m);
    }

    [Fact]
    public void Definir_clientes_adiciona_remove_e_troca_o_polo_preservando_os_que_ficam()
    {
        var processo = Processo.Criar(Dados(), Clientes((Maria, PoloProcessual.Ativo), (Acme, PoloProcessual.Ativo)));
        var vinculoDaMaria = processo.Clientes.Single(c => c.ClienteId == Maria);

        var alteracao = processo.DefinirClientes(Clientes((Maria, PoloProcessual.Passivo), (Jose, PoloProcessual.Terceiro)));

        alteracao.Houve.Should().BeTrue();
        alteracao.Adicionados.Should().Equal(Jose);
        alteracao.Removidos.Should().Equal(Acme);
        alteracao.PoloAlterado.Should().Equal(Maria);

        processo.Clientes.Select(c => (c.ClienteId, c.Polo)).Should().BeEquivalentTo(
            [(Maria, PoloProcessual.Passivo), (Jose, PoloProcessual.Terceiro)]);
        processo.Clientes.Single(c => c.ClienteId == Maria).Should().BeSameAs(vinculoDaMaria, "o vínculo que fica é o mesmo registro");
    }

    [Fact]
    public void Definir_os_mesmos_clientes_nao_altera_nada()
    {
        var processo = Processo.Criar(Dados(), Clientes((Maria, PoloProcessual.Ativo), (Acme, PoloProcessual.Passivo)));
        var antes = processo.Clientes.Select(c => c.Id).ToList();

        var alteracao = processo.DefinirClientes(Clientes((Acme, PoloProcessual.Passivo), (Maria, PoloProcessual.Ativo)));

        alteracao.Houve.Should().BeFalse();
        processo.Clientes.Select(c => c.Id).Should().BeEquivalentTo(antes);
    }

    [Fact]
    public void Definir_clientes_invalidos_nao_altera_os_vinculos()
    {
        var processo = Novo();

        var semCliente = () => processo.DefinirClientes([]);
        var repetido = () => processo.DefinirClientes(Clientes((Acme, PoloProcessual.Ativo), (Acme, PoloProcessual.Passivo)));

        semCliente.Should().Throw<ExcecaoDeDominio>();
        repetido.Should().Throw<ExcecaoDeDominio>();
        processo.Clientes.Should().ContainSingle().Which.ClienteId.Should().Be(Maria);
    }

    [Fact]
    public void Alterar_status_aceita_so_valores_do_enum()
    {
        var processo = Novo();

        processo.AlterarStatus(StatusProcesso.Encerrado);
        processo.Status.Should().Be(StatusProcesso.Encerrado);

        var acao = () => processo.AlterarStatus((StatusProcesso)99);
        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Versao_dos_vinculos_aumenta_so_quando_os_vinculos_mudam()
    {
        var processo = Processo.Criar(Dados(), Clientes((Maria, PoloProcessual.Ativo), (Acme, PoloProcessual.Passivo)));
        processo.VersaoDosVinculos.Should().Be(0);

        processo.DefinirClientes(Clientes((Acme, PoloProcessual.Passivo), (Maria, PoloProcessual.Ativo)));
        processo.VersaoDosVinculos.Should().Be(0, "nada mudou");

        processo.DefinirClientes(Clientes((Maria, PoloProcessual.Terceiro), (Acme, PoloProcessual.Passivo)));
        processo.VersaoDosVinculos.Should().Be(1);

        processo.DefinirClientes(Clientes((Maria, PoloProcessual.Terceiro)));
        processo.VersaoDosVinculos.Should().Be(2);
    }
}
