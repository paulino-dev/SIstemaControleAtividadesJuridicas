using System.Net;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class SessaoWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Redefinir_a_senha_derruba_a_sessao_aberta_do_usuario()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var pedroId = await _app.CriarPedroAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);
        (await doPedro.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await _app.ExecutarAsync(adminId, new RedefinirSenhaUsuario(pedroId, "senha-nova-do-pedro")))
            .Sucesso.Should().BeTrue();

        (await doPedro.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await AplicacaoDeTeste.EntrarAsync(_app.NovoCliente(), AplicacaoDeTeste.EmailDoPedro, "senha-nova-do-pedro"))
            .StatusCode.Should().Be(HttpStatusCode.Redirect, "a senha nova abre uma sessão nova");
    }

    [Fact]
    public async Task Inativar_e_reativar_nao_revive_a_sessao_antiga()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var pedroId = await _app.CriarPedroAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await _app.ExecutarAsync(adminId, new DefinirAtivoUsuario(pedroId, false))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(adminId, new DefinirAtivoUsuario(pedroId, true))).Sucesso.Should().BeTrue();

        (await doPedro.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Sessao_de_outro_usuario_nao_e_afetada()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var pedroId = await _app.CriarPedroAsync();
        var doAdmin = await _app.ClienteAutenticadoAsync();

        (await _app.ExecutarAsync(adminId, new RedefinirSenhaUsuario(pedroId, "senha-nova-do-pedro")))
            .Sucesso.Should().BeTrue();

        (await doAdmin.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Pagina_ja_renderizada_de_sessao_revogada_nao_mostra_dados()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var doAdmin = await _app.ClienteAutenticadoAsync();

        // Um segundo diretor redefine a senha do primeiro.
        var diretoria = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<IPerfilRepositorio>().ListarAtivosAsync(default))
                .Single(p => p.Tipo == Domain.Identidade.TipoPerfil.Diretoria).Id);
        var dora = await _app.ExecutarAsync(adminId,
            new CriarUsuario("Dora", "dora@x.com", "senha-da-dora", diretoria, null));
        dora.Sucesso.Should().BeTrue(dora.Mensagem);
        (await _app.ExecutarAsync(dora.Valor, new RedefinirSenhaUsuario(adminId, "senha-nova-da-diana")))
            .Sucesso.Should().BeTrue();

        var resposta = await doAdmin.GetAsync("/usuarios");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Usuario_com_versao_armazenada_zerada_nao_aceita_o_cookie()
    {
        var pedroId = await _app.CriarPedroAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);
        (await doPedro.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Simula uma linha anterior à migração.
        await _app.EmEscopoAsync(null, async sp =>
            await sp.GetRequiredService<AppDbContext>().Usuarios.Where(u => u.Id == pedroId)
                .ExecuteUpdateAsync(c => c.SetProperty(u => u.VersaoDeSessao, Guid.Empty)));

        (await doPedro.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }
}
