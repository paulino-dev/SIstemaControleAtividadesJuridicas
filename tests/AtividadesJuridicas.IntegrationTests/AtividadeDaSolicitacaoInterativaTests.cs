using AngleSharp.Dom;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Application.Solicitacoes;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Solicitacoes;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O formulário de atividade aberto da solicitação do cliente (<c>/atividades/nova?solicitacao={id}</c>), clicando: o
/// bloco da solicitação, o cliente travado, o comentário e os documentos, e as respostas da criação (cancelada,
/// alterada, já em atividade). Também o cliente travado na edição de uma atividade que veio de solicitação e o
/// "Ver solicitação" na ficha dela.
/// </summary>
public class AtividadeDaSolicitacaoInterativaTests : TelaInterativa
{
    private static readonly Guid SolicitacaoId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006a1");
    private static readonly Guid AtividadeNova = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006f1");
    private static readonly Guid MariaId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006c1");
    private static readonly Guid JoseId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006c2");
    private static readonly Guid CarlaId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006c3");
    private static readonly Guid Area = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006a9");
    private static readonly Guid ProcessoDaMaria = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006b1");
    private static readonly Guid ProcessoDoJose = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006b2");
    private static readonly DateTime AbertaEm = new(2026, 11, 10, 15, 30, 0, DateTimeKind.Utc);

    private const string Descricao = "Preciso de uma certidão atualizada.";
    private const string Comentario = "Anexar a descrição da solicitação como comentário da atividade";

    private static readonly DocumentoDoPortal Rg = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006d1"), "rg.pdf", 2048, AbertaEm);
    private static readonly DocumentoDoPortal Procuracao = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006d2"), "procuracao.pdf", 4096, AbertaEm);
    private static readonly DocumentoDoPortal Comprovante = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006d3"), "comprovante.pdf", 1024, AbertaEm.AddDays(1));

    private static SolicitacaoDetalhe Solicitacao(
        uint versao = 4, IReadOnlyList<DocumentoDoPortal>? documentos = null,
        SituacaoDaSolicitacao situacao = SituacaoDaSolicitacao.Aguardando, string descricao = Descricao) => new(
        SolicitacaoId, AbertaEm, MariaId, "Maria Silva", CarlaId, "Carla Cliente", UrgenciaDaSolicitacao.Alta, descricao,
        situacao, situacao == SituacaoDaSolicitacao.Cancelada ? AbertaEm.AddHours(2) : null,
        documentos ?? [Rg, Procuracao], null, null, versao);

    private static SolicitacaoDetalhe EmAtividade() =>
        Solicitacao(versao: 8, situacao: SituacaoDaSolicitacao.EmAtividade) with
        {
            AtividadeId = AtividadeNova, AtividadeTitulo = "Emitir a certidão",
        };

    // Os processos que o servidor tem, cada um com o seu cliente: a busca filtrada pelo cliente traz só os dele.
    private static readonly (Guid ClienteId, ProcessoResumo Processo)[] Processos =
    [
        (MariaId, new ProcessoResumo(ProcessoDaMaria, null, "Maria — inventário", "Cível", StatusProcesso.Ativo, ["Maria Silva"], null)),
        (JoseId, new ProcessoResumo(ProcessoDoJose, null, "José — despejo", "Cível", StatusProcesso.Ativo, ["José Souza"], null)),
    ];

    private static readonly ProcessoDetalhe DetalheDoProcessoDaMaria = new(
        ProcessoDaMaria, null, "Maria — inventário", Area, "Cível", StatusProcesso.Ativo,
        null, null, null, null, null, null,
        [new ClienteDoProcessoDetalhe(MariaId, "Maria Silva", "529.982.247-25", PoloProcessual.Ativo, true)]);

    /// <summary>
    /// A Diretoria e as respostas do formulário. <paramref name="leituras"/>: o que cada leitura da solicitação devolve,
    /// na ordem (a última se repete): a carga e as releituras depois de uma recusa.
    /// </summary>
    private DespachanteCombinado Respostas(params SolicitacaoDetalhe[] leituras)
    {
        var lidas = 0;
        return Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(
                Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Ok([new AreaAtuacaoResumo(Area, "Cível", true)]))
            .Responde<ObterSolicitacao, Resultado<SolicitacaoDetalhe>>(_ =>
                Resultado<SolicitacaoDetalhe>.Ok(leituras[Math.Min(lidas++, leituras.Length - 1)]))
            .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(listar =>
            {
                List<ProcessoResumo> itens = [.. Processos.Where(p => listar.ClienteId is null || p.ClienteId == listar.ClienteId).Select(p => p.Processo)];
                return Resultado<PaginaDeProcessos>.Ok(new(itens, itens.Count, 1, 20, itens.Count));
            })
            .Responde<ObterProcesso, Resultado<ProcessoDetalhe>>(Resultado<ProcessoDetalhe>.Ok(DetalheDoProcessoDaMaria))
            .Responde<CriarAtividadeDaSolicitacao, Resultado<Guid>>(Resultado<Guid>.Ok(AtividadeNova));
    }

    private IRenderedComponent<AtividadeFormulario> DaSolicitacao(params SolicitacaoDetalhe[] leituras)
    {
        Respostas(leituras.Length == 0 ? [Solicitacao()] : leituras);
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/atividades/nova?solicitacao={SolicitacaoId}");
        return Desenhar<AtividadeFormulario>();
    }

    private string Endereco => Services.GetRequiredService<NavigationManager>().Uri;

    private static IElement Rotulo(IRenderedComponent<AtividadeFormulario> tela, string texto) =>
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.TrimStart().StartsWith(texto));

    private static void Digitar(IRenderedComponent<AtividadeFormulario> tela, string rotulo, string valor) =>
        Rotulo(tela, rotulo).QuerySelector("input, textarea")!.Change(valor);

    private static string? Valor(IRenderedComponent<AtividadeFormulario> tela, string rotulo) =>
        Rotulo(tela, rotulo).QuerySelector("input, textarea")!.GetAttribute("value");

    // A descrição da atividade não vem da solicitação: o diretor a escreve.
    private const string DescricaoDaAtividade = "Emitir a certidão no cartório.";

    private static void Preencher(IRenderedComponent<AtividadeFormulario> tela)
    {
        Digitar(tela, "Título", "Emitir a certidão");
        Digitar(tela, "Descrição", DescricaoDaAtividade);
        tela.FindAll("div.aj-segmentado button").Single(b => b.TextContent == "2").Click();
    }

    private static void Salvar(IRenderedComponent<AtividadeFormulario> tela) =>
        tela.Find("form#formulario-da-atividade").Submit();

    private static IElement Bloco(IRenderedComponent<AtividadeFormulario> tela) =>
        tela.Find("section[aria-labelledby=titulo-da-solicitacao]");

    private static IElement CaixaDoComentario(IRenderedComponent<AtividadeFormulario> tela) =>
        tela.FindAll("label.aj-opcao").Single(l => l.TextContent == Comentario).QuerySelector("input")!;

    /// <summary>Os documentos da lista, com a caixa de cada um: o nome (o texto da opção sem a nota) e se está marcado.</summary>
    private static (string Nome, bool Marcado)[] Documentos(IRenderedComponent<AtividadeFormulario> tela) =>
    [
        .. tela.FindAll("[aria-labelledby=documentos-da-solicitacao] label.aj-opcao").Select(l => (
            l.TextContent[..^l.QuerySelector(".aj-opcao__nota")!.TextContent.Length],
            l.QuerySelector("input")!.HasAttribute("checked"))),
    ];

    private static void Clicar(IRenderedComponent<AtividadeFormulario> tela, string texto) =>
        tela.FindAll("button").Single(b => b.TextContent == texto).Click();

    private IReadOnlyList<CriarAtividadeDaSolicitacao> Enviadas => [.. Despachante.Recebidas.OfType<CriarAtividadeDaSolicitacao>()];

    [Fact]
    public void Abre_com_o_cliente_travado_e_o_titulo_e_a_descricao_vazios()
    {
        var tela = DaSolicitacao();

        var bloco = Bloco(tela);
        bloco.QuerySelector("h2")!.TextContent.Should().Be("Solicitação do cliente");
        bloco.TextContent.Should().Contain("Carla Cliente")
            .And.Contain(Services.GetRequiredService<FusoDoEscritorio>().Formatar(AbertaEm))
            .And.Contain("Alta")
            .And.Contain(Descricao);

        // O cliente da solicitação, em texto e sem como trocá-lo.
        tela.Find("div.aj-vinculo__escolhido .aj-vinculo__titulo").TextContent.Should().Be("Maria Silva");
        tela.FindAll("input[form=busca-de-cliente]").Should().BeEmpty();
        tela.FindAll("button[aria-label='Sem cliente']").Should().BeEmpty();

        // A descrição da solicitação fica no bloco (e vai para o comentário); a da atividade é o diretor quem escreve.
        Valor(tela, "Descrição").Should().BeNullOrEmpty();
        Valor(tela, "Título").Should().BeNullOrEmpty();
        CaixaDoComentario(tela).HasAttribute("checked").Should().BeTrue();
        Documentos(tela).Should().Equal(("rg.pdf", true), ("procuracao.pdf", true));

        // Cancelar e voltar levam à solicitação.
        var ficha = $"/solicitacoes/{SolicitacaoId}";
        tela.FindAll("a.aj-botao").Where(a => a.TextContent == "Cancelar").Should().HaveCount(3)
            .And.OnlyContain(a => a.GetAttribute("href") == ficha);
        tela.Find("a.aj-voltar").GetAttribute("href").Should().Be(ficha);
        tela.Find("a.aj-voltar").TextContent.Should().Be("Solicitação");
    }

    [Fact]
    public void Sem_documentos_a_lista_nao_aparece()
    {
        var tela = DaSolicitacao(Solicitacao(documentos: []));

        Bloco(tela).TextContent.Should().Contain(Descricao);
        CaixaDoComentario(tela).HasAttribute("checked").Should().BeTrue();
        tela.Markup.Should().NotContain("Documentos da solicitação").And.NotContain("Marcar todos").And.NotContain("Desmarcar todos");
    }

    [Fact]
    public void Marcar_todos_e_desmarcar_todos()
    {
        var tela = DaSolicitacao();

        Clicar(tela, "Desmarcar todos");

        Documentos(tela).Should().Equal(("rg.pdf", false), ("procuracao.pdf", false));

        // Uma caixa marcada à mão e depois "Marcar todos".
        tela.FindAll("[aria-labelledby=documentos-da-solicitacao] input")[1].Change(true);
        Documentos(tela).Should().Equal(("rg.pdf", false), ("procuracao.pdf", true));

        Clicar(tela, "Marcar todos");

        Documentos(tela).Should().Equal(("rg.pdf", true), ("procuracao.pdf", true));
        CaixaDoComentario(tela).HasAttribute("checked").Should().BeTrue("os botões valem só para os documentos");
    }

    [Fact]
    public void Busca_de_processo_so_traz_processos_do_cliente()
    {
        var tela = DaSolicitacao();

        tela.Find("input[form=busca-de-processo]").Input("processo");
        tela.Find("form#busca-de-processo").Submit();

        var listar = Despachante.Recebidas.OfType<ListarProcessos>().Should().ContainSingle().Subject;
        listar.Busca.Should().Be("processo");
        listar.ClienteId.Should().Be(MariaId);
        tela.FindAll("ul.aj-resultados .aj-resultados__titulo").Select(t => t.TextContent).Should().Equal("Maria — inventário");

        // Escolher o processo mantém o cliente da solicitação, em texto e sem o seletor.
        tela.Find("button.aj-resultados__item").Click();

        tela.FindAll("label.aj-rotulo").Should().NotContain(l => l.TextContent.StartsWith("Cliente do processo"));
        tela.FindAll("div.aj-vinculo__escolhido .aj-vinculo__titulo").Select(t => t.TextContent)
            .Should().Equal("Maria — inventário", "Maria Silva");

        // Sem o processo, o cliente continua.
        tela.Find("button[aria-label='Sem processo']").Click();

        tela.FindAll("div.aj-vinculo__escolhido .aj-vinculo__titulo").Select(t => t.TextContent).Should().Equal("Maria Silva");
        tela.FindAll("input[form=busca-de-cliente]").Should().BeEmpty();
    }

    [Fact]
    public void Salvar_cria_e_abre_a_ficha_da_atividade_nova()
    {
        var tela = DaSolicitacao();
        Preencher(tela);
        CaixaDoComentario(tela).Change(false);
        tela.FindAll("[aria-labelledby=documentos-da-solicitacao] input")[0].Change(false);

        Salvar(tela);

        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/atividades/{AtividadeNova}"));
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade salva.");
        Despachante.Recebidas.OfType<SalvarAtividade>().Should().BeEmpty();
        var enviada = Enviadas.Should().ContainSingle().Subject;
        enviada.SolicitacaoId.Should().Be(SolicitacaoId);
        enviada.VersaoDaSolicitacao.Should().Be(4);
        enviada.Titulo.Should().Be("Emitir a certidão");
        enviada.Descricao.Should().Be("Emitir a certidão no cartório.");
        enviada.Complexidade.Should().Be(2);
        enviada.ProcessoId.Should().BeNull();
        enviada.AnexarDescricaoComoComentario.Should().BeFalse();
        enviada.DocumentosDaSolicitacao.Should().Equal(Procuracao.Id);
        enviada.ConfirmouSolicitacaoCancelada.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancelada_durante_a_criacao_pede_confirmacao(bool descricaoMudou)
    {
        var cancelada = Solicitacao(versao: 5, situacao: SituacaoDaSolicitacao.Cancelada,
            descricao: descricaoMudou ? "Desisti, obrigado." : Descricao);
        var tela = DaSolicitacao(Solicitacao(), cancelada);
        Despachante.Responde<CriarAtividadeDaSolicitacao, Resultado<Guid>>(criar => criar.ConfirmouSolicitacaoCancelada
            ? criar.VersaoDaSolicitacao == 5
                ? Resultado<Guid>.Ok(AtividadeNova)
                : Resultado<Guid>.Falha(TipoErro.Conflito, SolicitacoesOpcoes.AlteradaDuranteACriacao)
            : Resultado<Guid>.Falha(TipoErro.ConfirmacaoNecessaria, SolicitacaoDoCliente.CanceladaPeloCliente));
        Preencher(tela);

        Salvar(tela);

        var aviso = tela.WaitForElement("[role=alertdialog]");
        aviso.QuerySelector("strong")!.TextContent.Should().Be(SolicitacaoDoCliente.CanceladaPeloCliente);
        aviso.QuerySelector("a")!.TextContent.Should().Be("Voltar à solicitação");
        aviso.QuerySelector("a")!.GetAttribute("href").Should().Be($"/solicitacoes/{SolicitacaoId}");
        aviso.QuerySelectorAll("button").Select(b => b.TextContent).Should().Equal("Criar mesmo assim");
        if (descricaoMudou)
        {
            aviso.TextContent.Should().Contain(SolicitacoesOpcoes.AlteradaDuranteACriacao);
            Bloco(tela).TextContent.Should().Contain("Desisti, obrigado.");
        }
        else
            aviso.TextContent.Should().NotContain(SolicitacoesOpcoes.AlteradaDuranteACriacao);
        Valor(tela, "Título").Should().Be("Emitir a certidão");
        Valor(tela, "Descrição").Should().Be(DescricaoDaAtividade, "a descrição da atividade é a que o diretor tem no campo");

        Clicar(tela, "Criar mesmo assim");

        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/atividades/{AtividadeNova}"));
        Enviadas.Should().HaveCount(2);
        Enviadas[1].ConfirmouSolicitacaoCancelada.Should().BeTrue();
        Enviadas[1].VersaoDaSolicitacao.Should().Be(5, "a confirmação vai com a versão relida");
        Enviadas[1].Titulo.Should().Be("Emitir a certidão");
    }

    [Fact]
    public void Alterada_durante_a_criacao_recarrega_o_bloco_e_mantem_o_digitado()
    {
        var alterada = Solicitacao(versao: 6, documentos: [Rg, Procuracao, Comprovante], descricao: "Preciso de duas certidões.");
        var tela = DaSolicitacao(Solicitacao(), alterada);
        Despachante.Responde<CriarAtividadeDaSolicitacao, Resultado<Guid>>(criar => criar.VersaoDaSolicitacao == 6
            ? Resultado<Guid>.Ok(AtividadeNova)
            : Resultado<Guid>.Falha(TipoErro.Conflito, SolicitacoesOpcoes.AlteradaDuranteACriacao));
        Preencher(tela);
        tela.FindAll("[aria-labelledby=documentos-da-solicitacao] input")[0].Change(false);   // o rg, desmarcado

        Salvar(tela);

        tela.WaitForAssertion(() => tela.Find("p.aj-aviso--erro").TextContent.Should().Contain(SolicitacoesOpcoes.AlteradaDuranteACriacao));
        Bloco(tela).TextContent.Should().Contain("Preciso de duas certidões.").And.NotContain(Descricao);
        Documentos(tela).Should().Equal(("rg.pdf", false), ("procuracao.pdf", true), ("comprovante.pdf", true));
        Valor(tela, "Título").Should().Be("Emitir a certidão");
        Valor(tela, "Descrição").Should().Be(DescricaoDaAtividade);

        Salvar(tela);

        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/atividades/{AtividadeNova}"));
        Enviadas[^1].VersaoDaSolicitacao.Should().Be(6);
        Enviadas[^1].DocumentosDaSolicitacao.Should().Equal(Procuracao.Id, Comprovante.Id);
    }

    [Fact]
    public void Documento_removido_some_depois_do_conflito()
    {
        var tela = DaSolicitacao(Solicitacao(), Solicitacao(versao: 5, documentos: [Procuracao]));
        Despachante.Responde<CriarAtividadeDaSolicitacao, Resultado<Guid>>(criar => criar.VersaoDaSolicitacao == 5
            ? Resultado<Guid>.Ok(AtividadeNova)
            : Resultado<Guid>.Falha(TipoErro.Conflito, SolicitacoesOpcoes.AlteradaDuranteACriacao));
        Preencher(tela);

        Salvar(tela);

        tela.WaitForAssertion(() => tela.Markup.Should().Contain(SolicitacoesOpcoes.AlteradaDuranteACriacao));
        Documentos(tela).Should().Equal(("procuracao.pdf", true));

        Salvar(tela);

        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/atividades/{AtividadeNova}"));
        Enviadas[^1].DocumentosDaSolicitacao.Should().Equal(Procuracao.Id);
    }

    [Fact]
    public void Ja_virou_atividade_mostra_o_link()
    {
        // Na carga: o aviso com o link, e nenhum formulário.
        var carga = DaSolicitacao(EmAtividade());

        carga.Markup.Should().Contain(SolicitacaoDoCliente.JaVirouAtividade);
        carga.Find($"a[href='/atividades/{AtividadeNova}']").TextContent.Should().Be("Ver atividade");
        carga.FindAll("form#formulario-da-atividade").Should().BeEmpty();
        carga.FindAll("button[form=formulario-da-atividade]").Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarAreasAtuacao>().Should().BeEmpty();

        // Na gravação (outra aba criou antes): o mesmo aviso, acima do formulário, que continua.
        var gravacao = DaSolicitacao(Solicitacao(), EmAtividade());
        Despachante.Responde<CriarAtividadeDaSolicitacao, Resultado<Guid>>(
            Resultado<Guid>.Falha(TipoErro.Conflito, SolicitacaoDoCliente.JaVirouAtividade));
        Preencher(gravacao);

        Salvar(gravacao);

        gravacao.WaitForAssertion(() => gravacao.Find($"a[href='/atividades/{AtividadeNova}']").TextContent.Should().Be("Ver atividade"));
        gravacao.Markup.Should().Contain(SolicitacaoDoCliente.JaVirouAtividade);
        gravacao.FindAll("p.aj-aviso--erro").Should().BeEmpty("o aviso com o link substitui a mensagem solta");
        gravacao.Find("form#formulario-da-atividade");
        Valor(gravacao, "Título").Should().Be("Emitir a certidão");
        Endereco.Should().NotEndWith($"/atividades/{AtividadeNova}");
    }

    [Fact]
    public void Ja_virou_atividade_na_gravacao_trava_o_salvar_e_as_caixas_do_bloco()
    {
        var tela = DaSolicitacao(Solicitacao(), EmAtividade());
        Despachante.Responde<CriarAtividadeDaSolicitacao, Resultado<Guid>>(
            Resultado<Guid>.Falha(TipoErro.Conflito, SolicitacaoDoCliente.JaVirouAtividade));
        Preencher(tela);

        // Antes da recusa, as três barras (desktop, tablet e mobile) salvam e o bloco mexe.
        tela.FindAll("button[form=formulario-da-atividade]").Should().HaveCount(3)
            .And.OnlyContain(b => !b.HasAttribute("disabled"));
        Bloco(tela).QuerySelectorAll("input, button").Should().NotBeEmpty().And.OnlyContain(e => !e.HasAttribute("disabled"));

        Salvar(tela);

        // Depois dela, salvar de novo só repetiria a recusa: o botão das três barras e o bloco ficam travados, e o que o
        // diretor digitou continua à vista.
        tela.WaitForAssertion(() => tela.Find($"a[href='/atividades/{AtividadeNova}']"));
        tela.WaitForAssertion(() => tela.FindAll("button[form=formulario-da-atividade]").Should().HaveCount(3)
            .And.OnlyContain(b => b.HasAttribute("disabled")));
        Bloco(tela).QuerySelectorAll("input, button").Should().NotBeEmpty().And.OnlyContain(e => e.HasAttribute("disabled"));
        Valor(tela, "Título").Should().Be("Emitir a certidão");
        Enviadas.Should().ContainSingle();
    }

    private static readonly Guid AtividadeComProcesso = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006e3");

    private static readonly ProcessoDetalhe DetalheDoProcessoDoJose = new(
        ProcessoDoJose, null, "José — despejo", Area, "Cível", StatusProcesso.Ativo,
        null, null, null, null, null, null,
        [new ClienteDoProcessoDetalhe(JoseId, "José Souza", "111.444.777-35", PoloProcessual.Ativo, true)]);

    /// <summary>A edição de uma atividade da solicitação (cliente: Maria) que tem <paramref name="processo"/>.</summary>
    private IRenderedComponent<AtividadeFormulario> EdicaoComProcesso(Guid processoId, string processoTitulo, ProcessoDetalhe processo)
    {
        Respostas()
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(Resultado<AtividadeDetalhe>.Ok(new(
                AtividadeComProcesso, "Emitir a certidão", Descricao, null, StatusAtividade.Criada, false,
                null, MariaId, "Maria Silva", processoId, processoTitulo, Area, "Cível", TipoAutor.Usuario, "Diana Diretora", null, null,
                [], [], new AcoesDaAtividade(true, true, true, false, false, false, true), 7, 2, SolicitacaoId)))
            .Responde<ObterProcesso, Resultado<ProcessoDetalhe>>(Resultado<ProcessoDetalhe>.Ok(processo));
        return Desenhar<AtividadeFormulario>(p => p.Add(x => x.Id, (Guid?)AtividadeComProcesso));
    }

    [Fact]
    public void Edicao_da_atividade_da_solicitacao_com_o_processo_sem_o_cliente_avisa_junto_do_cliente_travado()
    {
        var tela = EdicaoComProcesso(ProcessoDoJose, "José — despejo", DetalheDoProcessoDoJose);

        // O cliente segue só em texto (não há como trocá-lo), e o aviso diz o que fazer.
        tela.FindAll("div.aj-vinculo__escolhido .aj-vinculo__titulo").Select(t => t.TextContent)
            .Should().Equal("José — despejo", "Maria Silva");
        tela.FindAll("label.aj-rotulo").Should().NotContain(l => l.TextContent.StartsWith("Cliente do processo"));
        tela.FindAll("input[form=busca-de-cliente]").Should().BeEmpty();
        tela.Find(".aj-campo-erro").TextContent
            .Should().Be("O cliente desta atividade não é mais cliente deste processo. Escolha outro processo ou remova-o.");
        tela.FindAll(".aj-campo-erro").Should().ContainSingle();

        // Remover o processo resolve: sem processo, o cliente da solicitação continua e não há o que avisar.
        tela.Find("button[aria-label='Sem processo']").Click();

        tela.FindAll(".aj-campo-erro").Should().BeEmpty();
        tela.Find("div.aj-vinculo__escolhido .aj-vinculo__titulo").TextContent.Should().Be("Maria Silva");
    }

    [Fact]
    public void Edicao_da_atividade_da_solicitacao_com_o_cliente_no_processo_nao_avisa()
    {
        var tela = EdicaoComProcesso(ProcessoDaMaria, "Maria — inventário", DetalheDoProcessoDaMaria);

        tela.FindAll("div.aj-vinculo__escolhido .aj-vinculo__titulo").Select(t => t.TextContent)
            .Should().Equal("Maria — inventário", "Maria Silva");
        tela.FindAll(".aj-campo-erro").Should().BeEmpty();
    }

    [Fact]
    public void Edicao_de_atividade_da_solicitacao_trava_o_cliente()
    {
        var atividadeId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006e1");
        Respostas()
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(Resultado<AtividadeDetalhe>.Ok(new(
                atividadeId, "Emitir a certidão", Descricao, null, StatusAtividade.Criada, false,
                null, MariaId, "Maria Silva", null, null, Area, "Cível", TipoAutor.Usuario, "Diana Diretora", null, null, [], [],
                new AcoesDaAtividade(true, true, true, false, false, false, true), 7, 2, SolicitacaoId)))
            .Responde<SalvarAtividade, Resultado<Guid>>(Resultado<Guid>.Ok(atividadeId));
        var tela = Desenhar<AtividadeFormulario>(p => p.Add(x => x.Id, (Guid?)atividadeId));

        tela.Find("div.aj-vinculo__escolhido .aj-vinculo__titulo").TextContent.Should().Be("Maria Silva");
        tela.FindAll("input[form=busca-de-cliente]").Should().BeEmpty();
        tela.FindAll("button[aria-label='Sem cliente']").Should().BeEmpty();
        tela.FindAll("section[aria-labelledby=titulo-da-solicitacao]").Should().BeEmpty("o bloco é só da criação");

        tela.Find("input[form=busca-de-processo]").Input("processo");
        tela.Find("form#busca-de-processo").Submit();

        Despachante.Recebidas.OfType<ListarProcessos>().Should().ContainSingle().Which.ClienteId.Should().Be(MariaId);
        tela.Find("button.aj-resultados__item").Click();
        tela.FindAll("label.aj-rotulo").Should().NotContain(l => l.TextContent.StartsWith("Cliente do processo"));

        Salvar(tela);

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<SalvarAtividade>().Should().ContainSingle()
            .Which.ClienteId.Should().Be(MariaId));
        Despachante.Recebidas.OfType<SalvarAtividade>().Single().ProcessoId.Should().Be(ProcessoDaMaria);
        Despachante.Recebidas.OfType<ObterSolicitacao>().Should().BeEmpty();
    }

    [Fact]
    public void Producao_nao_abre_o_formulario_com_solicitacao()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao);
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/atividades/nova?solicitacao={SolicitacaoId}");

        var tela = Desenhar<AtividadeFormulario>();

        tela.Markup.Should().Contain("Acesso negado.");
        tela.FindAll("form#formulario-da-atividade").Should().BeEmpty();
        Despachante.Recebidas.Should().ContainSingle().Which.Should().BeOfType<ObterMeuAcesso>();
    }

    [Fact]
    public void Solicitacao_inexistente_mostra_nao_encontrada()
    {
        Respostas().Responde<ObterSolicitacao, Resultado<SolicitacaoDetalhe>>(
            Resultado<SolicitacaoDetalhe>.Falha(TipoErro.NaoEncontrado, SolicitacoesOpcoes.NaoEncontrada));
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/atividades/nova?solicitacao={SolicitacaoId}");

        var tela = Desenhar<AtividadeFormulario>();

        tela.Markup.Should().Contain("Solicitação não encontrada.");
        tela.FindAll("form#formulario-da-atividade").Should().BeEmpty();
    }

    // ---- A ficha da atividade: o caminho de volta para a solicitação ----

    private static readonly Guid AtividadeDaFicha = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000006e2");

    private static AtividadeDetalhe DetalheDaAtividade(Guid? solicitacaoId, AcoesDaAtividade acoes) => new(
        AtividadeDaFicha, "Emitir a certidão", Descricao, null, StatusAtividade.Criada, false,
        null, MariaId, "Maria Silva", null, null, Area, "Cível", TipoAutor.Usuario, "Diana Diretora", null, null, [], [],
        acoes, 7, 2, solicitacaoId);

    private IRenderedComponent<ComBarraSuperior<AtividadeFicha>> FichaDaAtividade(UsuarioAutenticado ator, AtividadeDetalhe detalhe)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(ator)
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(Resultado<AtividadeDetalhe>.Ok(detalhe))
            .Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
                Resultado<IReadOnlyList<ComentarioDetalhe>>.Ok([]))
            .Responde<ListarDocumentosDaAtividade, Resultado<Pagina<DocumentoResumo>>>(
                Resultado<Pagina<DocumentoResumo>>.Ok(new([], 0, 1, 20)));
        return Desenhar<ComBarraSuperior<AtividadeFicha>>(p => p.AddUnmatched(nameof(AtividadeFicha.Id), AtividadeDaFicha));
    }

    private static List<string> Textos(IEnumerable<IElement> elementos) =>
        [.. elementos.Select(e => e.TextContent.Trim()).Where(t => t.Length > 0)];

    private const string NaFaixa = ".aj-painel__botoes--faixa a, .aj-painel__botoes--faixa button";
    private const string NoMaisAcoes = ".aj-barra-superior .aj-menu-acoes__item";

    [Fact]
    public void Ficha_da_atividade_da_solicitacao_mostra_ver_solicitacao_depois_do_cadastro_e_antes_de_excluir()
    {
        var ficha = FichaDaAtividade(AtorDaDiretoria, DetalheDaAtividade(SolicitacaoId, new(true, true, false, false, false, false, true)));

        // Na faixa: botão fantasma para a ficha da solicitação, depois de Responsáveis e antes de Excluir.
        var faixa = ficha.FindAll(NaFaixa);
        Textos(faixa).Should().Equal("Editar", "Responsáveis", "Ver solicitação", "Excluir");
        var verNaFaixa = faixa.Single(e => e.TextContent == "Ver solicitação");
        verNaFaixa.TagName.Should().Be("A");
        verNaFaixa.GetAttribute("href").Should().Be($"/solicitacoes/{SolicitacaoId}");
        verNaFaixa.ClassList.Should().BeEquivalentTo("aj-botao", "aj-botao--fantasma");

        // No "Mais ações" do mobile: o mesmo destino, antes de Excluir.
        var menu = ficha.FindAll(NoMaisAcoes);
        Textos(menu).Should().Equal("Editar", "Responsáveis", "Ver solicitação", "Excluir");
        var verNoMenu = menu.Single(e => e.TextContent == "Ver solicitação");
        verNoMenu.GetAttribute("href").Should().Be($"/solicitacoes/{SolicitacaoId}");
        verNoMenu.ClassList.Should().BeEquivalentTo("aj-menu-acoes__item");
    }

    [Fact]
    public void Ver_solicitacao_basta_para_a_faixa_e_o_mais_acoes_aparecerem()
    {
        // Sem nenhuma outra ação de cadastro: a solicitação de origem sozinha mantém a faixa e o menu.
        var ficha = FichaDaAtividade(AtorDaDiretoria, DetalheDaAtividade(SolicitacaoId, new(false, false, false, false, false, false, false)));

        Textos(ficha.FindAll(NaFaixa)).Should().Equal("Ver solicitação");
        ficha.FindAll(".aj-barra-superior button[aria-label='Mais ações']").Should().ContainSingle();
        Textos(ficha.FindAll(NoMaisAcoes)).Should().Equal("Ver solicitação");
        ficha.FindAll(".aj-painel__divisor").Should().BeEmpty("sem ação primária não há o que separar");
    }

    [Fact]
    public void Ver_solicitacao_fica_depois_do_divisor_das_acoes_primarias()
    {
        var ficha = FichaDaAtividade(AtorDaDiretoria, DetalheDaAtividade(SolicitacaoId, new(false, false, true, false, true, false, false)));

        Textos(ficha.FindAll(NaFaixa)).Should().Equal("Aprovar", "Devolver", "Trocar revisor", "Ver solicitação");
        ficha.FindAll(".aj-painel__divisor").Should().ContainSingle();
        Textos(ficha.FindAll(NoMaisAcoes)).Should().Equal("Trocar revisor", "Ver solicitação");
    }

    [Fact]
    public void Atividade_sem_solicitacao_nao_mostra_ver_solicitacao()
    {
        var ficha = FichaDaAtividade(AtorDaDiretoria, DetalheDaAtividade(null, new(true, true, false, false, false, false, true)));

        Textos(ficha.FindAll(NaFaixa)).Should().Equal("Editar", "Responsáveis", "Excluir");
        Textos(ficha.FindAll(NoMaisAcoes)).Should().Equal("Editar", "Responsáveis", "Excluir");
        ficha.Markup.Should().NotContain("Ver solicitação").And.NotContain("/solicitacoes/");
    }

    [Fact]
    public void Producao_nao_ve_ver_solicitacao()
    {
        // O servidor só devolve o SolicitacaoId à Diretoria: a Produção chega sem ele e a ficha não inventa o botão.
        var ficha = FichaDaAtividade(AtorDaProducao, DetalheDaAtividade(null, new(false, false, false, true, false, false, false)));

        Textos(ficha.FindAll(NaFaixa)).Should().Equal("Concluir minha parte");
        ficha.FindAll(".aj-barra-superior button[aria-label='Mais ações']").Should().BeEmpty();
        ficha.Markup.Should().NotContain("Ver solicitação").And.NotContain("/solicitacoes/");
    }
}
