using System.Net;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class PerfisWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public async Task InitializeAsync()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var estagio = await _app.ExecutarAsync(admin, new SalvarPerfil(null, "Estagiário", TipoPerfil.Producao));
        estagio.Sucesso.Should().BeTrue(estagio.Mensagem);
        (await _app.ExecutarAsync(admin, new DefinirAtivoPerfil(estagio.Valor, false))).Sucesso.Should().BeTrue();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_os_perfis_ativos_e_inativos_com_as_acoes()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/perfis"));

        html.Should().Contain("Perfis").And.Contain("Diretor").And.Contain("Produção").And.Contain("Estagiário");
        html.Should().Contain("Diretoria").And.Contain("Inativo");
        html.Should().Contain("Novo perfil").And.Contain("Renomear").And.Contain("Inativar").And.Contain("Ativar");
        html.Should().Contain("<th>Usuários ativos</th>");
        html.Should().Contain("<td>Estagiário</td>").And.Contain("<td>Diretor</td>").And.Contain("<td>Produção</td>");
        html.Should().Contain("<td>Inativo</td>").And.Contain("<td>Ativo</td>");
        html.Should().NotContain("Acesso negado");
    }

    [Fact]
    public async Task Producao_nao_ve_a_gestao_de_perfis()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/perfis"));

        html.Should().Contain("Acesso negado.");
        html.Should().NotContain("Estagiário").And.NotContain("Novo perfil").And.NotContain("Renomear");
    }

    [Fact]
    public async Task Menu_mostra_perfis_so_para_a_diretoria()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/perfis\"");
        (await doPedro.GetStringAsync("/")).Should().NotContain("href=\"/perfis\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/perfis");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
