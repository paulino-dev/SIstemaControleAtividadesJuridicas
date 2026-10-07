using System.Net;
using System.Text.Json;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class EventosDeSegurancaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco.NovaConexao());

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Producao_tentando_listar_usuarios_gera_evento_de_acesso_negado()
    {
        var pedroId = await _app.CriarPedroAsync();

        // A página nem chega a pedir a lista a quem não é da Diretoria; a tentativa é feita direto no caso de uso.
        (await _app.ExecutarAsync(pedroId, new ListarUsuarios())).Sucesso.Should().BeFalse();

        var evento = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().ContainSingle().Subject;
        evento.UsuarioId.Should().Be(pedroId);
        evento.Operacao.Should().Be(OperacaoAuditada.Evento);
        evento.Dados.Should().Contain("ListarUsuarios");
    }

    [Fact]
    public async Task Sair_gera_evento_de_logout_do_usuario()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var cliente = await _app.ClienteAutenticadoAsync();
        var token = AplicacaoDeTeste.Token(await cliente.GetStringAsync("/"));

        var saida = await cliente.PostAsync("/conta/sair", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        saida.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var evento = (await _app.EventosAsync(EventosDeSeguranca.Logout)).Should().ContainSingle().Subject;
        evento.UsuarioId.Should().Be(adminId);
        evento.EntidadeId.Should().Be(adminId);
        evento.Ip.Should().Be(AplicacaoDeTeste.IpDeTeste);
    }

    [Fact]
    public async Task Evento_anotado_e_gravado_na_mesma_transacao_da_alteracao()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var pedroId = await _app.CriarPedroAsync();

        (await _app.ExecutarAsync(adminId, new DefinirAtivoUsuario(pedroId, false))).Sucesso.Should().BeTrue();

        var evento = (await _app.EventosAsync(EventosDeSeguranca.UsuarioInativado)).Should().ContainSingle().Subject;
        evento.UsuarioId.Should().Be(adminId);
        evento.EntidadeId.Should().Be(pedroId);

        var alteracao = await _app.EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAuditoria
            .AsNoTracking().SingleAsync(r => r.EntidadeId == pedroId && r.Operacao == OperacaoAuditada.Alteracao));
        alteracao.IdCorrelacao.Should().Be(evento.IdCorrelacao, "evento e alteração nascem da mesma requisição");
    }

    [Fact]
    public async Task Registro_imediato_nao_grava_alteracoes_pendentes_do_escopo()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);

        await _app.EmEscopoAsync(adminId, async sp =>
        {
            var usuario = await sp.GetRequiredService<IUsuarioRepositorio>().ObterPorIdAsync(adminId, default);
            usuario!.AlterarDados("Nome Não Salvo", usuario.Email, null);
            await sp.GetRequiredService<IAuditoria>().RegistrarAsync("evento_de_teste", dados: new { motivo = "teste" });
            return 0;
        });

        var evento = (await _app.EventosAsync("evento_de_teste")).Should().ContainSingle().Subject;
        JsonDocument.Parse(evento.Dados!).RootElement.GetProperty("motivo").GetString().Should().Be("teste");

        var nome = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync(u => u.Id == adminId)).Nome);
        nome.Should().Be("Diana Diretora");
    }
}
