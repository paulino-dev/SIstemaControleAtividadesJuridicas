using System.Runtime.CompilerServices;
using AtividadesJuridicas.Application;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

/// <summary>
/// O portão do executor é o que separa o cliente do que é interno. Estes testes percorrem todas as requisições
/// da aplicação: uma requisição nova nasce fechada ao cliente, e abri-la exige mexer na lista fixada aqui.
/// </summary>
public class PortaoDoClienteTests
{
    private static readonly UsuarioAutenticado Cliente =
        new(Guid.CreateVersion7(), "Carla Cliente", TipoPerfil.Cliente, Guid.CreateVersion7(), Guid.CreateVersion7());

    private static IReadOnlyList<(Type Tipo, Type Resposta)> Requisicoes() =>
        [.. typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequisicao<>))
                .Select(i => (t, i.GetGenericArguments()[0])))];

    private static bool Aberta(Type tipo) => typeof(IRequisicaoAbertaAoCliente).IsAssignableFrom(tipo);

    /// <summary>Executa a requisição sem construí-la: o portão decide pelo tipo, antes de olhar o conteúdo.</summary>
    private static async Task<object?> ExecutarAsync(ExecutorDeRequisicoes executor, Type tipo, Type resposta)
    {
        var requisicao = RuntimeHelpers.GetUninitializedObject(tipo);
        var tarefa = (Task)typeof(ExecutorDeRequisicoes).GetMethod(nameof(ExecutorDeRequisicoes.ExecutarAsync))!
            .MakeGenericMethod(resposta).Invoke(executor, [requisicao, CancellationToken.None])!;
        await tarefa;
        return tarefa.GetType().GetProperty("Result")!.GetValue(tarefa);
    }

    [Fact]
    public void As_requisicoes_abertas_ao_cliente_sao_exatamente_estas()
    {
        Requisicoes().Where(r => Aberta(r.Tipo)).Select(r => r.Tipo.Name).Order().Should().Equal(
            ["AlterarMinhaSenha", "AutenticarUsuario", "BaixarDocumento", "ListarDocumentosComuns", "ListarMeuAndamento", "ListarMeusDocumentos", "ObterMeuAcesso"],
            "abrir uma requisição ao cliente é uma decisão de segurança: acrescente-a aqui e na spec do portal");
    }

    [Fact]
    public void Toda_requisicao_fechada_ao_cliente_responde_com_Resultado()
    {
        Requisicoes().Where(r => !Aberta(r.Tipo) && !typeof(Resultado).IsAssignableFrom(r.Resposta))
            .Select(r => r.Tipo.Name).Should().BeEmpty(
                "o portão só sabe negar com Resultado; uma requisição com outra resposta precisa ser aberta ao cliente ou passar a responder com Resultado");
    }

    [Fact]
    public async Task Toda_requisicao_fechada_e_negada_ao_cliente_sem_chegar_ao_caso_de_uso()
    {
        var auditoria = new AuditoriaFalsa();
        // Nenhum manipulador registrado: se o portão deixasse passar, o executor lançaria ao procurar o caso de uso.
        var executor = new ExecutorDeRequisicoes(new ProvedorFalso(), auditoria, new ContextoDeAcessoFixo(Cliente));
        var fechadas = Requisicoes().Where(r => !Aberta(r.Tipo)).ToList();

        fechadas.Should().HaveCountGreaterThan(40, "a aplicação tem dezenas de casos de uso internos");
        foreach (var (tipo, resposta) in fechadas)
        {
            var obtido = await ExecutarAsync(executor, tipo, resposta);

            obtido.Should().BeAssignableTo<Resultado>($"{tipo.Name} deve ser negada com Resultado")
                .Which.Erro.Should().Be(TipoErro.AcessoNegado, $"{tipo.Name} não está aberta ao cliente");
        }

        auditoria.Eventos.Should().HaveCount(fechadas.Count)
            .And.OnlyContain(e => e.Evento == EventosDeSeguranca.AcessoNegado && e.Imediato);
        auditoria.Acessos.Should().BeEmpty("consulta negada não é consulta feita");
    }

    [Fact]
    public async Task Requisicao_aberta_passa_pelo_portao()
    {
        var executor = new ExecutorDeRequisicoes(new ProvedorFalso(), new AuditoriaFalsa(), new ContextoDeAcessoFixo(Cliente));

        foreach (var (tipo, resposta) in Requisicoes().Where(r => Aberta(r.Tipo)))
        {
            var acao = () => ExecutarAsync(executor, tipo, resposta);

            // Passou pelo portão e parou onde se espera: não há manipulador registrado neste teste.
            // O portão também lança InvalidOperationException ao barrar o que não responde com Resultado: a mensagem distingue.
            (await acao.Should().ThrowAsync<InvalidOperationException>($"{tipo.Name} deve chegar ao caso de uso"))
                .Which.Message.Should().NotContain("não está aberta ao cliente");
        }
    }

    private sealed class ContextoQueConta(UsuarioAutenticado? ator) : IContextoDeAcesso
    {
        public int Consultas { get; private set; }

        public Task<UsuarioAutenticado?> ObterAsync(CancellationToken ct)
        {
            Consultas++;
            return Task.FromResult(ator);
        }
    }

    private sealed class ObterMeuAcessoFalso(UsuarioAutenticado? ator) : IManipulador<ObterMeuAcesso, UsuarioAutenticado?>
    {
        public Task<UsuarioAutenticado?> ManipularAsync(ObterMeuAcesso requisicao, CancellationToken ct) => Task.FromResult(ator);
    }

    [Fact]
    public async Task Requisicao_aberta_nao_faz_o_portao_consultar_o_ator()
    {
        var contexto = new ContextoQueConta(Cliente);
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<ObterMeuAcesso, UsuarioAutenticado?>>(new ObterMeuAcessoFalso(Cliente)),
            new AuditoriaFalsa(), contexto);

        (await executor.ExecutarAsync(new ObterMeuAcesso(), default)).Should().Be(Cliente);

        contexto.Consultas.Should().Be(0);
    }

    private sealed class ListarClientesFalso : IManipulador<ListarClientes, Resultado<PaginaDeClientes>>
    {
        public int Chamadas { get; private set; }

        public Task<Resultado<PaginaDeClientes>> ManipularAsync(ListarClientes requisicao, CancellationToken ct)
        {
            Chamadas++;
            return Task.FromResult(Resultado<PaginaDeClientes>.Ok(new PaginaDeClientes([], 0, 1, 20, 0, 0)));
        }
    }

    public static TheoryData<UsuarioAutenticado?> QuemNaoEBarrado() => new()
    {
        new UsuarioAutenticado(Guid.CreateVersion7(), "Diana", TipoPerfil.Diretoria, Guid.CreateVersion7()),
        new UsuarioAutenticado(Guid.CreateVersion7(), "Pedro", TipoPerfil.Producao, Guid.CreateVersion7()),
        null,
    };

    [Theory]
    [MemberData(nameof(QuemNaoEBarrado))]
    public async Task Diretoria_producao_e_anonimo_nao_sao_barrados_pelo_portao(UsuarioAutenticado? ator)
    {
        var manipulador = new ListarClientesFalso();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<ListarClientes, Resultado<PaginaDeClientes>>>(manipulador),
            new AuditoriaFalsa(), new ContextoDeAcessoFixo(ator));

        (await executor.ExecutarAsync(new ListarClientes(), default)).Sucesso.Should().BeTrue();

        manipulador.Chamadas.Should().Be(1, "o portão só fecha para o perfil Cliente; cada caso de uso mantém a sua regra");
    }

    [Fact]
    public async Task Tipo_de_perfil_desconhecido_nasce_fechado_como_o_cliente()
    {
        var desconhecido = new UsuarioAutenticado(
            Guid.CreateVersion7(), "Futuro", (TipoPerfil)99, Guid.CreateVersion7());
        var manipulador = new ListarClientesFalso();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso()
                .Com<IManipulador<ListarClientes, Resultado<PaginaDeClientes>>>(manipulador)
                .Com<IManipulador<ObterMeuAcesso, UsuarioAutenticado?>>(new ObterMeuAcessoFalso(desconhecido)),
            new AuditoriaFalsa(), new ContextoDeAcessoFixo(desconhecido));

        var fechada = await executor.ExecutarAsync(new ListarClientes(), default);
        var aberta = await executor.ExecutarAsync(new ObterMeuAcesso(), default);

        fechada.Erro.Should().Be(TipoErro.AcessoNegado);
        manipulador.Chamadas.Should().Be(0, "o portão fecha para todo perfil que não seja da equipe");
        aberta.Should().Be(desconhecido);
    }

    [Fact]
    public async Task A_recusa_do_portao_fica_registrada_com_a_requisicao_e_o_alvo()
    {
        var auditoria = new AuditoriaFalsa();
        var manipulador = new ListarClientesFalso();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<ListarClientes, Resultado<PaginaDeClientes>>>(manipulador),
            auditoria, new ContextoDeAcessoFixo(Cliente));
        var pedido = new ListarClientes("Maria", Pagina: 2);

        var resultado = await executor.ExecutarAsync(pedido, default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        resultado.Mensagem.Should().Be("Acesso negado.");
        resultado.Valor.Should().BeNull();
        manipulador.Chamadas.Should().Be(0);
        var evento = auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
        evento.Dados.Should().BeEquivalentTo(new { requisicao = "ListarClientes", recursoId = (Guid?)null, detalhe = pedido.Detalhe });
        auditoria.Acessos.Should().BeEmpty();
    }
}
