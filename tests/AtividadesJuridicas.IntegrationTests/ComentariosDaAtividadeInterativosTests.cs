using AngleSharp.Dom;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Web.Components.Atividades;
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Os comentários da ficha da atividade, clicando: os balões com avatar, nome, data e texto, os estados da lista, e o
/// comentário novo nas duas formas (desktop e mobile; tablet), ligadas ao mesmo texto.
/// </summary>
public class ComentariosDaAtividadeInterativosTests : TelaInterativa
{
    private static readonly Guid Atividade = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1");

    // 19:45 e 11:30 em UTC são 16:45 e 08:30 no horário do escritório.
    private static readonly ComentarioDetalhe DaBeatriz = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1"),
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000e1"), "Beatriz Lins",
        "Minuta anexada.\nDeixei em aberto a cláusula de multa por atraso.", new DateTime(2026, 10, 7, 19, 45, 0, DateTimeKind.Utc));
    private static readonly ComentarioDetalhe DaMarina = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c2"),
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000e2"), "Marina Couto",
        "Otávio, confira os valores da cláusula terceira antes da revisão.", new DateTime(2026, 10, 8, 11, 30, 0, DateTimeKind.Utc));

    // O que o servidor tem: a lista lê daqui, e comentar acrescenta aqui.
    private readonly List<ComentarioDetalhe> _noServidor = [DaBeatriz, DaMarina];

    public ComentariosDaAtividadeInterativosTests()
    {
        Despachante
            .Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
                _ => Resultado<IReadOnlyList<ComentarioDetalhe>>.Ok([.. _noServidor]))
            .Responde<ComentarAtividade, Resultado<Guid>>(pedido =>
            {
                _noServidor.Add(new ComentarioDetalhe(Guid.NewGuid(), Guid.NewGuid(), "Diana Diretora", pedido.Texto,
                    new DateTime(2026, 10, 9, 13, 0, 0, DateTimeKind.Utc)));
                return Resultado<Guid>.Ok(Guid.NewGuid());
            });
    }

    private IRenderedComponent<ComentariosDaAtividade> Painel(
        Action<int>? aoContar = null, Action? naoEncontrada = null, Action? acessoEncerrado = null) =>
        Desenhar<ComentariosDaAtividade>(p => p
            .Add(c => c.AtividadeId, Atividade)
            .Add(c => c.AoContar, (int quantos) => aoContar?.Invoke(quantos))
            .Add(c => c.OnAtividadeNaoEncontrada, () => naoEncontrada?.Invoke())
            .Add(c => c.OnAcessoEncerrado, () => acessoEncerrado?.Invoke()));

    // O valor que o Blazor liga à caixa de texto: o bUnit o escreve como atributo "value" ou como conteúdo.
    private static string Valor(IElement caixa) => caixa.GetAttribute("value") ?? caixa.TextContent;

    private static IElement CaixaDoDesktop(IRenderedComponent<ComentariosDaAtividade> painel) =>
        painel.Find("div.aj-desktop-mobile textarea");

    private static IElement CaixaDoTablet(IRenderedComponent<ComentariosDaAtividade> painel) =>
        painel.Find("div.aj-so-tablet textarea");

    private static IElement Comentar(IRenderedComponent<ComentariosDaAtividade> painel) =>
        painel.Find("div.aj-desktop-mobile button");

    private static IElement EnviarComentario(IRenderedComponent<ComentariosDaAtividade> painel) =>
        painel.Find("div.aj-so-tablet button");

    [Fact]
    public void Lista_mostra_avatar_nome_data_completa_e_curta_e_texto()
    {
        var painel = Painel();

        painel.Find("section").ClassName.Should().Be("aj-painel aj-painel--ficha aj-painel--comentarios");
        painel.Find("section > h2").TextContent.Should().Be("Comentários");
        var itens = painel.FindAll("ul.aj-comentarios > li.aj-comentario");
        itens.Should().HaveCount(2);

        var avatar = itens[0].QuerySelector("span.aj-avatar")!;
        avatar.TextContent.Should().Be("BL");
        avatar.ClassList.Should().Contain(AvatarDePessoa.Classe("Beatriz Lins"));
        avatar.GetAttribute("aria-hidden").Should().Be("true");
        itens[0].QuerySelector(".aj-comentario__balao .aj-comentario__cabeca strong")!.TextContent.Should().Be("Beatriz Lins");
        itens[0].QuerySelector("span.aj-desktop-mobile span.aj-comentario__data")!.TextContent.Should().Be("07/10/2026 16:45");
        itens[0].QuerySelector("span.aj-so-tablet span.aj-comentario__data")!.TextContent.Should().Be("07/10 16:45");
        itens[0].QuerySelector(".aj-comentario__balao .aj-comentario__texto")!.TextContent
            .Should().Be("Minuta anexada.\nDeixei em aberto a cláusula de multa por atraso.");

        itens[1].QuerySelector("span.aj-avatar")!.TextContent.Should().Be("MC");
        itens[1].QuerySelector("span.aj-desktop-mobile span.aj-comentario__data")!.TextContent.Should().Be("08/10/2026 08:30");
        itens[1].QuerySelector("span.aj-so-tablet span.aj-comentario__data")!.TextContent.Should().Be("08/10 08:30");
    }

    [Fact]
    public void Autor_nulo_aparece_como_autor_nao_identificado()
    {
        _noServidor.Clear();
        _noServidor.Add(DaBeatriz with { UsuarioNome = null });

        var painel = Painel();

        var item = painel.Find("li.aj-comentario");
        item.QuerySelector(".aj-comentario__cabeca strong")!.TextContent.Should().Be("Autor não identificado");
        item.QuerySelector("span.aj-avatar")!.TextContent.Should().Be("?");
    }

    [Fact]
    public void Sem_comentarios_mostra_o_vazio_e_as_duas_formas_do_comentario_novo()
    {
        _noServidor.Clear();

        var painel = Painel();

        painel.Find(".aj-estado-lista").TextContent.Should().Be("Nenhum comentário.");
        painel.FindAll("ul.aj-comentarios").Should().BeEmpty();

        var desktop = painel.Find("div.aj-desktop-mobile div.aj-comentarios__novo");
        desktop.QuerySelector("label.aj-rotulo")!.TextContent.Should().StartWith("Novo comentário");
        var caixa = desktop.QuerySelector("textarea.aj-campo")!;
        caixa.GetAttribute("rows").Should().Be("3");
        caixa.GetAttribute("placeholder").Should().Be("Escreva um comentário para a equipe");
        caixa.GetAttribute("maxlength").Should().Be("4000");
        desktop.QuerySelector("button.aj-botao.aj-botao--secundario")!.TextContent.Should().Be("Comentar");

        var tablet = painel.Find("div.aj-so-tablet div.aj-campo-com-botao");
        tablet.QuerySelector("label.aj-rotulo span.aj-so-leitor")!.TextContent.Should().Be("Novo comentário");
        var caixaDoTablet = tablet.QuerySelector("textarea.aj-campo.aj-campo--folgado")!;
        caixaDoTablet.GetAttribute("rows").Should().Be("2");
        caixaDoTablet.GetAttribute("placeholder").Should().Be("Escreva um comentário");
        var enviar = tablet.QuerySelector("button.aj-botao.aj-botao--primario.aj-botao--icone.aj-botao--enviar")!;
        enviar.GetAttribute("aria-label").Should().Be("Enviar comentário");
        enviar.QuerySelector("svg").Should().NotBeNull();
    }

    [Fact]
    public void Falha_da_consulta_mostra_o_erro_sem_fingir_lista_vazia_e_tentar_de_novo_le_outra_vez()
    {
        var chamadas = 0;
        Despachante.Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(_ => ++chamadas == 1
            ? Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.Validacao, "Não foi possível consultar os comentários.")
            : Resultado<IReadOnlyList<ComentarioDetalhe>>.Ok([DaBeatriz]));
        var contagens = new List<int>();

        var painel = Painel(aoContar: contagens.Add);

        painel.Find("[role=alert]").TextContent.Should().Contain("Não foi possível consultar os comentários.");
        painel.Markup.Should().NotContain("Nenhum comentário.");
        contagens.Should().BeEmpty();   // falha não conta

        painel.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        painel.WaitForAssertion(() => painel.FindAll("li.aj-comentario").Should().ContainSingle());
        painel.FindAll("[role=alert]").Should().BeEmpty();
        chamadas.Should().Be(2);
        contagens.Should().Equal(1);
    }

    [Fact]
    public void Comentar_envia_o_texto_recarrega_a_lista_limpa_as_caixas_e_avisa()
    {
        var contagens = new List<int>();
        var painel = Painel(aoContar: contagens.Add);

        CaixaDoDesktop(painel).Input("Conferi os valores da cláusula terceira.");
        Comentar(painel).Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Comentário enviado."));
        Despachante.Recebidas.OfType<ComentarAtividade>().Should().ContainSingle()
            .Which.Should().Be(new ComentarAtividade(Atividade, "Conferi os valores da cláusula terceira."));
        Despachante.Recebidas.OfType<ListarComentariosDaAtividade>().Should().HaveCount(2);
        var itens = painel.FindAll("li.aj-comentario");
        itens.Should().HaveCount(3);
        itens[2].QuerySelector(".aj-comentario__texto")!.TextContent.Should().Be("Conferi os valores da cláusula terceira.");
        Valor(CaixaDoDesktop(painel)).Should().BeEmpty();
        Valor(CaixaDoTablet(painel)).Should().BeEmpty();
        contagens.Should().Equal(2, 3);
    }

    [Fact]
    public void O_texto_digitado_numa_forma_vale_na_outra()
    {
        var painel = Painel();

        CaixaDoTablet(painel).Input("Revisar a cláusula terceira.");

        Valor(CaixaDoDesktop(painel)).Should().Be("Revisar a cláusula terceira.");
        Comentar(painel).Click();
        painel.WaitForAssertion(() => Despachante.Recebidas.OfType<ComentarAtividade>().Should().ContainSingle()
            .Which.Texto.Should().Be("Revisar a cláusula terceira."));

        CaixaDoDesktop(painel).Input("Anexei o cálculo.");

        Valor(CaixaDoTablet(painel)).Should().Be("Anexei o cálculo.");
        EnviarComentario(painel).Click();
        painel.WaitForAssertion(() => Despachante.Recebidas.OfType<ComentarAtividade>().Select(c => c.Texto).Should()
            .Equal("Revisar a cláusula terceira.", "Anexei o cálculo."));
    }

    [Fact]
    public void Com_o_envio_em_curso_as_duas_formas_esperam_e_sai_um_comentario_so()
    {
        var envio = new TaskCompletionSource<Resultado<Guid>>();
        Despachante.RespondeDepois<ComentarAtividade, Resultado<Guid>>(envio.Task);
        var painel = Painel();
        CaixaDoDesktop(painel).Input("Conferi.");

        Comentar(painel).Click();

        Comentar(painel).TextContent.Should().Be("Enviando…");
        Comentar(painel).HasAttribute("disabled").Should().BeTrue();
        EnviarComentario(painel).HasAttribute("disabled").Should().BeTrue();
        painel.FindAll("textarea").Should().HaveCount(2).And.OnlyContain(t => t.HasAttribute("disabled"));

        envio.SetResult(Resultado<Guid>.Ok(Guid.NewGuid()));

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Comentário enviado."));
        Despachante.Recebidas.OfType<ComentarAtividade>().Should().ContainSingle();
        Comentar(painel).TextContent.Should().Be("Comentar");
        painel.FindAll("textarea").Should().OnlyContain(t => !t.HasAttribute("disabled"));
    }

    [Fact]
    public void Atividade_que_sumiu_avisa_a_ficha_e_troca_o_painel_pela_mensagem()
    {
        Despachante.Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
            Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.NaoEncontrado, "Atividade não encontrada."));
        var avisada = false;

        var painel = Painel(naoEncontrada: () => { avisada = true; });

        avisada.Should().BeTrue();
        painel.Find("section > h2").TextContent.Should().Be("Comentários");
        painel.Find(".aj-estado-lista").TextContent.Should().Be("Atividade não encontrada.");
        painel.FindAll("textarea").Should().BeEmpty();
        painel.FindAll("li.aj-comentario").Should().BeEmpty();
    }

    [Fact]
    public void Acesso_negado_avisa_a_pagina_e_nao_finge_lista_vazia()
    {
        Despachante.Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
            Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.AcessoNegado, "Acesso negado."));
        var encerrado = false;

        var painel = Painel(acessoEncerrado: () => { encerrado = true; });

        encerrado.Should().BeTrue();
        painel.Find("[role=alert]").TextContent.Should().Contain("Você não tem mais acesso a esta atividade.");
        painel.Markup.Should().NotContain("Nenhum comentário.");
        painel.FindAll("textarea").Should().BeEmpty();
    }
}
