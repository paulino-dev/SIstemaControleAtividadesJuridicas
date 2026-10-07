using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ConcorrenciaDeLoginTests(BancoFixture banco) : TesteComBanco(banco)
{
    private static readonly DateTime Agora = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    private Task<TentativaDeLogin> ReservarAsync(Guid id, DateTime agora) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<IUsuarioRepositorio>().ReservarTentativaDeLoginAsync(id, agora, default));

    private Task<Usuario> LerAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync(u => u.Id == id));

    private Task<Application.Comum.Resultado<SessaoIniciada>> EntrarAsync(string senha) =>
        ExecutarAsync(null, new AutenticarUsuario(Admin.Email, senha));

    [Fact]
    public async Task Seis_reservas_seguidas_admitem_quatro_bloqueiam_na_quinta_e_recusam_a_sexta()
    {
        var id = await IdDoAdminAsync();

        var resultados = new List<TentativaDeLogin>();
        for (var i = 0; i < 6; i++) resultados.Add(await ReservarAsync(id, Agora));

        resultados.Should().Equal(
            TentativaDeLogin.Admitida, TentativaDeLogin.Admitida, TentativaDeLogin.Admitida,
            TentativaDeLogin.Admitida, TentativaDeLogin.AdmitidaEBloqueou, TentativaDeLogin.Recusada);
        var usuario = await LerAsync(id);
        usuario.FalhasDeLogin.Should().Be(0);
        usuario.BloqueadoAte.Should().Be(Agora + Usuario.DuracaoDoBloqueio);
    }

    [Fact]
    public async Task Vinte_reservas_simultaneas_admitem_so_cinco()
    {
        var id = await IdDoAdminAsync();

        var resultados = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => ReservarAsync(id, Agora)));

        resultados.Count(r => r == TentativaDeLogin.Admitida).Should().Be(4);
        resultados.Count(r => r == TentativaDeLogin.AdmitidaEBloqueou).Should().Be(1);
        resultados.Count(r => r == TentativaDeLogin.Recusada).Should().Be(15);
    }

    [Fact]
    public async Task Senha_correta_durante_o_bloqueio_e_recusada_e_o_bloqueio_continua()
    {
        var id = await IdDoAdminAsync();
        for (var i = 0; i < 5; i++) await EntrarAsync("senha-errada");

        var resultado = await EntrarAsync(Admin.Senha);

        resultado.Sucesso.Should().BeFalse();
        resultado.Mensagem.Should().Contain("Muitas tentativas de login.");
        (await LerAsync(id)).BloqueadoAte.Should().NotBeNull();
    }

    [Fact]
    public async Task Vinte_senhas_erradas_simultaneas_geram_so_cinco_tentativas_admitidas()
    {
        var id = await IdDoAdminAsync();

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => EntrarAsync("senha-errada")));

        (await LerAsync(id)).BloqueadoAte.Should().NotBeNull();
        var eventos = (await AuditoriaAsync())
            .Where(r => r.EntidadeId == id &&
                        (r.Evento == EventosDeSeguranca.LoginFalha || r.Evento == EventosDeSeguranca.LoginBloqueado))
            .ToList();
        eventos.Should().HaveCount(20);
        eventos.Count(r => r.Evento == EventosDeSeguranca.LoginBloqueado).Should().Be(1);
        eventos.Count(r => r.Evento == EventosDeSeguranca.LoginFalha && r.Dados is null).Should().Be(4);
        eventos.Count(r => r.Evento == EventosDeSeguranca.LoginFalha && r.Dados != null && r.Dados.Contains("bloqueado"))
            .Should().Be(15);
    }

    [Fact]
    public async Task Login_correto_depois_de_falhas_zera_o_estado_sem_carimbar()
    {
        var id = await IdDoAdminAsync();
        await EntrarAsync("senha-errada");
        await EntrarAsync("senha-errada");

        (await EntrarAsync(Admin.Senha)).Sucesso.Should().BeTrue();

        var usuario = await LerAsync(id);
        usuario.FalhasDeLogin.Should().Be(0);
        usuario.BloqueadoAte.Should().BeNull();
        usuario.UltimoAcessoEm.Should().NotBeNull();
        usuario.AtualizadoEm.Should().BeNull();
    }

    private async Task<Guid> BloquearEmAgoraAsync()
    {
        var id = await IdDoAdminAsync();
        for (var i = 0; i < 5; i++) await ReservarAsync(id, Agora);
        (await LerAsync(id)).BloqueadoAte.Should().Be(Agora + Usuario.DuracaoDoBloqueio);
        return id;
    }

    [Fact]
    public async Task Reserva_no_instante_do_fim_do_bloqueio_e_admitida_e_zera_o_bloqueio_contando_uma_falha()
    {
        var id = await BloquearEmAgoraAsync();

        var tentativa = await ReservarAsync(id, Agora + Usuario.DuracaoDoBloqueio);

        tentativa.Should().Be(TentativaDeLogin.Admitida);
        var usuario = await LerAsync(id);
        usuario.FalhasDeLogin.Should().Be(1);
        usuario.BloqueadoAte.Should().BeNull();
    }

    [Fact]
    public async Task Reserva_um_segundo_antes_do_fim_do_bloqueio_e_recusada()
    {
        var id = await BloquearEmAgoraAsync();

        var tentativa = await ReservarAsync(id, Agora + Usuario.DuracaoDoBloqueio - TimeSpan.FromSeconds(1));

        tentativa.Should().Be(TentativaDeLogin.Recusada);
        (await LerAsync(id)).BloqueadoAte.Should().Be(Agora + Usuario.DuracaoDoBloqueio);
    }
}
