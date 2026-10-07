using System.Security.Claims;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Infra;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A revogação de sessão dentro de uma tela já aberta (circuito Blazor) passa pelo Despachante,
/// não pelo validador do cookie; estes testes exercitam o Despachante de verdade.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public class DespachanteSessaoTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private sealed class EstadoFixo(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(principal));
    }

    private Despachante CriarDespachante(Guid usuarioId, Guid? versao)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, usuarioId.ToString()) };
        if (versao is { } v) claims.Add(new Claim(IdentidadeHttp.ClaimDaVersao, v.ToString()));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "teste"));

        return new Despachante(
            _app.Services.GetRequiredService<IServiceScopeFactory>(), new EstadoFixo(principal),
            new OrigemDaConexao { Ip = AplicacaoDeTeste.IpDeTeste }, NullLogger<Despachante>.Instance);
    }

    private async Task<(Guid AdminId, Guid DoraId, Guid VersaoDaDora)> CriarDiretoraComSessaoAsync()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var diretoria = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<IPerfilRepositorio>().ListarAtivosAsync(default))
                .Single(p => p.Tipo == TipoPerfil.Diretoria).Id);
        var dora = await _app.ExecutarAsync(adminId,
            new CriarUsuario("Dora", "dora@x.com", "senha-da-dora", diretoria, null));
        dora.Sucesso.Should().BeTrue(dora.Mensagem);

        var sessao = await _app.ExecutarAsync(null, new AutenticarUsuario("dora@x.com", "senha-da-dora"));
        sessao.Sucesso.Should().BeTrue(sessao.Mensagem);
        return (adminId, dora.Valor, sessao.Valor!.VersaoDeSessao);
    }

    [Fact]
    public async Task Tela_aberta_perde_o_acesso_quando_a_senha_do_usuario_e_redefinida()
    {
        var (adminId, doraId, versao) = await CriarDiretoraComSessaoAsync();
        var despachante = CriarDespachante(doraId, versao);

        (await despachante.EnviarAsync(new ObterMeuAcesso())).Should().NotBeNull().And.Match<UsuarioAutenticado>(u => u.Id == doraId);
        (await despachante.EnviarAsync(new ListarPerfisAtivos())).Sucesso.Should().BeTrue();

        (await _app.ExecutarAsync(adminId, new RedefinirSenhaUsuario(doraId, "senha-nova-da-dora")))
            .Sucesso.Should().BeTrue();

        (await despachante.EnviarAsync(new ObterMeuAcesso())).Should().BeNull();
        (await despachante.EnviarAsync(new ListarPerfisAtivos())).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Principal_sem_a_claim_de_versao_nao_tem_acesso()
    {
        var (_, doraId, _) = await CriarDiretoraComSessaoAsync();
        var despachante = CriarDespachante(doraId, versao: null);

        (await despachante.EnviarAsync(new ObterMeuAcesso())).Should().BeNull();
        (await despachante.EnviarAsync(new ListarPerfisAtivos())).Erro.Should().Be(TipoErro.AcessoNegado);
    }
}
