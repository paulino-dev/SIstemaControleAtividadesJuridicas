using System.Net;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class BloqueioDeLoginTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco.NovaConexao());

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> TentarAsync(string senha)
    {
        var resposta = await AplicacaoDeTeste.EntrarAsync(_app.NovoCliente(), AplicacaoDeTeste.EmailDoAdmin, senha);
        return resposta.StatusCode == HttpStatusCode.Redirect
            ? "entrou"
            : WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cinco_senhas_erradas_bloqueiam_ate_a_senha_certa()
    {
        for (var i = 0; i < 4; i++)
            (await TentarAsync("senha-errada")).Should().Contain("E-mail ou senha inválidos.");

        (await TentarAsync("senha-errada")).Should().Contain("Muitas tentativas de login.");
        (await TentarAsync(AplicacaoDeTeste.SenhaDoAdmin)).Should().Contain("Muitas tentativas de login.");

        (await _app.EventosAsync(EventosDeSeguranca.LoginBloqueado)).Should().ContainSingle();
        (await _app.EventosAsync(EventosDeSeguranca.LoginSucesso)).Should().BeEmpty();
    }

    [Fact]
    public async Task Falhas_de_login_nao_mexem_nos_carimbos_nem_geram_registro_de_alteracao()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);

        await TentarAsync("senha-errada");
        await TentarAsync("senha-errada");

        var (usuario, alteracoes) = await _app.EmEscopoAsync(null, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            return (await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == adminId),
                    await db.RegistrosAuditoria.CountAsync(r =>
                        r.EntidadeId == adminId && r.Operacao == OperacaoAuditada.Alteracao));
        });

        usuario.FalhasDeLogin.Should().Be(2);
        usuario.AtualizadoEm.Should().BeNull();
        alteracoes.Should().Be(0);
        (await _app.EventosAsync(EventosDeSeguranca.LoginFalha)).Should().HaveCount(2)
            .And.OnlyContain(e => e.EntidadeId == adminId);
    }

    [Fact]
    public async Task Login_correto_gera_evento_de_sucesso_e_zera_as_falhas()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        await TentarAsync("senha-errada");

        (await TentarAsync(AplicacaoDeTeste.SenhaDoAdmin)).Should().Be("entrou");

        var usuario = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync(u => u.Id == adminId));
        usuario.FalhasDeLogin.Should().Be(0);
        usuario.UltimoAcessoEm.Should().NotBeNull();
        (await _app.EventosAsync(EventosDeSeguranca.LoginSucesso)).Should().ContainSingle()
            .Which.EntidadeId.Should().Be(adminId);
    }

    [Fact]
    public async Task Email_inexistente_nunca_bloqueia()
    {
        for (var i = 0; i < 7; i++)
        {
            var resposta = await AplicacaoDeTeste.EntrarAsync(_app.NovoCliente(), "ninguem@x.com", "qualquer-senha");
            WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync())
                .Should().Contain("E-mail ou senha inválidos.").And.NotContain("Muitas tentativas");
        }

        (await _app.EventosAsync(EventosDeSeguranca.LoginBloqueado)).Should().BeEmpty();
    }
}
