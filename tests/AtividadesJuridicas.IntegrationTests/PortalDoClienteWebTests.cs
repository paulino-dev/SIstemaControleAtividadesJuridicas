using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class PortalDoClienteWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _maria, _carla;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        (_carla, _maria) = await _app.CriarCarlaAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private Task<HttpClient> DaCarlaAsync() =>
        _app.ClienteAutenticadoAsync(UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);

    [Fact]
    public async Task Cliente_entra_pela_tela_de_login()
    {
        var cliente = _app.NovoCliente();

        var resposta = await AplicacaoDeTeste.EntrarAsync(
            cliente, UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cliente_com_sessao_aberta_consegue_entrar_de_novo()
    {
        var cliente = await DaCarlaAsync();

        var resposta = await AplicacaoDeTeste.EntrarAsync(
            cliente, UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect, "o login é aberto ao cliente; o portão não o barra");
    }

    [Fact]
    public async Task Inativar_o_cliente_derruba_a_sessao_aberta_do_usuario_dele()
    {
        var cliente = await DaCarlaAsync();
        (await cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await _app.ExecutarAsync(_admin, new DefinirAtivoCliente(_maria, false))).Sucesso.Should().BeTrue();

        var depois = await cliente.GetAsync("/");
        depois.StatusCode.Should().Be(HttpStatusCode.Redirect);
        depois.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
        var novo = await AplicacaoDeTeste.EntrarAsync(
            _app.NovoCliente(), UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);
        novo.StatusCode.Should().Be(HttpStatusCode.OK);
        WebUtility.HtmlDecode(await novo.Content.ReadAsStringAsync()).Should().Contain("E-mail ou senha inválidos.");
    }

    [Fact]
    public async Task Trocar_o_cliente_derruba_a_sessao_web()
    {
        var jose = (await _app.ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("José Souza", null, null, new EnderecoEntrada(), null), "111.444.777-35", null, null))).Valor;
        var perfilCliente = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking().SingleAsync(p => p.Tipo == TipoPerfil.Cliente)).Id);
        var cliente = await DaCarlaAsync();
        (await cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);

        var troca = await _app.ExecutarAsync(_admin, new EditarUsuario(
            _carla, "Carla Cliente", UsuariosDeClienteDeTeste.EmailDaCarla, perfilCliente, null, jose));
        troca.Sucesso.Should().BeTrue(troca.Mensagem);

        var depois = await cliente.GetAsync("/");
        depois.StatusCode.Should().Be(HttpStatusCode.Redirect);
        depois.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }

    [Fact]
    public async Task Cliente_nao_baixa_documento_interno()
    {
        var enviado = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, _maria, null, "procuracao.pdf", "application/pdf",
            new MemoryStream(Encoding.UTF8.GetBytes("conteúdo"))));
        enviado.Sucesso.Should().BeTrue(enviado.Mensagem);
        var cliente = await DaCarlaAsync();

        var resposta = await cliente.GetAsync($"/documentos/{enviado.Valor}/baixar");

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var negado = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().ContainSingle().Subject;
        negado.UsuarioId.Should().Be(_carla);
        ((string?)JsonNode.Parse(negado.Dados!)!["requisicao"]).Should().Be("BaixarDocumento");
    }
}
