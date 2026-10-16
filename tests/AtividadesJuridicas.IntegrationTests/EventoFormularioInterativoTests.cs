using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class EventoFormularioInterativoTests : TelaInterativa
{
    private static readonly Guid EventoId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f2");
    private static readonly Guid Rafael = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d005");
    private static readonly Guid Iara = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d006");
    private static readonly Guid Sumido = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d007");
    private static readonly Guid ClienteId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1");
    private static readonly Guid ProcessoId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");
    private static readonly Guid AtividadeId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a1");
    private static readonly FusoDoEscritorio Fuso = new("America/Sao_Paulo");

    // A equipe ativa que o formulário oferece (ListarUsuariosParaAgenda).
    private static readonly IReadOnlyList<UsuarioDaAgenda> Equipe =
        [new(AtorDaDiretoria.Id, "Diana Diretora"), new(AtorDaProducao.Id, "Pedro Produção"), new(Rafael, "Rafael Teles")];

    private static readonly ClienteResumo Litoral = new(
        ClienteId, TipoCliente.PessoaJuridica, "Transportes Litoral Ltda.", "11.222.333/0001-81", null, null, null, true);

    private static readonly ProcessoResumo ProcessoEncontrado = new(
        ProcessoId, null, "Litoral — reclamação trabalhista", "Trabalhista", StatusProcesso.Ativo, ["Transportes Litoral Ltda."], null);

    private static readonly AtividadeResumo Minuta = new(
        AtividadeId, "Minuta de acordo trabalhista", StatusAtividade.Criada, new DateOnly(2026, 10, 9), false,
        "Transportes Litoral Ltda.", "Litoral — reclamação trabalhista", "Trabalhista", ["Pedro Produção"], null, 2);

    // Audiência de 45 minutos (fora das quatro durações), com os três vínculos; participam a diretora (quem criou), a
    // Iara, inativa, e um usuário que não é mais encontrado.
    private static EventoDetalhe Audiencia(uint versao = 7, bool gerencia = true) => new(
        EventoId, "Audiência de conciliação", "Levar os documentos.", "Fórum Cível, sala 4", TipoEvento.Audiencia,
        Fuso.ParaUtc(new DateTime(2026, 10, 7, 9, 0, 0)), 45, Fuso.ParaUtc(new DateTime(2026, 10, 7, 9, 45, 0)),
        ClienteId, "Transportes Litoral Ltda.", ProcessoId, "Litoral — reclamação trabalhista", AtividadeId, "Minuta de acordo trabalhista",
        AtorDaDiretoria.Id, "Diana Diretora",
        [new ParticipanteDetalhe(AtorDaDiretoria.Id, "Diana Diretora", true), new ParticipanteDetalhe(Iara, "Iara Inativa", false),
         new ParticipanteDetalhe(Sumido, null, false)],
        new AcoesDoEvento(gerencia, gerencia), versao);

    // A Diretoria e as respostas de que o formulário precisa: a equipe, as três buscas e o salvar.
    private DespachanteCombinado Respostas() => Despachante
        .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
        .Responde<ListarUsuariosParaAgenda, Resultado<IReadOnlyList<UsuarioDaAgenda>>>(Resultado<IReadOnlyList<UsuarioDaAgenda>>.Ok(Equipe))
        .Responde<ListarClientes, Resultado<PaginaDeClientes>>(Resultado<PaginaDeClientes>.Ok(new([Litoral], 1, 1, 20, 1, 0)))
        .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(Resultado<PaginaDeProcessos>.Ok(new([ProcessoEncontrado], 1, 1, 20, 1)))
        .Responde<ListarAtividades, Resultado<PaginaDeAtividades>>(Resultado<PaginaDeAtividades>.Ok(new([Minuta], 1, 1, 20, 1, 0)))
        .Responde<SalvarEvento, Resultado<EventoSalvo>>(Resultado<EventoSalvo>.Ok(new EventoSalvo(EventoId, [])));

    private IRenderedComponent<EventoFormulario> Novo()
    {
        Respostas();
        return Desenhar<EventoFormulario>();
    }

    private IRenderedComponent<EventoFormulario> Edicao(uint versao = 7)
    {
        Respostas().Responde<ObterEvento, Resultado<EventoDetalhe>>(Resultado<EventoDetalhe>.Ok(Audiencia(versao)));
        return Desenhar<EventoFormulario>(p => p.Add(x => x.Id, (Guid?)EventoId));
    }

    // O primeiro rótulo com o texto: o do desktop, quando o campo se repete por visão (as cópias têm o mesmo valor).
    private static IElement Rotulo(IRenderedComponent<EventoFormulario> tela, string texto) =>
        tela.FindAll("label.aj-rotulo").First(l => l.TextContent.TrimStart().StartsWith(texto));

    private static void Digitar(IRenderedComponent<EventoFormulario> tela, string rotulo, string valor) =>
        Rotulo(tela, rotulo).QuerySelector("input, textarea")!.Change(valor);

    private static void Enviar(IRenderedComponent<EventoFormulario> tela) => tela.Find("form#formulario-do-evento").Submit();

    // O texto do chip sem a sigla do avatar: só os nós de texto do próprio rótulo.
    private static string Nome(IElement rotulo) => string.Concat(rotulo.ChildNodes.OfType<IText>().Select(t => t.Text)).Trim();

    private static IElement OpcaoDoDesktop(IRenderedComponent<EventoFormulario> tela, string nome) =>
        tela.FindAll("div.aj-so-desktop label.aj-opcao").Single(l => Nome(l) == nome);

    private static IElement ChipDoTablet(IRenderedComponent<EventoFormulario> tela, string nome) =>
        tela.FindAll("div.aj-so-tablet label.aj-chip--pessoa").Single(l => Nome(l) == nome);

    private static IElement Ladrilho(IRenderedComponent<EventoFormulario> tela, string tipo) =>
        tela.FindAll("label.aj-escolha__opcao").Single(l => l.TextContent == tipo).QuerySelector("input")!;

    // O número de minutos de "Outra duração" (no tablet e no mobile), quando aparece.
    private static IReadOnlyList<IElement> OutraDuracao(IRenderedComponent<EventoFormulario> tela) =>
        [.. tela.FindAll("div.aj-tablet-mobile label.aj-rotulo").Where(l => l.TextContent.TrimStart().StartsWith("Duração (minutos)"))];

    private static void Procurar(IRenderedComponent<EventoFormulario> tela, string busca, string texto)
    {
        tela.Find($"input[form={busca}]").Input(texto);
        tela.Find($"form#{busca}").Submit();
    }

    private string Endereco => Services.GetRequiredService<NavigationManager>().Uri;

    private SalvarEvento Enviado => Despachante.Recebidas.OfType<SalvarEvento>().Should().ContainSingle().Subject;

    private static string Hoje => DateOnly.FromDateTime(Fuso.ParaLocal(DateTime.UtcNow)).ToString("yyyy-MM-dd");

    [Fact]
    public void Enter_na_busca_de_processo_mostra_os_resultados_e_nao_salva()
    {
        var tela = Novo();

        Procurar(tela, "busca-de-processo", "litoral");

        Despachante.Recebidas.OfType<ListarProcessos>().Should().ContainSingle().Which.Busca.Should().Be("litoral");
        tela.Find("ul.aj-resultados .aj-resultados__titulo").TextContent.Should().Be("Litoral — reclamação trabalhista");
        tela.Find("ul.aj-resultados .aj-resultados__meta").TextContent.Should().Be("Trabalhista · Transportes Litoral Ltda.");
        tela.Find("ul.aj-resultados .aj-resultados__acao").TextContent.Should().Be("Escolher");
        Despachante.Recebidas.OfType<SalvarEvento>().Should().BeEmpty();   // o Enter da busca não envia o evento
    }

    [Fact]
    public void Enter_na_busca_de_processo_do_tablet_e_do_mobile_tambem_busca_e_nao_salva()
    {
        var tela = Novo();

        Procurar(tela, "busca-de-processo-curta", "litoral");

        Despachante.Recebidas.OfType<ListarProcessos>().Should().ContainSingle().Which.Busca.Should().Be("litoral");
        tela.Find("ul.aj-resultados .aj-resultados__titulo").TextContent.Should().Be("Litoral — reclamação trabalhista");
        Despachante.Recebidas.OfType<SalvarEvento>().Should().BeEmpty();
    }

    [Fact]
    public void Cliente_indisponivel_na_edicao_mostra_o_texto_e_o_avatar_com_interrogacao()
    {
        Respostas().Responde<ObterEvento, Resultado<EventoDetalhe>>(Resultado<EventoDetalhe>.Ok(Audiencia() with { ClienteNome = null }));
        var tela = Desenhar<EventoFormulario>(p => p.Add(x => x.Id, (Guid?)EventoId));

        var escolhido = tela.FindAll("div.aj-vinculo__escolhido").First();
        escolhido.QuerySelector(".aj-vinculo__avatar")!.TextContent.Should().Be("?");
        escolhido.QuerySelector(".aj-vinculo__nome")!.TextContent.Should().Be("Cliente indisponível");
    }

    [Fact]
    public void Trocar_o_cliente_indisponivel_por_outro_mostra_a_sigla_do_escolhido()
    {
        Respostas().Responde<ObterEvento, Resultado<EventoDetalhe>>(Resultado<EventoDetalhe>.Ok(Audiencia() with { ClienteNome = null }));
        var tela = Desenhar<EventoFormulario>(p => p.Add(x => x.Id, (Guid?)EventoId));
        tela.Find("div.aj-vinculo__escolhido .aj-vinculo__avatar").TextContent.Should().Be("?");

        tela.Find("button[aria-label=\"Remover cliente\"]").Click();
        Procurar(tela, "busca-de-cliente", "litoral");
        tela.Find("ul.aj-resultados button.aj-resultados__item").Click();

        var escolhido = tela.FindAll("div.aj-vinculo__escolhido").First();
        escolhido.QuerySelector(".aj-vinculo__avatar")!.TextContent.Should().Be("TL");
        escolhido.QuerySelector(".aj-vinculo__nome")!.TextContent.Should().Be("Transportes Litoral Ltda.");
    }

    [Fact]
    public void Escolher_o_cliente_mostra_o_escolhido_com_avatar_e_a_troca_so_no_desktop_e_remover_volta_a_busca()
    {
        var tela = Novo();
        Procurar(tela, "busca-de-cliente", "litoral");
        tela.Find("ul.aj-resultados .aj-resultados__meta").TextContent.Should().Be("CNPJ 11.222.333/0001-81");

        tela.Find("ul.aj-resultados button.aj-resultados__item").Click();

        var escolhido = tela.Find("div.aj-vinculo__escolhido");
        escolhido.ClassList.Should().Contain("aj-vinculo__escolhido--com-avatar");
        escolhido.QuerySelector(".aj-vinculo__avatar")!.TextContent.Should().Be("TL");
        escolhido.QuerySelector(".aj-vinculo__nome")!.TextContent.Should().Be("Transportes Litoral Ltda.");
        tela.FindAll("ul.aj-resultados").Should().BeEmpty();
        // Com cliente, a busca é para trocá-lo, e só no desktop.
        tela.FindAll("input[form=busca-de-cliente]").Should().ContainSingle()
            .Which.GetAttribute("placeholder").Should().Be("Trocar cliente: nome, CPF ou CNPJ");
        tela.Find("div.aj-so-desktop input[form=busca-de-cliente]");

        tela.Find("button[aria-label='Remover cliente']").Click();

        tela.FindAll("div.aj-vinculo__escolhido").Should().BeEmpty();
        tela.Find("input[form=busca-de-cliente]").GetAttribute("placeholder").Should().Be("Escolher cliente: nome, CPF ou CNPJ");

        // Escolhido de novo, o cliente vai no pedido.
        Procurar(tela, "busca-de-cliente", "litoral");
        tela.Find("ul.aj-resultados button.aj-resultados__item").Click();
        Digitar(tela, "Título", "Reunião com o cliente");
        Enviar(tela);
        tela.WaitForAssertion(() => Endereco.Should().Contain("/agenda?data="));
        Enviado.ClienteId.Should().Be(ClienteId);
    }

    [Fact]
    public void Escolher_e_remover_processo_e_atividade_e_o_pedido_leva_so_o_que_ficou()
    {
        var tela = Novo();
        Digitar(tela, "Título", "Reunião sobre o acordo");

        Procurar(tela, "busca-de-processo", "litoral");
        tela.Find("ul.aj-resultados button.aj-resultados__item").Click();
        Procurar(tela, "busca-de-atividade", "minuta");
        Despachante.Recebidas.OfType<ListarAtividades>().Should().ContainSingle().Which.Busca.Should().Be("minuta");
        tela.Find("ul.aj-resultados .aj-resultados__titulo").TextContent.Should().Be("Minuta de acordo trabalhista");
        tela.Find("ul.aj-resultados button.aj-resultados__item").Click();

        tela.FindAll("div.aj-vinculo__escolhido .aj-vinculo__nome").Select(n => n.TextContent)
            .Should().Equal("Litoral — reclamação trabalhista", "Minuta de acordo trabalhista");
        tela.FindAll("div.aj-vinculo__escolhido .aj-vinculo__avatar").Should().BeEmpty();   // só o cliente tem avatar
        // Escolhidos, o processo e a atividade não têm busca; o cliente segue sem escolha (os vínculos são independentes).
        tela.FindAll("input[form=busca-de-processo], input[form=busca-de-processo-curta]").Should().BeEmpty();
        tela.FindAll("input[form=busca-de-atividade]").Should().BeEmpty();
        tela.Find("input[form=busca-de-cliente]").GetAttribute("placeholder").Should().Be("Escolher cliente: nome, CPF ou CNPJ");

        tela.Find("button[aria-label='Remover processo']").Click();

        // A busca volta, com o exemplo de cada visão (desktop; tablet e mobile).
        tela.FindAll("input[form=busca-de-processo], input[form=busca-de-processo-curta]").Select(b => b.GetAttribute("placeholder"))
            .Should().Equal("Título, parte contrária ou número CNJ", "Título, parte ou número CNJ");
        Enviar(tela);
        tela.WaitForAssertion(() => Endereco.Should().Contain("/agenda?data="));
        Enviado.ProcessoId.Should().BeNull();
        Enviado.AtividadeId.Should().Be(AtividadeId);
        Enviado.ClienteId.Should().BeNull();
    }

    [Fact]
    public void Salvar_sem_titulo_data_nem_hora_mostra_campo_obrigatorio_e_o_aviso_e_nao_envia()
    {
        var tela = Novo();
        Rotulo(tela, "Data").QuerySelector("input")!.Change("");
        Rotulo(tela, "Hora").QuerySelector("input")!.Change("");

        Enviar(tela);

        tela.Find("p.aj-aviso--erro").TextContent.Should().Be("Informe a data e a hora do evento.");
        // Cada campo aparece duas vezes (desktop; tablet e mobile), as duas em erro, com a frase do quadro.
        foreach (var campo in new[] { "Título", "Data", "Hora" })
            tela.FindAll("label.aj-rotulo--erro").Where(l => l.TextContent.TrimStart().StartsWith(campo)).Should().HaveCount(2)
                .And.OnlyContain(l => l.QuerySelector("span.aj-campo-erro")!.TextContent == "Campo obrigatório");
        tela.FindAll("input[aria-invalid='true']").Should().HaveCount(6);
        Despachante.Recebidas.OfType<SalvarEvento>().Should().BeEmpty();

        Digitar(tela, "Título", "Reunião de alinhamento");

        // Preencher o título revalida o campo: o erro dele sai e os outros ficam.
        tela.FindAll("label.aj-rotulo--erro").Should().NotContain(l => l.TextContent.TrimStart().StartsWith("Título"))
            .And.Contain(l => l.TextContent.TrimStart().StartsWith("Data"));
    }

    [Fact]
    public void Data_fora_dos_anos_aceitos_mostra_o_aviso_e_nao_envia()
    {
        var tela = Novo();
        Digitar(tela, "Título", "Reunião de alinhamento");
        Rotulo(tela, "Data").QuerySelector("input")!.Change("1999-12-31");

        Enviar(tela);

        tela.Find("p.aj-aviso--erro").TextContent.Should().Be("A data deve estar entre os anos 2000 e 2100.");
        Despachante.Recebidas.OfType<SalvarEvento>().Should().BeEmpty();
    }

    [Fact]
    public void Edicao_sem_participantes_mostra_o_erro_no_grupo_e_marcar_alguem_tira_o_erro_e_envia_a_lista()
    {
        var tela = Edicao();
        foreach (var nome in new[] { "Diana Diretora", "Iara Inativa", "Usuário não encontrado" })
            OpcaoDoDesktop(tela, nome).QuerySelector("input")!.Change(false);

        Enviar(tela);

        // O erro nas três marcações (caixas, chips do tablet, chips do mobile); no mobile, a legenda em erro.
        tela.FindAll("span.aj-campo-erro").Where(s => s.TextContent == "Marque ao menos um participante.").Should().HaveCount(3);
        tela.Find("legend.aj-rotulo--erro").TextContent.Should().Be("Participantes");
        Despachante.Recebidas.OfType<SalvarEvento>().Should().BeEmpty();

        ChipDoTablet(tela, "Pedro Produção").QuerySelector("input")!.Change(true);

        tela.Markup.Should().NotContain("Marque ao menos um participante.");
        OpcaoDoDesktop(tela, "Pedro Produção").QuerySelector("input")!.HasAttribute("checked").Should().BeTrue();
        Enviar(tela);
        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/agenda/{EventoId}"));
        Enviado.Participantes.Should().Equal(AtorDaProducao.Id);
    }

    [Fact]
    public void Edicao_abre_com_inativos_e_sumidos_marcados_ninguem_travado_e_a_duracao_em_outra_duracao()
    {
        var tela = Edicao();

        // Desktop: caixas na ordem dos nomes, a nota do "você" e a do inativo; marcados os participantes atuais; nenhum travado.
        tela.FindAll("div.aj-so-desktop label.aj-opcao").Select(l => (
                Nome(l), l.QuerySelector(".aj-opcao__nota")?.TextContent,
                l.QuerySelector("input")!.HasAttribute("checked"), l.QuerySelector("input")!.HasAttribute("disabled")))
            .Should().Equal(
                ("Diana Diretora", "(você)", true, false), ("Iara Inativa", "(inativo)", true, false),
                ("Pedro Produção", (string?)null, false, false), ("Rafael Teles", null, false, false),
                ("Usuário não encontrado", null, true, false));
        // Tablet: chips com a sigla e o nome inteiro; o próprio usuário dourado (o avatar sem modificador).
        var chips = tela.FindAll("div.aj-so-tablet label.aj-chip--pessoa");
        chips.Select(c => (c.QuerySelector(".aj-avatar")!.TextContent, Nome(c))).Should().Equal(
            ("DD", "Diana Diretora (você)"), ("II", "Iara Inativa (inativo)"), ("PP", "Pedro Produção"), ("RT", "Rafael Teles"),
            ("?", "Usuário não encontrado"));
        chips[0].QuerySelector(".aj-avatar")!.ClassName.Should().Be("aj-avatar");
        chips[2].QuerySelector(".aj-avatar")!.ClassName.Should().Be($"aj-avatar {AvatarDePessoa.Classe("Pedro Produção")}");
        // Mobile: o primeiro nome; "Você" para o próprio usuário.
        tela.FindAll("div.aj-so-mobile label.aj-chip--pessoa").Select(Nome)
            .Should().Equal("Você", "Iara (inativo)", "Pedro", "Rafael", "Usuário não encontrado");
        // 45 minutos não é uma das quatro: as listas abrem em "Outra duração", com o número de minutos.
        tela.Find("div.aj-so-tablet select option[selected]").GetAttribute("value").Should().Be("outra");
        tela.Find("div.aj-so-mobile select option[selected]").GetAttribute("value").Should().Be("outra");
        OutraDuracao(tela).Should().ContainSingle().Which.QuerySelector("input")!.GetAttribute("value").Should().Be("45");
        Rotulo(tela, "Duração (minutos)").QuerySelector("input")!.GetAttribute("value").Should().Be("45");
        tela.Markup.Should().Contain("Participantes").And.NotContain("Quem cria o evento sempre participa.");
        tela.FindAll("div.aj-vinculo__escolhido .aj-vinculo__nome").Select(n => n.TextContent).Should().Equal(
            "Transportes Litoral Ltda.", "Litoral — reclamação trabalhista", "Minuta de acordo trabalhista");
    }

    [Fact]
    public void Outra_duracao_mostra_o_numero_de_minutos_e_escolher_uma_das_quatro_o_esconde()
    {
        var tela = Novo();
        OutraDuracao(tela).Should().BeEmpty();

        tela.Find("div.aj-so-tablet select").Change("outra");

        OutraDuracao(tela).Should().ContainSingle().Which.QuerySelector("input")!.GetAttribute("value").Should().Be("60");
        tela.Find("div.aj-so-tablet select option[selected]").GetAttribute("value").Should().Be("outra");
        OutraDuracao(tela)[0].QuerySelector("input")!.Change("45");
        // O número do desktop e a lista do mobile acompanham o mesmo estado.
        Rotulo(tela, "Duração (minutos)").QuerySelector("input")!.GetAttribute("value").Should().Be("45");
        tela.Find("div.aj-so-mobile select option[selected]").GetAttribute("value").Should().Be("outra");

        tela.Find("div.aj-so-mobile select").Change("90");

        OutraDuracao(tela).Should().BeEmpty();
        tela.Find("div.aj-so-tablet select option[selected]").GetAttribute("value").Should().Be("90");
        Digitar(tela, "Título", "Reunião de alinhamento");
        Enviar(tela);
        tela.WaitForAssertion(() => Enviado.DuracaoEstimadaMinutos.Should().Be(90));
    }

    [Fact]
    public void Novo_evento_salvo_envia_o_pedido_e_volta_a_agenda_de_origem_na_data_do_evento_com_o_recado()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/agenda/novo?visao=semana&data=2026-10-08");
        var sobreposicao = new SobreposicaoDeAgenda(
            Rafael, "Rafael Teles", Fuso.ParaUtc(new DateTime(2026, 10, 20, 14, 0, 0)), Fuso.ParaUtc(new DateTime(2026, 10, 20, 15, 0, 0)));
        Respostas().Responde<SalvarEvento, Resultado<EventoSalvo>>(Resultado<EventoSalvo>.Ok(new EventoSalvo(EventoId, [sobreposicao])));
        var tela = Desenhar<EventoFormulario>();
        tela.Find("a.aj-voltar").GetAttribute("href").Should().Be("/agenda?visao=semana&data=2026-10-08");
        tela.FindAll("a.aj-botao").Where(a => a.TextContent == "Cancelar").Should().HaveCount(2)
            .And.OnlyContain(a => a.GetAttribute("href") == "/agenda?visao=semana&data=2026-10-08");

        Digitar(tela, "Título", "Audiência de instrução");
        Ladrilho(tela, "Audiência").Change(true);
        tela.Find("div.aj-so-desktop select option[selected]").TextContent.Should().Be("Audiência");   // a lista do desktop acompanha
        Rotulo(tela, "Data").QuerySelector("input")!.Change("2026-10-20");
        Rotulo(tela, "Hora").QuerySelector("input")!.Change("14:30:00");
        tela.Find("div.aj-so-mobile select").Change("90");
        Digitar(tela, "Local", "Fórum Central");
        ChipDoTablet(tela, "Rafael Teles").QuerySelector("input")!.Change(true);
        Enviar(tela);

        tela.WaitForAssertion(() => Endereco.Should().EndWith("/agenda?visao=semana&data=2026-10-20"));
        var enviado = Enviado;
        enviado.Id.Should().BeNull();
        enviado.Titulo.Should().Be("Audiência de instrução");
        enviado.Tipo.Should().Be(TipoEvento.Audiencia);
        enviado.InicioEm.Should().Be(Fuso.ParaUtc(new DateTime(2026, 10, 20, 14, 30, 0)));
        enviado.DuracaoEstimadaMinutos.Should().Be(90);
        enviado.Local.Should().Be("Fórum Central");
        enviado.ClienteId.Should().BeNull();
        enviado.ProcessoId.Should().BeNull();
        enviado.AtividadeId.Should().BeNull();
        enviado.Participantes.Should().Equal(AtorDaDiretoria.Id, Rafael);   // quem cria, travado, e o marcado, na ordem dos nomes
        enviado.VersaoEsperada.Should().BeNull();
        var recado = Services.GetRequiredService<Recados>().Retirar()!;
        recado.Texto.Should().Be("Evento salvo.");
        recado.Tipo.Should().Be(TipoDeAviso.Sucesso);
        recado.Sobreposicoes.Should().Equal(sobreposicao);
    }

    [Fact]
    public void Novo_evento_aberto_sem_origem_volta_a_agenda_so_com_a_data_do_evento()
    {
        var tela = Novo();
        tela.Find("a.aj-voltar").GetAttribute("href").Should().Be("/agenda");
        Digitar(tela, "Título", "Prazo do recurso");
        Rotulo(tela, "Data").QuerySelector("input")!.Change("2026-11-03");

        Enviar(tela);

        tela.WaitForAssertion(() => Endereco.Should().EndWith("/agenda?data=2026-11-03"));
    }

    [Fact]
    public void Edicao_salva_envia_a_versao_e_volta_para_a_ficha_com_a_origem_e_o_recado()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/agenda/{EventoId}/editar?visao=dia&data=2026-10-07");
        var tela = Edicao(versao: 7);
        var ficha = $"/agenda/{EventoId}?visao=dia&data=2026-10-07";
        tela.Find("a.aj-voltar").GetAttribute("href").Should().Be(ficha);
        tela.Find("a.aj-voltar").TextContent.Should().Be("Audiência de conciliação");
        tela.FindAll("a.aj-botao").Where(a => a.TextContent == "Cancelar").Should().HaveCount(2).And.OnlyContain(a => a.GetAttribute("href") == ficha);

        Digitar(tela, "Título", "Audiência de conciliação remarcada");
        Enviar(tela);

        tela.WaitForAssertion(() => Endereco.Should().EndWith(ficha));
        var enviado = Enviado;
        enviado.Id.Should().Be(EventoId);
        enviado.Titulo.Should().Be("Audiência de conciliação remarcada");
        enviado.Tipo.Should().Be(TipoEvento.Audiencia);
        enviado.InicioEm.Should().Be(Fuso.ParaUtc(new DateTime(2026, 10, 7, 9, 0, 0)));
        enviado.DuracaoEstimadaMinutos.Should().Be(45);
        enviado.Local.Should().Be("Fórum Cível, sala 4");
        enviado.Descricao.Should().Be("Levar os documentos.");
        enviado.ClienteId.Should().Be(ClienteId);
        enviado.ProcessoId.Should().Be(ProcessoId);
        enviado.AtividadeId.Should().Be(AtividadeId);
        // Os participantes atuais, inclusive a inativa e o que não é mais encontrado, na ordem dos nomes.
        enviado.Participantes.Should().Equal(AtorDaDiretoria.Id, Iara, Sumido);
        enviado.VersaoEsperada.Should().Be(7u);
        var recado = Services.GetRequiredService<Recados>().Retirar()!;
        recado.Texto.Should().Be("Evento salvo.");
        recado.Sobreposicoes.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Clique_duplo_em_salvar_evento_envia_uma_vez_e_o_botao_fica_travado_ate_a_navegacao(bool edicao)
    {
        var resposta = new TaskCompletionSource<Resultado<EventoSalvo>>();
        var tela = edicao ? Edicao() : Novo();
        if (!edicao) Digitar(tela, "Título", "Reunião de alinhamento");
        Despachante.RespondeDepois<SalvarEvento, Resultado<EventoSalvo>>(resposta.Task);

        Enviar(tela);
        Enviar(tela);   // o segundo clique, com o primeiro envio em curso
        resposta.SetResult(Resultado<EventoSalvo>.Ok(new EventoSalvo(EventoId, [])));
        var destino = edicao ? $"/agenda/{EventoId}" : $"/agenda?data={Hoje}";
        tela.WaitForAssertion(() => Endereco.Should().EndWith(destino));
        Enviar(tela);   // um clique tardio, com a navegação a caminho

        Despachante.Recebidas.OfType<SalvarEvento>().Should().ContainSingle();
        tela.FindAll("button[form=formulario-do-evento]").Should().HaveCount(3).And.OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Conflito_na_edicao_mostra_o_aviso_e_mantem_o_formulario_com_o_digitado()
    {
        var tela = Edicao();
        Despachante.Responde<SalvarEvento, Resultado<EventoSalvo>>(Resultado<EventoSalvo>.Falha(
            TipoErro.Conflito, "O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        Digitar(tela, "Título", "Audiência remarcada");

        Enviar(tela);

        tela.WaitForAssertion(() => tela.Find("p.aj-aviso--erro").TextContent
            .Should().Be("O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        Rotulo(tela, "Título").QuerySelector("input")!.GetAttribute("value").Should().Be("Audiência remarcada");
        tela.FindAll("button[form=formulario-do-evento]").Should().HaveCount(3).And.OnlyContain(b => !b.HasAttribute("disabled"));
        Endereco.Should().NotContain("/agenda/");
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Producao_que_so_participa_ve_o_aviso_sem_formulario_e_sem_consultar_os_usuarios()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao)
            .Responde<ObterEvento, Resultado<EventoDetalhe>>(Resultado<EventoDetalhe>.Ok(Audiencia(gerencia: false)));

        var tela = Desenhar<EventoFormulario>(p => p.Add(x => x.Id, (Guid?)EventoId));

        tela.Find("p.aj-aviso--atencao").TextContent.Should().Be("Só a Diretoria e quem criou o evento podem editá-lo.");
        tela.Find("a.aj-voltar").GetAttribute("href").Should().Be($"/agenda/{EventoId}");
        tela.Find("a.aj-voltar").TextContent.Should().Be("Audiência de conciliação");
        tela.FindAll("form#formulario-do-evento").Should().BeEmpty();
        tela.FindAll("button[form=formulario-do-evento]").Should().BeEmpty();
        // Review Focus 4: sem a consulta dos usuários (nenhuma resposta combinada para ela; se a tela a fizesse, falharia).
        Despachante.Recebidas.Select(r => r.GetType()).Should().Equal(typeof(ObterMeuAcesso), typeof(ObterEvento));
    }

    [Fact]
    public void Falha_ao_consultar_os_usuarios_mostra_tentar_de_novo_e_o_clique_recarrega()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuariosParaAgenda, Resultado<IReadOnlyList<UsuarioDaAgenda>>>(
                Resultado<IReadOnlyList<UsuarioDaAgenda>>.Falha(TipoErro.Validacao, "Falha de teste."));
        var tela = Desenhar<EventoFormulario>();

        tela.Markup.Should().Contain("Falha de teste.");
        tela.FindAll("form#formulario-do-evento").Should().BeEmpty();

        Respostas();
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.Find("form#formulario-do-evento"));
        tela.Markup.Should().NotContain("Falha de teste.");
    }

    [Fact]
    public void Acesso_negado_na_consulta_dos_usuarios_encerra_a_tela()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuariosParaAgenda, Resultado<IReadOnlyList<UsuarioDaAgenda>>>(
                Resultado<IReadOnlyList<UsuarioDaAgenda>>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var tela = Desenhar<EventoFormulario>();

        tela.Find("p.aj-aviso--erro").TextContent.Should().Be("Acesso negado.");
        tela.FindAll("form#formulario-do-evento").Should().BeEmpty();
        tela.FindAll("button").Should().NotContain(b => b.TextContent == "Tentar de novo");
    }

    [Fact]
    public void Aviso_de_data_e_hora_se_revela_a_cada_envio()
    {
        var tela = Novo();
        Digitar(tela, "Título", "Reunião de alinhamento");
        Rotulo(tela, "Data").QuerySelector("input")!.Change("");

        Enviar(tela);

        var aviso = tela.Find("p.aj-aviso--erro");
        aviso.TextContent.Should().Be("Informe a data e a hora do evento.");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
        var antes = FocosDeElementoPedidos;

        Enviar(tela);

        FocosDeElementoPedidos.Should().Be(antes + 1);
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso--erro"));
        Despachante.Recebidas.OfType<SalvarEvento>().Should().BeEmpty();
    }

    [Fact]
    public void Grupo_de_participantes_em_erro_fica_invalido_e_aponta_para_o_erro_da_mesma_visao()
    {
        var tela = Edicao();
        foreach (var nome in new[] { "Diana Diretora", "Iara Inativa", "Usuário não encontrado" })
            OpcaoDoDesktop(tela, nome).QuerySelector("input")!.Change(false);

        Enviar(tela);

        var grupos = new[]
        {
            (Grupo: tela.Find("div.aj-so-desktop [aria-labelledby=titulo-dos-participantes]"), Visao: "desktop"),
            (Grupo: tela.Find("div.aj-so-tablet [aria-labelledby=titulo-dos-participantes]"), Visao: "tablet"),
            (Grupo: tela.FindAll("fieldset.aj-conjunto").Single(f => f.QuerySelector("legend")!.TextContent == "Participantes"), Visao: "mobile"),
        };
        foreach (var (grupo, visao) in grupos)
        {
            grupo.GetAttribute("aria-invalid").Should().Be("true", visao);
            grupo.GetAttribute("aria-describedby").Should().Be($"erro-dos-participantes-{visao}");
            tela.Find($"#erro-dos-participantes-{visao}").TextContent.Should().Be("Marque ao menos um participante.");
        }

        ChipDoTablet(tela, "Pedro Produção").QuerySelector("input")!.Change(true);

        tela.Find("div.aj-so-desktop [aria-labelledby=titulo-dos-participantes]").HasAttribute("aria-invalid").Should().BeFalse();
        tela.Find("div.aj-so-desktop [aria-labelledby=titulo-dos-participantes]").HasAttribute("aria-describedby").Should().BeFalse();
        tela.FindAll("[id^=erro-dos-participantes]").Should().BeEmpty();
        OpcaoDoDesktop(tela, "Pedro Produção").QuerySelector("input")!.HasAttribute("checked").Should().BeTrue();
    }
}
