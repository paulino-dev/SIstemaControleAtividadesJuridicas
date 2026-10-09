using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Atividades;
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
    public async Task Cliente_baixa_pelo_endereco_o_documento_dele_e_recebe_404_nos_demais()
    {
        var jose = (await _app.ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("José Souza", null, null, new EnderecoEntrada(), null), "111.444.777-35", null, null))).Valor;
        var doCliente = await EnviarAsync(CategoriaDocumento.DocumentoDoCliente, _maria, "do-cliente.pdf");
        var comum = (await _app.ExecutarAsync(_admin, new EnviarDocumentoComum(
            null, "comum.pdf", "application/pdf", Conteudo(), true))).Valor;
        var doOutro = await EnviarAsync(CategoriaDocumento.DocumentoDoCliente, jose, "do-outro.pdf");
        var geral = await EnviarAsync(CategoriaDocumento.DocumentoGeral, _maria, "geral.pdf");
        var contrato = await EnviarAsync(CategoriaDocumento.ContratoHonorarios, _maria, "contrato.pdf");
        var atividade = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Elaborar contestação", "Prazo de 15 dias.", new DateOnly(2090, 6, 20), _maria, null, null, 1));
        atividade.Sucesso.Should().BeTrue(atividade.Mensagem);
        var daAtividade = await _app.ExecutarAsync(_admin, new EnviarDocumentoDaAtividade(
            atividade.Valor, null, "da-atividade.pdf", "application/pdf", Conteudo()));
        daAtividade.Sucesso.Should().BeTrue(daAtividade.Mensagem);
        var cliente = await DaCarlaAsync();

        foreach (var (id, nome) in new[] { (doCliente, "do-cliente.pdf"), (comum, "comum.pdf") })
        {
            var resposta = await cliente.GetAsync($"/documentos/{id}/baixar");

            resposta.StatusCode.Should().Be(HttpStatusCode.OK, nome);
            resposta.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
            (resposta.Content.Headers.ContentDisposition.FileNameStar ?? resposta.Content.Headers.ContentDisposition.FileName)!
                .Trim('"').Should().Be(nome);
        }

        (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().BeEmpty();
        var negados = new List<Guid> { doOutro, geral, contrato, daAtividade.Valor };
        foreach (var id in negados)
            (await cliente.GetAsync($"/documentos/{id}/baixar")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var eventos = await _app.EventosAsync(EventosDeSeguranca.AcessoNegado);
        eventos.Should().OnlyContain(e => e.UsuarioId == _carla && e.Entidade == "Documento");
        eventos.Should().HaveCount(4);
        eventos.Select(e => e.EntidadeId).Should().BeEquivalentTo(negados.Select(id => (Guid?)id));
        eventos.Select(e => (string?)JsonNode.Parse(e.Dados!)!["requisicao"]).Should().OnlyContain(r => r == "BaixarDocumento");
        eventos.Select(e => (string?)JsonNode.Parse(e.Dados!)!["resposta"]).Should().OnlyContain(r => r == "nao_encontrado");
    }

    private static MemoryStream Conteudo() => new(Encoding.UTF8.GetBytes("conteúdo"));

    private async Task<Guid> EnviarAsync(CategoriaDocumento categoria, Guid cliente, string arquivo)
    {
        var enviado = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            categoria, cliente, null, arquivo, "application/pdf", Conteudo()));
        enviado.Sucesso.Should().BeTrue(enviado.Mensagem);
        return enviado.Valor;
    }

    private static string Decodificado(string html) => WebUtility.HtmlDecode(html);

    private Task<int> RegistrosDeConsultaDaCarlaAsync() => _app.EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().CountAsync(r => r.UsuarioId == _carla));

    [Fact]
    public async Task Cliente_ve_o_inicio_com_o_menu_do_portal()
    {
        var cliente = await DaCarlaAsync();

        var html = Decodificado(await cliente.GetStringAsync("/"));

        html.Should().Contain("Carla Cliente").And.Contain("Sair").And.NotContain("Carregando");
        // Lista exata: um item de menu novo fora do bloco da equipe apareceria aqui e quebraria o teste.
        var menu = Regex.Match(html, "<nav[^>]*>.*?</nav>", RegexOptions.Singleline).Value;
        Regex.Matches(menu, "href=\"([^\"]*)\"").Select(m => m.Groups[1].Value)
            .Should().Equal("/", "/meus-documentos", "/documentos-comuns", "/minha-senha");
        html.Should().NotContain("Seu perfil é de");
    }

    [Theory]
    [InlineData("/clientes")]
    [InlineData("/processos")]
    [InlineData("/atividades")]
    [InlineData("/agenda")]
    [InlineData("/areas")]
    [InlineData("/usuarios")]
    [InlineData("/produtores")]
    [InlineData("/perfis")]
    [InlineData("/auditoria")]
    public async Task Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado(string endereco)
    {
        var cliente = await DaCarlaAsync();
        var negadosAntes = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Count;
        var consultasAntes = await RegistrosDeConsultaDaCarlaAsync();

        var html = Decodificado(await cliente.GetStringAsync(endereco));

        html.Should().Contain("Página não encontrada");
        html.Should().NotContain("Maria Silva").And.NotContain("diana@x.com").And.NotContain("Acesso negado")
            .And.NotContain("Carregando");
        (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().HaveCount(negadosAntes);
        (await RegistrosDeConsultaDaCarlaAsync()).Should().Be(consultasAntes);
    }

    [Fact]
    public async Task Cliente_em_rota_inexistente_ve_nao_encontrada()
    {
        var cliente = await DaCarlaAsync();

        var resposta = await cliente.GetAsync("/rota-que-nao-existe");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var html = Decodificado(await resposta.Content.ReadAsStringAsync());
        html.Should().Contain("Página não encontrada").And.NotContain("Carregando").And.NotContain("href=\"/clientes\"");
    }

    [Fact]
    public async Task Cliente_abre_a_troca_de_senha()
    {
        var cliente = await DaCarlaAsync();

        var html = Decodificado(await cliente.GetStringAsync("/minha-senha"));

        html.Should().Contain("Senha atual").And.NotContain("Página não encontrada");
    }

    [Fact]
    public async Task Diretoria_e_producao_continuam_abrindo_as_paginas_internas()
    {
        await _app.CriarPedroAsync();
        var admin = await _app.ClienteAutenticadoAsync();
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var clientes = Decodificado(await admin.GetStringAsync("/clientes"));
        var atividades = Decodificado(await pedro.GetStringAsync("/atividades"));

        clientes.Should().NotContain("Página não encontrada").And.NotContain("Carregando").And.Contain("Maria Silva");
        atividades.Should().NotContain("Página não encontrada").And.NotContain("Carregando");
    }

    [Fact]
    public async Task Cadastro_de_usuarios_mostra_o_cliente_e_o_tipo_cliente()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = Decodificado(await admin.GetStringAsync("/usuarios"));

        html.Should().Contain("Cliente (Cliente)").And.Contain("<th>Cliente</th>");
        var linha = Regex.Matches(html, "<tr[^>]*>.*?</tr>", RegexOptions.Singleline)
            .Select(m => m.Value).Single(l => l.Contains("Carla Cliente"));
        linha.Should().Contain("Maria Silva");
    }

    [Fact]
    public async Task Cadastro_de_usuarios_avisa_que_o_cliente_inativo_esta_sem_acesso()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        Decodificado(await admin.GetStringAsync("/usuarios")).Should().NotContain("sem acesso ao portal");

        (await _app.ExecutarAsync(_admin, new DefinirAtivoCliente(_maria, false))).Sucesso.Should().BeTrue();

        var html = Decodificado(await admin.GetStringAsync("/usuarios"));
        html.Should().Contain("Maria Silva (inativo — sem acesso ao portal)");
    }

    [Fact]
    public async Task Tela_de_perfis_mostra_o_perfil_cliente_e_nao_o_oferece_ao_criar()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = Decodificado(await admin.GetStringAsync("/perfis"));

        var linha = Regex.Matches(html, "<tr[^>]*>.*?</tr>", RegexOptions.Singleline)
            .Select(m => m.Value).Single(l => Regex.IsMatch(l, "<td>Cliente</td>.*<td>Cliente</td>", RegexOptions.Singleline));
        linha.Should().Contain("Renomear");
        var select = Regex.Match(html, "<select[^>]*>.*?</select>", RegexOptions.Singleline).Value;
        select.Should().Contain(">Produção</option>").And.Contain(">Diretoria</option>").And.NotContain("Cliente");
    }
}
