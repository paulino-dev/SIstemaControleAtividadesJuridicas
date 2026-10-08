using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class PendenciasEAuditoriaPorRegistroTests
{
    private readonly Cenario _c = new();
    private readonly PendenciasDoUsuarioFalsa _pendencias = new();

    private Task<Resultado<PendenciasDoUsuario>> Obter(Usuario? ator, Guid id) =>
        new ObterPendenciasDoUsuarioManipulador(_c.Como(ator), _c.Usuarios, _pendencias, _c.Relogio)
            .ManipularAsync(new ObterPendenciasDoUsuario(id), default);

    [Fact]
    public async Task Producao_e_anonimo_nao_obtem_pendencias()
    {
        var pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        (await Obter(pedro, pedro.Id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Obter(null, pedro.Id)).Erro.Should().Be(TipoErro.AcessoNegado);
        _pendencias.Chamadas.Should().BeEmpty();
    }

    [Fact]
    public async Task Usuario_inexistente_nao_e_encontrado()
    {
        var resultado = await Obter(_c.Diretor, Guid.CreateVersion7());

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        _pendencias.Chamadas.Should().BeEmpty();
    }

    [Fact]
    public async Task Diretor_recebe_o_que_a_porta_contou_com_o_agora_do_relogio()
    {
        var pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _pendencias.Resposta = new PendenciasDoUsuario(3, 1, 2);

        var resultado = await Obter(_c.Diretor, pedro.Id);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        resultado.Valor.Should().Be(new PendenciasDoUsuario(3, 1, 2));
        _pendencias.Chamadas.Should().ContainSingle().Which.Should().Be((pedro.Id, _c.Relogio.AgoraUtc));
    }

    [Fact]
    public void Nao_e_consulta_auditavel()
    {
        new ObterPendenciasDoUsuario(Guid.CreateVersion7()).Should().NotBeAssignableTo<IConsultaAuditavel>();
    }

    [Fact]
    public void Detalhe_da_consulta_de_auditoria_inclui_os_filtros_novos()
    {
        var registro = Guid.CreateVersion7();
        var correlacao = Guid.CreateVersion7();

        var detalhe = new ConsultarAuditoria(new FiltroDeAuditoria(EntidadeId: registro, IdCorrelacao: correlacao)).Detalhe;

        detalhe.Should().Contain(registro.ToString()).And.Contain(correlacao.ToString());
        detalhe.Should().Contain("EntidadeId").And.Contain("IdCorrelacao");
    }

    private sealed class PendenciasDoUsuarioFalsa : IPendenciasDoUsuario
    {
        public PendenciasDoUsuario Resposta { get; set; } = new(0, 0, 0);
        public List<(Guid UsuarioId, DateTime AgoraUtc)> Chamadas { get; } = [];

        public Task<PendenciasDoUsuario> ContarAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct)
        {
            Chamadas.Add((usuarioId, agoraUtc));
            return Task.FromResult(Resposta);
        }
    }
}
