using System.Text.Json;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AndamentoTests(BancoFixture banco) : TesteComBanco(banco)
{
    private const string Cnj = "1234567-85.2024.8.02.0001";
    private static readonly DateOnly Entrega = new(2090, 6, 15);

    private Guid _admin, _pedro, _maria, _jose, _carla, _joao, _trabalhista, _civel;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _pedro = (await ExecutarAsync(_admin, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null))).Valor;
        _maria = await ClienteAsync("Maria Silva", "529.982.247-25");
        _jose = await ClienteAsync("José Souza", "111.444.777-35");
        _carla = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _maria));
        _joao = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _jose, "João Cliente", "joao@x.com"));
        _trabalhista = (await ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"))).Valor;
        _civel = (await ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Cível"))).Valor;
    }

    private async Task<Guid> ClienteAsync(string nome, string cpf) =>
        (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada(nome, null, null, new EnderecoEntrada(), null), cpf, null, null))).Valor;

    private async Task<Guid> ProcessoAsync(string? cnj, string titulo, Guid area, Guid cliente)
    {
        var salvo = await ExecutarAsync(_admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(cnj, titulo, area, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(cliente, PoloProcessual.Ativo)]));
        salvo.Sucesso.Should().BeTrue(salvo.Mensagem);
        return salvo.Valor;
    }

    private async Task<Guid> CriarAsync(
        string titulo, Guid? cliente, Guid? processo = null, string descricao = "Texto da atividade.")
    {
        var criada = await ExecutarAsync(_admin, new SalvarAtividade(
            null, titulo, descricao, Entrega, cliente, processo, null, 1));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        return criada.Valor;
    }

    private async Task DirecionarAsync(Guid id, string direcionamento = "Redigir")
    {
        var definido = await ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            id, [new ResponsavelEntrada(_pedro, direcionamento)], _admin));
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
    }

    /// <summary>Pelo fluxo real: direcionar, o responsável conclui a parte, o revisor aprova.</summary>
    private async Task ConcluirAsync(Guid id)
    {
        await DirecionarAsync(id);
        (await ExecutarAsync(_pedro, new ConcluirMinhaParte(id))).Sucesso.Should().BeTrue();
        var aprovada = await ExecutarAsync(_admin, new AprovarAtividade(id));
        aprovada.Sucesso.Should().BeTrue(aprovada.Mensagem);
    }

    private async Task<AndamentoDoCliente> AndamentoAsync(Guid usuario, ListarMeuAndamento? consulta = null)
    {
        var resultado = await ExecutarAsync(usuario, consulta ?? new ListarMeuAndamento());
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor!;
    }

    private static string[] Titulos(AndamentoDoCliente andamento) => [.. andamento.Atividades.Itens.Select(a => a.Titulo)];

    [Fact]
    public async Task Andamento_traz_so_as_atividades_vivas_do_cliente_com_os_totais()
    {
        await CriarAsync("Criada da Maria", _maria);
        await DirecionarAsync(await CriarAsync("Direcionada da Maria", _maria));
        await ConcluirAsync(await CriarAsync("Concluída da Maria", _maria));
        var excluida = await CriarAsync("Excluída da Maria", _maria);
        (await ExecutarAsync(_admin, new ExcluirAtividade(excluida))).Sucesso.Should().BeTrue();
        await CriarAsync("Do José", _jose);
        await CriarAsync("Avulsa", null);

        var andamento = await AndamentoAsync(_carla);

        Titulos(andamento).Should().Equal("Direcionada da Maria", "Criada da Maria", "Concluída da Maria");
        andamento.Atividades.Total.Should().Be(3);
        andamento.EmAndamento.Should().Be(2);
        andamento.Concluidas.Should().Be(1);
        var doJose = await AndamentoAsync(_joao);
        Titulos(doJose).Should().Equal("Do José");
    }

    [Fact]
    public async Task Ordem_em_andamento_primeiro_e_concluidas_da_mais_recente_para_a_mais_antiga()
    {
        // Criadas nesta ordem: A, B, C ficam em andamento; X, Y, Z são concluídas na ordem Y, Z, X (diferente da criação).
        await CriarAsync("A em andamento", _maria);
        await DirecionarAsync(await CriarAsync("B em andamento", _maria));
        await CriarAsync("C em andamento", _maria);
        var x = await CriarAsync("X concluída por último", _maria);
        var y = await CriarAsync("Y concluída primeiro", _maria);
        var z = await CriarAsync("Z concluída no meio", _maria);
        await ConcluirAsync(y);
        await ConcluirAsync(z);
        await ConcluirAsync(x);

        var andamento = await AndamentoAsync(_carla);

        Titulos(andamento).Should().Equal(
            "C em andamento", "B em andamento", "A em andamento",
            "X concluída por último", "Z concluída no meio", "Y concluída primeiro");
        andamento.Atividades.Itens.Skip(3).Select(a => a.ConcluidaEm!.Value).Should().BeInDescendingOrder();
        andamento.Atividades.Itens.Take(3).Should().OnlyContain(a => a.ConcluidaEm == null);
    }

    [Fact]
    public async Task Atividade_reaberta_volta_a_em_andamento_sem_data_de_conclusao()
    {
        var id = await CriarAsync("Reaberta depois", _maria);
        await ConcluirAsync(id);
        var concluida = (await AndamentoAsync(_carla)).Atividades.Itens.Should().ContainSingle().Subject;
        concluida.Situacao.Should().Be(SituacaoDoAndamento.Concluida);
        concluida.ConcluidaEm.Should().NotBeNull();

        var reaberta = await ExecutarAsync(_admin, new ReabrirAtividade(id, "Cliente pediu ajuste", [_pedro]));
        reaberta.Sucesso.Should().BeTrue(reaberta.Mensagem);

        var andamento = await AndamentoAsync(_carla);
        var linha = andamento.Atividades.Itens.Should().ContainSingle().Subject;
        linha.Situacao.Should().Be(SituacaoDoAndamento.EmAndamento);
        linha.ConcluidaEm.Should().BeNull();
        andamento.EmAndamento.Should().Be(1);
        andamento.Concluidas.Should().Be(0);
    }

    [Fact]
    public async Task Atividade_excluida_ou_que_muda_de_cliente_sai_do_andamento()
    {
        var excluida = await CriarAsync("Será excluída", _maria);
        var trocada = await CriarAsync("Muda de cliente", _maria);
        await CriarAsync("Fica", _maria);
        Titulos(await AndamentoAsync(_carla)).Should().Equal("Fica", "Muda de cliente", "Será excluída");

        (await ExecutarAsync(_admin, new ExcluirAtividade(excluida))).Sucesso.Should().BeTrue();
        var edicao = await ExecutarAsync(_admin, new SalvarAtividade(
            trocada, "Muda de cliente", "Texto da atividade.", Entrega, _jose, null, null, 1));
        edicao.Sucesso.Should().BeTrue(edicao.Mensagem);

        var daCarla = await AndamentoAsync(_carla);
        Titulos(daCarla).Should().Equal("Fica");
        daCarla.EmAndamento.Should().Be(1);
        Titulos(await AndamentoAsync(_joao)).Should().Equal("Muda de cliente");
    }

    [Fact]
    public async Task Processo_aparece_pelo_numero_cnj_e_na_falta_dele_pelo_titulo()
    {
        var comNumero = await ProcessoAsync(Cnj, "Reclamação trabalhista", _trabalhista, _maria);
        var semNumero = await ProcessoAsync(null, "Ação de cobrança", _civel, _maria);
        await CriarAsync("Com número", _maria, comNumero);
        await CriarAsync("Sem número", _maria, semNumero);
        await CriarAsync("Sem processo", _maria);

        var linhas = (await AndamentoAsync(_carla)).Atividades.Itens.ToDictionary(a => a.Titulo);

        linhas["Com número"].Processo.Should().Be(Cnj);
        linhas["Com número"].AreaNome.Should().Be("Trabalhista");
        linhas["Sem número"].Processo.Should().Be("Ação de cobrança");
        linhas["Sem número"].AreaNome.Should().Be("Cível");
        linhas["Sem processo"].Processo.Should().BeNull();
        linhas["Sem processo"].AreaNome.Should().BeNull();
    }

    [Fact]
    public async Task Trocar_o_cliente_do_usuario_troca_o_andamento_que_ele_ve()
    {
        await CriarAsync("Da Maria", _maria);
        await CriarAsync("Do José", _jose);
        Titulos(await AndamentoAsync(_carla)).Should().Equal("Da Maria");

        var perfilCliente = await IdDoPerfilAsync(TipoPerfil.Cliente);
        var troca = await ExecutarAsync(_admin, new EditarUsuario(
            _carla, "Carla Cliente", UsuariosDeClienteDeTeste.EmailDaCarla, perfilCliente, null, _jose));
        troca.Sucesso.Should().BeTrue(troca.Mensagem);

        Titulos(await AndamentoAsync(_carla)).Should().Equal("Do José");
    }

    [Fact]
    public async Task Cliente_sem_atividades_recebe_lista_vazia_e_totais_zerados()
    {
        await CriarAsync("Da Maria", _maria);

        var andamento = await AndamentoAsync(_joao);

        andamento.Atividades.Itens.Should().BeEmpty();
        andamento.Atividades.Total.Should().Be(0);
        andamento.EmAndamento.Should().Be(0);
        andamento.Concluidas.Should().Be(0);
    }

    [Fact]
    public async Task Descricao_responsaveis_e_direcionamentos_nao_saem_na_resposta()
    {
        var id = await CriarAsync("Petição inicial", _maria, descricao: "DESCRICAO-QUE-SO-A-EQUIPE-LE-7Q3");
        await DirecionarAsync(id, "DIRECIONAMENTO-INTERNO-9K2");

        var andamento = await AndamentoAsync(_carla);
        var json = JsonSerializer.Serialize(andamento, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        json.Should().Contain("Petição inicial");
        json.Should().NotContain("DESCRICAO-QUE-SO-A-EQUIPE-LE-7Q3");
        json.Should().NotContain("DIRECIONAMENTO-INTERNO-9K2");
        json.Should().NotContain("Pedro");
        json.Should().NotContain(id.ToString());
        json.Should().NotContain("Direcionada", "o status interno não sai: só a situação simplificada");
    }

    [Fact]
    public async Task Filtro_por_situacao_traz_so_a_situacao_pedida_e_nao_muda_os_totais()
    {
        await CriarAsync("Criada da Maria", _maria);
        await DirecionarAsync(await CriarAsync("Direcionada da Maria", _maria));
        await ConcluirAsync(await CriarAsync("Concluída da Maria", _maria));

        var concluidas = await AndamentoAsync(_carla, new ListarMeuAndamento(SituacaoDoAndamento.Concluida));
        var emAndamento = await AndamentoAsync(_carla, new ListarMeuAndamento(SituacaoDoAndamento.EmAndamento));
        var todas = await AndamentoAsync(_carla);

        Titulos(concluidas).Should().Equal("Concluída da Maria");
        concluidas.Atividades.Total.Should().Be(1);
        Titulos(emAndamento).Should().Equal("Direcionada da Maria", "Criada da Maria");
        emAndamento.Atividades.Total.Should().Be(2);
        Titulos(todas).Should().Equal("Direcionada da Maria", "Criada da Maria", "Concluída da Maria");
        todas.Atividades.Total.Should().Be(3);
        foreach (var andamento in new[] { concluidas, emAndamento, todas })
        {
            andamento.EmAndamento.Should().Be(2);
            andamento.Concluidas.Should().Be(1);
        }
    }

    [Fact]
    public async Task Andamento_e_paginado()
    {
        var tamanho = AtividadesOpcoes.TamanhoDaPagina;
        for (var i = 1; i <= tamanho + 1; i++) await CriarAsync($"Atividade {i:00}", _maria);

        var primeira = await AndamentoAsync(_carla);
        var segunda = await AndamentoAsync(_carla, new ListarMeuAndamento(Pagina: 2));
        var foraDaFaixa = await AndamentoAsync(_carla, new ListarMeuAndamento(Pagina: 0));

        primeira.Atividades.Itens.Should().HaveCount(tamanho);
        primeira.Atividades.Itens[0].Titulo.Should().Be($"Atividade {tamanho + 1:00}");
        Titulos(segunda).Should().Equal("Atividade 01");
        primeira.Atividades.Total.Should().Be(tamanho + 1);
        segunda.Atividades.Total.Should().Be(tamanho + 1);
        primeira.Atividades.Numero.Should().Be(1);
        segunda.Atividades.Numero.Should().Be(2);
        // Paginacao.Normalizar leva a página 0 para a 1.
        foraDaFaixa.Atividades.Numero.Should().Be(1);
        Titulos(foraDaFaixa).Should().Equal(Titulos(primeira));
        segunda.EmAndamento.Should().Be(tamanho + 1);
    }

    [Fact]
    public async Task Consulta_fica_no_registro_de_consultas()
    {
        await CriarAsync("Da Maria", _maria);

        await AndamentoAsync(_carla, new ListarMeuAndamento(SituacaoDoAndamento.EmAndamento));

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso
            .AsNoTracking().Where(a => a.UsuarioId == _carla && a.TipoRecurso == "Atividade").ToListAsync());
        var acesso = acessos.Should().ContainSingle().Subject;
        acesso.Acao.Should().Be(AcaoDeAcesso.Listou);
        acesso.RecursoId.Should().BeNull();
        acesso.Detalhe.Should().Contain("EmAndamento");
    }
}
