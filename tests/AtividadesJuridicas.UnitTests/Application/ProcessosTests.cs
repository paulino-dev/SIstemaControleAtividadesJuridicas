using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class ProcessosTests
{
    private const string Cnj = "1234567-85.2024.8.02.0001";

    private readonly Cenario _c = new();
    private readonly AreaAtuacao _trabalhista = AreaAtuacao.Criar("Trabalhista");
    private readonly AreaAtuacao _civel = AreaAtuacao.Criar("Cível");
    private readonly ClientePessoaFisica _maria;
    private readonly ClientePessoaJuridica _acme;
    private readonly ClientePessoaFisica _jose;

    public ProcessosTests()
    {
        _c.Areas.Itens.AddRange([_trabalhista, _civel]);
        _maria = ClientePessoaFisica.Criar(DadosDe("Maria Silva"), Cpf.Criar("529.982.247-25"), null, null);
        _acme = ClientePessoaJuridica.Criar(DadosDe("Acme Ltda"), Cnpj.Criar("11.222.333/0001-81"), null, null, null, null);
        _jose = ClientePessoaFisica.Criar(DadosDe("José Souza"), Cpf.Criar("111.444.777-35"), null, null);
        _c.Clientes.Itens.AddRange([_maria, _acme, _jose]);
    }

    private static DadosDoCliente DadosDe(string nome) => new(nome, null, null, Endereco.Vazio, null);

    private Usuario Producao() => _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

    private DadosDoProcessoEntrada Dados(string titulo = "Reclamação trabalhista", string? cnj = Cnj, Guid? area = null) => new(
        cnj, titulo, area ?? _trabalhista.Id, "2ª Vara", "Maceió", "Empresa X", new DateOnly(2024, 3, 10), 1500.50m, null);

    private static ClienteDoProcessoEntrada[] Vinculos(params (Cliente Cliente, PoloProcessual Polo)[] itens) =>
        [.. itens.Select(i => new ClienteDoProcessoEntrada(i.Cliente.Id, i.Polo))];

    private Task<Resultado<Guid>> Salvar(Usuario? ator, Guid? id, DadosDoProcessoEntrada? dados = null,
        ClienteDoProcessoEntrada[]? clientes = null) =>
        new SalvarProcessoManipulador(_c.Como(ator), _c.Processos, _c.Areas, _c.Clientes, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new SalvarProcesso(
                id, dados ?? Dados(), clientes ?? Vinculos((_maria, PoloProcessual.Ativo))), default);

    private Task<Resultado> AlterarStatus(Usuario? ator, Guid id, StatusProcesso status) =>
        new AlterarStatusProcessoManipulador(_c.Como(ator), _c.Processos, _c.Unidade)
            .ManipularAsync(new AlterarStatusProcesso(id, status), default);

    private Task<Resultado<Pagina<ProcessoResumo>>> Listar(Usuario? ator, string? busca = null, Guid? cliente = null,
        StatusProcesso? status = null, int pagina = 1) =>
        new ListarProcessosManipulador(_c.Como(ator), _c.Processos)
            .ManipularAsync(new ListarProcessos(busca, cliente, status, pagina), default);

    private Task<Resultado<ProcessoDetalhe>> Obter(Usuario? ator, Guid id) =>
        new ObterProcessoManipulador(_c.Como(ator), _c.Processos).ManipularAsync(new ObterProcesso(id), default);

    // ---- Salvar ----

    [Fact]
    public async Task Diretor_cria_processo_com_numero_normalizado_e_clientes()
    {
        var criado = await Salvar(_c.Diretor, null,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Passivo)));

        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        var processo = _c.Processos.Itens.Should().ContainSingle().Subject;
        processo.Id.Should().Be(criado.Valor);
        processo.NumeroCnj!.Valor.Should().Be("12345678520248020001");
        processo.Status.Should().Be(StatusProcesso.Ativo);
        processo.Clientes.Should().HaveCount(2);
        _c.Unidade.Salvamentos.Should().Be(1);
        _c.Auditoria.Eventos.Should().BeEmpty("a criação já é auditada pelos registros do processo e dos vínculos");
    }

    [Fact]
    public async Task Numero_em_branco_vira_processo_sem_numero_e_dois_sem_numero_sao_aceitos()
    {
        (await Salvar(_c.Diretor, null, Dados("A", "  "))).Sucesso.Should().BeTrue();
        (await Salvar(_c.Diretor, null, Dados("B", null))).Sucesso.Should().BeTrue();

        _c.Processos.Itens.Should().HaveCount(2).And.OnlyContain(p => p.NumeroCnj == null);
    }

    [Fact]
    public async Task Mesmo_numero_com_outra_pontuacao_e_conflito()
    {
        await Salvar(_c.Diretor, null);

        var repetido = await Salvar(_c.Diretor, null, Dados("Outro", "12345678520248020001"));

        repetido.Erro.Should().Be(TipoErro.Conflito);
        _c.Processos.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Conflito_de_unicidade_do_numero_no_banco_vira_conflito_com_a_mensagem_do_numero()
    {
        _c.Unidade.Falha = new ExcecaoDeConflito("duplicado", null, "ix_processos_numero_cnj");

        var resultado = await Salvar(_c.Diretor, null);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("Já existe um processo com este número.");
    }

    [Fact]
    public async Task Conflito_de_outra_restricao_vira_conflito_com_a_mensagem_dos_clientes()
    {
        _c.Unidade.Falha = new ExcecaoDeConflito("duplicado", null, "ix_processos_clientes_processo_id_cliente_id");

        var resultado = await Salvar(_c.Diretor, null);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("Os clientes do processo não podem se repetir.");
    }

    [Theory]
    [InlineData("1.005")]
    [InlineData("10000000000000000")]
    [InlineData("-1")]
    public async Task Valor_da_causa_recusado_pelo_dominio_vira_validacao_e_nada_e_gravado(string texto)
    {
        var dados = Dados() with { ValorCausa = decimal.Parse(texto, System.Globalization.CultureInfo.InvariantCulture) };

        var resultado = await Salvar(_c.Diretor, null, dados);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(int.MaxValue / 20 + 5)]
    public async Task Pagina_absurda_devolve_lista_vazia_com_o_total_certo_sem_estourar(int pagina)
    {
        await Salvar(_c.Diretor, null);

        var lista = (await Listar(_c.Diretor, pagina: pagina)).Valor!;

        lista.Itens.Should().BeEmpty();
        lista.Total.Should().Be(1);
    }

    [Fact]
    public async Task Editar_mantendo_o_proprio_numero_nao_e_conflito()
    {
        var criado = await Salvar(_c.Diretor, null);

        var editado = await Salvar(_c.Diretor, criado.Valor, Dados("Reclamação trabalhista — fase recursal"));

        editado.Sucesso.Should().BeTrue(editado.Mensagem);
        _c.Processos.Itens.Single().Titulo.Should().Be("Reclamação trabalhista — fase recursal");
    }

    [Theory]
    [InlineData("1234567-86.2024.8.02.0001", "Título")]
    [InlineData(Cnj, "")]
    public async Task Numero_ou_titulo_invalido_e_erro_de_validacao(string cnj, string titulo)
    {
        var resultado = await Salvar(_c.Diretor, null, Dados(titulo, cnj));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        _c.Processos.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Sem_cliente_ou_com_cliente_repetido_e_erro_de_validacao()
    {
        var semCliente = await Salvar(_c.Diretor, null, clientes: []);
        var repetido = await Salvar(_c.Diretor, null,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_maria, PoloProcessual.Passivo)));

        semCliente.Erro.Should().Be(TipoErro.Validacao);
        repetido.Erro.Should().Be(TipoErro.Validacao);
        _c.Processos.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Cliente_ou_area_inexistente_e_erro_de_validacao()
    {
        var clienteFantasma = await Salvar(_c.Diretor, null,
            clientes: [new ClienteDoProcessoEntrada(Guid.CreateVersion7(), PoloProcessual.Ativo)]);
        var areaFantasma = await Salvar(_c.Diretor, null, Dados(area: Guid.CreateVersion7()));

        clienteFantasma.Erro.Should().Be(TipoErro.Validacao);
        areaFantasma.Erro.Should().Be(TipoErro.Validacao);
        _c.Processos.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Area_inativa_e_recusada_em_processo_novo_e_na_troca_mas_mantida_em_quem_ja_a_tinha()
    {
        var criado = await Salvar(_c.Diretor, null);
        _trabalhista.Inativar();
        _civel.Inativar();

        (await Salvar(_c.Diretor, null, Dados("Novo", null))).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_c.Diretor, criado.Valor, Dados(area: _civel.Id))).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_c.Diretor, criado.Valor, Dados("Título novo"))).Sucesso.Should().BeTrue();

        _c.Processos.Itens.Single().AreaAtuacaoId.Should().Be(_trabalhista.Id);
    }

    [Fact]
    public async Task Cliente_inativo_e_recusado_em_vinculo_novo_mas_mantido_em_quem_ja_o_tinha()
    {
        var criado = await Salvar(_c.Diretor, null);
        _maria.Inativar();
        _acme.Inativar();

        (await Salvar(_c.Diretor, null, Dados("Novo", null))).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_c.Diretor, criado.Valor,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Passivo)))).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_c.Diretor, criado.Valor,
            clientes: Vinculos((_maria, PoloProcessual.Passivo), (_jose, PoloProcessual.Ativo)))).Sucesso.Should().BeTrue();

        _c.Processos.Itens.Single().Clientes.Select(c => c.ClienteId).Should().BeEquivalentTo([_maria.Id, _jose.Id]);
    }

    [Fact]
    public async Task Trocar_os_clientes_anota_o_evento_no_processo()
    {
        var criado = await Salvar(_c.Diretor, null,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Ativo)));

        await Salvar(_c.Diretor, criado.Valor,
            clientes: Vinculos((_maria, PoloProcessual.Passivo), (_jose, PoloProcessual.Terceiro)));

        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Should().BeEquivalentTo(new
        {
            Evento = EventosDeCadastro.ClientesDoProcessoAlterados, Entidade = "Processo", EntidadeId = criado.Valor,
            Imediato = false,
        });
        evento.Dados.Should().BeEquivalentTo(new
        {
            adicionados = new[] { _jose.Id }, removidos = new[] { _acme.Id }, poloAlterado = new[] { _maria.Id },
        });
    }

    [Fact]
    public async Task Editar_sem_mexer_nos_clientes_nao_anota_evento()
    {
        var criado = await Salvar(_c.Diretor, null);

        await Salvar(_c.Diretor, criado.Valor, Dados("Título novo"));

        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Edicao_recusada_nao_altera_o_processo()
    {
        var criado = await Salvar(_c.Diretor, null);

        var resultado = await Salvar(_c.Diretor, criado.Valor, Dados("Título novo"),
            [new ClienteDoProcessoEntrada(Guid.CreateVersion7(), PoloProcessual.Ativo)]);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        var processo = _c.Processos.Itens.Single();
        processo.Titulo.Should().Be("Reclamação trabalhista");
        processo.Clientes.Should().ContainSingle().Which.ClienteId.Should().Be(_maria.Id);
        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Producao_e_anonimo_nao_criam_editam_nem_mudam_a_situacao()
    {
        var criado = await Salvar(_c.Diretor, null);
        var producao = Producao();

        (await Salvar(producao, null, Dados("Outro", null))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(producao, criado.Valor, Dados("Invadido"))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await AlterarStatus(producao, criado.Valor, StatusProcesso.Encerrado)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(null, null, Dados("Outro", null))).Erro.Should().Be(TipoErro.AcessoNegado);

        var processo = _c.Processos.Itens.Should().ContainSingle().Subject;
        processo.Titulo.Should().Be("Reclamação trabalhista");
        processo.Status.Should().Be(StatusProcesso.Ativo);
    }

    [Fact]
    public async Task Id_inexistente_e_nao_encontrado()
    {
        (await Salvar(_c.Diretor, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await AlterarStatus(_c.Diretor, Guid.CreateVersion7(), StatusProcesso.Suspenso)).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await Obter(_c.Diretor, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    // ---- Situação ----

    [Fact]
    public async Task Diretor_muda_a_situacao_e_definir_a_mesma_nao_grava()
    {
        var criado = await Salvar(_c.Diretor, null);
        var salvamentos = _c.Unidade.Salvamentos;

        (await AlterarStatus(_c.Diretor, criado.Valor, StatusProcesso.Arquivado)).Sucesso.Should().BeTrue();
        (await AlterarStatus(_c.Diretor, criado.Valor, StatusProcesso.Arquivado)).Sucesso.Should().BeTrue();

        _c.Processos.Itens.Single().Status.Should().Be(StatusProcesso.Arquivado);
        _c.Unidade.Salvamentos.Should().Be(salvamentos + 1);
    }

    // ---- Consultar ----

    [Fact]
    public async Task Producao_lista_com_area_clientes_e_numero_formatado()
    {
        await Salvar(_c.Diretor, null,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Passivo)));

        var item = (await Listar(Producao())).Valor!.Itens.Should().ContainSingle().Subject;

        item.Should().BeEquivalentTo(new
        {
            NumeroCnj = Cnj, Titulo = "Reclamação trabalhista", AreaNome = "Trabalhista",
            Status = StatusProcesso.Ativo, ParteContraria = "Empresa X",
        });
        item.Clientes.Should().Equal("Acme Ltda", "Maria Silva");
    }

    [Fact]
    public async Task Filtra_por_cliente_situacao_e_texto_sem_acentos()
    {
        var a = await Salvar(_c.Diretor, null, Dados("Reclamação trabalhista", Cnj));
        await Salvar(_c.Diretor, null, Dados("Ação de cobrança", null, _civel.Id), Vinculos((_acme, PoloProcessual.Ativo)));
        await AlterarStatus(_c.Diretor, a.Valor, StatusProcesso.Suspenso);

        async Task<IEnumerable<string>> TitulosAsync(string? busca = null, Guid? cliente = null, StatusProcesso? status = null) =>
            (await Listar(_c.Diretor, busca, cliente, status)).Valor!.Itens.Select(p => p.Titulo);

        (await TitulosAsync()).Should().Equal("Ação de cobrança", "Reclamação trabalhista");
        (await TitulosAsync(cliente: _acme.Id)).Should().Equal("Ação de cobrança");
        (await TitulosAsync(status: StatusProcesso.Suspenso)).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("reclamacao")).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("EMPRESA x")).Should().Equal("Ação de cobrança", "Reclamação trabalhista");
        (await TitulosAsync("12345678520248020001")).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("inexistente")).Should().BeEmpty();
    }

    [Fact]
    public async Task Producao_abre_a_ficha_com_os_clientes_e_seus_documentos()
    {
        var criado = await Salvar(_c.Diretor, null,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Passivo)));

        var ficha = (await Obter(Producao(), criado.Valor)).Valor!;

        ficha.Should().BeEquivalentTo(new
        {
            Id = criado.Valor, NumeroCnj = Cnj, Titulo = "Reclamação trabalhista", AreaAtuacaoId = _trabalhista.Id,
            AreaNome = "Trabalhista", Status = StatusProcesso.Ativo, Orgao = "2ª Vara", Comarca = "Maceió",
            ParteContraria = "Empresa X", DataDistribuicao = new DateOnly(2024, 3, 10), ValorCausa = 1500.50m,
        });
        ficha.Clientes.Should().BeEquivalentTo(
        [
            new ClienteDoProcessoDetalhe(_acme.Id, "Acme Ltda", "11.222.333/0001-81", PoloProcessual.Passivo, true),
            new ClienteDoProcessoDetalhe(_maria.Id, "Maria Silva", "529.982.247-25", PoloProcessual.Ativo, true),
        ], opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public async Task Anonimo_nao_consulta()
    {
        var criado = await Salvar(_c.Diretor, null);

        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Obter(null, criado.Valor)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public void Consultas_declaram_o_que_registram()
    {
        var id = Guid.CreateVersion7();
        IConsultaAuditavel listar = new ListarProcessos("Reclamação", id, StatusProcesso.Ativo, 2);
        IConsultaAuditavel obter = new ObterProcesso(id);

        listar.TipoRecurso.Should().Be("Processo");
        listar.Acao.Should().Be(AcaoDeAcesso.Listou);
        listar.RecursoId.Should().BeNull();
        listar.Detalhe.Should().Contain("Reclamação").And.NotContain("\\u00");

        obter.TipoRecurso.Should().Be("Processo");
        obter.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        obter.RecursoId.Should().Be(id);
    }

    private sealed class UnidadeQueConflita : IUnidadeDeTrabalho
    {
        public Task SalvarAsync(CancellationToken ct) => throw new ExcecaoDeConcorrencia("outro");
    }

    [Fact]
    public async Task Concorrencia_vira_conflito_com_mensagem_de_recarregar()
    {
        var criado = await Salvar(_c.Diretor, null);
        const string mensagem = "O processo foi alterado por outra pessoa. Recarregue e tente novamente.";

        var salvar = await new SalvarProcessoManipulador(
                _c.Como(_c.Diretor), _c.Processos, _c.Areas, _c.Clientes, new UnidadeQueConflita(), _c.Auditoria)
            .ManipularAsync(new SalvarProcesso(criado.Valor, Dados("Novo título"), Vinculos((_maria, PoloProcessual.Ativo))), default);
        var status = await new AlterarStatusProcessoManipulador(_c.Como(_c.Diretor), _c.Processos, new UnidadeQueConflita())
            .ManipularAsync(new AlterarStatusProcesso(criado.Valor, StatusProcesso.Encerrado), default);

        salvar.Erro.Should().Be(TipoErro.Conflito);
        salvar.Mensagem.Should().Be(mensagem);
        status.Erro.Should().Be(TipoErro.Conflito);
        status.Mensagem.Should().Be(mensagem);
    }
}
