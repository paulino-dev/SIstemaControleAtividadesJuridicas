using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class EventoTests
{
    private static readonly DateTime T0 = new(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc);

    private readonly PessoaDoEvento _ana = new(Guid.CreateVersion7(), true);
    private readonly PessoaDoEvento _bia = new(Guid.CreateVersion7(), true);
    private readonly PessoaDoEvento _caio = new(Guid.CreateVersion7(), true);
    private readonly PessoaDoEvento _duda = new(Guid.CreateVersion7(), true);

    private static DadosDoEvento Dados(
        string titulo = "Audiência de instrução", string? descricao = "Sala 3", string? local = "Fórum central",
        TipoEvento tipo = TipoEvento.Audiencia, DateTime? inicio = null, int duracao = 60) =>
        new(titulo, descricao, local, tipo, inicio ?? T0, duracao);

    private Evento Novo(params PessoaDoEvento[] participantes) =>
        Evento.Criar(Dados(), VinculoDoEvento.Nenhum, _ana, participantes);

    private static Guid[] Ids(Evento evento) => [.. evento.Participantes.Select(p => p.UsuarioId)];

    private static Processo NovoProcesso(params Guid[] clientes) => Processo.Criar(
        new DadosDoProcesso(null, "Reclamação trabalhista", Guid.CreateVersion7(), null, null, null, null, null, null),
        [.. clientes.Select(c => new ClienteDoProcesso(c, PoloProcessual.Ativo))]);

    // ---- Criação e dados ----

    [Fact]
    public void Nasce_com_dados_aparados_e_o_criador_entre_os_participantes()
    {
        var evento = Evento.Criar(
            Dados("  Audiência  ", "  Sala 3  ", "  Fórum  "), VinculoDoEvento.Nenhum, _ana, [_bia]);

        evento.Titulo.Should().Be("Audiência");
        evento.Descricao.Should().Be("Sala 3");
        evento.Local.Should().Be("Fórum");
        evento.Tipo.Should().Be(TipoEvento.Audiencia);
        evento.InicioEm.Should().Be(T0);
        evento.DuracaoEstimadaMinutos.Should().Be(60);
        evento.CriadorId.Should().Be(_ana.UsuarioId);
        evento.ClienteId.Should().BeNull();
        evento.ProcessoId.Should().BeNull();
        evento.AtividadeId.Should().BeNull();
        evento.VersaoDosParticipantes.Should().Be(0);
        Ids(evento).Should().BeEquivalentTo([_ana.UsuarioId, _bia.UsuarioId]);
        evento.Participantes.Should().OnlyContain(p => p.EventoId == evento.Id);
    }

    [Fact]
    public void Criador_fora_da_lista_e_incluido_sozinho()
    {
        var evento = Novo();

        Ids(evento).Should().BeEquivalentTo([_ana.UsuarioId]);
    }

    [Fact]
    public void Criador_na_lista_nao_e_duplicado()
    {
        var evento = Novo(_ana, _bia);

        Ids(evento).Should().BeEquivalentTo([_ana.UsuarioId, _bia.UsuarioId]);
        evento.Participantes.Should().HaveCount(2);
    }

    [Fact]
    public void Descricao_e_local_vazios_viram_nulos()
    {
        var evento = Evento.Criar(Dados(descricao: "   ", local: ""), VinculoDoEvento.Nenhum, _ana, []);

        evento.Descricao.Should().BeNull();
        evento.Local.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "O título do evento é obrigatório.")]
    [InlineData("   ", "O título do evento é obrigatório.")]
    public void Titulo_e_obrigatorio(string? titulo, string mensagem)
    {
        var criar = () => Evento.Criar(Dados(titulo!), VinculoDoEvento.Nenhum, _ana, []);

        criar.Should().Throw<ExcecaoDeDominio>().WithMessage(mensagem);
    }

    [Fact]
    public void Limites_de_tamanho_valem_depois_de_aparar()
    {
        Evento.Criar(Dados(new string('a', 200) + "  ", new string('d', 4000), new string('l', 200)),
            VinculoDoEvento.Nenhum, _ana, []).Titulo.Should().HaveLength(200);

        Action titulo = () => Evento.Criar(Dados(new string('a', 201)), VinculoDoEvento.Nenhum, _ana, []);
        Action descricao = () => Evento.Criar(Dados(descricao: new string('d', 4001)), VinculoDoEvento.Nenhum, _ana, []);
        Action local = () => Evento.Criar(Dados(local: new string('l', 201)), VinculoDoEvento.Nenhum, _ana, []);

        titulo.Should().Throw<ExcecaoDeDominio>().WithMessage("O título do evento deve ter até 200 caracteres.");
        descricao.Should().Throw<ExcecaoDeDominio>().WithMessage("A descrição do evento deve ter até 4000 caracteres.");
        local.Should().Throw<ExcecaoDeDominio>().WithMessage("O local do evento deve ter até 200 caracteres.");
    }

    [Fact]
    public void Tipo_fora_do_enum_e_recusado()
    {
        var criar = () => Evento.Criar(Dados(tipo: (TipoEvento)99), VinculoDoEvento.Nenhum, _ana, []);

        criar.Should().Throw<ExcecaoDeDominio>().WithMessage("Tipo de evento inválido.");
    }

    [Theory]
    [InlineData(0, "A duração deve ser maior que zero.")]
    [InlineData(-5, "A duração deve ser maior que zero.")]
    [InlineData(10081, "A duração deve ser de no máximo 7 dias.")]
    public void Duracao_fora_do_intervalo_e_recusada(int duracao, string mensagem)
    {
        var criar = () => Evento.Criar(Dados(duracao: duracao), VinculoDoEvento.Nenhum, _ana, []);

        criar.Should().Throw<ExcecaoDeDominio>().WithMessage(mensagem);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10080)]
    public void Duracao_nos_extremos_e_aceita(int duracao)
    {
        Evento.Criar(Dados(duracao: duracao), VinculoDoEvento.Nenhum, _ana, [])
            .DuracaoEstimadaMinutos.Should().Be(duracao);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Inicio_que_nao_e_utc_e_recusado(DateTimeKind tipo)
    {
        var inicio = DateTime.SpecifyKind(T0, tipo);

        var criar = () => Evento.Criar(Dados(inicio: inicio), VinculoDoEvento.Nenhum, _ana, []);
        var alterar = () => Novo().Alterar(Dados(inicio: inicio), VinculoDoEvento.Nenhum);

        criar.Should().Throw<ArgumentException>();
        alterar.Should().Throw<ArgumentException>();
    }

    // ---- Alterar ----

    [Fact]
    public void Alterar_troca_dados_e_vinculo_sem_mexer_em_participantes_nem_criador()
    {
        var evento = Novo(_bia);
        var cliente = Guid.CreateVersion7();
        var atividade = Guid.CreateVersion7();
        var antes = evento.Participantes.ToList();

        evento.Alterar(
            Dados("Reunião", null, null, TipoEvento.Reuniao, T0.AddDays(1), 30),
            VinculoDoEvento.Criar(cliente, null, atividade));

        evento.Titulo.Should().Be("Reunião");
        evento.Descricao.Should().BeNull();
        evento.Tipo.Should().Be(TipoEvento.Reuniao);
        evento.InicioEm.Should().Be(T0.AddDays(1));
        evento.DuracaoEstimadaMinutos.Should().Be(30);
        evento.ClienteId.Should().Be(cliente);
        evento.AtividadeId.Should().Be(atividade);
        evento.CriadorId.Should().Be(_ana.UsuarioId);
        evento.Participantes.Should().Equal(antes);
        evento.VersaoDosParticipantes.Should().Be(0);
    }

    [Fact]
    public void Alterar_recusado_nao_muda_nada()
    {
        var evento = Novo(_bia);

        var alterar = () => evento.Alterar(Dados("Outro", duracao: 0), VinculoDoEvento.Criar(Guid.CreateVersion7(), null, null));

        alterar.Should().Throw<ExcecaoDeDominio>();
        evento.Titulo.Should().Be("Audiência de instrução");
        evento.ClienteId.Should().BeNull();
        evento.DuracaoEstimadaMinutos.Should().Be(60);
    }

    // ---- Vínculo ----

    [Fact]
    public void Sem_processo_cliente_e_atividade_sao_livres()
    {
        var cliente = Guid.CreateVersion7();
        var atividade = Guid.CreateVersion7();

        var vinculo = VinculoDoEvento.Criar(cliente, null, atividade);

        vinculo.ClienteId.Should().Be(cliente);
        vinculo.ProcessoId.Should().BeNull();
        vinculo.AtividadeId.Should().Be(atividade);
        VinculoDoEvento.Nenhum.Should().Be(VinculoDoEvento.Criar(null, null, null));
    }

    [Fact]
    public void Com_processo_o_cliente_tem_de_ser_um_dos_clientes_dele()
    {
        var cliente = Guid.CreateVersion7();
        var processo = NovoProcesso(cliente, Guid.CreateVersion7());

        var vinculo = VinculoDoEvento.Criar(cliente, processo, null);
        var foraDoProcesso = () => VinculoDoEvento.Criar(Guid.CreateVersion7(), processo, null);

        vinculo.ProcessoId.Should().Be(processo.Id);
        vinculo.ClienteId.Should().Be(cliente);
        foraDoProcesso.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("O cliente do evento deve ser um dos clientes do processo.");
    }

    [Fact]
    public void Processo_sem_cliente_e_aceito()
    {
        var processo = NovoProcesso(Guid.CreateVersion7());

        var vinculo = VinculoDoEvento.Criar(null, processo, null);

        vinculo.ProcessoId.Should().Be(processo.Id);
        vinculo.ClienteId.Should().BeNull();
    }

    [Fact]
    public void Guid_vazio_no_vinculo_e_erro_de_dominio()
    {
        var cliente = () => VinculoDoEvento.Criar(Guid.Empty, null, null);
        var atividade = () => VinculoDoEvento.Criar(null, null, Guid.Empty);

        cliente.Should().Throw<ExcecaoDeDominio>().WithMessage("Cliente inválido.");
        atividade.Should().Throw<ExcecaoDeDominio>().WithMessage("Atividade inválida.");
    }

    // ---- Participantes na criação ----

    [Fact]
    public void Participante_inativo_na_criacao_e_recusado()
    {
        var inativo = new PessoaDoEvento(Guid.CreateVersion7(), false);

        var criar = () => Novo(_bia, inativo);

        criar.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("Usuário inativo não pode ser incluído como participante.");
    }

    [Fact]
    public void Criador_inativo_e_recusado()
    {
        var inativo = new PessoaDoEvento(Guid.CreateVersion7(), false);

        var criar = () => Evento.Criar(Dados(), VinculoDoEvento.Nenhum, inativo, [_bia]);

        criar.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("Usuário inativo não pode ser incluído como participante.");
    }

    [Fact]
    public void Participante_repetido_na_criacao_e_recusado()
    {
        var criar = () => Novo(_bia, _bia);

        criar.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("Um usuário não pode se repetir entre os participantes.");
    }

    [Fact]
    public void Guid_vazio_na_criacao_e_erro_de_dominio()
    {
        var vazio = new PessoaDoEvento(Guid.Empty, true);

        var participante = () => Novo(vazio);
        var criador = () => Evento.Criar(Dados(), VinculoDoEvento.Nenhum, vazio, []);

        participante.Should().Throw<ExcecaoDeDominio>().WithMessage("Usuário inválido.");
        criador.Should().Throw<ExcecaoDeDominio>().WithMessage("Usuário inválido.");
    }

    [Fact]
    public void Entradas_nulas_na_criacao_sao_recusadas()
    {
        var criador = () => Evento.Criar(Dados(), VinculoDoEvento.Nenhum, null!, []);
        var lista = () => Evento.Criar(Dados(), VinculoDoEvento.Nenhum, _ana, null!);
        var elemento = () => Evento.Criar(Dados(), VinculoDoEvento.Nenhum, _ana, [_bia, null!]);
        var dados = () => Evento.Criar(null!, VinculoDoEvento.Nenhum, _ana, []);
        var vinculo = () => Evento.Criar(Dados(), null!, _ana, []);

        criador.Should().Throw<ArgumentNullException>();
        lista.Should().Throw<ArgumentNullException>();
        elemento.Should().Throw<ArgumentException>();
        dados.Should().Throw<ArgumentNullException>();
        vinculo.Should().Throw<ArgumentNullException>();
    }

    // ---- DefinirParticipantes ----

    [Fact]
    public void Definir_devolve_adicionados_e_removidos_e_preserva_a_entrada_de_quem_fica()
    {
        var evento = Novo(_bia, _caio);
        var entradaDeBia = evento.Participantes.Single(p => p.UsuarioId == _bia.UsuarioId);

        var alteracao = evento.DefinirParticipantes([_bia, _duda]);

        alteracao.Adicionados.Should().Equal(_duda.UsuarioId);
        alteracao.Removidos.Should().BeEquivalentTo([_ana.UsuarioId, _caio.UsuarioId]);
        alteracao.Houve.Should().BeTrue();
        Ids(evento).Should().BeEquivalentTo([_bia.UsuarioId, _duda.UsuarioId]);
        evento.Participantes.Single(p => p.UsuarioId == _bia.UsuarioId).Should().BeSameAs(entradaDeBia);
        evento.Participantes.Single(p => p.UsuarioId == _duda.UsuarioId).EventoId.Should().Be(evento.Id);
    }

    [Fact]
    public void O_criador_pode_ser_retirado()
    {
        var evento = Novo(_bia);

        evento.DefinirParticipantes([_bia]);

        evento.Participa(_ana.UsuarioId).Should().BeFalse();
        evento.Participa(_bia.UsuarioId).Should().BeTrue();
        evento.CriadorId.Should().Be(_ana.UsuarioId);
    }

    [Fact]
    public void Contador_so_aumenta_quando_algo_muda()
    {
        var evento = Novo(_bia);

        var igual = evento.DefinirParticipantes([_bia, _ana]);
        var mudou = evento.DefinirParticipantes([_bia, _ana, _caio]);

        igual.Houve.Should().BeFalse();
        igual.Adicionados.Should().BeEmpty();
        igual.Removidos.Should().BeEmpty();
        mudou.Houve.Should().BeTrue();
        evento.VersaoDosParticipantes.Should().Be(1);
    }

    [Fact]
    public void Quem_ja_participava_pode_ficar_mesmo_inativo_mas_o_novo_inativo_e_recusado()
    {
        var evento = Novo(_bia);
        var biaInativa = _bia with { Ativo = false };

        var mantem = evento.DefinirParticipantes([biaInativa, _ana]);
        var novoInativo = () => evento.DefinirParticipantes([_ana, biaInativa, new PessoaDoEvento(Guid.CreateVersion7(), false)]);

        mantem.Houve.Should().BeFalse();
        novoInativo.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("Usuário inativo não pode ser incluído como participante.");
    }

    [Fact]
    public void Definir_com_lista_vazia_e_recusado()
    {
        var evento = Novo(_bia);

        var definir = () => evento.DefinirParticipantes([]);

        definir.Should().Throw<ExcecaoDeDominio>().WithMessage("O evento precisa de ao menos um participante.");
    }

    [Fact]
    public void Definir_com_repeticao_ou_guid_vazio_ou_nulo_e_recusado()
    {
        var evento = Novo(_bia);

        var repetido = () => evento.DefinirParticipantes([_bia, _bia]);
        var vazio = () => evento.DefinirParticipantes([new PessoaDoEvento(Guid.Empty, true)]);
        var nulo = () => evento.DefinirParticipantes([_bia, null!]);
        var lista = () => evento.DefinirParticipantes(null!);

        repetido.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("Um usuário não pode se repetir entre os participantes.");
        vazio.Should().Throw<ExcecaoDeDominio>().WithMessage("Usuário inválido.");
        nulo.Should().Throw<ArgumentException>();
        lista.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Quando_definir_lanca_nada_muda_mesmo_com_remocoes_no_mesmo_pedido()
    {
        var evento = Novo(_bia, _caio);
        var entradas = evento.Participantes.ToList();
        var inativo = new PessoaDoEvento(Guid.CreateVersion7(), false);

        // Remove Ana e Caio, mantém Bia e inclui um inativo novo: o pedido inteiro é recusado.
        var inativoNovo = () => evento.DefinirParticipantes([_bia, inativo]);
        var repetido = () => evento.DefinirParticipantes([_bia, _duda, _duda]);
        var vazio = () => evento.DefinirParticipantes([]);
        var guidVazio = () => evento.DefinirParticipantes([_bia, new PessoaDoEvento(Guid.Empty, true)]);

        inativoNovo.Should().Throw<ExcecaoDeDominio>();
        repetido.Should().Throw<ExcecaoDeDominio>();
        vazio.Should().Throw<ExcecaoDeDominio>();
        guidVazio.Should().Throw<ExcecaoDeDominio>();
        evento.Participantes.Should().Equal(entradas);
        evento.Participantes.Should().HaveCount(3);
        evento.VersaoDosParticipantes.Should().Be(0);
    }

    // ---- Participa, FimEm e Sobrepoe ----

    [Fact]
    public void Participa_diz_se_o_usuario_esta_entre_os_participantes()
    {
        var evento = Novo(_bia);

        evento.Participa(_bia.UsuarioId).Should().BeTrue();
        evento.Participa(_caio.UsuarioId).Should().BeFalse();
    }

    [Fact]
    public void Fim_e_o_inicio_mais_a_duracao()
    {
        Novo().FimEm.Should().Be(T0.AddMinutes(60));
    }

    // Evento de 14:00 a 15:00.
    [Theory]
    [InlineData("14:15", "14:45", true)]   // dentro
    [InlineData("13:30", "14:30", true)]   // parcial antes
    [InlineData("14:30", "15:30", true)]   // parcial depois
    [InlineData("13:00", "16:00", true)]   // contendo
    [InlineData("13:00", "14:00", false)]  // encostado antes
    [InlineData("15:00", "16:00", false)]  // encostado depois
    [InlineData("16:00", "17:00", false)]  // disjunto
    [InlineData("12:00", "13:00", false)]  // disjunto antes
    public void Sobrepoe_por_horario(string inicio, string fim, bool esperado)
    {
        var evento = Novo();

        evento.Sobrepoe(Hora(inicio), Hora(fim)).Should().Be(esperado);
    }

    private static DateTime Hora(string hhmm) =>
        new(2026, 10, 12, int.Parse(hhmm[..2]), int.Parse(hhmm[3..]), 0, DateTimeKind.Utc);

    // ---- Onda final ----

    [Fact]
    public void Contador_aumenta_na_remocao_e_na_troca_combinada()
    {
        var evento = Novo(_bia, _caio);

        evento.DefinirParticipantes([_ana, _bia]);          // só remove o Caio
        evento.VersaoDosParticipantes.Should().Be(1);

        evento.DefinirParticipantes([_ana, _duda]);         // remove a Bia e inclui a Duda
        evento.VersaoDosParticipantes.Should().Be(2);

        evento.DefinirParticipantes([_duda, _ana]);         // nada muda
        evento.VersaoDosParticipantes.Should().Be(2);
    }

    [Fact]
    public void Alterar_aceita_processo_e_depois_o_vinculo_limpo()
    {
        var cliente = Guid.CreateVersion7();
        var processo = NovoProcesso(cliente);
        var evento = Novo();

        evento.Alterar(Dados(), VinculoDoEvento.Criar(cliente, processo, null));
        evento.ClienteId.Should().Be(cliente);
        evento.ProcessoId.Should().Be(processo.Id);

        evento.Alterar(Dados(), VinculoDoEvento.Nenhum);
        evento.ClienteId.Should().BeNull();
        evento.ProcessoId.Should().BeNull();
        evento.AtividadeId.Should().BeNull();
    }

    [Fact]
    public void Sobrepoe_recusa_argumentos_fora_de_utc_e_intervalo_vazio_ou_invertido()
    {
        var evento = Novo();
        var inicio = DateTime.SpecifyKind(T0, DateTimeKind.Unspecified);

        var naoUtc = () => evento.Sobrepoe(inicio, T0.AddHours(1));
        var fimNaoUtc = () => evento.Sobrepoe(T0, DateTime.SpecifyKind(T0.AddHours(1), DateTimeKind.Local));
        var vazio = () => evento.Sobrepoe(T0, T0);
        var invertido = () => evento.Sobrepoe(T0, T0.AddMinutes(-1));

        naoUtc.Should().Throw<ExcecaoDeDominio>();
        fimNaoUtc.Should().Throw<ExcecaoDeDominio>();
        vazio.Should().Throw<ExcecaoDeDominio>();
        invertido.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData(1999, 12, 31, 23, 59)]
    [InlineData(2101, 1, 1, 0, 0)]
    [InlineData(1, 1, 1, 0, 0)]
    public void Inicio_fora_dos_anos_2000_a_2100_e_recusado(int ano, int mes, int dia, int hora, int minuto)
    {
        var inicio = new DateTime(ano, mes, dia, hora, minuto, 0, DateTimeKind.Utc);

        var criar = () => Evento.Criar(Dados(inicio: inicio), VinculoDoEvento.Nenhum, _ana, []);
        var alterar = () => Novo().Alterar(Dados(inicio: inicio), VinculoDoEvento.Nenhum);

        criar.Should().Throw<ExcecaoDeDominio>();
        alterar.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Inicio_nos_extremos_do_intervalo_de_anos_e_aceito()
    {
        Evento.Criar(Dados(inicio: new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)), VinculoDoEvento.Nenhum, _ana, [])
            .InicioEm.Year.Should().Be(2000);
        Evento.Criar(Dados(inicio: new DateTime(2100, 12, 31, 23, 59, 0, DateTimeKind.Utc)), VinculoDoEvento.Nenhum, _ana, [])
            .InicioEm.Year.Should().Be(2100);
    }
    [Fact]
    public void Alterar_sem_vinculo_mantem_o_vinculo_atual_mesmo_que_o_cliente_tenha_saido_do_processo()
    {
        var cliente = Guid.CreateVersion7();
        var processo = NovoProcesso(cliente);
        var atividade = Guid.CreateVersion7();
        var evento = Novo();
        evento.Alterar(Dados(), VinculoDoEvento.Criar(cliente, processo, atividade));
        processo.DefinirClientes([new ClienteDoProcesso(Guid.CreateVersion7(), PoloProcessual.Ativo)]);

        evento.Alterar(Dados("Remarcada", inicio: T0.AddHours(3)));

        evento.Titulo.Should().Be("Remarcada");
        evento.InicioEm.Should().Be(T0.AddHours(3));
        (evento.ClienteId, evento.ProcessoId, evento.AtividadeId).Should().Be((cliente, processo.Id, atividade));
    }

    [Fact]
    public void Alterar_sem_vinculo_recusa_dados_invalidos_sem_mudar_nada()
    {
        var evento = Novo();

        var alterar = () => evento.Alterar(Dados("Outro", duracao: 0));

        alterar.Should().Throw<ExcecaoDeDominio>();
        evento.Titulo.Should().Be("Audiência de instrução");
    }
}
