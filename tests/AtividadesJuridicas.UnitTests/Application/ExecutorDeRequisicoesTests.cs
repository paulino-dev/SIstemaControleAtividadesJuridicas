using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.UnitTests.Application;

public sealed class ProvedorFalso : IServiceProvider
{
    private readonly Dictionary<Type, object> _servicos = [];

    public ProvedorFalso Com<T>(T servico) where T : notnull
    {
        _servicos[typeof(T)] = servico;
        return this;
    }

    public object? GetService(Type serviceType) => _servicos.GetValueOrDefault(serviceType);
}

public class ExecutorDeRequisicoesTests
{
    private sealed record Eco(string Texto) : IRequisicao<string>;

    private sealed class EcoManipulador : IManipulador<Eco, string>
    {
        public Task<string> ManipularAsync(Eco requisicao, CancellationToken ct) =>
            Task.FromResult(requisicao.Texto.ToUpperInvariant());
    }

    private sealed record Quebra : IRequisicao<string>;

    private sealed class QuebraManipulador : IManipulador<Quebra, string>
    {
        // Lança de forma síncrona, antes de devolver a Task.
        public Task<string> ManipularAsync(Quebra requisicao, CancellationToken ct) =>
            throw new InvalidOperationException("falha síncrona");
    }

    private static ExecutorDeRequisicoes Criar(ProvedorFalso provedor) => new(provedor, new AuditoriaFalsa());

    [Fact]
    public async Task Executa_o_manipulador_registrado_para_a_requisicao()
    {
        var executor = Criar(new ProvedorFalso().Com<IManipulador<Eco, string>>(new EcoManipulador()));

        (await executor.ExecutarAsync(new Eco("oi"), default)).Should().Be("OI");
    }

    [Fact]
    public async Task Excecao_sincrona_do_manipulador_chega_sem_embrulho_de_reflexao()
    {
        var executor = Criar(new ProvedorFalso().Com<IManipulador<Quebra, string>>(new QuebraManipulador()));

        var acao = () => executor.ExecutarAsync(new Quebra(), default);

        (await acao.Should().ThrowAsync<InvalidOperationException>()).WithMessage("falha síncrona");
    }

    [Fact]
    public async Task Requisicao_sem_manipulador_falha_com_erro_claro()
    {
        var acao = () => Criar(new ProvedorFalso()).ExecutarAsync(new Eco("oi"), default);

        await acao.Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed record Negada : IRequisicao<Resultado>;

    private sealed class NegadaManipulador : IManipulador<Negada, Resultado>
    {
        public Task<Resultado> ManipularAsync(Negada requisicao, CancellationToken ct) =>
            Task.FromResult(Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado."));
    }

    private sealed record Invalida : IRequisicao<Resultado<int>>;

    private sealed class InvalidaManipulador : IManipulador<Invalida, Resultado<int>>
    {
        public Task<Resultado<int>> ManipularAsync(Invalida requisicao, CancellationToken ct) =>
            Task.FromResult(Resultado<int>.Falha(TipoErro.Validacao, "Inválido."));
    }

    [Fact]
    public async Task Acesso_negado_e_registrado_imediatamente_com_o_nome_da_requisicao()
    {
        var auditoria = new AuditoriaFalsa();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<Negada, Resultado>>(new NegadaManipulador()), auditoria);

        var resultado = await executor.ExecutarAsync(new Negada(), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        var evento = auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
        evento.Imediato.Should().BeTrue();
        evento.Dados.Should().BeEquivalentTo(new { requisicao = "Negada" });
    }

    [Fact]
    public async Task Outras_falhas_nao_geram_evento_de_acesso_negado()
    {
        var auditoria = new AuditoriaFalsa();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<Invalida, Resultado<int>>>(new InvalidaManipulador()), auditoria);

        await executor.ExecutarAsync(new Invalida(), default);

        auditoria.Eventos.Should().BeEmpty();
    }

    private sealed record Listagem(bool Falhar) : IRequisicao<Resultado<int>>, IConsultaAuditavel
    {
        public string TipoRecurso => "Coisa";
        public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
        public string? Detalhe => "filtro=x";
    }

    private sealed class ListagemManipulador : IManipulador<Listagem, Resultado<int>>
    {
        public Task<Resultado<int>> ManipularAsync(Listagem requisicao, CancellationToken ct) => Task.FromResult(
            requisicao.Falhar ? Resultado<int>.Falha(TipoErro.AcessoNegado, "Acesso negado.") : Resultado<int>.Ok(3));
    }

    [Fact]
    public async Task Consulta_bem_sucedida_registra_o_acesso()
    {
        var auditoria = new AuditoriaFalsa();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<Listagem, Resultado<int>>>(new ListagemManipulador()), auditoria);

        await executor.ExecutarAsync(new Listagem(Falhar: false), default);

        var acesso = auditoria.Acessos.Should().ContainSingle().Subject;
        acesso.TipoRecurso.Should().Be("Coisa");
        acesso.Acao.Should().Be(AcaoDeAcesso.Listou);
        acesso.Detalhe.Should().Be("filtro=x");
        auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Consulta_recusada_nao_registra_acesso_mas_registra_a_tentativa()
    {
        var auditoria = new AuditoriaFalsa();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<Listagem, Resultado<int>>>(new ListagemManipulador()), auditoria);

        await executor.ExecutarAsync(new Listagem(Falhar: true), default);

        auditoria.Acessos.Should().BeEmpty();
        auditoria.Eventos.Should().ContainSingle().Which.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
    }
}
