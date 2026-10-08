using System.Text.Json;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>As regras do cadastro de processos que dependem das atividades, contra o PostgreSQL.</summary>
[Collection(ColecaoBanco.Nome)]
public class ProcessosEAtividadesTests(BancoFixture banco) : TesteComBanco(banco)
{
    private static readonly DateOnly Entrega = new(2090, 6, 15);

    private Guid _diana, _pedro, _trabalhista, _civel, _maria, _acme;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _diana = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _pedro = (await ExecutarAsync(_diana, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null))).Valor;
        _trabalhista = (await ExecutarAsync(_diana, new SalvarAreaAtuacao(null, "Trabalhista"))).Valor;
        _civel = (await ExecutarAsync(_diana, new SalvarAreaAtuacao(null, "Cível"))).Valor;
        _maria = (await ExecutarAsync(_diana, new SalvarClientePessoaFisica(null, DadosDe("Maria Silva"), "529.982.247-25", null, null))).Valor;
        _acme = (await ExecutarAsync(_diana, new SalvarClientePessoaJuridica(
            null, DadosDe("Acme Ltda"), "11.222.333/0001-81", null, null, null, null))).Valor;
    }

    private static DadosDoClienteEntrada DadosDe(string nome) => new(nome, null, null, new EnderecoEntrada(), null);

    private DadosDoProcessoEntrada Dados(string titulo = "Reclamação trabalhista", Guid? area = null, string? parte = "Empresa X") =>
        new(null, titulo, area ?? _trabalhista, "2ª Vara", "Maceió", parte, new DateOnly(2024, 3, 10), 1500.50m, null);

    private async Task<Guid> ProcessoAsync(string titulo = "Reclamação trabalhista", params Guid[] clientes)
    {
        var vinculos = (clientes.Length == 0 ? [_maria, _acme] : clientes)
            .Select(c => new ClienteDoProcessoEntrada(c, PoloProcessual.Ativo)).ToArray();
        var criado = await ExecutarAsync(_diana, new SalvarProcesso(null, Dados(titulo), vinculos));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    private async Task<Guid> AtividadeAsync(Guid cliente, Guid processo)
    {
        var criada = await ExecutarAsync(_diana, new SalvarAtividade(
            null, "Elaborar contestação", "Prazo de 15 dias.", Entrega, cliente, processo, null));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        return criada.Valor;
    }

    private async Task DirecionarAsync(Guid id, Guid revisor, params Guid[] responsaveis)
    {
        var definido = await ExecutarAsync(_diana, new DefinirResponsaveisDaAtividade(
            id, [.. responsaveis.Select(r => new ResponsavelEntrada(r, null))], revisor));
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
    }

    private Task<Resultado<Guid>> EditarAsync(Guid id, DadosDoProcessoEntrada dados, params Guid[] clientes) =>
        ExecutarAsync(_diana, new SalvarProcesso(
            id, dados, [.. clientes.Select(c => new ClienteDoProcessoEntrada(c, PoloProcessual.Passivo))]));

    private Task<Atividade> AtividadeNoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Atividades.IgnoreQueryFilters().AsNoTracking().SingleAsync(a => a.Id == id));

    private Task<Processo> ProcessoNoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Processos.AsNoTracking().Include(p => p.Clientes).SingleAsync(p => p.Id == id));

    private Task<List<AtividadeHistoricoStatus>> HistoricoNoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Set<AtividadeHistoricoStatus>().IgnoreQueryFilters().AsNoTracking()
            .Where(h => h.AtividadeId == id).OrderBy(h => h.Ordem).ToListAsync());

    private static JsonElement DadosDe(RegistroAuditoria registro) => JsonDocument.Parse(registro.Dados!).RootElement;

    [Fact]
    public async Task Retirar_cliente_com_atividade_e_recusado_e_o_processo_fica_como_estava()
    {
        var processo = await ProcessoAsync();
        await AtividadeAsync(_acme, processo);
        var alteracoesAntes = (await AuditoriaAsync()).Count(r => r.Entidade == "Processo" && r.Operacao == OperacaoAuditada.Alteracao);

        var recusado = await EditarAsync(processo, Dados("Título alterado", _civel, "Outra parte"), _maria);

        recusado.Erro.Should().Be(TipoErro.Validacao);
        recusado.Mensagem.Should().Be("O cliente Acme Ltda tem atividade vinculada a este processo e não pode ser retirado.");
        var depois = await ProcessoNoBancoAsync(processo);
        depois.Titulo.Should().Be("Reclamação trabalhista");
        depois.ParteContraria.Should().Be("Empresa X");
        depois.AreaAtuacaoId.Should().Be(_trabalhista);
        depois.Clientes.Select(c => c.ClienteId).Should().BeEquivalentTo([_maria, _acme]);
        depois.Clientes.Should().OnlyContain(c => c.Polo == PoloProcessual.Ativo);
        (await AuditoriaAsync()).Count(r => r.Entidade == "Processo" && r.Operacao == OperacaoAuditada.Alteracao)
            .Should().Be(alteracoesAntes);
    }

    [Fact]
    public async Task Atividade_excluida_nao_impede_retirar_o_cliente()
    {
        var processo = await ProcessoAsync();
        var atividade = await AtividadeAsync(_acme, processo);
        (await ExecutarAsync(_diana, new ExcluirAtividade(atividade))).Sucesso.Should().BeTrue();

        var editado = await EditarAsync(processo, Dados(), _maria);

        editado.Sucesso.Should().BeTrue(editado.Mensagem);
        (await ProcessoNoBancoAsync(processo)).Clientes.Should().ContainSingle().Which.ClienteId.Should().Be(_maria);
    }

    [Fact]
    public async Task Atividade_excluida_nao_acompanha_a_troca_de_area_do_processo()
    {
        var processo = await ProcessoAsync();
        var excluida = await AtividadeAsync(_maria, processo);
        var viva = await AtividadeAsync(_acme, processo);
        (await ExecutarAsync(_diana, new ExcluirAtividade(excluida))).Sucesso.Should().BeTrue();

        var editado = await EditarAsync(processo, Dados(area: _civel), _maria, _acme);

        editado.Sucesso.Should().BeTrue(editado.Mensagem);
        (await AtividadeNoBancoAsync(excluida)).AreaAtuacaoId.Should().Be(_trabalhista, "a excluída não é tocada");
        (await AtividadeNoBancoAsync(viva)).AreaAtuacaoId.Should().Be(_civel);
        (await AuditoriaAsync()).Should().NotContain(r => r.Entidade == "Atividade" && r.EntidadeId == excluida
            && r.Operacao == OperacaoAuditada.Alteracao);
    }

    [Fact]
    public async Task Trocar_a_area_do_processo_atualiza_as_atividades_na_mesma_transacao()
    {
        var processo = await ProcessoAsync();
        var outroProcesso = await ProcessoAsync("Outro processo");
        var viva = await AtividadeAsync(_maria, processo);
        var concluida = await AtividadeAsync(_acme, processo);
        var deOutro = await AtividadeAsync(_maria, outroProcesso);

        // A concluída: Diana é a única responsável e a revisora, e conclui a própria parte.
        await DirecionarAsync(concluida, _diana, _diana);
        (await ExecutarAsync(_diana, new ConcluirMinhaParte(concluida))).Sucesso.Should().BeTrue();
        (await AtividadeNoBancoAsync(concluida)).Status.Should().Be(StatusAtividade.Concluida);

        var historicoVivaAntes = await HistoricoNoBancoAsync(viva);
        var historicoConcluidaAntes = await HistoricoNoBancoAsync(concluida);
        var intactaAntes = await AtividadeNoBancoAsync(deOutro);

        var editado = await EditarAsync(processo, Dados(area: _civel), _maria, _acme);

        editado.Sucesso.Should().BeTrue(editado.Mensagem);
        (await ProcessoNoBancoAsync(processo)).AreaAtuacaoId.Should().Be(_civel);
        (await AtividadeNoBancoAsync(viva)).AreaAtuacaoId.Should().Be(_civel);
        (await AtividadeNoBancoAsync(concluida)).AreaAtuacaoId.Should().Be(_civel);
        (await AtividadeNoBancoAsync(concluida)).Status.Should().Be(StatusAtividade.Concluida);
        var intactaDepois = await AtividadeNoBancoAsync(deOutro);
        intactaDepois.AreaAtuacaoId.Should().Be(_trabalhista);
        intactaDepois.Versao.Should().Be(intactaAntes.Versao);

        (await HistoricoNoBancoAsync(viva)).Should().HaveCount(historicoVivaAntes.Count);
        (await HistoricoNoBancoAsync(concluida)).Should().HaveCount(historicoConcluidaAntes.Count);

        var auditoria = await AuditoriaAsync();
        var doProcesso = auditoria.Where(r => r.Entidade == "Processo" && r.EntidadeId == processo
            && r.Operacao == OperacaoAuditada.Alteracao && DadosDe(r).TryGetProperty("AreaAtuacaoId", out _)).Should().ContainSingle().Subject;
        doProcesso.IdCorrelacao.Should().NotBeNull();

        var dasAtividades = auditoria.Where(r => r.Entidade == "Atividade" && r.Operacao == OperacaoAuditada.Alteracao
            && DadosDe(r).TryGetProperty("AreaAtuacaoId", out _)).ToList();
        dasAtividades.Select(r => r.EntidadeId).Should().BeEquivalentTo([(Guid?)viva, concluida]);
        foreach (var registro in dasAtividades)
        {
            var dados = DadosDe(registro);
            dados.EnumerateObject().Select(p => p.Name).Should().Equal("AreaAtuacaoId");
            dados.GetProperty("AreaAtuacaoId").GetProperty("de").GetGuid().Should().Be(_trabalhista);
            dados.GetProperty("AreaAtuacaoId").GetProperty("para").GetGuid().Should().Be(_civel);
            registro.IdCorrelacao.Should().Be(doProcesso.IdCorrelacao);
        }

        auditoria.Should().NotContain(r => r.Entidade == "Atividade" && r.EntidadeId == deOutro
            && r.Operacao == OperacaoAuditada.Alteracao);
    }

    [Fact]
    public async Task Atividade_alterada_ao_mesmo_tempo_faz_a_troca_de_area_responder_conflito()
    {
        var processo = await ProcessoAsync();
        var atividade = await AtividadeAsync(_maria, processo);
        await DirecionarAsync(atividade, _diana, _pedro, _diana);

        await EmEscopoAsync(_diana, async spA =>
        {
            // O escopo A carrega o processo e as atividades e troca a área, sem gravar ainda.
            var existente = (await spA.GetRequiredService<IProcessoRepositorio>().ObterPorIdAsync(processo, default))!;
            existente.Alterar(new DadosDoProcesso(
                null, existente.Titulo, _civel, null, null, null, null, null, null));
            (await spA.GetRequiredService<IAtividadesDoProcesso>().AcompanharAreaAsync(processo, _civel, default))
                .Should().Be(1);

            // O escopo B conclui uma parte da mesma atividade e grava.
            (await ExecutarAsync(_pedro, new ConcluirMinhaParte(atividade))).Sucesso.Should().BeTrue();

            var salvar = () => spA.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            await salvar.Should().ThrowAsync<ExcecaoDeConcorrencia>();
            return 0;
        });

        (await ProcessoNoBancoAsync(processo)).AreaAtuacaoId.Should().Be(_trabalhista);
        (await AtividadeNoBancoAsync(atividade)).AreaAtuacaoId.Should().Be(_trabalhista);
    }
}
