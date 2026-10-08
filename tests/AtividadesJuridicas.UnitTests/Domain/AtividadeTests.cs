using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Domain;

public class AtividadeTests
{
    private static readonly DateTime T0 = new(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Entrega = new(2026, 10, 20);

    // Pessoas: dois diretores e três de Produção.
    private readonly PessoaDaAtividade _diana = new(Guid.CreateVersion7(), TipoPerfil.Diretoria, true);
    private readonly PessoaDaAtividade _davi = new(Guid.CreateVersion7(), TipoPerfil.Diretoria, true);
    private readonly PessoaDaAtividade _pedro = new(Guid.CreateVersion7(), TipoPerfil.Producao, true);
    private readonly PessoaDaAtividade _paula = new(Guid.CreateVersion7(), TipoPerfil.Producao, true);
    private readonly PessoaDaAtividade _ester = new(Guid.CreateVersion7(), TipoPerfil.Producao, true);

    private static DadosDaAtividade Dados(string titulo = "Elaborar contestação", DateOnly? entrega = null, bool semEntrega = false) =>
        new(titulo, "Prazo de 15 dias.", semEntrega ? null : entrega ?? Entrega);

    private Atividade Nova(bool semEntrega = false) =>
        Atividade.Criar(Dados(semEntrega: semEntrega), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);

    private static ResponsavelInformado R(PessoaDaAtividade pessoa, string? direcionamento = null) => new(pessoa, direcionamento);

    /// <summary>Direcionada a Pedro e Paula, com Diana de revisora.</summary>
    private Atividade Direcionada()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_pedro, "Redigir"), R(_paula, "Juntar documentos")], _diana, T0.AddMinutes(1), _diana.UsuarioId);
        return atividade;
    }

    /// <summary>Feita: Pedro e Paula concluíram.</summary>
    private Atividade Feita()
    {
        var atividade = Direcionada();
        atividade.ConcluirParte(_pedro, T0.AddHours(1));
        atividade.ConcluirParte(_paula, T0.AddHours(2));
        return atividade;
    }

    private Atividade Concluida()
    {
        var atividade = Feita();
        atividade.Aprovar(_diana, T0.AddHours(3));
        return atividade;
    }

    // O histórico é mantido na ordem das transições; Ordem numera cada linha dentro da atividade.
    private static (StatusAtividade? De, StatusAtividade Para)[] Percurso(Atividade atividade) =>
        [.. atividade.Historico.OrderBy(h => h.Ordem).Select(h => (h.StatusAnterior, h.StatusNovo))];

    // ---- Criação e dados ----

    [Fact]
    public void Nasce_criada_com_uma_linha_de_historico_e_sem_responsaveis()
    {
        var atividade = Nova();

        atividade.Status.Should().Be(StatusAtividade.Criada);
        atividade.Titulo.Should().Be("Elaborar contestação");
        atividade.Descricao.Should().Be("Prazo de 15 dias.");
        atividade.DataEntrega.Should().Be(Entrega);
        atividade.TipoAutor.Should().Be(TipoAutor.Usuario);
        atividade.AutorId.Should().Be(_diana.UsuarioId);
        atividade.RevisorId.Should().BeNull();
        atividade.ConcluidaEm.Should().BeNull();
        atividade.Responsaveis.Should().BeEmpty();
        var linha = atividade.Historico.Should().ContainSingle().Subject;
        linha.StatusAnterior.Should().BeNull();
        linha.StatusNovo.Should().Be(StatusAtividade.Criada);
        linha.UsuarioId.Should().Be(_diana.UsuarioId);
        linha.DataHora.Should().Be(T0);
        linha.Ordem.Should().Be(1);
        linha.AtividadeId.Should().Be(atividade.Id);
    }

    [Fact]
    public void Atividade_do_sistema_nao_tem_autor()
    {
        var atividade = Atividade.Criar(Dados(), VinculoDaAtividade.Avulsa, TipoAutor.Sistema, null, T0);

        atividade.TipoAutor.Should().Be(TipoAutor.Sistema);
        atividade.AutorId.Should().BeNull();
        atividade.Historico.Single().UsuarioId.Should().BeNull();
    }

    [Fact]
    public void Autor_e_coerente_com_o_tipo()
    {
        var semAutor = () => Atividade.Criar(Dados(), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, null, T0);
        var sistemaComAutor = () => Atividade.Criar(Dados(), VinculoDaAtividade.Avulsa, TipoAutor.Sistema, _diana.UsuarioId, T0);

        semAutor.Should().Throw<ExcecaoDeDominio>().WithMessage("O autor da atividade é obrigatório.");
        sistemaComAutor.Should().Throw<ExcecaoDeDominio>().WithMessage("Atividade criada pelo sistema não tem autor.");
    }

    [Theory]
    [InlineData("", "Descrição", "O título da atividade é obrigatório.")]
    [InlineData("   ", "Descrição", "O título da atividade é obrigatório.")]
    [InlineData("Título", "", "A descrição da atividade é obrigatória.")]
    [InlineData("Título", "  ", "A descrição da atividade é obrigatória.")]
    public void Titulo_e_descricao_sao_obrigatorios(string titulo, string descricao, string mensagem)
    {
        var acao = () => Atividade.Criar(new DadosDaAtividade(titulo, descricao, null), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage(mensagem);
    }

    [Fact]
    public void Titulo_e_descricao_tem_limite_e_sao_aparados()
    {
        var longoDemais = () => Atividade.Criar(new DadosDaAtividade(new string('a', 201), "d", null), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);
        var descricaoLonga = () => Atividade.Criar(new DadosDaAtividade("t", new string('a', 10001), null), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);
        var aparada = Atividade.Criar(new DadosDaAtividade("  Petição  ", "  Texto  ", null), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);

        longoDemais.Should().Throw<ExcecaoDeDominio>().WithMessage("O título da atividade deve ter até 200 caracteres.");
        descricaoLonga.Should().Throw<ExcecaoDeDominio>().WithMessage("A descrição da atividade deve ter até 10000 caracteres.");
        aparada.Titulo.Should().Be("Petição");
        aparada.Descricao.Should().Be("Texto");
    }

    [Fact]
    public void Data_de_entrega_e_opcional_enquanto_criada_e_obrigatoria_depois()
    {
        var criada = Nova(semEntrega: true);
        criada.DataEntrega.Should().BeNull();

        var direcionada = Direcionada();
        var acao = () => direcionada.Alterar(Dados(semEntrega: true), VinculoDaAtividade.Avulsa);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("A data de entrega é obrigatória depois que a atividade é direcionada.");
        direcionada.DataEntrega.Should().Be(Entrega);
    }

    [Fact]
    public void Alterar_troca_dados_e_vinculo_sem_mexer_no_status()
    {
        var atividade = Direcionada();
        var cliente = Guid.CreateVersion7();
        var area = Guid.CreateVersion7();

        atividade.Alterar(Dados("Novo título", new DateOnly(2026, 11, 1)), VinculoDaAtividade.Criar(cliente, null, area));

        atividade.Titulo.Should().Be("Novo título");
        atividade.DataEntrega.Should().Be(new DateOnly(2026, 11, 1));
        atividade.ClienteId.Should().Be(cliente);
        atividade.ProcessoId.Should().BeNull();
        atividade.AreaAtuacaoId.Should().Be(area);
        atividade.Status.Should().Be(StatusAtividade.Direcionada);
        atividade.Historico.Should().HaveCount(2);
    }

    // ---- Vínculo ----

    private static Processo NovoProcesso(Guid area, params Guid[] clientes) => Processo.Criar(
        new DadosDoProcesso(null, "Reclamação trabalhista", area, null, null, null, null, null, null),
        [.. clientes.Select(c => new ClienteDoProcesso(c, PoloProcessual.Ativo))]);

    [Fact]
    public void Atividade_avulsa_nao_tem_cliente_processo_nem_area()
    {
        VinculoDaAtividade.Avulsa.Should().Be(VinculoDaAtividade.Criar(null, null, null));
        VinculoDaAtividade.Avulsa.ClienteId.Should().BeNull();
        VinculoDaAtividade.Avulsa.ProcessoId.Should().BeNull();
        VinculoDaAtividade.Avulsa.AreaAtuacaoId.Should().BeNull();
    }

    [Fact]
    public void Cliente_sem_processo_aceita_area_informada_ou_nenhuma()
    {
        var cliente = Guid.CreateVersion7();
        var area = Guid.CreateVersion7();

        VinculoDaAtividade.Criar(cliente, null, area).Should().BeEquivalentTo(new { ClienteId = (Guid?)cliente, ProcessoId = (Guid?)null, AreaAtuacaoId = (Guid?)area });
        VinculoDaAtividade.Criar(cliente, null, null).AreaAtuacaoId.Should().BeNull();
        VinculoDaAtividade.Criar(null, null, area).AreaAtuacaoId.Should().Be(area, "uma atividade avulsa pode ter área");
    }

    [Fact]
    public void Com_processo_o_cliente_e_obrigatorio_e_tem_de_ser_do_processo()
    {
        var area = Guid.CreateVersion7();
        var maria = Guid.CreateVersion7();
        var processo = NovoProcesso(area, maria);

        var semCliente = () => VinculoDaAtividade.Criar(null, processo, null);
        var clienteDeFora = () => VinculoDaAtividade.Criar(Guid.CreateVersion7(), processo, null);

        semCliente.Should().Throw<ExcecaoDeDominio>().WithMessage("Com processo, o cliente da atividade é obrigatório.");
        clienteDeFora.Should().Throw<ExcecaoDeDominio>().WithMessage("O cliente da atividade deve ser um dos clientes do processo.");
    }

    [Fact]
    public void Com_processo_a_area_e_sempre_a_do_processo()
    {
        var area = Guid.CreateVersion7();
        var maria = Guid.CreateVersion7();
        var processo = NovoProcesso(area, maria);

        var vinculo = VinculoDaAtividade.Criar(maria, processo, Guid.CreateVersion7());

        vinculo.ClienteId.Should().Be(maria);
        vinculo.ProcessoId.Should().Be(processo.Id);
        vinculo.AreaAtuacaoId.Should().Be(area, "a área informada é ignorada quando há processo");
    }

    // ---- Direcionar ----

    [Fact]
    public void Definir_responsaveis_revisor_e_data_direciona_a_atividade()
    {
        var atividade = Nova();

        var alteracao = atividade.DefinirResponsaveis([R(_pedro, " Redigir "), R(_paula)], _diana, T0.AddMinutes(1), _diana.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Direcionada);
        atividade.RevisorId.Should().Be(_diana.UsuarioId);
        atividade.Responsaveis.Select(r => (r.UsuarioId, r.Direcionamento, r.Pendente)).Should().BeEquivalentTo(
            [(_pedro.UsuarioId, (string?)"Redigir", true), (_paula.UsuarioId, (string?)null, true)]);
        atividade.Responsaveis.Should().OnlyContain(r => r.AtividadeId == atividade.Id);
        alteracao.Adicionados.Should().BeEquivalentTo([_pedro.UsuarioId, _paula.UsuarioId]);
        alteracao.Houve.Should().BeTrue();
        Percurso(atividade).Should().Equal((null, StatusAtividade.Criada), (StatusAtividade.Criada, StatusAtividade.Direcionada));
        atividade.Historico.Last().UsuarioId.Should().Be(_diana.UsuarioId);
        atividade.VersaoDasPartes.Should().Be(1);
    }

    [Fact]
    public void Sem_responsaveis_a_atividade_continua_criada_mas_guarda_o_revisor()
    {
        var atividade = Nova(semEntrega: true);

        atividade.DefinirResponsaveis([], _diana, T0, _diana.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Criada);
        atividade.RevisorId.Should().Be(_diana.UsuarioId);
        atividade.Historico.Should().ContainSingle();
    }

    [Fact]
    public void Direcionar_exige_revisor_e_data_de_entrega()
    {
        var semData = Nova(semEntrega: true);
        var semRevisor = Nova();

        var acaoSemData = () => semData.DefinirResponsaveis([R(_pedro)], _diana, T0, _diana.UsuarioId);
        var acaoSemRevisor = () => semRevisor.DefinirResponsaveis([R(_pedro)], null, T0, _diana.UsuarioId);

        acaoSemData.Should().Throw<ExcecaoDeDominio>().WithMessage("Informe a data de entrega antes de direcionar a atividade.");
        acaoSemRevisor.Should().Throw<ExcecaoDeDominio>().WithMessage("Informe o revisor da atividade.");
        semData.Status.Should().Be(StatusAtividade.Criada);
        semData.Responsaveis.Should().BeEmpty();
        semData.RevisorId.Should().BeNull();
        semRevisor.Responsaveis.Should().BeEmpty();
    }

    [Fact]
    public void Um_usuario_nao_se_repete_entre_os_responsaveis()
    {
        var atividade = Nova();

        var acao = () => atividade.DefinirResponsaveis([R(_pedro), R(_pedro, "outra vez")], _diana, T0, _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("Um usuário não pode se repetir entre os responsáveis.");
        atividade.Responsaveis.Should().BeEmpty();
    }

    [Fact]
    public void Usuario_inativo_nao_entra_como_responsavel_mas_quem_ja_estava_permanece()
    {
        var atividade = Direcionada();
        var pedroInativo = _pedro with { Ativo = false };
        var esterInativa = _ester with { Ativo = false };

        var incluir = () => atividade.DefinirResponsaveis([R(_pedro), R(_paula), R(esterInativa)], _diana, T0, _diana.UsuarioId);
        incluir.Should().Throw<ExcecaoDeDominio>().WithMessage("Usuário inativo não pode ser incluído como responsável.");

        var alteracao = atividade.DefinirResponsaveis([R(pedroInativo, "Redigir"), R(_paula, "Juntar documentos")], _diana, T0, _diana.UsuarioId);
        alteracao.Houve.Should().BeFalse();
        atividade.Responsaveis.Should().HaveCount(2);
    }

    [Fact]
    public void Direcionamento_tem_limite()
    {
        var atividade = Nova();

        var acao = () => atividade.DefinirResponsaveis([R(_pedro, new string('a', 4001))], _diana, T0, _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O direcionamento deve ter até 4000 caracteres.");
    }

    [Fact]
    public void Redefinir_responsaveis_preserva_quem_fica_e_conta_o_que_mudou()
    {
        var atividade = Direcionada();
        atividade.ConcluirParte(_pedro, T0.AddHours(1));
        var entradaDoPedro = atividade.Responsaveis.Single(r => r.UsuarioId == _pedro.UsuarioId);

        var alteracao = atividade.DefinirResponsaveis([R(_pedro, "Redigir e revisar"), R(_ester, "Protocolar")], _diana, T0.AddHours(2), _diana.UsuarioId);

        alteracao.Adicionados.Should().Equal(_ester.UsuarioId);
        alteracao.Removidos.Should().Equal(_paula.UsuarioId);
        alteracao.DirecionamentoAlterado.Should().Equal(_pedro.UsuarioId);
        atividade.Responsaveis.Single(r => r.UsuarioId == _pedro.UsuarioId).Should().BeSameAs(entradaDoPedro);
        entradaDoPedro.Direcionamento.Should().Be("Redigir e revisar");
        entradaDoPedro.Pendente.Should().BeFalse("a conclusão de quem fica é preservada");
        atividade.Responsaveis.Single(r => r.UsuarioId == _ester.UsuarioId).Pendente.Should().BeTrue();
        atividade.Status.Should().Be(StatusAtividade.Direcionada);
    }

    [Fact]
    public void Atividade_direcionada_nao_fica_sem_responsavel()
    {
        var atividade = Direcionada();

        var acao = () => atividade.DefinirResponsaveis([], _diana, T0, _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("A atividade precisa de ao menos um responsável.");
        atividade.Responsaveis.Should().HaveCount(2);
    }

    [Fact]
    public void Remover_o_ultimo_pendente_avanca_a_atividade()
    {
        var atividade = Direcionada();
        atividade.ConcluirParte(_pedro, T0.AddHours(1));

        atividade.DefinirResponsaveis([R(_pedro, "Redigir")], _diana, T0.AddHours(2), _diana.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Feita);
        atividade.Historico.Last().UsuarioId.Should().Be(_diana.UsuarioId);
    }

    [Theory]
    [InlineData(StatusAtividade.Feita)]
    [InlineData(StatusAtividade.Concluida)]
    public void Responsaveis_nao_mudam_enquanto_a_atividade_aguarda_revisao_ou_esta_concluida(StatusAtividade status)
    {
        var atividade = status == StatusAtividade.Feita ? Feita() : Concluida();

        var acao = () => atividade.DefinirResponsaveis([R(_pedro), R(_paula), R(_ester)], _diana, T0, _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("Os responsáveis só podem ser alterados enquanto a atividade está criada, direcionada, em correção ou reaberta.");
        atividade.PodeAlterarResponsaveis.Should().BeFalse();
        atividade.Responsaveis.Should().HaveCount(2);
    }

    [Fact]
    public void Elemento_nulo_e_erro_de_programacao()
    {
        var atividade = Nova();

        var acao = () => atividade.DefinirResponsaveis([R(_pedro), null!], _diana, T0, _diana.UsuarioId);

        acao.Should().Throw<ArgumentException>();
        atividade.Responsaveis.Should().BeEmpty();
    }

    // ---- Regra do revisor ----

    [Fact]
    public void Com_exatamente_um_responsavel_de_diretoria_ele_e_o_revisor_e_nao_pode_ser_outro()
    {
        var atividade = Nova();

        var outro = () => atividade.DefinirResponsaveis([R(_davi), R(_pedro)], _diana, T0, _diana.UsuarioId);
        outro.Should().Throw<ExcecaoDeDominio>().WithMessage(Atividade.RevisorTravado);

        atividade.DefinirResponsaveis([R(_davi), R(_pedro)], null, T0, _diana.UsuarioId);
        atividade.RevisorId.Should().Be(_davi.UsuarioId);

        var trocar = () => atividade.TrocarRevisor(_diana, [_davi, _pedro]);
        trocar.Should().Throw<ExcecaoDeDominio>().WithMessage(Atividade.RevisorTravado);
        atividade.RevisorId.Should().Be(_davi.UsuarioId);
    }

    [Fact]
    public void Com_dois_responsaveis_de_diretoria_o_revisor_e_escolhido()
    {
        var atividade = Nova();

        atividade.DefinirResponsaveis([R(_davi), R(_diana), R(_pedro)], _diana, T0, _diana.UsuarioId);
        atividade.RevisorId.Should().Be(_diana.UsuarioId);

        atividade.TrocarRevisor(_davi, [_davi, _diana, _pedro]);
        atividade.RevisorId.Should().Be(_davi.UsuarioId);
    }

    [Fact]
    public void Quando_a_reavaliacao_libera_a_escolha_o_revisor_atual_e_mantido()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_davi), R(_pedro)], null, T0, _diana.UsuarioId);

        // Davi sai dos responsáveis: nenhum diretor entre eles, a escolha fica livre e Davi continua revisor.
        atividade.DefinirResponsaveis([R(_pedro)], null, T0, _diana.UsuarioId);

        atividade.RevisorId.Should().Be(_davi.UsuarioId);
    }

    [Fact]
    public void Quando_a_reavaliacao_trava_o_revisor_passa_a_ser_o_responsavel_de_diretoria()
    {
        var atividade = Direcionada();   // revisora Diana, responsáveis Pedro e Paula

        atividade.DefinirResponsaveis([R(_pedro), R(_paula), R(_davi)], null, T0, _diana.UsuarioId);

        atividade.RevisorId.Should().Be(_davi.UsuarioId);
    }

    [Fact]
    public void Diretor_inativo_entre_os_responsaveis_nao_trava_o_revisor()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_davi), R(_pedro)], null, T0, _diana.UsuarioId);
        var daviInativo = _davi with { Ativo = false };

        atividade.TrocarRevisor(_diana, [daviInativo, _pedro]);

        atividade.RevisorId.Should().Be(_diana.UsuarioId);
    }

    [Fact]
    public void Revisor_tem_de_ser_diretor_ativo()
    {
        var atividade = Nova();

        var producao = () => atividade.DefinirResponsaveis([R(_paula)], _pedro, T0, _diana.UsuarioId);
        var inativo = () => atividade.DefinirResponsaveis([R(_paula)], _davi with { Ativo = false }, T0, _diana.UsuarioId);

        producao.Should().Throw<ExcecaoDeDominio>().WithMessage("O revisor deve ser um usuário ativo de Diretoria.");
        inativo.Should().Throw<ExcecaoDeDominio>().WithMessage("O revisor deve ser um usuário ativo de Diretoria.");
        atividade.RevisorId.Should().BeNull();
    }

    [Fact]
    public void Revisor_pode_ser_trocado_enquanto_aguarda_revisao_mas_nao_depois_de_concluida()
    {
        var feita = Feita();
        feita.TrocarRevisor(_davi, [_pedro, _paula]);
        feita.RevisorId.Should().Be(_davi.UsuarioId);

        var concluida = Concluida();
        var acao = () => concluida.TrocarRevisor(_davi, [_pedro, _paula]);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O revisor não pode ser trocado depois que a atividade foi concluída.");
        concluida.PodeTrocarRevisor.Should().BeFalse();
    }

    [Fact]
    public void Trocar_revisor_exige_a_lista_atual_de_responsaveis()
    {
        var atividade = Direcionada();

        var acao = () => atividade.TrocarRevisor(_davi, [_pedro]);

        acao.Should().Throw<ArgumentException>();
    }

    // ---- Concluir a parte ----

    [Fact]
    public void A_atividade_so_fica_feita_quando_o_ultimo_responsavel_conclui()
    {
        var atividade = Direcionada();

        atividade.ConcluirParte(_pedro, T0.AddHours(1));
        atividade.Status.Should().Be(StatusAtividade.Direcionada);
        atividade.Responsaveis.Single(r => r.UsuarioId == _pedro.UsuarioId).ConcluidoEm.Should().Be(T0.AddHours(1));

        atividade.ConcluirParte(_paula, T0.AddHours(2));
        atividade.Status.Should().Be(StatusAtividade.Feita);
        atividade.ConcluidaEm.Should().BeNull();
        var linha = atividade.Historico.Last();
        linha.StatusAnterior.Should().Be(StatusAtividade.Direcionada);
        linha.UsuarioId.Should().Be(_paula.UsuarioId, "a transição leva quem a provocou");
        linha.DataHora.Should().Be(T0.AddHours(2));
        atividade.VersaoDasPartes.Should().Be(3);
    }

    [Fact]
    public void Quem_nao_e_responsavel_ou_ja_concluiu_nao_conclui()
    {
        var atividade = Direcionada();
        atividade.ConcluirParte(_pedro, T0.AddHours(1));

        var deFora = () => atividade.ConcluirParte(_ester, T0.AddHours(2));
        var deNovo = () => atividade.ConcluirParte(_pedro, T0.AddHours(2));

        deFora.Should().Throw<ExcecaoDeDominio>().WithMessage("Você não é responsável por esta atividade.");
        deNovo.Should().Throw<ExcecaoDeDominio>().WithMessage("Sua parte já está concluída.");
        atividade.EhResponsavel(_pedro.UsuarioId).Should().BeTrue();
        atividade.EhResponsavel(_ester.UsuarioId).Should().BeFalse();
    }

    [Theory]
    [InlineData(StatusAtividade.Criada)]
    [InlineData(StatusAtividade.Feita)]
    [InlineData(StatusAtividade.Concluida)]
    public void A_parte_so_e_concluida_em_atividade_direcionada_em_correcao_ou_reaberta(StatusAtividade status)
    {
        var atividade = status switch
        {
            StatusAtividade.Criada => Nova(),
            StatusAtividade.Feita => Feita(),
            _ => Concluida(),
        };

        var acao = () => atividade.ConcluirParte(_pedro, T0.AddDays(1));

        acao.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("A parte só pode ser concluída enquanto a atividade está direcionada, em correção ou reaberta.");
        atividade.Status.Should().Be(status);
    }

    [Fact]
    public void Quando_o_revisor_e_o_unico_responsavel_concluir_a_parte_conclui_a_atividade()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_diana)], null, T0.AddMinutes(1), _diana.UsuarioId);

        atividade.ConcluirParte(_diana, T0.AddHours(1));

        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(T0.AddHours(1));
        Percurso(atividade).Should().Equal(
            (null, StatusAtividade.Criada),
            (StatusAtividade.Criada, StatusAtividade.Direcionada),
            (StatusAtividade.Direcionada, StatusAtividade.Feita),
            (StatusAtividade.Feita, StatusAtividade.Concluida));
        atividade.Historico.Select(h => h.Ordem).Should().Equal(1, 2, 3, 4);
        atividade.Historico.TakeLast(2).Should().OnlyContain(h => h.DataHora == T0.AddHours(1) && h.UsuarioId == _diana.UsuarioId);
    }

    [Fact]
    public void Revisor_que_e_um_entre_varios_responsaveis_nao_conclui_a_atividade_sozinho()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_diana), R(_pedro)], null, T0, _diana.UsuarioId);

        atividade.ConcluirParte(_diana, T0.AddHours(1));
        atividade.ConcluirParte(_pedro, T0.AddHours(2));

        atividade.Status.Should().Be(StatusAtividade.Feita);
    }

    // ---- Aprovar e devolver ----

    [Fact]
    public void Revisor_aprova_e_a_atividade_fica_concluida()
    {
        var atividade = Feita();

        atividade.Aprovar(_diana, T0.AddHours(3));

        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(T0.AddHours(3));
        atividade.Historico.Last().Should().BeEquivalentTo(new
        {
            StatusAnterior = (StatusAtividade?)StatusAtividade.Feita, StatusNovo = StatusAtividade.Concluida,
            UsuarioId = (Guid?)_diana.UsuarioId, Motivo = (string?)null,
        });
    }

    [Fact]
    public void So_o_revisor_aprova_e_devolve()
    {
        var atividade = Feita();

        var aprovar = () => atividade.Aprovar(_davi, T0.AddHours(3));
        var devolver = () => atividade.Devolver(_pedro, "Faltou a procuração", [_pedro.UsuarioId], T0.AddHours(3));

        aprovar.Should().Throw<ExcecaoDeDominio>().WithMessage(Atividade.SoORevisor);
        devolver.Should().Throw<ExcecaoDeDominio>().WithMessage(Atividade.SoORevisor);
        atividade.Status.Should().Be(StatusAtividade.Feita);
    }

    [Theory]
    [InlineData(StatusAtividade.Criada)]
    [InlineData(StatusAtividade.Direcionada)]
    [InlineData(StatusAtividade.Concluida)]
    public void Aprovar_e_devolver_so_valem_para_atividade_feita_ou_corrigida(StatusAtividade status)
    {
        var atividade = status switch
        {
            StatusAtividade.Criada => Nova(),
            StatusAtividade.Direcionada => Direcionada(),
            _ => Concluida(),
        };

        var aprovar = () => atividade.Aprovar(_diana, T0.AddDays(1));
        var devolver = () => atividade.Devolver(_diana, "motivo", [_pedro.UsuarioId], T0.AddDays(1));

        aprovar.Should().Throw<ExcecaoDeDominio>().WithMessage("A atividade só pode ser aprovada ou devolvida quando está feita ou corrigida.");
        devolver.Should().Throw<ExcecaoDeDominio>().WithMessage("A atividade só pode ser aprovada ou devolvida quando está feita ou corrigida.");
        atividade.Status.Should().Be(status);
    }

    [Fact]
    public void Devolver_reabre_so_as_partes_selecionadas_e_grava_o_motivo()
    {
        var atividade = Feita();

        atividade.Devolver(_diana, "  Faltou a procuração  ", [_paula.UsuarioId], T0.AddHours(3));

        atividade.Status.Should().Be(StatusAtividade.ParaCorrecao);
        atividade.Responsaveis.Single(r => r.UsuarioId == _paula.UsuarioId).Pendente.Should().BeTrue();
        atividade.Responsaveis.Single(r => r.UsuarioId == _pedro.UsuarioId).Pendente.Should().BeFalse();
        var linha = atividade.Historico.Last();
        linha.StatusNovo.Should().Be(StatusAtividade.ParaCorrecao);
        linha.Motivo.Should().Be("Faltou a procuração");
        linha.UsuarioId.Should().Be(_diana.UsuarioId);
    }

    [Fact]
    public void A_atividade_devolvida_fica_corrigida_quando_o_ultimo_selecionado_conclui()
    {
        var atividade = Feita();
        atividade.Devolver(_diana, "Ajustar", [_pedro.UsuarioId, _paula.UsuarioId], T0.AddHours(3));

        atividade.ConcluirParte(_pedro, T0.AddHours(4));
        atividade.Status.Should().Be(StatusAtividade.ParaCorrecao);

        atividade.ConcluirParte(_paula, T0.AddHours(5));
        atividade.Status.Should().Be(StatusAtividade.Corrigida);

        atividade.Devolver(_diana, "Ainda falta", [_paula.UsuarioId], T0.AddHours(6));
        atividade.Status.Should().Be(StatusAtividade.ParaCorrecao);
        atividade.ConcluirParte(_paula, T0.AddHours(7));
        atividade.Aprovar(_diana, T0.AddHours(8));

        atividade.Status.Should().Be(StatusAtividade.Concluida);
        Percurso(atividade).Select(p => p.Para).Should().Equal(
            StatusAtividade.Criada, StatusAtividade.Direcionada, StatusAtividade.Feita, StatusAtividade.ParaCorrecao,
            StatusAtividade.Corrigida, StatusAtividade.ParaCorrecao, StatusAtividade.Corrigida, StatusAtividade.Concluida);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Devolver_e_reabrir_exigem_motivo(string motivo)
    {
        var feita = Feita();
        var concluida = Concluida();

        var devolver = () => feita.Devolver(_diana, motivo, [_pedro.UsuarioId], T0.AddHours(3));
        var reabrir = () => concluida.Reabrir(motivo, [_pedro.UsuarioId], T0.AddHours(4), _diana.UsuarioId);

        devolver.Should().Throw<ExcecaoDeDominio>().WithMessage("Informe o motivo.");
        reabrir.Should().Throw<ExcecaoDeDominio>().WithMessage("Informe o motivo.");
        feita.Status.Should().Be(StatusAtividade.Feita);
        concluida.Status.Should().Be(StatusAtividade.Concluida);
    }

    [Fact]
    public void Motivo_tem_limite()
    {
        var feita = Feita();

        var acao = () => feita.Devolver(_diana, new string('a', 2001), [_pedro.UsuarioId], T0.AddHours(3));

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O motivo deve ter até 2000 caracteres.");
    }

    [Fact]
    public void Devolver_e_reabrir_exigem_ao_menos_um_responsavel_da_atividade()
    {
        var feita = Feita();

        var nenhum = () => feita.Devolver(_diana, "Ajustar", [], T0.AddHours(3));
        var deFora = () => feita.Devolver(_diana, "Ajustar", [_pedro.UsuarioId, _ester.UsuarioId], T0.AddHours(3));

        nenhum.Should().Throw<ExcecaoDeDominio>().WithMessage("Selecione ao menos um responsável.");
        deFora.Should().Throw<ExcecaoDeDominio>().WithMessage("Responsável selecionado não faz parte da atividade.");
        feita.Status.Should().Be(StatusAtividade.Feita);
        feita.Responsaveis.Should().OnlyContain(r => !r.Pendente, "nada muda quando a devolução é recusada");
    }

    // ---- Reabrir ----

    [Fact]
    public void Reabrir_limpa_a_conclusao_e_as_partes_selecionadas()
    {
        var atividade = Concluida();

        atividade.Reabrir("Cliente pediu ajuste", [_pedro.UsuarioId], T0.AddDays(2), _davi.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Reaberta);
        atividade.ConcluidaEm.Should().BeNull();
        atividade.Responsaveis.Single(r => r.UsuarioId == _pedro.UsuarioId).Pendente.Should().BeTrue();
        atividade.Responsaveis.Single(r => r.UsuarioId == _paula.UsuarioId).Pendente.Should().BeFalse();
        var linha = atividade.Historico.Last();
        linha.StatusAnterior.Should().Be(StatusAtividade.Concluida);
        linha.Motivo.Should().Be("Cliente pediu ajuste");
        linha.UsuarioId.Should().Be(_davi.UsuarioId);
        atividade.PodeAlterarResponsaveis.Should().BeTrue();
    }

    [Fact]
    public void Atividade_reaberta_volta_a_feita_quando_o_ultimo_selecionado_conclui()
    {
        var atividade = Concluida();
        atividade.Reabrir("Ajuste", [_pedro.UsuarioId], T0.AddDays(2), _diana.UsuarioId);

        atividade.ConcluirParte(_pedro, T0.AddDays(3));

        atividade.Status.Should().Be(StatusAtividade.Feita);
        atividade.Historico.Last().StatusAnterior.Should().Be(StatusAtividade.Reaberta);
    }

    [Theory]
    [InlineData(StatusAtividade.Criada)]
    [InlineData(StatusAtividade.Direcionada)]
    [InlineData(StatusAtividade.Feita)]
    public void So_atividade_concluida_e_reaberta(StatusAtividade status)
    {
        var atividade = status switch
        {
            StatusAtividade.Criada => Nova(),
            StatusAtividade.Direcionada => Direcionada(),
            _ => Feita(),
        };

        var acao = () => atividade.Reabrir("motivo", [_pedro.UsuarioId], T0.AddDays(1), _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("Só uma atividade concluída pode ser reaberta.");
        atividade.Status.Should().Be(status);
    }

    // ---- Atraso ----

    [Fact]
    public void Atrasada_e_data_de_entrega_passada_em_atividade_nao_concluida()
    {
        var direcionada = Direcionada();   // entrega em 20/10/2026
        var concluida = Concluida();
        var semData = Nova(semEntrega: true);

        direcionada.EstaAtrasada(new DateOnly(2026, 10, 20)).Should().BeFalse("no dia da entrega ainda não está atrasada");
        direcionada.EstaAtrasada(new DateOnly(2026, 10, 21)).Should().BeTrue();
        concluida.EstaAtrasada(new DateOnly(2026, 12, 1)).Should().BeFalse();
        semData.EstaAtrasada(new DateOnly(2026, 12, 1)).Should().BeFalse();
    }

    // ---- Pessoa ----

    [Fact]
    public void Pessoa_e_montada_do_usuario_e_do_seu_perfil()
    {
        var diretor = Perfil.Criar("Diretor", TipoPerfil.Diretoria);
        var producao = Perfil.Criar("Produção", TipoPerfil.Producao);
        var diana = Usuario.Criar("Diana", Email.Criar("diana@x.com"), "hash", diretor);
        var pedro = Usuario.Criar("Pedro", Email.Criar("pedro@x.com"), "hash", producao);
        pedro.Inativar();

        PessoaDaAtividade.De(diana, diretor).Should().Be(new PessoaDaAtividade(diana.Id, TipoPerfil.Diretoria, true));
        PessoaDaAtividade.De(diana, diretor).EhDiretoriaAtiva.Should().BeTrue();
        PessoaDaAtividade.De(pedro, producao).Should().Be(new PessoaDaAtividade(pedro.Id, TipoPerfil.Producao, false));
        PessoaDaAtividade.De(pedro, producao).EhDiretoriaAtiva.Should().BeFalse();

        var perfilErrado = () => PessoaDaAtividade.De(pedro, diretor);
        perfilErrado.Should().Throw<ArgumentException>();
    }

    // ================= Rodada de correção 1 =================

    private const string MsgSoAguardandoRevisao = "A atividade só pode ser aprovada ou devolvida quando está feita ou corrigida.";
    private const string MsgSoReabreConcluida = "Só uma atividade concluída pode ser reaberta.";

    private Atividade ParaCorrecao()
    {
        var atividade = Feita();
        atividade.Devolver(_diana, "Ajustar", [_pedro.UsuarioId, _paula.UsuarioId], T0.AddHours(3));
        return atividade;
    }

    private Atividade Corrigida()
    {
        var atividade = ParaCorrecao();
        atividade.ConcluirParte(_pedro, T0.AddHours(4));
        atividade.ConcluirParte(_paula, T0.AddHours(5));
        return atividade;
    }

    private Atividade Reaberta()
    {
        var atividade = Concluida();
        atividade.Reabrir("Ajuste", [_pedro.UsuarioId, _paula.UsuarioId], T0.AddDays(2), _diana.UsuarioId);
        return atividade;
    }

    private Atividade Em(StatusAtividade status) => status switch
    {
        StatusAtividade.Criada => Nova(),
        StatusAtividade.Direcionada => Direcionada(),
        StatusAtividade.Feita => Feita(),
        StatusAtividade.ParaCorrecao => ParaCorrecao(),
        StatusAtividade.Corrigida => Corrigida(),
        StatusAtividade.Concluida => Concluida(),
        _ => Reaberta(),
    };

    // ---- A: o revisor precisa ser diretor ativo ----

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Revisor_que_deixou_de_ser_diretor_ativo_nao_aprova_nem_devolve(bool rebaixado)
    {
        var atividade = Feita();
        var antiga = rebaixado ? _diana with { Tipo = TipoPerfil.Producao } : _diana with { Ativo = false };

        var aprovar = () => atividade.Aprovar(antiga, T0.AddHours(3));
        var devolver = () => atividade.Devolver(antiga, "Ajustar", [_pedro.UsuarioId], T0.AddHours(3));

        aprovar.Should().Throw<ExcecaoDeDominio>().WithMessage(Atividade.RevisorPrecisaSerDiretor);
        devolver.Should().Throw<ExcecaoDeDominio>().WithMessage(Atividade.RevisorPrecisaSerDiretor);
        atividade.Status.Should().Be(StatusAtividade.Feita);
        atividade.Responsaveis.Should().OnlyContain(r => !r.Pendente);
    }

    [Fact]
    public void Revisor_unico_responsavel_rebaixado_deixa_a_atividade_feita_ate_trocar_o_revisor()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_diana)], null, T0.AddMinutes(1), _diana.UsuarioId);
        var dianaRebaixada = _diana with { Tipo = TipoPerfil.Producao };

        atividade.ConcluirParte(dianaRebaixada, T0.AddHours(1));

        atividade.Status.Should().Be(StatusAtividade.Feita);
        atividade.ConcluidaEm.Should().BeNull();

        atividade.TrocarRevisor(_davi, [dianaRebaixada]);
        atividade.Aprovar(_davi, T0.AddHours(2));

        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(T0.AddHours(2));
    }

    [Fact]
    public void Remover_o_ultimo_pendente_deixando_so_o_revisor_rebaixado_nao_conclui_a_atividade()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_diana), R(_pedro)], null, T0, _diana.UsuarioId);
        atividade.ConcluirParte(_diana, T0.AddHours(1));
        var dianaRebaixada = _diana with { Tipo = TipoPerfil.Producao };

        atividade.DefinirResponsaveis([R(dianaRebaixada)], null, T0.AddHours(2), _davi.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Feita);
        atividade.ConcluidaEm.Should().BeNull();
        atividade.RevisorId.Should().Be(_diana.UsuarioId);
    }

    // ---- B: informado igual ao atual na regra de travamento ----

    [Fact]
    public void Informar_o_revisor_atual_quando_a_reavaliacao_trava_passa_o_cargo_ao_diretor_responsavel()
    {
        var atividade = Direcionada();   // revisora Diana

        var acao = () => atividade.DefinirResponsaveis([R(_pedro), R(_paula), R(_davi)], _diana, T0, _diana.UsuarioId);

        acao.Should().NotThrow();
        atividade.RevisorId.Should().Be(_davi.UsuarioId);
    }

    [Fact]
    public void Informar_um_terceiro_quando_a_reavaliacao_trava_continua_recusado()
    {
        var atividade = Direcionada();   // revisora Diana
        var ester = _ester with { Tipo = TipoPerfil.Diretoria };

        var acao = () => atividade.DefinirResponsaveis([R(_pedro), R(_paula), R(_davi)], ester, T0, _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage(Atividade.RevisorTravado);
        atividade.RevisorId.Should().Be(_diana.UsuarioId);
    }

    // ---- C: TrocarRevisor valida a lista ----

    [Fact]
    public void Trocar_revisor_recusa_elemento_nulo_e_repeticao_na_lista()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_davi), R(_pedro)], null, T0, _diana.UsuarioId);

        var nulo = () => atividade.TrocarRevisor(_diana, [_davi, _pedro, null!]);
        var repetido = () => atividade.TrocarRevisor(_diana, [_davi, _davi, _pedro]);

        nulo.Should().Throw<ArgumentException>();
        repetido.Should().Throw<ArgumentException>();
        atividade.RevisorId.Should().Be(_davi.UsuarioId);
    }

    // ---- D: Guid.Empty ----

    [Fact]
    public void Responsavel_ou_revisor_com_id_vazio_e_recusado_sem_mudar_nada()
    {
        var atividade = Nova();
        var vazioProducao = new PessoaDaAtividade(Guid.Empty, TipoPerfil.Producao, true);
        var vazioDiretoria = new PessoaDaAtividade(Guid.Empty, TipoPerfil.Diretoria, true);

        var responsavel = () => atividade.DefinirResponsaveis([R(_pedro), R(vazioProducao)], _diana, T0, _diana.UsuarioId);
        var revisor = () => atividade.DefinirResponsaveis([R(_pedro)], vazioDiretoria, T0, _diana.UsuarioId);

        responsavel.Should().Throw<ExcecaoDeDominio>().WithMessage("Usuário inválido.");
        revisor.Should().Throw<ExcecaoDeDominio>().WithMessage("Usuário inválido.");
        atividade.Responsaveis.Should().BeEmpty();
        atividade.RevisorId.Should().BeNull();
        atividade.Status.Should().Be(StatusAtividade.Criada);
    }

    [Fact]
    public void Vinculo_com_cliente_ou_area_vazios_e_recusado()
    {
        var cliente = () => VinculoDaAtividade.Criar(Guid.Empty, null, null);
        var area = () => VinculoDaAtividade.Criar(null, null, Guid.Empty);

        cliente.Should().Throw<ExcecaoDeDominio>().WithMessage("Cliente inválido.");
        area.Should().Throw<ExcecaoDeDominio>().WithMessage("Área de atuação inválida.");
    }

    // ---- E: horários em UTC ----

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Horario_que_nao_e_utc_e_recusado_em_todas_as_operacoes(DateTimeKind tipo)
    {
        var agora = DateTime.SpecifyKind(T0.AddHours(5), tipo);
        var direcionada = Direcionada();
        var feitaParaAprovar = Feita();
        var feitaParaDevolver = Feita();
        var concluida = Concluida();

        var acoes = new Action[]
        {
            () => Atividade.Criar(Dados(), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, agora),
            () => Nova().DefinirResponsaveis([R(_pedro)], _diana, agora, _diana.UsuarioId),
            () => direcionada.ConcluirParte(_pedro, agora),
            () => feitaParaAprovar.Aprovar(_diana, agora),
            () => feitaParaDevolver.Devolver(_diana, "Ajustar", [_pedro.UsuarioId], agora),
            () => concluida.Reabrir("Ajuste", [_pedro.UsuarioId], agora, _diana.UsuarioId),
        };

        foreach (var acao in acoes) acao.Should().Throw<ArgumentException>();
        direcionada.Status.Should().Be(StatusAtividade.Direcionada);
        feitaParaAprovar.Status.Should().Be(StatusAtividade.Feita);
        feitaParaDevolver.Status.Should().Be(StatusAtividade.Feita);
        concluida.Status.Should().Be(StatusAtividade.Concluida);
    }

    // ---- F1: VersaoDasPartes ----

    [Fact]
    public void Versao_das_partes_so_sobe_quando_os_responsaveis_ou_as_partes_mudam()
    {
        var igual = Direcionada();
        igual.DefinirResponsaveis([R(_pedro, "Redigir"), R(_paula, "Juntar documentos")], _diana, T0, _diana.UsuarioId);
        igual.VersaoDasPartes.Should().Be(1);

        var direcionamento = Direcionada();
        direcionamento.DefinirResponsaveis([R(_pedro, "Redigir com calma"), R(_paula, "Juntar documentos")], _diana, T0, _diana.UsuarioId);
        direcionamento.VersaoDasPartes.Should().Be(2);

        var remocao = Direcionada();
        remocao.DefinirResponsaveis([R(_pedro, "Redigir")], _diana, T0, _diana.UsuarioId);
        remocao.VersaoDasPartes.Should().Be(2);

        var feita = Feita();
        var antesDevolver = feita.VersaoDasPartes;
        feita.Devolver(_diana, "Ajustar", [_pedro.UsuarioId], T0.AddHours(3));
        feita.VersaoDasPartes.Should().Be(antesDevolver + 1);

        var concluida = Concluida();
        var antesReabrir = concluida.VersaoDasPartes;
        concluida.Reabrir("Ajuste", [_pedro.UsuarioId], T0.AddDays(1), _diana.UsuarioId);
        concluida.VersaoDasPartes.Should().Be(antesReabrir + 1);
    }

    // ---- F2: recusas por status ----

    [Theory]
    [InlineData(StatusAtividade.ParaCorrecao)]
    [InlineData(StatusAtividade.Reaberta)]
    public void Aprovar_e_devolver_sao_recusados_em_correcao_e_reaberta(StatusAtividade status)
    {
        var atividade = Em(status);

        var aprovar = () => atividade.Aprovar(_diana, T0.AddDays(5));
        var devolver = () => atividade.Devolver(_diana, "motivo", [_pedro.UsuarioId], T0.AddDays(5));

        aprovar.Should().Throw<ExcecaoDeDominio>().WithMessage(MsgSoAguardandoRevisao);
        devolver.Should().Throw<ExcecaoDeDominio>().WithMessage(MsgSoAguardandoRevisao);
        atividade.Status.Should().Be(status);
    }

    [Fact]
    public void Concluir_parte_e_recusado_em_atividade_corrigida()
    {
        var atividade = Corrigida();

        var acao = () => atividade.ConcluirParte(_pedro, T0.AddDays(5));

        acao.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("A parte só pode ser concluída enquanto a atividade está direcionada, em correção ou reaberta.");
        atividade.Status.Should().Be(StatusAtividade.Corrigida);
    }

    [Theory]
    [InlineData(StatusAtividade.ParaCorrecao)]
    [InlineData(StatusAtividade.Corrigida)]
    [InlineData(StatusAtividade.Reaberta)]
    public void Reabrir_e_recusado_fora_de_atividade_concluida(StatusAtividade status)
    {
        var atividade = Em(status);

        var acao = () => atividade.Reabrir("motivo", [_pedro.UsuarioId], T0.AddDays(5), _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage(MsgSoReabreConcluida);
        atividade.Status.Should().Be(status);
    }

    [Fact]
    public void Responsaveis_nao_mudam_em_atividade_corrigida()
    {
        var atividade = Corrigida();

        var acao = () => atividade.DefinirResponsaveis([R(_pedro), R(_paula), R(_ester)], _diana, T0.AddDays(5), _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("Os responsáveis só podem ser alterados enquanto a atividade está criada, direcionada, em correção ou reaberta.");
        atividade.PodeAlterarResponsaveis.Should().BeFalse();
        atividade.Responsaveis.Should().HaveCount(2);
    }

    // ---- F3 e F4: responsáveis em correção e reaberta ----

    [Theory]
    [InlineData(StatusAtividade.ParaCorrecao)]
    [InlineData(StatusAtividade.Reaberta)]
    public void Responsavel_pode_ser_incluido_em_correcao_e_reaberta_e_entra_pendente(StatusAtividade status)
    {
        var atividade = Em(status);

        var alteracao = atividade.DefinirResponsaveis(
            [R(_pedro, "Redigir"), R(_paula, "Juntar documentos"), R(_ester, "Protocolar")], null, T0.AddDays(5), _diana.UsuarioId);

        alteracao.Adicionados.Should().Equal(_ester.UsuarioId);
        atividade.Status.Should().Be(status);
        atividade.Responsaveis.Single(r => r.UsuarioId == _ester.UsuarioId).Pendente.Should().BeTrue();
    }

    [Fact]
    public void Remover_o_ultimo_pendente_em_correcao_deixa_a_atividade_corrigida()
    {
        var atividade = ParaCorrecao();
        atividade.ConcluirParte(_pedro, T0.AddHours(4));

        atividade.DefinirResponsaveis([R(_pedro, "Redigir")], null, T0.AddHours(5), _diana.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Corrigida);
        atividade.Historico.Last().UsuarioId.Should().Be(_diana.UsuarioId);
    }

    [Fact]
    public void Remover_o_ultimo_pendente_em_atividade_reaberta_deixa_a_atividade_feita()
    {
        var atividade = Reaberta();
        atividade.ConcluirParte(_pedro, T0.AddDays(3));

        atividade.DefinirResponsaveis([R(_pedro, "Redigir")], null, T0.AddDays(4), _diana.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Feita);
        atividade.Historico.Last().UsuarioId.Should().Be(_diana.UsuarioId);
    }

    // ---- F5 e F6: atalho do revisor único responsável ----

    [Fact]
    public void Atalho_do_revisor_tambem_vale_ao_concluir_uma_atividade_reaberta()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_diana)], null, T0.AddMinutes(1), _diana.UsuarioId);
        atividade.ConcluirParte(_diana, T0.AddHours(1));
        atividade.Reabrir("Ajuste", [_diana.UsuarioId], T0.AddDays(1), _diana.UsuarioId);

        atividade.ConcluirParte(_diana, T0.AddDays(2));

        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(T0.AddDays(2));
        Percurso(atividade).TakeLast(3).Should().Equal(
            (StatusAtividade.Concluida, StatusAtividade.Reaberta),
            (StatusAtividade.Reaberta, StatusAtividade.Feita),
            (StatusAtividade.Feita, StatusAtividade.Concluida));
        atividade.Historico.Select(h => h.Ordem).Should().Equal(1, 2, 3, 4, 5, 6, 7);
    }

    [Fact]
    public void Atalho_do_revisor_tambem_vale_ao_concluir_a_correcao()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_diana), R(_pedro)], null, T0, _diana.UsuarioId);
        atividade.ConcluirParte(_diana, T0.AddHours(1));
        atividade.ConcluirParte(_pedro, T0.AddHours(2));
        atividade.Devolver(_diana, "Ajustar", [_diana.UsuarioId], T0.AddHours(3));
        atividade.DefinirResponsaveis([R(_diana)], null, T0.AddHours(4), _diana.UsuarioId);
        atividade.Status.Should().Be(StatusAtividade.ParaCorrecao);

        atividade.ConcluirParte(_diana, T0.AddHours(5));

        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(T0.AddHours(5));
        Percurso(atividade).TakeLast(2).Should().Equal(
            (StatusAtividade.ParaCorrecao, StatusAtividade.Corrigida),
            (StatusAtividade.Corrigida, StatusAtividade.Concluida));
    }

    [Fact]
    public void Atalho_do_revisor_tambem_vale_ao_remover_o_ultimo_pendente()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_diana), R(_pedro)], null, T0, _diana.UsuarioId);
        atividade.ConcluirParte(_diana, T0.AddHours(1));
        atividade.Status.Should().Be(StatusAtividade.Direcionada);

        atividade.DefinirResponsaveis([R(_diana)], null, T0.AddHours(2), _diana.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(T0.AddHours(2));
        atividade.Historico.TakeLast(2).Should().OnlyContain(h => h.UsuarioId == _diana.UsuarioId && h.DataHora == T0.AddHours(2));
        Percurso(atividade).TakeLast(2).Should().Equal(
            (StatusAtividade.Direcionada, StatusAtividade.Feita),
            (StatusAtividade.Feita, StatusAtividade.Concluida));
    }

    [Fact]
    public void Atalho_do_revisor_ao_remover_o_ultimo_pendente_nao_vale_quando_outro_diretor_age()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_diana), R(_pedro)], null, T0, _diana.UsuarioId);
        atividade.ConcluirParte(_diana, T0.AddHours(1));
        var historicoAntes = atividade.Historico.Count;

        atividade.DefinirResponsaveis([R(_diana)], null, T0.AddHours(2), _davi.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Feita);
        atividade.ConcluidaEm.Should().BeNull();
        atividade.Historico.Should().HaveCount(historicoAntes + 1);
        atividade.Historico.Last().UsuarioId.Should().Be(_davi.UsuarioId);
    }

    [Fact]
    public void Remocao_que_avanca_a_atividade_grava_o_motivo_e_a_conclusao_da_parte_nao()
    {
        var removendo = Nova();
        removendo.DefinirResponsaveis([R(_diana), R(_pedro)], null, T0, _diana.UsuarioId);
        removendo.ConcluirParte(_diana, T0.AddHours(1));
        removendo.DefinirResponsaveis([R(_diana)], null, T0.AddHours(2), _diana.UsuarioId);

        var concluindo = Nova();
        concluindo.DefinirResponsaveis([R(_diana), R(_pedro)], null, T0, _diana.UsuarioId);
        concluindo.ConcluirParte(_diana, T0.AddHours(1));
        concluindo.ConcluirParte(_pedro, T0.AddHours(2));

        removendo.Historico.TakeLast(2).Should().OnlyContain(h => h.Motivo == "Responsável pendente removido.");
        concluindo.Historico.Last().Motivo.Should().BeNull();

        var correcao = ParaCorrecao();
        correcao.ConcluirParte(_pedro, T0.AddHours(4));
        correcao.DefinirResponsaveis([R(_pedro, "Redigir")], null, T0.AddHours(5), _diana.UsuarioId);
        correcao.Status.Should().Be(StatusAtividade.Corrigida);
        correcao.Historico.Last().Motivo.Should().Be("Responsável pendente removido.");
    }

    // ---- F7: revisor mantido quando a diretoria passa de um para dois ----

    [Fact]
    public void Revisor_e_mantido_quando_os_responsaveis_de_diretoria_passam_de_um_para_dois()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_davi), R(_pedro)], null, T0, _diana.UsuarioId);

        atividade.DefinirResponsaveis([R(_davi), R(_diana), R(_pedro)], null, T0, _diana.UsuarioId);

        atividade.RevisorId.Should().Be(_davi.UsuarioId);
    }

    // ---- F8: Reabrir ----

    [Fact]
    public void Reabrir_recusa_selecao_vazia_responsavel_de_fora_e_motivo_longo()
    {
        var atividade = Concluida();
        var concluidaEm = atividade.ConcluidaEm;

        var nenhum = () => atividade.Reabrir("Ajuste", [], T0.AddDays(2), _diana.UsuarioId);
        var deFora = () => atividade.Reabrir("Ajuste", [_ester.UsuarioId], T0.AddDays(2), _diana.UsuarioId);
        var longo = () => atividade.Reabrir(new string('a', 2001), [_pedro.UsuarioId], T0.AddDays(2), _diana.UsuarioId);

        nenhum.Should().Throw<ExcecaoDeDominio>().WithMessage("Selecione ao menos um responsável.");
        deFora.Should().Throw<ExcecaoDeDominio>().WithMessage("Responsável selecionado não faz parte da atividade.");
        longo.Should().Throw<ExcecaoDeDominio>().WithMessage("O motivo deve ter até 2000 caracteres.");
        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(concluidaEm);
        atividade.Historico.Should().HaveCount(4);
    }
}
