using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using TelaAndamento = AtividadesJuridicas.Web.Components.Pages.Andamento;
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
            .Should().Equal("/", "/andamento", "/meus-documentos", "/documentos-comuns", "/minha-senha");
        html.Should().NotContain("Seu perfil é de");
    }

    private static string Navegacao(string html, string classe) =>
        Regex.Match(html, $"<nav class=\"{classe}\"[^>]*>.*?</nav>", RegexOptions.Singleline).Value;

    private static string[] Destinos(string trecho) =>
        Regex.Matches(trecho, "href=\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToArray();

    [Fact]
    public async Task Trilho_e_barra_inferior_do_cliente_levam_ao_portal()
    {
        var cliente = await DaCarlaAsync();

        var html = Decodificado(await cliente.GetStringAsync("/"));

        var trilho = Navegacao(html, "aj-trilho");
        var barra = Navegacao(html, "aj-nav-inferior");
        trilho.Should().NotBeEmpty();
        barra.Should().NotBeEmpty();
        Destinos(trilho).Should().Equal("/", "/andamento", "/meus-documentos", "/documentos-comuns", "/minha-senha");
        Destinos(barra).Should().Equal("/", "/andamento", "/meus-documentos");
        // O rótulo é "Documentos", sozinho: "Meus documentos" e "Documentos comuns" não casam.
        barra.Should().MatchRegex(@"</span>\s*Documentos\s*</a>");
    }

    [Fact]
    public async Task Navegacoes_da_equipe_nao_tem_destino_so_do_portal()
    {
        await _app.CriarPedroAsync();
        var admin = await _app.ClienteAutenticadoAsync();
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var htmlAdmin = Decodificado(await admin.GetStringAsync("/"));
        var htmlPedro = Decodificado(await pedro.GetStringAsync("/"));

        foreach (var html in new[] { htmlAdmin, htmlPedro })
        {
            html.Should().NotContain("href=\"/andamento\"").And.NotContain("href=\"/meus-documentos\"");
            Destinos(Navegacao(html, "aj-trilho")).Should().Equal(
                "/", "/atividades", "/agenda", "/clientes", "/processos", "/minha-senha");
            Destinos(Navegacao(html, "aj-nav-inferior")).Should().Equal("/", "/atividades", "/agenda");
        }

        var menuAdmin = Destinos(Regex.Match(htmlAdmin, "<nav[^>]*>.*?</nav>", RegexOptions.Singleline).Value);
        var posicao = Array.IndexOf(menuAdmin, "/documentos-comuns");
        posicao.Should().BeGreaterThan(0);
        menuAdmin[posicao - 1].Should().Be("/processos");
        menuAdmin[posicao + 1].Should().Be("/areas");
        htmlPedro.Should().NotContain("href=\"/documentos-comuns\"");
    }

    private const string Cnj = "1234567-85.2024.8.02.0001";
    private const string Direcionamento = "Direcionamento reservado da equipe";

    /// <summary>
    /// O andamento da Maria, pelos casos de uso: uma atividade em andamento com processo, uma em andamento sem
    /// processo (direcionada ao Pedro) e uma concluída pelo fluxo real.
    /// </summary>
    private async Task<Guid> CriarAndamentoDaMariaAsync()
    {
        var pedro = await _app.CriarPedroAsync();
        var area = await _app.ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        area.Sucesso.Should().BeTrue(area.Mensagem);
        var processo = await _app.ExecutarAsync(_admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(Cnj, "Reclamação trabalhista", area.Valor, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(_maria, PoloProcessual.Ativo)]));
        processo.Sucesso.Should().BeTrue(processo.Mensagem);

        await CriarAtividadeAsync("Contestação trabalhista", "Descrição da contestação.", processo.Valor);
        var semProcesso = await CriarAtividadeAsync("Parecer sem processo", "Descrição do parecer.", null);
        var concluida = await CriarAtividadeAsync("Recurso protocolado", "Descrição do recurso.", null);

        foreach (var id in new[] { semProcesso, concluida })
        {
            var definido = await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
                id, [new ResponsavelEntrada(pedro, Direcionamento)], _admin));
            definido.Sucesso.Should().BeTrue(definido.Mensagem);
        }

        (await _app.ExecutarAsync(pedro, new ConcluirMinhaParte(concluida))).Sucesso.Should().BeTrue();
        var aprovada = await _app.ExecutarAsync(_admin, new AprovarAtividade(concluida));
        aprovada.Sucesso.Should().BeTrue(aprovada.Mensagem);
        return pedro;
    }

    private async Task<Guid> CriarAtividadeAsync(string titulo, string descricao, Guid? processo)
    {
        var criada = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, titulo, descricao, new DateOnly(2090, 6, 15), _maria, processo, null, 1));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        return criada.Valor;
    }

    [Fact]
    public async Task Cliente_ve_o_andamento_com_os_totais_e_a_situacao()
    {
        await CriarAndamentoDaMariaAsync();
        var cliente = await DaCarlaAsync();

        var html = Decodificado(await cliente.GetStringAsync("/andamento"));

        // Os totais estão no subtítulo e nos chips, e não dependem do filtro.
        html.Should().Contain("<p class=\"aj-subtitulo\">2 em andamento · 1 concluída</p>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-chip\" aria-pressed=\"true\"[^>]*>Todas · 3</button>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Em andamento · 2</button>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Concluídas · 1</button>");

        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Atividade</th>").And.Contain("<th scope=\"col\">Concluída em</th>")
            .And.Contain("<td class=\"aj-tabela__principal\">Contestação trabalhista</td>")
            .And.Contain("Parecer sem processo").And.Contain("Recurso protocolado")
            .And.Contain("<td>Trabalhista</td>").And.Contain($"<td class=\"aj-tabela__data\">{Cnj}</td>")
            .And.Contain("<span class=\"aj-etiqueta aj-etiqueta--andamento\">Em andamento</span>")
            .And.Contain("<span class=\"aj-etiqueta aj-etiqueta--concluida\">Concluída</span>")
            .And.Contain("<td class=\"aj-tabela__data\">—</td>")
            .And.Contain("<span class=\"aj-subtitulo\">Página 1 de 1 · 3 atividades</span>");
        // Só o dia da conclusão, sem a hora.
        tabela.Should().MatchRegex("<td class=\"aj-tabela__data\">\\d{2}/\\d{2}/\\d{4}</td>");

        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().Contain("<span class=\"aj-cartao__titulo\">Contestação trabalhista</span>")
            .And.Contain("Parecer sem processo").And.Contain("Recurso protocolado")
            .And.Contain(Cnj).And.Contain("Sem processo")
            .And.Contain("<span class=\"aj-etiqueta aj-etiqueta--andamento\">Em andamento</span>")
            .And.MatchRegex("<span class=\"aj-cartao__data\">Concluída em \\d{2}/\\d{2}/\\d{4}</span>")
            .And.NotContain("aj-paginacao");
        Regex.Matches(cartoes, "class=\"aj-painel aj-painel--cartao\"").Should().HaveCount(3);
        Regex.Matches(cartoes, "aj-cartao__rodape").Should().ContainSingle();

        html.Should().NotContain("Descrição da contestação.").And.NotContain("Descrição do parecer.")
            .And.NotContain("Descrição do recurso.").And.NotContain("Pedro").And.NotContain(Direcionamento)
            .And.NotContain("Carregando").And.NotContain("Página não encontrada").And.NotContain("Nenhuma atividade.");
    }

    [Fact]
    public async Task Cliente_sem_atividades_ve_zeros_e_nenhuma_atividade()
    {
        await CriarAndamentoDaMariaAsync();
        var jose = (await _app.ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("José Souza", null, null, new EnderecoEntrada(), null), "111.444.777-35", null, null))).Valor;
        await _app.EmEscopoAsync(_admin, sp =>
            UsuariosDeClienteDeTeste.GravarAsync(sp, jose, "José Usuário", "jose@x.com"));
        var cliente = await _app.ClienteAutenticadoAsync("jose@x.com", UsuariosDeClienteDeTeste.SenhaDaCarla);

        var html = Decodificado(await cliente.GetStringAsync("/andamento"));

        html.Should().Contain("<p class=\"aj-subtitulo\">0 em andamento · 0 concluídas</p>")
            .And.Contain(">Todas · 0</button>").And.Contain(">Em andamento · 0</button>").And.Contain(">Concluídas · 0</button>")
            .And.Contain("<div class=\"aj-estado-lista\"><strong>Nenhuma atividade.</strong></div>");
        html.Should().NotContain("Contestação trabalhista").And.NotContain("Carregando")
            .And.NotContain("<table").And.NotContain("aj-painel--cartao").And.NotContain("aj-paginacao");
    }

    [Fact]
    public async Task Andamento_com_falha_na_consulta_mostra_o_erro_e_os_chips_sem_numero()
    {
        var despachante = new DespachanteCombinado().Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(
            Resultado<AndamentoDoCliente>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = Decodificado(await TelaDeTeste.RenderizarComAsync<TelaAndamento>(despachante, new()));

        // Falha não é "nenhuma atividade": o erro ocupa o lugar da lista, com a nova tentativa.
        html.Should().MatchRegex("<div class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\"")
            .And.Contain("Falha de teste.").And.Contain(">Tentar de novo</button>")
            .And.Contain(">Todas</button>").And.Contain(">Em andamento</button>").And.Contain(">Concluídas</button>");
        html.Should().NotContain("Nenhuma atividade.").And.NotContain("Carregando").And.NotContain(" · ")
            .And.NotContain("<table").And.NotContain("aj-painel--cartao");
    }

    [Fact]
    public async Task Andamento_com_acesso_encerrado_nao_mostra_chips_nem_lista()
    {
        var despachante = new DespachanteCombinado().Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(
            Resultado<AndamentoDoCliente>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var html = Decodificado(await TelaDeTeste.RenderizarComAsync<TelaAndamento>(despachante, new()));

        html.Should().Contain("Seu acesso foi encerrado. Entre novamente.").And.Contain("aj-aviso--erro");
        html.Should().NotContain("aj-chip").And.NotContain("Tentar de novo").And.NotContain("<table").And.NotContain("Acesso negado.");
    }

    [Fact]
    public async Task Inicio_do_cliente_mostra_os_totais_e_os_atalhos()
    {
        await CriarAndamentoDaMariaAsync();
        var cliente = await DaCarlaAsync();

        var html = Decodificado(await cliente.GetStringAsync("/"));

        var corpo = Regex.Match(html, "<main[^>]*>.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().MatchRegex(@"<span class=""aj-ajuda"">Em andamento</span>\s*<b>2</b>")
            .And.MatchRegex(@"<span class=""aj-ajuda"">Concluídas</span>\s*<b>1</b>");
        // O quadro tem três atalhos; a senha fica no menu e no avatar.
        Regex.Matches(corpo, "href=\"([^\"]*)\"").Select(m => m.Groups[1].Value)
            .Should().Equal("/andamento", "/meus-documentos", "/documentos-comuns");
        corpo.Should().Contain("Olá, Carla Cliente.").And.NotContain("Carregando").And.NotContain("Contestação trabalhista");
    }

    [Fact]
    public async Task Equipe_em_andamento_ve_nao_encontrada_sem_consultar()
    {
        await CriarAndamentoDaMariaAsync();
        var admin = await _app.ClienteAutenticadoAsync();
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);
        var negadosAntes = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Count;
        var consultasAntes = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().CountAsync(r => r.TipoRecurso == "Atividade"));

        foreach (var equipe in new[] { admin, pedro })
        {
            var html = Decodificado(await equipe.GetStringAsync("/andamento"));

            html.Should().Contain("Página não encontrada").And.NotContain("Contestação trabalhista")
                .And.NotContain("em andamento ·").And.NotContain("aj-chip").And.NotContain("Carregando");
        }

        (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().HaveCount(negadosAntes);
        (await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().CountAsync(r => r.TipoRecurso == "Atividade")))
            .Should().Be(consultasAntes);
    }

    [Theory]
    [InlineData("/clientes")]
    [InlineData("/clientes/0199a3f2-7c1e-7000-8000-000000000001")]
    [InlineData("/clientes/novo")]
    [InlineData("/clientes/0199a3f2-7c1e-7000-8000-000000000001/editar")]
    [InlineData("/processos/0199a3f2-7c1e-7000-8000-000000000001")]
    [InlineData("/processos/novo")]
    [InlineData("/processos/0199a3f2-7c1e-7000-8000-000000000001/editar")]
    [InlineData("/processos")]
    [InlineData("/atividades")]
    [InlineData("/agenda")]
    [InlineData("/areas")]
    [InlineData("/usuarios")]
    [InlineData("/usuarios/novo")]
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

        html.Should().Contain("Cliente (Cliente)").And.Contain("<th scope=\"col\">Cliente</th>");
        var linha = Regex.Matches(BlocoDeVisao.De(html, "aj-so-desktop"), "<tr[^>]*>.*?</tr>", RegexOptions.Singleline)
            .Select(m => m.Value).Single(l => l.Contains("Carla Cliente"));
        linha.Should().Contain("Maria Silva");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should().Contain("Cliente (Cliente) · Maria Silva");
    }

    [Fact]
    public async Task Cadastro_de_usuarios_avisa_que_o_cliente_inativo_esta_sem_acesso()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        Decodificado(await admin.GetStringAsync("/usuarios")).Should().NotContain("sem acesso ao portal");

        (await _app.ExecutarAsync(_admin, new DefinirAtivoCliente(_maria, false))).Sucesso.Should().BeTrue();

        var html = Decodificado(await admin.GetStringAsync("/usuarios"));
        html.Should().Contain("Maria Silva (inativo — sem acesso ao portal)");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().Contain("Maria Silva (inativo — sem acesso ao portal)");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should().Contain("Maria Silva (inativo — sem acesso ao portal)");
    }

    [Fact]
    public async Task Tela_de_perfis_mostra_o_perfil_cliente_e_nao_o_oferece_ao_criar()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = Decodificado(await admin.GetStringAsync("/perfis"));

        var linha = Regex.Matches(BlocoDeVisao.De(html, "aj-so-desktop"), "<tr[^>]*>.*?</tr>", RegexOptions.Singleline)
            .Select(m => m.Value)
            .Single(l => Regex.IsMatch(l, "<td class=\"aj-tabela__principal\">Cliente</td>\\s*<td>Cliente</td>"));
        linha.Should().Contain("Renomear");
        var select = Regex.Match(html, "<select[^>]*>.*?</select>", RegexOptions.Singleline).Value;
        select.Should().Contain(">Produção</option>").And.Contain(">Diretoria</option>").And.NotContain("Cliente");
    }
}
