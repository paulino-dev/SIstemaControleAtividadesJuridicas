using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class RegistroDeAcessosWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Abrir_a_lista_de_usuarios_registra_o_acesso_com_o_ip_da_conexao()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var cliente = await _app.ClienteAutenticadoAsync();

        (await cliente.GetAsync("/usuarios")).EnsureSuccessStatusCode();

        var acessos = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().ToListAsync());
        acessos.Should().Contain(a =>
            a.TipoRecurso == "Usuario" && a.Acao == AcaoDeAcesso.Listou && a.UsuarioId == adminId
            && a.Ip == AplicacaoDeTeste.IpDeTeste);
    }
}
