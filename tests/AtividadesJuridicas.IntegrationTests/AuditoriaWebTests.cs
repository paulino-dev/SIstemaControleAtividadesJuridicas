using System.Net;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AuditoriaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_os_registros_sem_nenhum_hash_de_senha()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/auditoria"));

        html.Should().Contain("Auditoria").And.Contain("login_sucesso").And.Contain("pedro@x.com");
        html.Should().Contain("Diana Diretora");
        html.Should().NotContain("pbkdf2-sha256");
    }

    [Fact]
    public async Task Diretor_ve_os_campos_de_filtro_por_registro_e_por_correlacao_e_os_botoes_das_linhas()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/auditoria"));

        html.Should().Contain("Registro").And.Contain("Gravação");
        html.Should().Contain("Identificador do registro").And.Contain("Identificador de correlação");
        html.Should().Contain("Ver tudo deste registro").And.Contain("Ver o que foi gravado junto");
        html.Should().NotContain("Identificador inválido.");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_registros()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/auditoria"));

        html.Should().Contain("Acesso negado");
        html.Should().NotContain("login_sucesso").And.NotContain("diana@x.com");
    }

    [Fact]
    public async Task Menu_mostra_auditoria_so_para_a_diretoria()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/auditoria\"");
        (await doPedro.GetStringAsync("/")).Should().NotContain("href=\"/auditoria\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/auditoria");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
