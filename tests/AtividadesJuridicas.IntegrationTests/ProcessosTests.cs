using System.Text.Json;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ProcessosTests(BancoFixture banco) : TesteComBanco(banco)
{
    private const string Cnj = "1234567-85.2024.8.02.0001";

    private Guid _admin, _trabalhista, _civel, _maria, _acme, _jose;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        _trabalhista = (await ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"))).Valor;
        _civel = (await ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Cível"))).Valor;
        _maria = await PfAsync("Maria Silva", "529.982.247-25");
        _jose = await PfAsync("José Souza", "111.444.777-35");
        _acme = (await ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null, DadosDe("Acme Ltda"), "11.222.333/0001-81", null, null, null, null))).Valor;
    }

    private static DadosDoClienteEntrada DadosDe(string nome) => new(nome, null, null, new EnderecoEntrada(), null);

    private async Task<Guid> PfAsync(string nome, string cpf) =>
        (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(null, DadosDe(nome), cpf, null, null))).Valor;

    private DadosDoProcessoEntrada Dados(string titulo = "Reclamação trabalhista", string? cnj = Cnj, Guid? area = null,
        string? parte = "Empresa X") => new(
        cnj, titulo, area ?? _trabalhista, "2ª Vara do Trabalho", "Maceió", parte, new DateOnly(2024, 3, 10), 1500.50m, "Urgente");

    private static ClienteDoProcessoEntrada[] Vinculos(params (Guid Id, PoloProcessual Polo)[] itens) =>
        [.. itens.Select(i => new ClienteDoProcessoEntrada(i.Id, i.Polo))];

    private async Task<Guid> CriarAsync(DadosDoProcessoEntrada? dados = null, ClienteDoProcessoEntrada[]? clientes = null)
    {
        var criado = await ExecutarAsync(_admin, new SalvarProcesso(
            null, dados ?? Dados(), clientes ?? Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Passivo))));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    private async Task<Guid> ProducaoAsync()
    {
        var perfil = await IdDoPerfilAsync(TipoPerfil.Producao);
        return (await ExecutarAsync(_admin, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", perfil, null))).Valor;
    }

    private Task<List<ProcessoCliente>> VinculosNoBancoAsync(Guid processoId) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Set<ProcessoCliente>().IgnoreQueryFilters().AsNoTracking()
            .Where(v => v.ProcessoId == processoId).OrderBy(v => v.CriadoEm).ThenBy(v => v.Id).ToListAsync());

    [Fact]
    public async Task Processo_vai_e_volta_do_banco_inteiro_com_os_clientes()
    {
        var id = await CriarAsync();

        var ficha = (await ExecutarAsync(_admin, new ObterProcesso(id))).Valor!;

        ficha.Should().BeEquivalentTo(new
        {
            Id = id, NumeroCnj = Cnj, Titulo = "Reclamação trabalhista", AreaAtuacaoId = _trabalhista, AreaNome = "Trabalhista",
            Status = StatusProcesso.Ativo, Orgao = "2ª Vara do Trabalho", Comarca = "Maceió", ParteContraria = "Empresa X",
            DataDistribuicao = new DateOnly(2024, 3, 10), ValorCausa = 1500.50m, Observacoes = "Urgente",
        });
        ficha.Clientes.Should().BeEquivalentTo(
        [
            new ClienteDoProcessoDetalhe(_acme, "Acme Ltda", "11.222.333/0001-81", PoloProcessual.Passivo, true),
            new ClienteDoProcessoDetalhe(_maria, "Maria Silva", "529.982.247-25", PoloProcessual.Ativo, true),
        ], opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public async Task Criacao_audita_o_processo_e_cada_vinculo()
    {
        var id = await CriarAsync();
        var vinculos = await VinculosNoBancoAsync(id);

        var auditoria = await AuditoriaAsync();
        var doProcesso = auditoria.Single(r => r.Entidade == "Processo" && r.EntidadeId == id);
        doProcesso.Operacao.Should().Be(OperacaoAuditada.Criacao);
        var dados = JsonDocument.Parse(doProcesso.Dados!).RootElement;
        dados.GetProperty("NumeroCnj").GetString().Should().Be("12345678520248020001");
        dados.GetProperty("Status").GetString().Should().Be("Ativo");
        dados.GetProperty("AreaAtuacaoId").GetGuid().Should().Be(_trabalhista);
        dados.GetProperty("ValorCausa").GetDecimal().Should().Be(1500.50m);

        var dosVinculos = auditoria.Where(r => r.Entidade == "ProcessoCliente").ToList();
        dosVinculos.Should().HaveCount(2).And.OnlyContain(r => r.Operacao == OperacaoAuditada.Criacao);
        dosVinculos.Select(r => r.EntidadeId).Should().BeEquivalentTo(vinculos.Select(v => (Guid?)v.Id));
        dosVinculos.Should().OnlyContain(r => r.IdCorrelacao == doProcesso.IdCorrelacao);
        var doVinculoDaMaria = JsonDocument.Parse(
            dosVinculos.Single(r => r.EntidadeId == vinculos.Single(v => v.ClienteId == _maria).Id).Dados!).RootElement;
        doVinculoDaMaria.GetProperty("ProcessoId").GetGuid().Should().Be(id);
        doVinculoDaMaria.GetProperty("ClienteId").GetGuid().Should().Be(_maria);
        doVinculoDaMaria.GetProperty("Polo").GetString().Should().Be("Ativo");
    }

    [Fact]
    public async Task Remover_trocar_o_polo_e_adicionar_clientes_e_exclusao_logica_auditada()
    {
        var id = await CriarAsync();
        var antes = await VinculosNoBancoAsync(id);
        var daMaria = antes.Single(v => v.ClienteId == _maria);
        var daAcme = antes.Single(v => v.ClienteId == _acme);

        var editado = await ExecutarAsync(_admin, new SalvarProcesso(
            id, Dados(), Vinculos((_maria, PoloProcessual.Terceiro), (_jose, PoloProcessual.Ativo))));
        editado.Sucesso.Should().BeTrue(editado.Mensagem);

        // O vínculo removido continua no banco, excluído logicamente; o que ficou é o mesmo registro.
        var depois = await VinculosNoBancoAsync(id);
        depois.Should().HaveCount(3);
        var removido = depois.Single(v => v.Id == daAcme.Id);
        removido.ExcluidoEm.Should().NotBeNull();
        removido.ExcluidoPor.Should().Be(_admin);
        depois.Single(v => v.Id == daMaria.Id).Polo.Should().Be(PoloProcessual.Terceiro);
        depois.Single(v => v.Id == daMaria.Id).ExcluidoEm.Should().BeNull();

        // Recarregado, o processo não traz o vínculo removido de volta.
        var ficha = (await ExecutarAsync(_admin, new ObterProcesso(id))).Valor!;
        ficha.Clientes.Select(c => (c.Nome, c.Polo)).Should().Equal(
            ("José Souza", PoloProcessual.Ativo), ("Maria Silva", PoloProcessual.Terceiro));

        // Um registro por vínculo afetado, e o evento no processo.
        var auditoria = await AuditoriaAsync();
        auditoria.Single(r => r.EntidadeId == daAcme.Id && r.Operacao != OperacaoAuditada.Criacao)
            .Operacao.Should().Be(OperacaoAuditada.Exclusao);
        var troca = auditoria.Single(r => r.EntidadeId == daMaria.Id && r.Operacao == OperacaoAuditada.Alteracao);
        var polo = JsonDocument.Parse(troca.Dados!).RootElement.GetProperty("Polo");
        polo.GetProperty("de").GetString().Should().Be("Ativo");
        polo.GetProperty("para").GetString().Should().Be("Terceiro");
        auditoria.Count(r => r.Entidade == "ProcessoCliente" && r.Operacao == OperacaoAuditada.Criacao).Should().Be(3);

        var evento = auditoria.Single(r => r.Evento == EventosDeCadastro.ClientesDoProcessoAlterados);
        evento.Entidade.Should().Be("Processo");
        evento.EntidadeId.Should().Be(id);
        evento.UsuarioId.Should().Be(_admin);
        var dadosDoEvento = JsonDocument.Parse(evento.Dados!).RootElement;
        dadosDoEvento.GetProperty("adicionados").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_jose);
        dadosDoEvento.GetProperty("removidos").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_acme);
        dadosDoEvento.GetProperty("poloAlterado").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_maria);
    }

    [Fact]
    public async Task Cliente_removido_pode_ser_vinculado_de_novo()
    {
        var id = await CriarAsync();
        await ExecutarAsync(_admin, new SalvarProcesso(id, Dados(), Vinculos((_maria, PoloProcessual.Ativo))));

        var deVolta = await ExecutarAsync(_admin, new SalvarProcesso(
            id, Dados(), Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Terceiro))));

        deVolta.Sucesso.Should().BeTrue(deVolta.Mensagem);
        var daAcme = (await VinculosNoBancoAsync(id)).Where(v => v.ClienteId == _acme).ToList();
        daAcme.Should().HaveCount(2);
        daAcme.Count(v => v.ExcluidoEm == null).Should().Be(1);
    }

    [Fact]
    public async Task Salvar_sem_mudar_nada_nao_gera_registro_nem_evento()
    {
        var id = await CriarAsync();
        var antes = (await AuditoriaAsync()).Count;

        await ExecutarAsync(_admin, new SalvarProcesso(
            id, Dados(cnj: "12345678520248020001"), Vinculos((_acme, PoloProcessual.Passivo), (_maria, PoloProcessual.Ativo))));

        (await AuditoriaAsync()).Should().HaveCount(antes);
    }

    [Fact]
    public async Task Edicao_com_dados_invalidos_nao_muda_os_clientes()
    {
        var id = await CriarAsync();

        var resultado = await ExecutarAsync(_admin, new SalvarProcesso(
            id, Dados(titulo: ""), Vinculos((_jose, PoloProcessual.Ativo))));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        var ficha = (await ExecutarAsync(_admin, new ObterProcesso(id))).Valor!;
        ficha.Titulo.Should().Be("Reclamação trabalhista");
        ficha.Clientes.Select(c => c.Nome).Should().Equal("Acme Ltda", "Maria Silva");
        (await VinculosNoBancoAsync(id)).Should().HaveCount(2).And.OnlyContain(v => v.ExcluidoEm == null);
    }

    [Fact]
    public async Task Numero_duplicado_e_recusado_pelo_caso_de_uso_e_pelo_banco_e_sem_numero_pode_repetir()
    {
        await CriarAsync();

        (await ExecutarAsync(_admin, new SalvarProcesso(null, Dados("Outro", "12345678520248020001"),
            Vinculos((_maria, PoloProcessual.Ativo))))).Erro.Should().Be(TipoErro.Conflito);

        await CriarAsync(Dados("Sem número 1", null));
        await CriarAsync(Dados("Sem número 2", null));

        var direto = () => EmEscopoAsync(_admin, async sp =>
        {
            sp.GetRequiredService<AppDbContext>().Add(Processo.Criar(
                new DadosDoProcesso(NumeroCnj.Criar(Cnj), "Direto", _trabalhista, null, null, null, null, null, null),
                [new ClienteDoProcesso(_maria, PoloProcessual.Ativo)]));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });
        (await direto.Should().ThrowAsync<ExcecaoDeConflito>()).Which.Restricao.Should().Contain("numero_cnj");
    }

    [Fact]
    public async Task Area_e_cliente_inativos_nao_entram_em_vinculo_novo_mas_ficam_em_quem_ja_os_tinha()
    {
        var id = await CriarAsync();
        await ExecutarAsync(_admin, new DefinirAtivoAreaAtuacao(_trabalhista, false));
        await ExecutarAsync(_admin, new DefinirAtivoCliente(_acme, false));
        await ExecutarAsync(_admin, new DefinirAtivoCliente(_jose, false));

        (await ExecutarAsync(_admin, new SalvarProcesso(null, Dados("Novo", null), Vinculos((_maria, PoloProcessual.Ativo)))))
            .Erro.Should().Be(TipoErro.Validacao, "área inativa não entra em processo novo");
        (await ExecutarAsync(_admin, new SalvarProcesso(null, Dados("Novo", null, _civel), Vinculos((_acme, PoloProcessual.Ativo)))))
            .Erro.Should().Be(TipoErro.Validacao, "cliente inativo não entra em processo novo");
        (await ExecutarAsync(_admin, new SalvarProcesso(id, Dados(), Vinculos((_maria, PoloProcessual.Ativo), (_jose, PoloProcessual.Ativo)))))
            .Erro.Should().Be(TipoErro.Validacao, "cliente inativo não entra em vínculo novo");

        var mantido = await ExecutarAsync(_admin, new SalvarProcesso(
            id, Dados("Título novo"), Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Terceiro))));
        mantido.Sucesso.Should().BeTrue(mantido.Mensagem);

        var ficha = (await ExecutarAsync(_admin, new ObterProcesso(id))).Valor!;
        ficha.AreaNome.Should().Be("Trabalhista");
        ficha.Clientes.Single(c => c.ClienteId == _acme).Ativo.Should().BeFalse();
    }

    [Fact]
    public async Task Listagem_filtra_por_cliente_situacao_numero_e_texto_sem_acentos_nem_curingas()
    {
        var reclamacao = await CriarAsync();
        await CriarAsync(Dados("Ação de cobrança 100%", null, _civel, "Banco_Y"), Vinculos((_acme, PoloProcessual.Ativo)));
        await ExecutarAsync(_admin, new AlterarStatusProcesso(reclamacao, StatusProcesso.Suspenso));

        async Task<IEnumerable<string>> TitulosAsync(string? busca = null, Guid? cliente = null, StatusProcesso? status = null) =>
            (await ExecutarAsync(_admin, new ListarProcessos(busca, cliente, status))).Valor!.Itens.Select(p => p.Titulo);

        (await TitulosAsync()).Should().Equal("Ação de cobrança 100%", "Reclamação trabalhista");
        (await TitulosAsync(cliente: _maria)).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync(cliente: _acme)).Should().Equal("Ação de cobrança 100%", "Reclamação trabalhista");
        (await TitulosAsync(status: StatusProcesso.Suspenso)).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("RECLAMACAO")).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("cobranca")).Should().Equal("Ação de cobrança 100%");
        (await TitulosAsync("empresa")).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("%")).Should().Equal("Ação de cobrança 100%");
        (await TitulosAsync("_")).Should().Equal("Ação de cobrança 100%");
        (await TitulosAsync(Cnj)).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("12345678520248020001")).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("xyz")).Should().BeEmpty();

        var item = (await ExecutarAsync(_admin, new ListarProcessos("reclamacao"))).Valor!.Itens.Single();
        item.Should().BeEquivalentTo(new
        {
            NumeroCnj = Cnj, AreaNome = "Trabalhista", Status = StatusProcesso.Suspenso, ParteContraria = "Empresa X",
        });
        item.Clientes.Should().Equal("Acme Ltda", "Maria Silva");
    }

    [Fact]
    public async Task Cliente_removido_do_processo_some_do_filtro_por_cliente_e_da_listagem()
    {
        var id = await CriarAsync();
        await ExecutarAsync(_admin, new SalvarProcesso(id, Dados(), Vinculos((_maria, PoloProcessual.Ativo))));

        (await ExecutarAsync(_admin, new ListarProcessos(ClienteId: _acme))).Valor!.Itens.Should().BeEmpty();
        (await ExecutarAsync(_admin, new ListarProcessos())).Valor!.Itens.Single().Clientes.Should().Equal("Maria Silva");
    }

    [Fact]
    public async Task Mudanca_de_situacao_e_auditada()
    {
        var id = await CriarAsync();

        (await ExecutarAsync(_admin, new AlterarStatusProcesso(id, StatusProcesso.Encerrado))).Sucesso.Should().BeTrue();

        var alteracao = (await AuditoriaAsync()).Single(r => r.EntidadeId == id && r.Operacao == OperacaoAuditada.Alteracao);
        var status = JsonDocument.Parse(alteracao.Dados!).RootElement.GetProperty("Status");
        status.GetProperty("de").GetString().Should().Be("Ativo");
        status.GetProperty("para").GetString().Should().Be("Encerrado");
    }

    [Fact]
    public async Task Producao_consulta_e_fica_registrado_mas_nao_altera()
    {
        var id = await CriarAsync();
        var pedro = await ProducaoAsync();

        (await ExecutarAsync(pedro, new ListarProcessos("reclamação"))).Valor!.Itens.Should().ContainSingle();
        (await ExecutarAsync(pedro, new ObterProcesso(id))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(pedro, new SalvarProcesso(id, Dados("Invadido"), Vinculos((_maria, PoloProcessual.Ativo)))))
            .Erro.Should().Be(TipoErro.AcessoNegado);
        (await ExecutarAsync(pedro, new AlterarStatusProcesso(id, StatusProcesso.Encerrado))).Erro.Should().Be(TipoErro.AcessoNegado);

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso
            .AsNoTracking().Where(a => a.UsuarioId == pedro && a.TipoRecurso == "Processo").ToListAsync());
        acessos.Should().Contain(a => a.Acao == AcaoDeAcesso.Listou && a.Detalhe!.Contains("reclamação"));
        acessos.Should().Contain(a => a.Acao == AcaoDeAcesso.Visualizou && a.RecursoId == id);

        (await AuditoriaAsync()).Count(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == pedro).Should().Be(2);
        var ficha = (await ExecutarAsync(_admin, new ObterProcesso(id))).Valor!;
        ficha.Titulo.Should().Be("Reclamação trabalhista");
        ficha.Status.Should().Be(StatusProcesso.Ativo);
    }

    [Fact]
    public async Task Excluir_o_processo_exclui_logicamente_tambem_os_vinculos()
    {
        var id = await CriarAsync();

        await EmEscopoAsync(_admin, async sp =>
        {
            var processo = await sp.GetRequiredService<IProcessoRepositorio>().ObterPorIdAsync(id, default);
            sp.GetRequiredService<AppDbContext>().Remove(processo!);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });

        (await VinculosNoBancoAsync(id)).Should().HaveCount(2).And.OnlyContain(v => v.ExcluidoEm != null);
        (await ExecutarAsync(_admin, new ObterProcesso(id))).Erro.Should().Be(TipoErro.NaoEncontrado);
        var auditoria = await AuditoriaAsync();
        auditoria.Count(r => r.Operacao == OperacaoAuditada.Exclusao && r.Entidade == "ProcessoCliente").Should().Be(2);
        auditoria.Count(r => r.Operacao == OperacaoAuditada.Exclusao && r.Entidade == "Processo").Should().Be(1);
        await CriarAsync();
    }

    [Fact]
    public async Task Banco_recusa_apagar_fisicamente_um_processo_com_vinculos()
    {
        var id = await CriarAsync();

        var apagar = () => EmEscopoAsync(null, async sp =>
        {
            await sp.GetRequiredService<AppDbContext>().Database
                .ExecuteSqlInterpolatedAsync($"DELETE FROM processos WHERE id = {id}");
            return 0;
        });

        var erro = await apagar.Should().ThrowAsync<Exception>();
        erro.Which.ToString().Should().Contain("23503");
        (await VinculosNoBancoAsync(id)).Should().HaveCount(2);
        (await ExecutarAsync(_admin, new ObterProcesso(id))).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Edicoes_concorrentes_dos_clientes_nao_deixam_o_processo_sem_cliente()
    {
        var id = await CriarAsync(); // maria e acme

        // Dois escopos vivos ao mesmo tempo: EmEscopoAsync aninhado, cada chamada cria o seu.
        await EmEscopoAsync(_admin, async spA =>
        {
            var processo = (await spA.GetRequiredService<IProcessoRepositorio>().ObterPorIdAsync(id, default))!;
            processo.DefinirClientes([new ClienteDoProcesso(_maria, PoloProcessual.Ativo)]);

            var emB = await ExecutarAsync(_admin, new SalvarProcesso(id, Dados(), Vinculos((_acme, PoloProcessual.Passivo))));
            emB.Sucesso.Should().BeTrue(emB.Mensagem);

            var salvarA = () => spA.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            await salvarA.Should().ThrowAsync<ExcecaoDeConcorrencia>();
            return 0;
        });

        var vivos = (await VinculosNoBancoAsync(id)).Where(v => v.ExcluidoEm == null).ToList();
        vivos.Should().ContainSingle().Which.ClienteId.Should().Be(_acme);
    }

    [Fact]
    public async Task Editar_so_os_clientes_carimba_o_processo_sem_registro_de_alteracao_dele()
    {
        var id = await CriarAsync();
        var antes = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Processos.AsNoTracking()
            .SingleAsync(p => p.Id == id));
        antes.AtualizadoEm.Should().BeNull();

        await ExecutarAsync(_admin, new SalvarProcesso(id, Dados(), Vinculos((_maria, PoloProcessual.Terceiro))));

        var depois = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Processos.AsNoTracking()
            .SingleAsync(p => p.Id == id));
        depois.AtualizadoEm.Should().NotBeNull();
        depois.AtualizadoPor.Should().Be(_admin);
        depois.VersaoDosVinculos.Should().Be(1);

        var doProcesso = (await AuditoriaAsync()).Where(r => r.Entidade == "Processo" && r.EntidadeId == id).ToList();
        doProcesso.Should().NotContain(r => r.Operacao == OperacaoAuditada.Alteracao);
        doProcesso.Should().ContainSingle(r => r.Evento == EventosDeCadastro.ClientesDoProcessoAlterados);
    }
}
