using AtividadesJuridicas.Application.Comum;

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

    private static ExecutorDeRequisicoes Criar(ProvedorFalso provedor) => new(provedor);

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
}
