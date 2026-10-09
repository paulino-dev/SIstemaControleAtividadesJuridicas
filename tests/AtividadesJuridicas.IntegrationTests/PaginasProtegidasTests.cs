using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public partial class PaginasProtegidasTests(BancoFixture banco) : IAsyncLifetime
{
    private WebApplicationFactory<Program> _fabrica = null!;
    private string _conexao = null!;

    public async Task InitializeAsync()
    {
        _conexao = banco.NovaConexao();
        // Acrescentada por último, esta fonte vence o appsettings.Development.json.
        var configuracao = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Padrao"] = _conexao,
            ["AdminInicial:Nome"] = "Diana Diretora",
            ["AdminInicial:Email"] = "diana@x.com",
            ["AdminInicial:Senha"] = "senha-segura",
        };
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(configuracao)));

        // Cria um usuário de Produção agindo como a diretora semeada.
        await using var escopo = _fabrica.Services.CreateAsyncScope();
        var sp = escopo.ServiceProvider;
        var diana = await sp.GetRequiredService<IUsuarioRepositorio>().ObterPorEmailAsync(Email.Criar("diana@x.com"), default);
        sp.GetRequiredService<IdentidadeAtual>().DefinirInterno(diana!.Id);
        var producao = (await sp.GetRequiredService<IPerfilRepositorio>().ListarAtivosAsync(default))
            .Single(p => p.Tipo == TipoPerfil.Producao);
        var criado = await sp.GetRequiredService<IManipulador<CriarUsuario, Resultado<Guid>>>().ManipularAsync(
            new CriarUsuario("Pedro Produção", "pedro@x.com", "senha-do-pedro", producao.Id, null), default);
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
    }

    public async Task DisposeAsync()
    {
        await _fabrica.DisposeAsync();
        await banco.RemoverAsync(_conexao);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    private async Task<HttpClient> ClienteAutenticadoAsync(string email, string senha)
    {
        var cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var pagina = await cliente.GetStringAsync("/conta/entrar");
        var resposta = await cliente.PostAsync("/conta/entrar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "entrar",
            ["__RequestVerificationToken"] = TokenRegex().Match(pagina).Groups[1].Value,
            ["Entrada.Email"] = email,
            ["Entrada.Senha"] = senha,
        }));
        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return cliente;
    }

    [Fact]
    public async Task Diretor_ve_a_lista_de_usuarios_sem_nenhum_hash_de_senha()
    {
        var cliente = await ClienteAutenticadoAsync("diana@x.com", "senha-segura");

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/usuarios"));

        html.Should().Contain("Diana Diretora").And.Contain("pedro@x.com").And.Contain("Novo usuário");
        html.Should().NotContain("pbkdf2-sha256");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_dados_de_usuarios()
    {
        var cliente = await ClienteAutenticadoAsync("pedro@x.com", "senha-do-pedro");

        var html = await cliente.GetStringAsync("/usuarios");

        html.Should().Contain("Acesso negado");
        html.Should().NotContain("diana@x.com").And.NotContain("Novo usuário");
    }

    [Fact]
    public async Task Menu_de_producao_nao_mostra_o_link_de_usuarios()
    {
        var cliente = await ClienteAutenticadoAsync("pedro@x.com", "senha-do-pedro");

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/"));

        html.Should().Contain("Pedro Produção").And.NotContain("href=\"/usuarios\"");
    }

    [Fact]
    public async Task Qualquer_usuario_autenticado_abre_a_troca_de_senha()
    {
        var cliente = await ClienteAutenticadoAsync("pedro@x.com", "senha-do-pedro");

        var html = await cliente.GetStringAsync("/minha-senha");

        html.Should().Contain("Senha atual");
    }

    [Fact]
    public async Task Cadastro_de_usuarios_mostra_o_tipo_do_perfil_e_marca_o_perfil_inativo()
    {
        await using (var escopo = _fabrica.Services.CreateAsyncScope())
        {
            var sp = escopo.ServiceProvider;
            var diana = await sp.GetRequiredService<IUsuarioRepositorio>().ObterPorEmailAsync(Email.Criar("diana@x.com"), default);
            sp.GetRequiredService<IdentidadeAtual>().DefinirInterno(diana!.Id);
            var perfil = await sp.GetRequiredService<IManipulador<SalvarPerfil, Resultado<Guid>>>()
                .ManipularAsync(new SalvarPerfil(null, "Temporário", TipoPerfil.Producao), default);
            perfil.Sucesso.Should().BeTrue(perfil.Mensagem);
            var usuario = await sp.GetRequiredService<IManipulador<CriarUsuario, Resultado<Guid>>>().ManipularAsync(
                new CriarUsuario("Tânia Temporária", "tania@x.com", "senha-da-tania", perfil.Valor, null), default);
            usuario.Sucesso.Should().BeTrue(usuario.Mensagem);
            (await sp.GetRequiredService<IManipulador<DefinirAtivoUsuario, Resultado>>()
                .ManipularAsync(new DefinirAtivoUsuario(usuario.Valor, false), default)).Sucesso.Should().BeTrue();
            (await sp.GetRequiredService<IManipulador<DefinirAtivoPerfil, Resultado>>()
                .ManipularAsync(new DefinirAtivoPerfil(perfil.Valor, false), default)).Sucesso.Should().BeTrue();
        }
        var cliente = await ClienteAutenticadoAsync("diana@x.com", "senha-segura");

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/usuarios"));

        html.Should().Contain("Diretor (Diretoria)").And.Contain("Produção (Produção)");
        html.Should().Contain("Temporário (Produção — inativo)");
        // O perfil inativo não é opção para um usuário novo.
        // Opções do formulário: só os perfis ativos, e o selecionado de início é de Produção.
        var select = Regex.Match(html, "<select[^>]*>.*?</select>", RegexOptions.Singleline).Value;
        select.Should().Contain(">Diretor (Diretoria)</option>").And.Contain(">Produção (Produção)</option>");
        select.Should().Contain(">Cliente (Cliente)</option>");
        select.Should().NotContain("Temporário");
        Regex.Matches(select, "<option[^>]*selected[^>]*>[^<]*</option>").Select(m => m.Value).Should()
            .ContainSingle().Which.Should().Contain("Produção (Produção)").And.NotContain("Diretoria");
    }
}
