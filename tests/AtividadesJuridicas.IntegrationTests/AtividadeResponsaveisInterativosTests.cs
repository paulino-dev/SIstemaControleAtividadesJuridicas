using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class AtividadeResponsaveisInterativosTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a1");
    private static readonly Guid Beatriz = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b001");
    private static readonly Guid Camila = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b002");
    private static readonly Guid Helena = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b003");
    private static readonly Guid Marina = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b004");
    private static readonly Guid Otavio = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b005");
    private static readonly Guid Ricardo = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b006");
    private static readonly Guid Davi = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b007");

    // Editar, DefinirResponsaveis, TrocarRevisor, ConcluirMinhaParte, AprovarOuDevolver, Reabrir, Excluir.
    private static readonly AcoesDaAtividade Completa = new(true, true, true, false, false, false, true);
    private static readonly AcoesDaAtividade SoRevisor = new(true, false, true, false, false, false, true);
    private static readonly AcoesDaAtividade Nenhuma = new(true, false, false, false, false, false, true);

    private static UsuarioResumo Usuario(Guid id, string nome, TipoPerfil tipo, bool ativo = true) =>
        new(id, nome, $"{id:N}@x.com", Guid.Empty, tipo == TipoPerfil.Diretoria ? "Diretoria" : "Produção", tipo, null, ativo, null);

    // O Otávio é responsável e está inativo; o Ricardo é responsável e não vem na lista de usuários; o Davi é um
    // diretor inativo (não entra como revisor).
    private static readonly IReadOnlyList<UsuarioResumo> Usuarios =
    [
        Usuario(Marina, "Marina Couto", TipoPerfil.Diretoria),
        Usuario(Beatriz, "Beatriz Lins", TipoPerfil.Producao),
        Usuario(Otavio, "Otávio Brandão", TipoPerfil.Producao, ativo: false),
        Usuario(Camila, "Camila Prado", TipoPerfil.Producao),
        Usuario(Helena, "Helena Rocha", TipoPerfil.Diretoria),
        Usuario(Davi, "Davi Duarte", TipoPerfil.Diretoria, ativo: false),
    ];

    private static AtividadeDetalhe Atividade(AcoesDaAtividade acoes, Guid revisorId, string revisorNome) => new(
        Id, "Minuta de acordo trabalhista", "Redigir a minuta.", new DateOnly(2026, 10, 9), StatusAtividade.Direcionada, false,
        null, null, null, null, null, null, null, TipoAutor.Usuario, "Marina Couto", revisorId, revisorNome,
        [
            new ResponsavelDetalhe(Beatriz, "Beatriz Lins", true, "Redigir a minuta com as condições discutidas na audiência.", null),
            new ResponsavelDetalhe(Otavio, "Otávio Brandão", true, null, null),
            new ResponsavelDetalhe(Ricardo, "Ricardo Lemos", true, null, null),
        ],
        [], acoes, 7u, 2);

    private IRenderedComponent<AtividadeResponsaveis> Tela(
        AcoesDaAtividade acoes, Guid? revisorId = null, string revisorNome = "Marina Couto")
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
                Resultado<AtividadeDetalhe>.Ok(Atividade(acoes, revisorId ?? Marina, revisorNome)))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok(Usuarios))
            .Responde<DefinirResponsaveisDaAtividade, Resultado>(Resultado.Ok())
            .Responde<TrocarRevisorDaAtividade, Resultado>(Resultado.Ok());
        return Desenhar<AtividadeResponsaveis>(p => p.Add(t => t.Id, Id));
    }

    private static void Marcar(IRenderedComponent<AtividadeResponsaveis> tela, string nome, bool marcado) =>
        tela.FindAll("label.aj-opcao").Single(l => l.TextContent.StartsWith(nome)).QuerySelector("input")!.Change(marcado);

    private static void Direcionar(IRenderedComponent<AtividadeResponsaveis> tela, string nome, string texto) =>
        tela.FindAll("label.aj-rotulo--recuado").Single(l => l.TextContent.StartsWith($"Direcionamento de {nome}"))
            .QuerySelector("textarea")!.Change(texto);

    private string Destino => Services.GetRequiredService<NavigationManager>().Uri;

    [Fact]
    public void Opcoes_trazem_o_perfil_e_os_responsaveis_que_sairam_da_lista_marcados()
    {
        var tela = Tela(Completa);

        // Ativos que não são de cliente, por nome; depois os responsáveis de hoje que não entraram, marcados.
        tela.FindAll("label.aj-opcao").Select(l => l.TextContent).Should().Equal(
            "Beatriz Lins(Produção)", "Camila Prado(Produção)", "Helena Rocha(Diretoria)", "Marina Couto(Diretoria)",
            "Otávio Brandão(inativo)", "Ricardo Lemos(usuário indisponível)");
        tela.FindAll("label.aj-opcao").Where(l => l.QuerySelector("input")!.HasAttribute("checked")).Select(l => l.TextContent)
            .Should().Equal("Beatriz Lins(Produção)", "Otávio Brandão(inativo)", "Ricardo Lemos(usuário indisponível)");
        tela.FindAll("label.aj-rotulo--recuado").Should().HaveCount(3);
        tela.Markup.Should().Contain("Direcionamento de Beatriz Lins").And.Contain("Direcionamento de Ricardo Lemos")
            .And.NotContain("Direcionamento de Camila Prado");
        // O revisor começa em "manter / deduzir", com o atual no rótulo; só os diretores ativos são opção.
        tela.Find("select.aj-campo option[selected]").TextContent.Should().Be("Manter / deduzir (atual: Marina Couto)");
        tela.FindAll("select.aj-campo option").Select(o => o.TextContent).Should().Equal(
            "Manter / deduzir (atual: Marina Couto)", "Helena Rocha", "Marina Couto");
    }

    [Fact]
    public void Marcar_mostra_o_direcionamento_e_salvar_envia_os_marcados_aparados_com_o_revisor_e_a_versao()
    {
        var tela = Tela(Completa);

        Marcar(tela, "Camila Prado", true);
        tela.Markup.Should().Contain("Direcionamento de Camila Prado");
        Direcionar(tela, "Camila Prado", "  Conferir os cálculos das verbas rescisórias.  ");
        Marcar(tela, "Otávio Brandão", false);
        tela.Markup.Should().NotContain("Direcionamento de Otávio Brandão").And.Contain("Direcionamento de Beatriz Lins");
        tela.Find("select.aj-campo").Change(Helena.ToString());
        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        var enviado = Despachante.Recebidas.OfType<DefinirResponsaveisDaAtividade>().Should().ContainSingle().Subject;
        enviado.Id.Should().Be(Id);
        enviado.Responsaveis.Should().Equal(
            new ResponsavelEntrada(Beatriz, "Redigir a minuta com as condições discutidas na audiência."),
            new ResponsavelEntrada(Camila, "Conferir os cálculos das verbas rescisórias."),
            new ResponsavelEntrada(Ricardo, null));
        enviado.RevisorId.Should().Be(Helena);
        enviado.VersaoEsperada.Should().Be(7u);
        Despachante.Recebidas.OfType<TrocarRevisorDaAtividade>().Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Responsáveis salvos.");
    }

    [Fact]
    public void Manter_o_revisor_envia_revisor_nulo()
    {
        var tela = Tela(Completa);

        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<DefinirResponsaveisDaAtividade>().Should().ContainSingle()
            .Which.RevisorId.Should().BeNull());
    }

    [Fact]
    public void Modo_do_revisor_mostra_so_o_revisor_com_o_atual_escolhido_e_troca_com_a_versao()
    {
        var tela = Tela(SoRevisor);

        tela.Find("h1.aj-titulo").TextContent.Should().Be("Trocar revisor");
        tela.Find("section.aj-painel h2").TextContent.Should().Be("Revisor");
        tela.FindAll("label.aj-opcao").Should().BeEmpty();
        tela.Markup.Should().NotContain("Salvar responsáveis").And.NotContain("Manter / deduzir").And.Contain("Trocar revisor</button>");
        tela.Find("select.aj-campo option[selected]").TextContent.Should().Be("Marina Couto");
        tela.FindAll("select.aj-campo option").Select(o => o.TextContent).Should().Equal("Selecione…", "Helena Rocha", "Marina Couto");

        tela.Find("select.aj-campo").Change(Helena.ToString());
        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        Despachante.Recebidas.OfType<TrocarRevisorDaAtividade>().Should().ContainSingle()
            .Which.Should().Be(new TrocarRevisorDaAtividade(Id, Helena, 7u));
        Despachante.Recebidas.OfType<DefinirResponsaveisDaAtividade>().Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Revisor alterado.");
    }

    [Fact]
    public void Modo_do_revisor_sem_escolha_mostra_o_erro_no_campo_e_nao_envia()
    {
        // O revisor de hoje é um diretor inativo: não é opção, e o seletor começa em "Selecione…".
        var tela = Tela(SoRevisor, Davi, "Davi Duarte");
        tela.Find("select.aj-campo option[selected]").TextContent.Should().Be("Selecione…");

        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.Markup.Should().Contain("Escolha o novo revisor.").And.Contain("aj-rotulo aj-rotulo--erro");
        Despachante.Recebidas.OfType<TrocarRevisorDaAtividade>().Should().BeEmpty();
    }

    [Fact]
    public void Sem_nenhuma_das_permissoes_avisa_sem_formulario_e_sem_consultar_os_usuarios()
    {
        var tela = Tela(Nenhuma);

        tela.Markup.Should().Contain("Esta atividade não permite mais essa ação.");
        tela.FindAll("form#formulario-de-responsaveis").Should().BeEmpty();
        tela.Markup.Should().NotContain("Salvar responsáveis");
        Despachante.Recebidas.OfType<ListarUsuarios>().Should().BeEmpty();
    }

    [Fact]
    public void Producao_ve_acesso_negado_sem_consultar_a_atividade()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao);

        var tela = Desenhar<AtividadeResponsaveis>(p => p.Add(t => t.Id, Id));

        tela.Markup.Should().Contain("Acesso negado.");
        tela.FindAll("form#formulario-de-responsaveis").Should().BeEmpty();
        Despachante.Recebidas.Should().ContainSingle().Which.Should().BeOfType<ObterMeuAcesso>();
    }

    [Fact]
    public void Falha_da_consulta_dos_usuarios_oferece_tentar_de_novo_que_repete_a_carga_inteira()
    {
        var chamadas = 0;
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
                Resultado<AtividadeDetalhe>.Ok(Atividade(Completa, Marina, "Marina Couto")))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(_ => ++chamadas == 1
                ? Resultado<IReadOnlyList<UsuarioResumo>>.Falha(TipoErro.Validacao, "Não foi possível consultar os usuários.")
                : Resultado<IReadOnlyList<UsuarioResumo>>.Ok(Usuarios));
        var tela = Desenhar<AtividadeResponsaveis>(p => p.Add(t => t.Id, Id));

        tela.Markup.Should().Contain("Não foi possível consultar os usuários.");
        tela.FindAll("form#formulario-de-responsaveis").Should().BeEmpty();
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.FindAll("label.aj-opcao").Should().NotBeEmpty());
        tela.Markup.Should().NotContain("Não foi possível consultar os usuários.");
        // "Tentar de novo" repete a carga inteira: o perfil e a atividade também.
        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().HaveCount(2);
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
    }

    [Fact]
    public void Falha_ao_salvar_mostra_o_aviso_e_mantem_o_que_foi_marcado()
    {
        var tela = Tela(Completa);
        Despachante.Responde<DefinirResponsaveisDaAtividade, Resultado>(
            Resultado.Falha(TipoErro.Conflito, "A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));
        Marcar(tela, "Camila Prado", true);

        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));
        tela.FindAll("label.aj-opcao").Single(l => l.TextContent.StartsWith("Camila Prado"))
            .QuerySelector("input")!.HasAttribute("checked").Should().BeTrue();
        Destino.Should().NotEndWith($"/atividades/{Id}");
    }

    [Fact]
    public void Depois_de_salvar_com_sucesso_um_novo_envio_nao_dispara_outro_salvamento()
    {
        var resposta = new TaskCompletionSource<Resultado>();
        var tela = Tela(Completa);
        Despachante.RespondeDepois<DefinirResponsaveisDaAtividade, Resultado>(resposta.Task);

        tela.Find("form#formulario-de-responsaveis").Submit();
        resposta.SetResult(Resultado.Ok());
        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        tela.Find("form#formulario-de-responsaveis").Submit();

        Despachante.Recebidas.OfType<DefinirResponsaveisDaAtividade>().Should().ContainSingle();
        tela.FindAll("button[type=submit]").Should().HaveCount(3).And.OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Aberta_de_produtores_salva_e_volta_para_a_ficha_com_a_origem()
    {
        var produtor = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000d1");
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/atividades/{Id}/responsaveis?de=produtores&produtor={produtor}");
        var tela = Tela(Completa);
        var ficha = $"/atividades/{Id}?de=produtores&produtor={produtor}";
        tela.FindAll("a.aj-botao").Where(a => a.TextContent == "Cancelar").Should().HaveCount(3).And.OnlyContain(a => a.GetAttribute("href") == ficha);

        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.WaitForAssertion(() => Destino.Should().EndWith(ficha));
    }
}
