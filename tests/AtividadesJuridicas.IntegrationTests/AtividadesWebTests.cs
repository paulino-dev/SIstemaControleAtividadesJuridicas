using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Web.Components.Atividades;
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
    private Guid _contestacao;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();

        var contestacao = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Elaborar contestação", "Prazo de 15 dias.", new DateOnly(2020, 1, 15), null, null, null, 2));
        contestacao.Sucesso.Should().BeTrue(contestacao.Mensagem);
        _contestacao = contestacao.Valor;
        var direcionada = await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            contestacao.Valor, [new ResponsavelEntrada(_pedro, "Redigir a peça")], _admin));
        direcionada.Sucesso.Should().BeTrue(direcionada.Mensagem);

        var interno = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Recurso interno", "Sem responsáveis.", null, null, null, null, 3));
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
        // A situação é uma etiqueta do design system; a entrega vencida ganha a etiqueta Atrasada ao lado.
        html.Should().MatchRegex(
            @"<td>\s*<span class=""aj-etiqueta aj-etiqueta--direcionada"">Direcionada</span>\s*" +
            @"<span class=""aj-etiqueta aj-etiqueta--atrasada"">(<svg.*?</svg>)?Atrasada</span>\s*</td>");
        html.Should().MatchRegex(@"<td>\s*<span class=""aj-etiqueta aj-etiqueta--criada"">Criada</span>\s*</td>");
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

    // ---- Complexidade ----

    /// <summary>O seletor de complexidade do formulário: do rótulo até o fim do primeiro select depois dele.</summary>
    private static string SeletorDeComplexidade(string html)
    {
        var inicio = Regex.Match(html, @"Complexidade\s*<select");
        inicio.Success.Should().BeTrue("o formulário deve ter o campo Complexidade");
        return html[inicio.Index..html.IndexOf("</select>", inicio.Index, StringComparison.Ordinal)];
    }

    private static List<string> Opcoes(string select, bool soASelecionada = false) =>
        [.. Regex.Matches(select, soASelecionada
                ? @"<option[^>]*selected[^>]*>\s*([^<]*?)\s*</option>"
                : @"<option[^>]*>\s*([^<]*?)\s*</option>")
            .Select(m => m.Groups[1].Value)];

    [Fact]
    public async Task Lista_mostra_a_complexidade_de_cada_atividade()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/atividades"));

        html.Should().MatchRegex(@"<th>\s*Complexidade\s*</th>");
        html.Should().MatchRegex(@"<td>\s*2\s*</td>").And.MatchRegex(@"<td>\s*3\s*</td>");
    }

    [Fact]
    public async Task Formulario_de_atividade_nova_pede_a_complexidade_e_comeca_sem_escolha()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<FormularioDeAtividade>(
            _app, _admin, new() { [nameof(FormularioDeAtividade.AtividadeId)] = null }));

        var select = SeletorDeComplexidade(html);
        Opcoes(select).Should().Equal("Selecione…", "1", "2", "3");
        Opcoes(select, soASelecionada: true).Should().Equal("Selecione…");
    }

    [Fact]
    public async Task Formulario_de_edicao_abre_com_a_complexidade_da_atividade()
    {
        var atual = (await _app.ExecutarAsync(_admin, new ObterAtividade(_contestacao))).Valor!;

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<FormularioDeAtividade>(_app, _admin, new()
        {
            [nameof(FormularioDeAtividade.AtividadeId)] = _contestacao,
            [nameof(FormularioDeAtividade.Atual)] = atual,
        }));

        Opcoes(SeletorDeComplexidade(html), soASelecionada: true).Should().Equal("2");
    }

    [Fact]
    public async Task Formulario_de_atividade_lembra_que_o_titulo_aparece_para_o_cliente()
    {
        const string lembrete = "O título aparece para o cliente no portal.";
        var cliente = await _app.ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null));
        cliente.Sucesso.Should().BeTrue(cliente.Mensagem);
        var comCliente = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Petição da Maria", "Texto.", null, cliente.Valor, null, null, 1));
        comCliente.Sucesso.Should().BeTrue(comCliente.Mensagem);
        var atual = (await _app.ExecutarAsync(_admin, new ObterAtividade(comCliente.Valor))).Valor!;

        var daAtividadeComCliente = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<FormularioDeAtividade>(_app, _admin, new()
        {
            [nameof(FormularioDeAtividade.AtividadeId)] = comCliente.Valor,
            [nameof(FormularioDeAtividade.Atual)] = atual,
        }));
        var daAtividadeNova = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<FormularioDeAtividade>(
            _app, _admin, new() { [nameof(FormularioDeAtividade.AtividadeId)] = null }));
        var semCliente = (await _app.ExecutarAsync(_admin, new ObterAtividade(_contestacao))).Valor!;
        var daAtividadeSemCliente = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<FormularioDeAtividade>(_app, _admin, new()
        {
            [nameof(FormularioDeAtividade.AtividadeId)] = _contestacao,
            [nameof(FormularioDeAtividade.Atual)] = semCliente,
        }));

        daAtividadeComCliente.Should().Contain(lembrete);
        daAtividadeNova.Should().NotContain(lembrete);
        daAtividadeSemCliente.Should().NotContain(lembrete);
    }
}
