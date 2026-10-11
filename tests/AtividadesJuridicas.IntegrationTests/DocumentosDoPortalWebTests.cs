using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Components.Documentos;
using TelaDocumentosComuns = AtividadesJuridicas.Web.Components.Pages.DocumentosComuns;
using TelaMeusDocumentos = AtividadesJuridicas.Web.Components.Pages.MeusDocumentos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>As telas de documentos do portal, no HTML pré-renderizado.</summary>
[Collection(ColecaoBanco.Nome)]
public class DocumentosDoPortalWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _maria, _carla, _doCliente, _comum;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        (_carla, _maria) = await _app.CriarCarlaAsync();
        await _app.CriarPedroAsync();

        var doCliente = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            Domain.Documentos.CategoriaDocumento.DocumentoDoCliente, _maria, null, "do-cliente.pdf", "application/pdf", Conteudo()));
        doCliente.Sucesso.Should().BeTrue(doCliente.Mensagem);
        _doCliente = doCliente.Valor;

        var comum = await _app.ExecutarAsync(_admin, new EnviarDocumentoComum(
            null, "comum.pdf", "application/pdf", Conteudo(), true));
        comum.Sucesso.Should().BeTrue(comum.Mensagem);
        _comum = comum.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private static MemoryStream Conteudo() => new(Encoding.UTF8.GetBytes("conteúdo"));

    private static string Decodificado(string html) => WebUtility.HtmlDecode(html);

    private Task<HttpClient> DaCarlaAsync() =>
        _app.ClienteAutenticadoAsync(UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);

    private Task<HttpClient> DoPedroAsync() =>
        _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

    private Task<int> RegistrosDeConsultaAsync() => _app.EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().CountAsync());

    private static string BarraDaTela(string html) =>
        Regex.Match(html, "<header class=\"aj-barra-superior\">.*?</header>", RegexOptions.Singleline).Value;

    // O aviso que a página põe no lugar da lista quando a consulta responde acesso negado.
    private const string AvisoDeAcessoEncerrado =
        "<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">\\s*<svg.*?</svg>\\s*<span>Seu acesso foi encerrado\\. Entre novamente\\.</span></p>";

    private static Resultado<Pagina<DocumentoDoPortal>> AcessoNegado() =>
        Resultado<Pagina<DocumentoDoPortal>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

    [Fact]
    public async Task Diretor_ve_documentos_comuns_com_o_envio_e_sem_o_aviso_aberto()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = Decodificado(await admin.GetStringAsync("/documentos-comuns"));

        html.Should().Contain("<h1 class=\"aj-titulo\">Documentos comuns</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Visíveis para todos os clientes do escritório.</p>")
            .And.Contain("<span class=\"aj-subtitulo\">1 documento</span>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Enviar documento</button>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-fab\" aria-label=\"Enviar documento\"");

        // Controle positivo do que o cliente não vê: a Diretoria vê a coluna do autor, o nome de quem enviou e as ações.
        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Por</th>").And.Contain("<td>Diana Diretora</td>")
            .And.MatchRegex($"<td class=\"aj-tabela__principal\"><a href=\"/documentos/{_comum}/baixar\"[^>]*download[^>]*>comum\\.pdf</a></td>")
            .And.MatchRegex("<td class=\"aj-tabela__acao aj-tabela__acao--varias\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Renomear</button>\\s*<button type=\"button\" class=\"aj-botao aj-botao--perigo aj-botao--menor\"[^>]*>Excluir</button>");

        var linhas = BlocoDeVisao.De(html, "aj-tablet-mobile");
        linhas.Should().Contain("<span class=\"aj-linha__titulo\">comum.pdf</span>")
            .And.MatchRegex("<span class=\"aj-ajuda\">[^<]+ · \\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2} · Diana Diretora</span>")
            .And.Contain("aria-label=\"Ações de comum.pdf\"");
        // O menu nasce fechado, com as três ações na ordem: baixar, renomear e, por último, excluir.
        Regex.Matches(linhas, "class=\"aj-menu-acoes__item[^\"]*\"[^>]*>([^<]+)<").Select(m => m.Groups[1].Value)
            .Should().Equal("Baixar", "Renomear", "Excluir");
        linhas.Should().MatchRegex("<div class=\"aj-menu-acoes__lista\"[^>]*\\shidden")
            .And.Contain("class=\"aj-menu-acoes__item aj-menu-acoes__item--perigo\"");

        // Nada aberto de início: nem confirmação, nem painel de envio.
        html.Should().NotContain("Confirmar envio").And.NotContain("Arquivo (até").And.NotContain("Confirmar exclusão")
            .And.NotContain("Página não encontrada").And.NotContain("Carregando");
        html.Should().NotContain("do-cliente.pdf");
    }

    [Fact]
    public async Task Cliente_ve_documentos_comuns_e_meus_documentos_so_com_download()
    {
        var cliente = await DaCarlaAsync();

        foreach (var (endereco, nome, id, subtitulo) in new[]
                 {
                     ("/documentos-comuns", "comum.pdf", _comum, "Modelos e orientações do escritório para todos os clientes."),
                     ("/meus-documentos", "do-cliente.pdf", _doCliente, "1 documento"),
                 })
        {
            var html = Decodificado(await cliente.GetStringAsync(endereco));

            html.Should().Contain($"<p class=\"aj-subtitulo\">{subtitulo}</p>", endereco)
                // No mobile o subtítulo é sempre a contagem.
                .And.Contain("<span class=\"aj-subtitulo\">1 documento</span>", endereco);

            var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
            tabela.Should().MatchRegex(
                    $"<td class=\"aj-tabela__principal\"><a href=\"/documentos/{id}/baixar\"[^>]*download[^>]*>{Regex.Escape(nome)}</a></td>", endereco)
                .And.MatchRegex(
                    $"<a href=\"/documentos/{id}/baixar\"[^>]*class=\"aj-botao aj-botao--secundario aj-botao--com-icone aj-botao--compacto\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Baixar</a>", endereco)
                .And.Contain("<span class=\"aj-so-leitor\">Ações</span>", endereco)
                .And.NotContain("aj-paginacao", endereco);

            var linhas = BlocoDeVisao.De(html, "aj-tablet-mobile");
            linhas.Should().Contain($"<span class=\"aj-linha__titulo\">{nome}</span>", endereco)
                .And.MatchRegex(
                    $"<a href=\"/documentos/{id}/baixar\"[^>]*class=\"aj-botao aj-botao--secundario aj-botao--icone\"[^>]*aria-label=\"Baixar {Regex.Escape(nome)}\"", endereco)
                .And.MatchRegex("<span class=\"aj-ajuda\">[^<]+ · \\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}</span>", endereco);

            // O cliente não gerencia nem vê quem enviou. A Diretoria vê: o teste dela é o controle positivo.
            html.Should().NotContain("Enviar documento").And.NotContain("Renomear").And.NotContain("Excluir")
                .And.NotContain("Ações de").And.NotContain(">Por</th>").And.NotContain("Diana Diretora")
                .And.NotContain("Carregando").And.NotContain("Página não encontrada");
        }
    }

    [Fact]
    public async Task Cliente_sem_documentos_ve_nenhum_documento()
    {
        var jose = (await _app.ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("José Souza", null, null, new EnderecoEntrada(), null), "111.444.777-35", null, null))).Valor;
        await _app.EmEscopoAsync(_admin, sp =>
            UsuariosDeClienteDeTeste.GravarAsync(sp, jose, "José Usuário", "jose@x.com"));
        var cliente = await _app.ClienteAutenticadoAsync("jose@x.com", UsuariosDeClienteDeTeste.SenhaDaCarla);

        var html = Decodificado(await cliente.GetStringAsync("/meus-documentos"));

        html.Should().Contain("<div class=\"aj-estado-lista\"><strong>Nenhum documento.</strong></div>")
            .And.Contain("<h1 class=\"aj-titulo\">Meus documentos</h1>")
            .And.NotContain("do-cliente.pdf").And.NotContain("Carregando")
            .And.NotContain("<table").And.NotContain("aj-linha").And.NotContain("documentos</p>");
    }

    [Fact]
    public async Task Lista_de_documentos_com_falha_na_consulta_mostra_o_erro_e_nao_a_lista_vazia()
    {
        Func<int, Task<Resultado<Pagina<DocumentoDoPortal>>>> listar = _ => Task.FromResult(
            Resultado<Pagina<DocumentoDoPortal>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = Decodificado(await TelaDeTeste.RenderizarComAsync<DocumentosParaBaixar>(
            new DespachanteCombinado(), new() { ["Titulo"] = "Meus documentos", ["Listar"] = listar }));

        html.Should().Contain("<h1 class=\"aj-titulo\">Meus documentos</h1>")
            .And.MatchRegex("<div class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\"")
            .And.Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        html.Should().NotContain("Nenhum documento.").And.NotContain("<table").And.NotContain("aj-linha")
            .And.NotContain("aj-subtitulo");
    }

    // Sem contagem (aqui, por falha na consulta), a barra do mobile não cai na frase do desktop, que ela cortaria.
    [Fact]
    public async Task Lista_de_documentos_com_frase_e_falha_na_consulta_fica_sem_subtitulo_na_barra_do_mobile()
    {
        const string frase = "Modelos e orientações do escritório para todos os clientes.";
        Func<int, Task<Resultado<Pagina<DocumentoDoPortal>>>> listar = _ => Task.FromResult(
            Resultado<Pagina<DocumentoDoPortal>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = Decodificado(await TelaDeTeste.RenderizarComAsync<ComBarraSuperior<DocumentosParaBaixar>>(
            new DespachanteCombinado(), new() { ["Titulo"] = "Documentos comuns", ["Subtitulo"] = frase, ["Listar"] = listar }));

        html.Should().Contain($"<p class=\"aj-subtitulo\">{frase}</p>").And.Contain("Falha de teste.");
        BarraDaTela(html).Should().Contain("<h1 class=\"aj-titulo\">Documentos comuns</h1>").And.NotContain("aj-subtitulo");
    }

    [Fact]
    public async Task Documentos_comuns_da_diretoria_com_falha_na_consulta_fica_sem_subtitulo_na_barra_do_mobile()
    {
        var despachante = new DespachanteCombinado().Responde<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(
            Resultado<Pagina<DocumentoDoPortal>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = Decodificado(await TelaDeTeste.RenderizarComAsync<ComBarraSuperior<DocumentosComunsDaDiretoria>>(despachante, new()));

        html.Should().Contain("<p class=\"aj-subtitulo\">Visíveis para todos os clientes do escritório.</p>")
            .And.Contain("Falha de teste.");
        BarraDaTela(html).Should().Contain("<h1 class=\"aj-titulo\">Documentos comuns</h1>").And.NotContain("aj-subtitulo");
    }

    [Fact]
    public async Task Meus_documentos_com_acesso_encerrado_mostra_o_aviso_no_lugar_da_lista()
    {
        var despachante = new DespachanteCombinado()
            .Responde<ListarMeusDocumentos, Resultado<Pagina<DocumentoDoPortal>>>(AcessoNegado());

        var html = Decodificado(await TelaDeTeste.RenderizarComAsync<TelaMeusDocumentos>(despachante, new()));

        // A lista avisa a página, e a página troca o cabeçalho da lista pelo próprio: um título só.
        html.Should().Contain("<h1 class=\"aj-titulo\">Meus documentos</h1>").And.MatchRegex(AvisoDeAcessoEncerrado);
        Regex.Matches(html, "<h1\\b").Should().ContainSingle();
        html.Should().NotContain("<table").And.NotContain("aj-linha").And.NotContain("Nenhum documento.")
            .And.NotContain("Tentar de novo").And.NotContain("Acesso negado.");
    }

    [Theory]
    [InlineData(TipoPerfil.Cliente)]
    [InlineData(TipoPerfil.Diretoria)]
    public async Task Documentos_comuns_com_acesso_encerrado_mostra_o_aviso_no_lugar_da_lista(TipoPerfil tipo)
    {
        var ator = new UsuarioAutenticado(
            Guid.NewGuid(), "Fulano de Tal", tipo, Guid.NewGuid(), tipo == TipoPerfil.Cliente ? Guid.NewGuid() : null);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(ator)
            .Responde<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(AcessoNegado());

        var html = Decodificado(await TelaDeTeste.RenderizarComAsync<TelaDocumentosComuns>(despachante, new()));

        html.Should().Contain("<h1 class=\"aj-titulo\">Documentos comuns</h1>").And.MatchRegex(AvisoDeAcessoEncerrado);
        Regex.Matches(html, "<h1\\b").Should().ContainSingle();
        // Nem a lista do cliente, nem a gestão da Diretoria: o envio também sai.
        html.Should().NotContain("<table").And.NotContain("aj-linha").And.NotContain("Nenhum documento.")
            .And.NotContain("Tentar de novo").And.NotContain("Acesso negado.").And.NotContain("Enviar documento")
            .And.NotContain("Carregando");
    }

    [Fact]
    public async Task Lista_de_documentos_com_mais_de_uma_pagina_traz_a_paginacao_nas_duas_formas()
    {
        var itens = Enumerable.Range(1, DocumentosOpcoes.TamanhoDaPagina)
            .Select(n => new DocumentoDoPortal(Guid.NewGuid(), $"modelo-{n}.pdf", 1024, new DateTime(2026, 10, 1, 15, 0, 0, DateTimeKind.Utc)))
            .ToList();
        Func<int, Task<Resultado<Pagina<DocumentoDoPortal>>>> listar = pagina => Task.FromResult(
            Resultado<Pagina<DocumentoDoPortal>>.Ok(new Pagina<DocumentoDoPortal>(itens, 45, pagina, DocumentosOpcoes.TamanhoDaPagina)));

        var html = Decodificado(await TelaDeTeste.RenderizarComAsync<DocumentosParaBaixar>(
            new DespachanteCombinado(), new() { ["Titulo"] = "Meus documentos", ["Listar"] = listar }));

        // A tabela do desktop e a lista do tablet e do mobile têm cada uma a sua linha de paginação.
        foreach (var visao in new[] { "aj-so-desktop", "aj-tablet-mobile" })
        {
            var bloco = BlocoDeVisao.De(html, visao);

            bloco.Should().Contain("modelo-20.pdf", visao).And.Contain("<div class=\"aj-paginacao\">", visao)
                .And.Contain("<span class=\"aj-subtitulo\">Página 1 de 3 · 45 documentos</span>", visao)
                .And.MatchRegex("<button[^>]*disabled[^>]*>Anterior</button>", visao)
                .And.Contain(">Próxima</button>", visao)
                .And.NotMatchRegex("<button[^>]*disabled[^>]*>Próxima</button>", visao);
            Regex.Matches(bloco, "<div class=\"aj-paginacao\">").Should().ContainSingle(visao);
        }
    }

    [Fact]
    public async Task Producao_em_documentos_comuns_ve_nao_encontrada()
    {
        var pedro = await DoPedroAsync();
        var negadosAntes = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Count;

        var html = Decodificado(await pedro.GetStringAsync("/documentos-comuns"));

        html.Should().Contain("Página não encontrada").And.NotContain("comum.pdf").And.NotContain("Carregando");
        (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().HaveCount(negadosAntes);
    }

    [Fact]
    public async Task Equipe_em_meus_documentos_ve_nao_encontrada_sem_consultar()
    {
        var admin = await _app.ClienteAutenticadoAsync();
        var pedro = await DoPedroAsync();
        var negadosAntes = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Count;
        var consultasAntes = await RegistrosDeConsultaAsync();

        foreach (var equipe in new[] { admin, pedro })
        {
            var html = Decodificado(await equipe.GetStringAsync("/meus-documentos"));

            html.Should().Contain("Página não encontrada").And.NotContain("do-cliente.pdf").And.NotContain("Carregando");
        }

        (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().HaveCount(negadosAntes);
        (await RegistrosDeConsultaAsync()).Should().Be(consultasAntes);
    }

    [Fact]
    public async Task Menu_da_diretoria_tem_documentos_comuns_e_o_da_producao_nao()
    {
        var admin = await _app.ClienteAutenticadoAsync();
        var pedro = await DoPedroAsync();

        var daDiretoria = Hrefs(Decodificado(await admin.GetStringAsync("/")));
        var daProducao = Hrefs(Decodificado(await pedro.GetStringAsync("/")));

        daDiretoria.Should().Contain("/documentos-comuns").And.NotContain("/meus-documentos");
        daDiretoria.Should().ContainInOrder("/documentos-comuns", "/areas");
        daProducao.Should().Contain("/clientes").And.NotContain("/documentos-comuns").And.NotContain("/meus-documentos");
    }

    private static List<string> Hrefs(string html)
    {
        var menu = Regex.Match(html, "<nav[^>]*>.*?</nav>", RegexOptions.Singleline).Value;
        return Regex.Matches(menu, "href=\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToList();
    }
}
