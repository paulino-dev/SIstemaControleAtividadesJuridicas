using System.Net;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadesWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin;
    private Guid _pedro;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();

        var contestacao = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Elaborar contestação", "Prazo de 15 dias.", new DateOnly(2020, 1, 15), null, null, null));
        contestacao.Sucesso.Should().BeTrue(contestacao.Mensagem);
        var direcionada = await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            contestacao.Valor, [new ResponsavelEntrada(_pedro, "Redigir a peça")], _admin));
        direcionada.Sucesso.Should().BeTrue(direcionada.Mensagem);

        var interno = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Recurso interno", "Sem responsáveis.", null, null, null, null));
        interno.Sucesso.Should().BeTrue(interno.Mensagem);
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_todas_as_atividades_e_as_acoes_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/atividades"));

        html.Should().Contain("Atividades").And.Contain("Elaborar contestação").And.Contain("Recurso interno")
            .And.Contain("Direcionada").And.Contain("Criada").And.Contain("Atrasada").And.Contain("15/01/2020")
            .And.Contain("Pedro Produção");
        html.Should().Contain("Nova atividade").And.Contain("Abrir");

        // As palavras acima também aparecem nos filtros (<option>); só a tabela tem estas células.
        html.Should().MatchRegex(@"<td>\s*Elaborar contestação\s*</td>");
        html.Should().MatchRegex(@"<td>\s*Direcionada\s*<strong[^>]*>Atrasada</strong>\s*</td>");
        html.Should().MatchRegex(@"<td>\s*Criada\s*</td>");
        html.Should().MatchRegex(@"<td>\s*15/01/2020\s*</td>");
        html.Should().MatchRegex(@"<td>\s*Pedro Produção\s*</td>");
    }

    [Fact]
    public async Task Producao_ve_so_as_suas_e_nenhuma_acao_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/atividades"));

        html.Should().Contain("Elaborar contestação").And.Contain("Abrir");
        html.Should().NotContain("Recurso interno").And.NotContain("Nova atividade")
            .And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Menu_mostra_atividades_para_todos_os_perfis()
    {
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/atividades\"");
        (await doPedro.GetStringAsync("/")).Should().Contain("href=\"/atividades\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/atividades");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }

    [Fact]
    public async Task Listagem_fica_no_registro_de_consultas()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await cliente.GetAsync("/atividades")).EnsureSuccessStatusCode();

        var acessos = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().ToListAsync());
        acessos.Should().Contain(a =>
            a.TipoRecurso == "Atividade" && a.Acao == AcaoDeAcesso.Listou && a.UsuarioId == _pedro);
    }
}
