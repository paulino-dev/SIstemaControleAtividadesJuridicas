using System.Net;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ProcessosWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public async Task InitializeAsync()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = await _app.ExecutarAsync(admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        var maria = await _app.ExecutarAsync(admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null));
        var criado = await _app.ExecutarAsync(admin, new SalvarProcesso(
            null,
            new DadosDoProcessoEntrada("1234567-85.2024.8.02.0001", "Reclamação trabalhista", area.Valor, null, "Maceió",
                "Empresa X", null, null, null),
            [new ClienteDoProcessoEntrada(maria.Valor, PoloProcessual.Ativo)]));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_a_lista_e_as_acoes_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/processos"));

        html.Should().Contain("Processos").And.Contain("Reclamação trabalhista")
            .And.Contain("1234567-85.2024.8.02.0001").And.Contain("Trabalhista").And.Contain("Maria Silva");
        html.Should().Contain("Novo processo").And.Contain("Editar");
    }

    [Fact]
    public async Task Producao_ve_a_lista_mas_nenhuma_acao_de_cadastro()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/processos"));

        html.Should().Contain("Reclamação trabalhista").And.Contain("Ver");
        html.Should().NotContain("Novo processo").And.NotContain("Editar").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Menu_mostra_processos_para_todos_os_perfis()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/processos\"");
        (await doPedro.GetStringAsync("/")).Should().Contain("href=\"/processos\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/processos");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
