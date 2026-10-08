using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class ClientesTests
{
    private readonly Cenario _c = new();

    private Usuario Producao() => _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

    private static DadosDoClienteEntrada Dados(string nome = "Maria Silva", string? email = " Maria@X.com ") => new(
        nome, email, "(82) 99999-0000", new EnderecoEntrada("Rua A", "10", null, "Centro", "Maceió", "al", "57020-000"), null);

    private Task<Resultado<Guid>> SalvarPf(Usuario? ator, Guid? id, string cpf = "529.982.247-25", DadosDoClienteEntrada? dados = null) =>
        new SalvarClientePessoaFisicaManipulador(_c.Como(ator), _c.Clientes, _c.Unidade).ManipularAsync(
            new SalvarClientePessoaFisica(id, dados ?? Dados(), cpf, "123", new DateOnly(1980, 5, 17)), default);

    private Task<Resultado<Guid>> SalvarPj(Usuario? ator, Guid? id, string cnpj = "11.222.333/0001-81", DadosDoClienteEntrada? dados = null) =>
        new SalvarClientePessoaJuridicaManipulador(_c.Como(ator), _c.Clientes, _c.Unidade).ManipularAsync(
            new SalvarClientePessoaJuridica(id, dados ?? Dados("Acme Ltda", null), cnpj, "Acme", "João", null, null), default);

    private Task<Resultado<Pagina<ClienteResumo>>> Listar(Usuario? ator, string? busca = null, bool inativos = false, int pagina = 1) =>
        new ListarClientesManipulador(_c.Como(ator), _c.Clientes)
            .ManipularAsync(new ListarClientes(busca, inativos, pagina), default);

    private Task<Resultado<ClienteDetalhe>> Obter(Usuario? ator, Guid id) =>
        new ObterClienteManipulador(_c.Como(ator), _c.Clientes).ManipularAsync(new ObterCliente(id), default);

    private Task<Resultado> DefinirAtivo(Usuario? ator, Guid id, bool ativo) =>
        new DefinirAtivoClienteManipulador(_c.Como(ator), _c.Clientes, _c.Unidade)
            .ManipularAsync(new DefinirAtivoCliente(id, ativo), default);

    [Fact]
    public void Detalhe_da_listagem_guarda_acentos_literalmente()
    {
        IConsultaAuditavel consulta = new ListarClientes("José da Conceição");

        consulta.Detalhe.Should().Contain("José da Conceição");
        consulta.Detalhe.Should().NotContain("\\u00");
    }

    [Fact]
    public async Task Busca_por_nome_ignora_acentos_e_caixa()
    {
        await SalvarPf(_c.Diretor, null, "529.982.247-25", Dados("José Conceição"));
        await SalvarPf(_c.Diretor, null, "111.444.777-35", Dados("Jose Silva"));

        async Task<IEnumerable<string>> Nomes(string busca) =>
            (await Listar(_c.Diretor, busca)).Valor!.Itens.Select(c => c.Nome);

        (await Nomes("jose")).Should().BeEquivalentTo("Jose Silva", "José Conceição");
        (await Nomes("JOSÉ")).Should().BeEquivalentTo("Jose Silva", "José Conceição");
        (await Nomes("conceicao")).Should().Equal("José Conceição");
    }

    // ---- Salvar ----

    [Fact]
    public async Task Diretor_cria_pessoa_fisica_com_dados_normalizados()
    {
        var criado = await SalvarPf(_c.Diretor, null);

        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        var cliente = _c.Clientes.Itens.OfType<ClientePessoaFisica>().Single();
        cliente.Id.Should().Be(criado.Valor);
        cliente.Cpf.Valor.Should().Be("52998224725");
        cliente.Email!.Valor.Should().Be("maria@x.com");
        cliente.Endereco.Uf.Should().Be("AL");
        cliente.Endereco.Cep.Should().Be("57020000");
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Diretor_cria_pessoa_juridica_com_cnpj_alfanumerico()
    {
        var criado = await SalvarPj(_c.Diretor, null, "12.abc.345/01de-35");

        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        _c.Clientes.Itens.OfType<ClientePessoaJuridica>().Single().Cnpj.Valor.Should().Be("12ABC34501DE35");
    }

    [Fact]
    public async Task Mesmo_documento_com_outra_pontuacao_e_conflito()
    {
        await SalvarPf(_c.Diretor, null, "529.982.247-25");
        await SalvarPj(_c.Diretor, null, "11.222.333/0001-81");

        (await SalvarPf(_c.Diretor, null, "52998224725")).Erro.Should().Be(TipoErro.Conflito);
        (await SalvarPj(_c.Diretor, null, "11222333000181")).Erro.Should().Be(TipoErro.Conflito);
        _c.Clientes.Itens.Should().HaveCount(2);
    }

    [Fact]
    public async Task Editar_mantendo_o_proprio_documento_nao_e_conflito()
    {
        var criado = await SalvarPf(_c.Diretor, null);

        var editado = await SalvarPf(_c.Diretor, criado.Valor, dados: Dados("Maria S. Santos"));

        editado.Sucesso.Should().BeTrue(editado.Mensagem);
        _c.Clientes.Itens.Single().Nome.Should().Be("Maria S. Santos");
    }

    [Theory]
    [InlineData("529.982.247-24", "Maria", "maria@x.com")]
    [InlineData("529.982.247-25", "", "maria@x.com")]
    [InlineData("529.982.247-25", "Maria", "nao-e-email")]
    public async Task Dados_invalidos_sao_erro_de_validacao(string cpf, string nome, string email)
    {
        var resultado = await SalvarPf(_c.Diretor, null, cpf, Dados(nome, email));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        _c.Clientes.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Endereco_invalido_e_erro_de_validacao()
    {
        var dados = Dados() with { Endereco = new EnderecoEntrada(Uf: "ALA") };

        (await SalvarPf(_c.Diretor, null, dados: dados)).Erro.Should().Be(TipoErro.Validacao);
    }

    [Fact]
    public async Task Email_em_branco_vira_cliente_sem_email()
    {
        var criado = await SalvarPf(_c.Diretor, null, dados: Dados(email: "   "));

        criado.Sucesso.Should().BeTrue();
        _c.Clientes.Itens.Single().Email.Should().BeNull();
    }

    [Fact]
    public async Task Salvar_com_o_id_de_um_cliente_de_outro_tipo_e_nao_encontrado()
    {
        var pf = await SalvarPf(_c.Diretor, null);
        var pj = await SalvarPj(_c.Diretor, null);

        (await SalvarPj(_c.Diretor, pf.Valor, "11.444.777/0001-61")).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await SalvarPf(_c.Diretor, pj.Valor, "111.444.777-35")).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await SalvarPf(_c.Diretor, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
        _c.Clientes.Itens.Single(c => c.Id == pf.Valor).Tipo.Should().Be(TipoCliente.PessoaFisica);
    }

    [Fact]
    public async Task Producao_e_anonimo_nao_criam_editam_nem_inativam()
    {
        var criado = await SalvarPf(_c.Diretor, null);
        var producao = Producao();

        (await SalvarPf(producao, null, "111.444.777-35")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await SalvarPj(producao, null)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await SalvarPf(producao, criado.Valor, dados: Dados("Outro"))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await DefinirAtivo(producao, criado.Valor, false)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await SalvarPf(null, null, "111.444.777-35")).Erro.Should().Be(TipoErro.AcessoNegado);

        var cliente = _c.Clientes.Itens.Should().ContainSingle().Subject;
        cliente.Nome.Should().Be("Maria Silva");
        cliente.Ativo.Should().BeTrue();
    }

    // ---- Ativar / inativar ----

    [Fact]
    public async Task Inativar_e_reativar()
    {
        var criado = await SalvarPf(_c.Diretor, null);

        (await DefinirAtivo(_c.Diretor, criado.Valor, false)).Sucesso.Should().BeTrue();
        _c.Clientes.Itens.Single().Ativo.Should().BeFalse();

        (await DefinirAtivo(_c.Diretor, criado.Valor, true)).Sucesso.Should().BeTrue();
        _c.Clientes.Itens.Single().Ativo.Should().BeTrue();

        (await DefinirAtivo(_c.Diretor, Guid.CreateVersion7(), false)).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    // ---- Consultar ----

    [Fact]
    public async Task Producao_lista_e_ve_o_documento_formatado()
    {
        await SalvarPf(_c.Diretor, null);
        await SalvarPj(_c.Diretor, null);

        var pagina = (await Listar(Producao())).Valor!;

        pagina.Total.Should().Be(2);
        pagina.Itens.Should().BeEquivalentTo(
        [
            new { Nome = "Acme Ltda", Tipo = TipoCliente.PessoaJuridica, Documento = "11.222.333/0001-81", Cidade = "Maceió" },
            new { Nome = "Maria Silva", Tipo = TipoCliente.PessoaFisica, Documento = "529.982.247-25", Cidade = "Maceió" },
        ], opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public async Task Inativos_so_aparecem_quando_pedidos()
    {
        var criado = await SalvarPf(_c.Diretor, null);
        await DefinirAtivo(_c.Diretor, criado.Valor, false);

        (await Listar(_c.Diretor)).Valor!.Itens.Should().BeEmpty();
        (await Listar(_c.Diretor, inativos: true)).Valor!.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Busca_por_nome_parcial_ou_por_documento()
    {
        await SalvarPf(_c.Diretor, null);
        await SalvarPj(_c.Diretor, null);

        (await Listar(_c.Diretor, "mari")).Valor!.Itens.Should().ContainSingle().Which.Nome.Should().Be("Maria Silva");
        (await Listar(_c.Diretor, "52998224725")).Valor!.Itens.Should().ContainSingle().Which.Nome.Should().Be("Maria Silva");
        (await Listar(_c.Diretor, "11.222.333/0001-81")).Valor!.Itens.Should().ContainSingle().Which.Nome.Should().Be("Acme Ltda");
        (await Listar(_c.Diretor, "ninguém")).Valor!.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Pagina_invalida_vira_a_primeira()
    {
        await SalvarPf(_c.Diretor, null);

        (await Listar(_c.Diretor, pagina: 0)).Valor!.Numero.Should().Be(1);
        (await Listar(_c.Diretor, pagina: -5)).Valor!.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Producao_abre_a_ficha_completa()
    {
        var pf = await SalvarPf(_c.Diretor, null);
        var pj = await SalvarPj(_c.Diretor, null);
        var producao = Producao();

        var fisica = (await Obter(producao, pf.Valor)).Valor!;
        fisica.Should().BeEquivalentTo(new
        {
            Id = pf.Valor, Tipo = TipoCliente.PessoaFisica, Nome = "Maria Silva", Email = "maria@x.com",
            Cpf = "529.982.247-25", Rg = "123", DataNascimento = new DateOnly(1980, 5, 17),
            Cidade = "Maceió", Uf = "AL", Cep = "57020000", Ativo = true,
        });
        fisica.Cnpj.Should().BeNull();

        var juridica = (await Obter(producao, pj.Valor)).Valor!;
        juridica.Cnpj.Should().Be("11.222.333/0001-81");
        juridica.NomeFantasia.Should().Be("Acme");
        juridica.Cpf.Should().BeNull();
    }

    [Fact]
    public async Task Ficha_inexistente_e_nao_encontrada_e_anonimo_e_negado()
    {
        var pf = await SalvarPf(_c.Diretor, null);

        (await Obter(_c.Diretor, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await Obter(null, pf.Valor)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public void Consultas_declaram_o_que_registram()
    {
        var id = Guid.CreateVersion7();
        IConsultaAuditavel listar = new ListarClientes("maria", true, 2);
        IConsultaAuditavel obter = new ObterCliente(id);

        listar.TipoRecurso.Should().Be("Cliente");
        listar.Acao.Should().Be(AcaoDeAcesso.Listou);
        listar.RecursoId.Should().BeNull();
        listar.Detalhe.Should().Contain("maria");

        obter.TipoRecurso.Should().Be("Cliente");
        obter.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        obter.RecursoId.Should().Be(id);
    }
}
