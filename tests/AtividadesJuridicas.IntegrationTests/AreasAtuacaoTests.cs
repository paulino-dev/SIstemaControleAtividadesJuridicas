using System.Net;
using System.Text.Json.Nodes;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AreasAtuacaoTests(BancoFixture banco) : TesteComBanco(banco)
{
    [Fact]
    public async Task Criar_listar_e_auditar()
    {
        var adminId = await IdDoAdminAsync();

        var criada = await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Trabalhista"));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);

        var lista = await ExecutarAsync(adminId, new ListarAreasAtuacao());
        lista.Valor.Should().ContainSingle().Which.Should().Be(new AreaAtuacaoResumo(criada.Valor, "Trabalhista", true));

        var registro = (await AuditoriaAsync()).Single(r => r.EntidadeId == criada.Valor);
        registro.Entidade.Should().Be("AreaAtuacao");
        registro.Operacao.Should().Be(OperacaoAuditada.Criacao);
        registro.UsuarioId.Should().Be(adminId);
        registro.Dados.Should().Contain("Trabalhista");
    }

    [Fact]
    public async Task Nome_duplicado_e_barrado_tambem_pelo_banco()
    {
        var adminId = await IdDoAdminAsync();
        await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Cível"));

        (await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "cível"))).Erro.Should().Be(TipoErro.Conflito);

        var direto = () => EmEscopoAsync(adminId, async sp =>
        {
            sp.GetRequiredService<AppDbContext>().Add(AreaAtuacao.Criar("Cível"));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });
        await direto.Should().ThrowAsync<ExcecaoDeConflito>();
    }

    [Theory]
    [InlineData("CÍVEL")]
    [InlineData("  cível ")]
    public async Task Nome_repetido_com_acento_em_maiuscula_e_conflito_no_banco_real(string nome)
    {
        var adminId = await IdDoAdminAsync();
        await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Cível"));

        (await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, nome))).Erro.Should().Be(TipoErro.Conflito);
    }

    [Fact]
    public async Task Renomear_registra_de_e_para()
    {
        var adminId = await IdDoAdminAsync();
        var criada = await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Civel"));

        (await ExecutarAsync(adminId, new SalvarAreaAtuacao(criada.Valor, "Cível"))).Sucesso.Should().BeTrue();

        var alteracao = (await AuditoriaAsync())
            .Single(r => r.EntidadeId == criada.Valor && r.Operacao == OperacaoAuditada.Alteracao);
        // A coluna é jsonb: o PostgreSQL normaliza os espaços, então compara-se o JSON, não o texto.
        JsonNode.DeepEquals(JsonNode.Parse(alteracao.Dados!), JsonNode.Parse("""{"Nome":{"de":"Civel","para":"Cível"}}"""))
            .Should().BeTrue($"os dados registrados foram {alteracao.Dados}");
    }
}

[Collection(ColecaoBanco.Nome)]
public class AreasWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_as_areas_e_o_formulario()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        await _app.ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Trabalhista"));
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/areas"));

        html.Should().Contain("Áreas de atuação").And.Contain("Trabalhista").And.Contain("Nova área");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_o_link_no_menu()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        WebUtility.HtmlDecode(await cliente.GetStringAsync("/areas"))
            .Should().Contain("Acesso negado").And.NotContain("Nova área");
        (await cliente.GetStringAsync("/")).Should().NotContain("href=\"/areas\"");
    }
}
