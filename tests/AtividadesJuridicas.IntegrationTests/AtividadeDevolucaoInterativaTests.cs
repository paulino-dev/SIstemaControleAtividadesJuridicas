using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class AtividadeDevolucaoInterativaTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a2");
    private static readonly Guid Beatriz = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b001");
    private static readonly Guid Otavio = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b005");
    private static readonly DateTime Concluida = new(2026, 10, 8, 12, 15, 0, DateTimeKind.Utc);

    // Editar, DefinirResponsaveis, TrocarRevisor, ConcluirMinhaParte, AprovarOuDevolver, Reabrir, Excluir.
    private static readonly AcoesDaAtividade DaFeita = new(true, false, true, false, true, false, true);
    private static readonly AcoesDaAtividade DaConcluida = new(true, false, false, false, false, true, true);

    private static AtividadeDetalhe Atividade(StatusAtividade status, AcoesDaAtividade acoes) => new(
        Id, "Minuta de acordo trabalhista", "Redigir a minuta.", new DateOnly(2026, 10, 9), status, false,
        status == StatusAtividade.Concluida ? Concluida : null, null, null, null, null, null, null,
        TipoAutor.Usuario, "Diana Diretora", AtorDaDiretoria.Id, "Diana Diretora",
        [
            new ResponsavelDetalhe(Beatriz, "Beatriz Lins", true, null, Concluida),
            new ResponsavelDetalhe(Otavio, "Otávio Brandão", true, null, Concluida),
        ],
        [], acoes, 7u, 2);

    private IRenderedComponent<AtividadeDevolver> Devolver(AcoesDaAtividade? acoes = null)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
                Resultado<AtividadeDetalhe>.Ok(Atividade(StatusAtividade.Feita, acoes ?? DaFeita)))
            .Responde<DevolverAtividade, Resultado>(Resultado.Ok());
        return Desenhar<AtividadeDevolver>(p => p.Add(t => t.Id, Id));
    }

    private static void Escrever<T>(IRenderedComponent<T> tela, string motivo) where T : IComponent =>
        tela.Find("textarea.aj-campo").Change(motivo);

    private static void Marcar<T>(IRenderedComponent<T> tela, string nome, bool marcado) where T : IComponent =>
        tela.FindAll("fieldset.aj-conjunto label.aj-opcao").Single(l => l.TextContent == nome).QuerySelector("input")!.Change(marcado);

    private string Destino => Services.GetRequiredService<NavigationManager>().Uri;

    [Fact]
    public void Abre_com_o_titulo_da_atividade_e_todos_os_responsaveis_marcados()
    {
        var tela = Devolver();

        tela.Find("h1.aj-titulo").TextContent.Should().Be("Devolver para correção");
        tela.FindAll("fieldset.aj-conjunto label.aj-opcao").Select(l => l.TextContent).Should().Equal("Beatriz Lins", "Otávio Brandão");
        tela.FindAll("fieldset.aj-conjunto input").Should().HaveCount(2).And.OnlyContain(c => c.HasAttribute("checked"));
    }

    [Fact]
    public void Sem_motivo_mostra_o_erro_no_campo_e_nao_envia()
    {
        var tela = Devolver();
        Escrever(tela, "   ");

        tela.Find("form#formulario-do-motivo").Submit();

        tela.Markup.Should().Contain("Informe o motivo.").And.Contain("aj-rotulo aj-rotulo--erro")
            .And.NotContain("Marque ao menos um responsável.");
        Despachante.Recebidas.OfType<DevolverAtividade>().Should().BeEmpty();
    }

    [Fact]
    public void Sem_responsavel_marcado_mostra_o_erro_no_conjunto_e_nao_envia()
    {
        var tela = Devolver();
        Escrever(tela, "Rever a cláusula de quitação.");
        Marcar(tela, "Beatriz Lins", false);
        Marcar(tela, "Otávio Brandão", false);

        tela.Find("form#formulario-do-motivo").Submit();

        tela.Find("fieldset.aj-conjunto .aj-campo-erro").TextContent.Should().Be("Marque ao menos um responsável.");
        tela.Find("fieldset.aj-conjunto legend").ClassName.Should().Be("aj-rotulo aj-rotulo--erro");
        tela.Markup.Should().NotContain("Informe o motivo.");
        Despachante.Recebidas.OfType<DevolverAtividade>().Should().BeEmpty();
    }

    [Fact]
    public void Desmarcar_um_envia_so_o_outro_com_o_motivo_aparado_e_volta_a_ficha_com_o_recado()
    {
        var tela = Devolver();
        Escrever(tela, "  A cláusula de quitação precisa citar o período do contrato.  ");
        Marcar(tela, "Otávio Brandão", false);

        tela.Find("form#formulario-do-motivo").Submit();

        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        var enviado = Despachante.Recebidas.OfType<DevolverAtividade>().Should().ContainSingle().Subject;
        enviado.Id.Should().Be(Id);
        enviado.Motivo.Should().Be("A cláusula de quitação precisa citar o período do contrato.");
        enviado.Responsaveis.Should().Equal(Beatriz);
        Despachante.Recebidas.OfType<ReabrirAtividade>().Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade devolvida para correção.");
    }

    [Fact]
    public void Reabrir_e_a_mesma_tela_e_envia_a_reabertura_com_o_recado()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
                Resultado<AtividadeDetalhe>.Ok(Atividade(StatusAtividade.Concluida, DaConcluida)))
            .Responde<ReabrirAtividade, Resultado>(Resultado.Ok());
        var tela = Desenhar<AtividadeReabrir>(p => p.Add(t => t.Id, Id));

        tela.Find("h1.aj-titulo").TextContent.Should().Be("Reabrir atividade");
        tela.FindAll("button[type=submit]").Should().HaveCount(3).And.OnlyContain(b => b.TextContent == "Reabrir");
        Escrever(tela, "O cliente pediu uma nova versão.");
        tela.Find("form#formulario-do-motivo").Submit();

        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        var enviado = Despachante.Recebidas.OfType<ReabrirAtividade>().Should().ContainSingle().Subject;
        enviado.Motivo.Should().Be("O cliente pediu uma nova versão.");
        enviado.Responsaveis.Should().Equal(Beatriz, Otavio);
        Despachante.Recebidas.OfType<DevolverAtividade>().Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade reaberta.");
    }

    [Fact]
    public void Sem_a_permissao_avisa_e_nao_mostra_o_formulario()
    {
        // Devolver uma atividade que só permite reabrir.
        var tela = Devolver(DaConcluida);

        tela.Markup.Should().Contain("Esta atividade não permite mais essa ação.");
        tela.FindAll("form#formulario-do-motivo").Should().BeEmpty();
        tela.FindAll("button[type=submit]").Should().BeEmpty();
    }

    [Fact]
    public void Falha_ao_devolver_mostra_o_aviso_e_mantem_o_que_foi_digitado()
    {
        var tela = Devolver();
        Despachante.Responde<DevolverAtividade, Resultado>(
            Resultado.Falha(TipoErro.Validacao, "Somente o revisor da atividade pode aprová-la ou devolvê-la."));
        Escrever(tela, "Rever o valor da terceira parcela.");
        Marcar(tela, "Otávio Brandão", false);

        tela.Find("form#formulario-do-motivo").Submit();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Somente o revisor da atividade pode aprová-la ou devolvê-la."));
        tela.Markup.Should().Contain("Rever o valor da terceira parcela.");
        tela.FindAll("fieldset.aj-conjunto label.aj-opcao").Single(l => l.TextContent == "Otávio Brandão")
            .QuerySelector("input")!.HasAttribute("checked").Should().BeFalse();
        Destino.Should().NotEndWith($"/atividades/{Id}");
    }

    [Fact]
    public void Depois_de_devolver_com_sucesso_um_novo_envio_nao_dispara_outra_devolucao()
    {
        var resposta = new TaskCompletionSource<Resultado>();
        var tela = Devolver();
        Despachante.RespondeDepois<DevolverAtividade, Resultado>(resposta.Task);
        Escrever(tela, "Rever a cláusula de quitação.");

        tela.Find("form#formulario-do-motivo").Submit();
        resposta.SetResult(Resultado.Ok());
        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        tela.Find("form#formulario-do-motivo").Submit();

        Despachante.Recebidas.OfType<DevolverAtividade>().Should().ContainSingle();
        tela.FindAll("button[type=submit]").Should().HaveCount(3).And.OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Producao_ve_acesso_negado_sem_consultar_a_atividade()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao);

        var tela = Desenhar<AtividadeDevolver>(p => p.Add(t => t.Id, Id));

        tela.Markup.Should().Contain("Acesso negado.");
        tela.FindAll("form#formulario-do-motivo").Should().BeEmpty();
        Despachante.Recebidas.Should().ContainSingle().Which.Should().BeOfType<ObterMeuAcesso>();
    }

    [Fact]
    public void Devolver_aberta_de_produtores_envia_e_volta_para_a_ficha_com_a_origem()
    {
        var produtor = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000d1");
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/atividades/{Id}/devolver?de=produtores&produtor={produtor}");
        var tela = Devolver();
        var ficha = $"/atividades/{Id}?de=produtores&produtor={produtor}";
        tela.FindAll("a.aj-botao").Where(a => a.TextContent == "Cancelar").Should().HaveCount(3).And.OnlyContain(a => a.GetAttribute("href") == ficha);
        Escrever(tela, "Conferir a cláusula de quitação.");

        tela.Find("form#formulario-do-motivo").Submit();

        tela.WaitForAssertion(() => Destino.Should().EndWith(ficha));
        Despachante.Recebidas.OfType<DevolverAtividade>().Should().ContainSingle();
    }

    [Fact]
    public void Falha_ao_devolver_leva_o_foco_ao_aviso_de_erro()
    {
        var tela = Devolver();
        Despachante.Responde<DevolverAtividade, Resultado>(
            Resultado.Falha(TipoErro.Validacao, "Somente o revisor da atividade pode aprová-la ou devolvê-la."));
        Escrever(tela, "Rever o valor da terceira parcela.");

        tela.Find("form#formulario-do-motivo").Submit();

        tela.WaitForAssertion(() => tela.Find("p.aj-aviso--erro").TextContent.Should().Contain("Somente o revisor"));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso--erro"));
    }

    [Fact]
    public void Responsaveis_em_erro_ficam_invalidos_e_apontam_para_o_erro()
    {
        var tela = Devolver();
        Escrever(tela, "Rever a cláusula de quitação.");
        Marcar(tela, "Beatriz Lins", false);
        Marcar(tela, "Otávio Brandão", false);

        tela.Find("form#formulario-do-motivo").Submit();

        var conjunto = tela.Find("fieldset.aj-conjunto");
        conjunto.GetAttribute("aria-invalid").Should().Be("true");
        conjunto.GetAttribute("aria-describedby").Should().Be("erro-dos-responsaveis");
        tela.Find("#erro-dos-responsaveis").TextContent.Should().Be("Marque ao menos um responsável.");
        Despachante.Recebidas.OfType<DevolverAtividade>().Should().BeEmpty();
    }
}
