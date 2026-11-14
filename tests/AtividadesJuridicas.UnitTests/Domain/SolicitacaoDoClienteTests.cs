using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.UnitTests.Domain;

public class SolicitacaoDoClienteTests
{
    private static readonly DateTime T0 = new(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);

    private readonly Guid _clienteId = Guid.CreateVersion7();
    private readonly Guid _solicitanteId = Guid.CreateVersion7();

    private SolicitacaoDoCliente Nova(string descricao = "Preciso de uma cópia do contrato.", UrgenciaDaSolicitacao urgencia = UrgenciaDaSolicitacao.Media) =>
        SolicitacaoDoCliente.Abrir(_clienteId, _solicitanteId, descricao, urgencia);

    private SolicitacaoDoCliente Cancelada()
    {
        var solicitacao = Nova();
        solicitacao.Cancelar(T0);
        return solicitacao;
    }

    private static readonly DadosDaAtividade DadosDeAtividade = new("Atender a solicitação", "Descrição da atividade.", null, 1);

    /// <summary>A atividade criada a partir da solicitação: do cliente dela e com o Id dela.</summary>
    private static Atividade AtividadeDa(SolicitacaoDoCliente solicitacao) => Atividade.Criar(
        DadosDeAtividade, VinculoDaAtividade.Criar(solicitacao.ClienteId, null, null), TipoAutor.Sistema, null, T0, solicitacao.Id);

    // WithMessage trata "?" como curinga; aqui a mensagem precisa ser exatamente esta.
    private static void DeveRecusarCom(Action acao, string mensagem) =>
        acao.Should().Throw<ExcecaoDeDominio>().Which.Message.Should().Be(mensagem);

    // ---- Abertura ----

    [Fact]
    public void Abrir_apara_a_descricao_e_nasce_aguardando()
    {
        var solicitacao = Nova("  Preciso de uma cópia do contrato.  ", UrgenciaDaSolicitacao.Alta);

        solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        solicitacao.Descricao.Should().Be("Preciso de uma cópia do contrato.");
        solicitacao.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        solicitacao.ClienteId.Should().Be(_clienteId);
        solicitacao.SolicitanteId.Should().Be(_solicitanteId);
        solicitacao.CanceladaEm.Should().BeNull();
        solicitacao.AtividadeId.Should().BeNull();
        solicitacao.VersaoDosDocumentos.Should().Be(0);
        solicitacao.EhDoSolicitante(_solicitanteId).Should().BeTrue();
        solicitacao.EhDoSolicitante(Guid.CreateVersion7()).Should().BeFalse();
    }

    [Fact]
    public void Abrir_recusa_cliente_e_solicitante_vazios_como_erro_de_programacao()
    {
        var semCliente = () => SolicitacaoDoCliente.Abrir(Guid.Empty, _solicitanteId, "d", UrgenciaDaSolicitacao.Baixa);
        var semSolicitante = () => SolicitacaoDoCliente.Abrir(_clienteId, Guid.Empty, "d", UrgenciaDaSolicitacao.Baixa);

        semCliente.Should().Throw<ArgumentException>();
        semSolicitante.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("", 2, "A descrição da solicitação é obrigatória.")]
    [InlineData("   ", 2, "A descrição da solicitação é obrigatória.")]
    [InlineData(null, 2, "A descrição da solicitação é obrigatória.")]
    [InlineData("A", 9, "Urgência inválida.")]
    [InlineData("A", 0, "Urgência inválida.")]
    public void Abrir_recusa_descricao_vazia_e_urgencia_invalida(string? descricao, int urgencia, string mensagem)
    {
        Action acao = () => SolicitacaoDoCliente.Abrir(_clienteId, _solicitanteId, descricao!, (UrgenciaDaSolicitacao)urgencia);

        DeveRecusarCom(acao, mensagem);
    }

    [Fact]
    public void Abrir_recusa_descricao_longa_e_aceita_exatamente_o_limite()
    {
        Action longa = () => Nova(new string('a', 4001));
        var noLimite = Nova(new string('a', 4000));
        var noLimiteComEspacos = Nova($"  {new string('a', 4000)}  ");

        DeveRecusarCom(longa, "A descrição da solicitação deve ter até 4000 caracteres.");
        noLimite.Descricao.Should().HaveLength(4000);
        noLimiteComEspacos.Descricao.Should().HaveLength(4000);
        SolicitacaoDoCliente.TamanhoMaximoDescricao.Should().Be(4000);
    }

    // ---- Alterar ----

    [Fact]
    public void Alterar_troca_descricao_e_urgencia_so_enquanto_aguarda()
    {
        var solicitacao = Nova();

        solicitacao.Alterar("  Agora preciso da certidão.  ", UrgenciaDaSolicitacao.Alta);

        solicitacao.Descricao.Should().Be("Agora preciso da certidão.");
        solicitacao.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
    }

    [Fact]
    public void Alterar_com_dados_invalidos_nao_muda_nada()
    {
        var solicitacao = Nova("Original", UrgenciaDaSolicitacao.Baixa);

        var semDescricao = () => solicitacao.Alterar("  ", UrgenciaDaSolicitacao.Alta);
        var longa = () => solicitacao.Alterar(new string('a', 4001), UrgenciaDaSolicitacao.Alta);
        var urgenciaInvalida = () => solicitacao.Alterar("Nova", (UrgenciaDaSolicitacao)9);

        DeveRecusarCom(semDescricao, "A descrição da solicitação é obrigatória.");
        DeveRecusarCom(longa, "A descrição da solicitação deve ter até 4000 caracteres.");
        DeveRecusarCom(urgenciaInvalida, "Urgência inválida.");
        solicitacao.Descricao.Should().Be("Original");
        solicitacao.Urgencia.Should().Be(UrgenciaDaSolicitacao.Baixa);
    }

    // ---- Cancelar ----

    [Fact]
    public void Cancelar_deixa_cancelada_e_nada_mais_muda()
    {
        var solicitacao = Nova();

        solicitacao.Cancelar(T0);

        solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);
        solicitacao.CanceladaEm.Should().Be(T0);
        solicitacao.AtividadeId.Should().BeNull();
        DeveRecusarCom(() => solicitacao.Alterar("Outra", UrgenciaDaSolicitacao.Baixa), SolicitacaoDoCliente.NaoPodeMaisSerAlterada);
        DeveRecusarCom(() => solicitacao.Cancelar(T0.AddMinutes(1)), SolicitacaoDoCliente.NaoPodeMaisSerAlterada);
        DeveRecusarCom(solicitacao.RegistrarMudancaNosDocumentos, SolicitacaoDoCliente.NaoPodeMaisSerAlterada);
        solicitacao.CanceladaEm.Should().Be(T0);
        solicitacao.VersaoDosDocumentos.Should().Be(0);
    }

    [Fact]
    public void Cancelar_exige_horario_em_utc()
    {
        var solicitacao = Nova();

        var local = () => solicitacao.Cancelar(new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Local));
        var semFuso = () => solicitacao.Cancelar(new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Unspecified));

        local.Should().Throw<ArgumentException>();
        semFuso.Should().Throw<ArgumentException>();
        solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
    }

    // ---- Documentos ----

    [Fact]
    public void RegistrarMudancaNosDocumentos_aumenta_a_versao_dos_documentos()
    {
        var solicitacao = Nova();

        solicitacao.RegistrarMudancaNosDocumentos();
        solicitacao.RegistrarMudancaNosDocumentos();

        solicitacao.VersaoDosDocumentos.Should().Be(2);
        solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
    }

    // ---- Vínculo com a atividade ----

    [Fact]
    public void Vincular_em_aguardando_vira_em_atividade()
    {
        var solicitacao = Nova();
        var atividade = AtividadeDa(solicitacao);

        solicitacao.VincularAtividade(atividade, confirmouCancelada: false);

        solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);
        solicitacao.AtividadeId.Should().Be(atividade.Id);
        solicitacao.CanceladaEm.Should().BeNull();
    }

    [Fact]
    public void Vincular_recusa_atividade_nula_como_erro_de_programacao()
    {
        var solicitacao = Nova();

        var acao = () => solicitacao.VincularAtividade(null!, confirmouCancelada: false);

        acao.Should().Throw<ArgumentNullException>();
        solicitacao.AtividadeId.Should().BeNull();
    }

    [Fact]
    public void Vincular_recusa_atividade_de_outro_cliente()
    {
        var solicitacao = Nova();
        var deOutroCliente = Atividade.Criar(
            DadosDeAtividade, VinculoDaAtividade.Criar(Guid.CreateVersion7(), null, null), TipoAutor.Sistema, null, T0, solicitacao.Id);

        DeveRecusarCom(() => solicitacao.VincularAtividade(deOutroCliente, confirmouCancelada: false), SolicitacaoDoCliente.AtividadeDeOutroCliente);
        solicitacao.AtividadeId.Should().BeNull();
        solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
    }

    [Fact]
    public void Vincular_recusa_atividade_criada_de_outra_solicitacao()
    {
        var solicitacao = Nova();
        var vinculo = VinculoDaAtividade.Criar(_clienteId, null, null);
        var deOutraSolicitacao = Atividade.Criar(DadosDeAtividade, vinculo, TipoAutor.Sistema, null, T0, Guid.CreateVersion7());
        var semSolicitacao = Atividade.Criar(DadosDeAtividade, vinculo, TipoAutor.Sistema, null, T0);

        var daOutra = () => solicitacao.VincularAtividade(deOutraSolicitacao, confirmouCancelada: false);
        var avulsa = () => solicitacao.VincularAtividade(semSolicitacao, confirmouCancelada: false);

        daOutra.Should().Throw<ArgumentException>();
        avulsa.Should().Throw<ArgumentException>();
        solicitacao.AtividadeId.Should().BeNull();
    }

    [Fact]
    public void Vincular_cancelada_exige_confirmacao()
    {
        var solicitacao = Cancelada();
        var atividade = AtividadeDa(solicitacao);

        DeveRecusarCom(() => solicitacao.VincularAtividade(atividade, confirmouCancelada: false), SolicitacaoDoCliente.CanceladaPeloCliente);
        solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);
        solicitacao.AtividadeId.Should().BeNull();

        solicitacao.VincularAtividade(atividade, confirmouCancelada: true);

        solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);
        solicitacao.AtividadeId.Should().Be(atividade.Id);
        solicitacao.CanceladaEm.Should().Be(T0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Vincular_duas_vezes_e_recusado(bool confirmouCancelada)
    {
        var solicitacao = Nova();
        var primeira = AtividadeDa(solicitacao);
        solicitacao.VincularAtividade(primeira, confirmouCancelada);

        DeveRecusarCom(() => solicitacao.VincularAtividade(AtividadeDa(solicitacao), confirmouCancelada), SolicitacaoDoCliente.JaVirouAtividade);
        solicitacao.AtividadeId.Should().Be(primeira.Id);
    }

    [Fact]
    public void Desvincular_volta_ao_estado_anterior()
    {
        var aguardando = Nova();
        aguardando.VincularAtividade(AtividadeDa(aguardando), confirmouCancelada: false);
        var cancelada = Cancelada();
        cancelada.VincularAtividade(AtividadeDa(cancelada), confirmouCancelada: true);

        aguardando.DesvincularAtividade();
        cancelada.DesvincularAtividade();

        aguardando.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        aguardando.AtividadeId.Should().BeNull();
        cancelada.Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);
        cancelada.AtividadeId.Should().BeNull();
        cancelada.CanceladaEm.Should().Be(T0);
    }

    [Fact]
    public void Desvincular_sem_atividade_nao_faz_nada()
    {
        var aguardando = Nova();
        var cancelada = Cancelada();

        aguardando.DesvincularAtividade();
        cancelada.DesvincularAtividade();

        aguardando.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        cancelada.Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);
        cancelada.CanceladaEm.Should().Be(T0);
    }

    [Fact]
    public void Em_atividade_nao_se_altera_nem_cancela()
    {
        var solicitacao = Nova("Original", UrgenciaDaSolicitacao.Baixa);
        solicitacao.VincularAtividade(AtividadeDa(solicitacao), confirmouCancelada: false);

        DeveRecusarCom(() => solicitacao.Alterar("Outra", UrgenciaDaSolicitacao.Alta), SolicitacaoDoCliente.NaoPodeMaisSerAlterada);
        DeveRecusarCom(() => solicitacao.Cancelar(T0), SolicitacaoDoCliente.NaoPodeMaisSerAlterada);
        DeveRecusarCom(solicitacao.RegistrarMudancaNosDocumentos, SolicitacaoDoCliente.NaoPodeMaisSerAlterada);

        solicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);
        solicitacao.Descricao.Should().Be("Original");
        solicitacao.Urgencia.Should().Be(UrgenciaDaSolicitacao.Baixa);
        solicitacao.CanceladaEm.Should().BeNull();
        solicitacao.VersaoDosDocumentos.Should().Be(0);
    }

    [Fact]
    public void As_mensagens_do_dominio_sao_exatamente_estas()
    {
        SolicitacaoDoCliente.NaoPodeMaisSerAlterada.Should().Be("Esta solicitação não pode mais ser alterada.");
        SolicitacaoDoCliente.JaVirouAtividade.Should().Be("Esta solicitação já virou atividade.");
        SolicitacaoDoCliente.CanceladaPeloCliente.Should().Be("Cliente cancelou a solicitação, mesmo assim deseja criar a atividade?");
        SolicitacaoDoCliente.AtividadeDeOutroCliente.Should().Be("A atividade precisa ser do cliente da solicitação.");
    }
}
