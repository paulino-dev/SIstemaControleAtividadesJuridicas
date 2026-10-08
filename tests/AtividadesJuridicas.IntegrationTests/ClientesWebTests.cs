using System.Net;
using AtividadesJuridicas.Application.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ClientesWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public async Task InitializeAsync()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var criado = await _app.ExecutarAsync(adminId, new SalvarClientePessoaFisica(
            null,
            new DadosDoClienteEntrada("Maria Silva", "maria@x.com", null, new EnderecoEntrada(Cidade: "Maceió"), null),
            "529.982.247-25", null, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_a_lista_e_as_acoes_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/clientes"));

        html.Should().Contain("Clientes").And.Contain("Maria Silva").And.Contain("529.982.247-25").And.Contain("Maceió");
        html.Should().Contain("Nova pessoa física").And.Contain("Nova pessoa jurídica").And.Contain("Editar");
    }

    [Fact]
    public async Task Producao_ve_a_lista_mas_nenhuma_acao_de_cadastro()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/clientes"));

        html.Should().Contain("Maria Silva").And.Contain("Ver");
        html.Should().NotContain("Nova pessoa física").And.NotContain("Nova pessoa jurídica")
            .And.NotContain("Editar").And.NotContain("Inativar").And.NotContain("Acesso negado");
    }

    [Fact]
    public async Task Menu_mostra_clientes_para_todos_os_perfis()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/clientes\"");
        (await doPedro.GetStringAsync("/")).Should().Contain("href=\"/clientes\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/clientes");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
