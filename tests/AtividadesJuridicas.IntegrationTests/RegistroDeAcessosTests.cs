using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class RegistroDeAcessosTests(BancoFixture banco) : TesteComBanco(banco)
{
    private Task<List<RegistroAcesso>> AcessosAsync() => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().OrderBy(r => r.Id).ToListAsync());

    [Fact]
    public async Task Listar_usuarios_registra_quem_listou_o_que_e_de_onde()
    {
        var adminId = await IdDoAdminAsync();

        (await ExecutarAsync(adminId, new ListarUsuarios())).Sucesso.Should().BeTrue();

        var acesso = (await AcessosAsync()).Should().ContainSingle().Subject;
        acesso.UsuarioId.Should().Be(adminId);
        acesso.TipoRecurso.Should().Be("Usuario");
        acesso.Acao.Should().Be(AcaoDeAcesso.Listou);
        acesso.RecursoId.Should().BeNull();
        acesso.Ip.Should().Be("127.0.0.9");
        acesso.DataHora.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task Consulta_recusada_nao_gera_registro_de_acesso()
    {
        var adminId = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        var pedro = await ExecutarAsync(adminId, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null));

        (await ExecutarAsync(pedro.Valor, new ListarUsuarios())).Sucesso.Should().BeFalse();

        (await AcessosAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Saber_quem_sou_nao_gera_registro_de_acesso()
    {
        var adminId = await IdDoAdminAsync();

        (await ExecutarAsync(adminId, new ObterMeuAcesso())).Should().NotBeNull();

        (await AcessosAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Comandos_nao_geram_registro_de_acesso()
    {
        var adminId = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);

        await ExecutarAsync(adminId, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null));

        (await AcessosAsync()).Should().BeEmpty();
    }
}
