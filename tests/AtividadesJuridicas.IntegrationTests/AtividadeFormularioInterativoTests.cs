using AngleSharp.Dom;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class AtividadeFormularioInterativoTests : TelaInterativa
{
    private static readonly Guid AtividadeId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1");
    private static readonly Guid Area = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a9");
    private static readonly Guid ProcessoId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");
    private static readonly Guid Litoral = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1");

    private static readonly ProcessoResumo Encontrado = new(
        ProcessoId, null, "Litoral — reclamação trabalhista", "Trabalhista", StatusProcesso.Ativo, ["Transportes Litoral Ltda."], null);

    private static readonly ProcessoDetalhe Processo = new(
        ProcessoId, null, "Litoral — reclamação trabalhista", Area, "Trabalhista", StatusProcesso.Ativo,
        null, null, null, null, null, null,
        [new ClienteDoProcessoDetalhe(Litoral, "Transportes Litoral Ltda.", "11.222.333/0001-81", PoloProcessual.Ativo, true)]);

    // Atividade sem processo, na área Trabalhista, com complexidade 2.
    private static AtividadeDetalhe Minuta(uint versao) => new(
        AtividadeId, "Minuta de acordo trabalhista", "Redigir a minuta.", new DateOnly(2026, 10, 9), StatusAtividade.Criada, false,
        null, null, null, null, null, Area, "Trabalhista", TipoAutor.Usuario, "Diana Diretora", null, null, [], [],
        new AcoesDaAtividade(true, true, true, false, false, false, true), versao, 2);

    // A Diretoria e as respostas de que o formulário precisa: as áreas, a busca e a leitura do processo e o salvar.
    private DespachanteCombinado Respostas() => Despachante
        .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
        .Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(
            Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Ok([new AreaAtuacaoResumo(Area, "Trabalhista", true)]))
        .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(Resultado<PaginaDeProcessos>.Ok(new([Encontrado], 1, 1, 20, 1)))
        .Responde<ObterProcesso, Resultado<ProcessoDetalhe>>(Resultado<ProcessoDetalhe>.Ok(Processo))
        .Responde<SalvarAtividade, Resultado<Guid>>(Resultado<Guid>.Ok(AtividadeId));

    private IRenderedComponent<AtividadeFormulario> Nova()
    {
        Respostas();
        return Desenhar<AtividadeFormulario>();
    }

    private IRenderedComponent<AtividadeFormulario> Edicao(uint versao = 7)
    {
        Respostas().Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(Resultado<AtividadeDetalhe>.Ok(Minuta(versao)));
        return Desenhar<AtividadeFormulario>(p => p.Add(x => x.Id, (Guid?)AtividadeId));
    }

    private static IElement Rotulo(IRenderedComponent<AtividadeFormulario> tela, string texto) =>
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.TrimStart().StartsWith(texto));

    private static void Digitar(IRenderedComponent<AtividadeFormulario> tela, string rotulo, string valor) =>
        Rotulo(tela, rotulo).QuerySelector("input, textarea")!.Change(valor);

    private static void Complexidade(IRenderedComponent<AtividadeFormulario> tela, string nivel) =>
        tela.FindAll("div.aj-segmentado button").Single(b => b.TextContent == nivel).Click();

    private static void ProcurarProcesso(IRenderedComponent<AtividadeFormulario> tela, string texto)
    {
        tela.Find("input[form=busca-de-processo]").Input(texto);
        tela.Find("form#busca-de-processo").Submit();
    }

    private string Endereco => Services.GetRequiredService<NavigationManager>().Uri;

    private SalvarAtividade Enviada => Despachante.Recebidas.OfType<SalvarAtividade>().Should().ContainSingle().Subject;

    [Fact]
    public void Enter_na_busca_de_processo_mostra_os_resultados_e_nao_salva()
    {
        var tela = Nova();

        ProcurarProcesso(tela, "litoral");

        Despachante.Recebidas.OfType<ListarProcessos>().Should().ContainSingle().Which.Busca.Should().Be("litoral");
        tela.Find("ul.aj-resultados .aj-resultados__titulo").TextContent.Should().Be("Litoral — reclamação trabalhista");
        tela.Find("ul.aj-resultados .aj-resultados__meta").TextContent.Should().Be("Trabalhista · Transportes Litoral Ltda.");
        tela.Find("ul.aj-resultados .aj-resultados__acao").TextContent.Should().Be("Escolher");
        Despachante.Recebidas.OfType<SalvarAtividade>().Should().BeEmpty();   // o Enter da busca não envia a atividade
    }

    [Fact]
    public void Escolher_o_processo_preenche_o_unico_cliente_e_a_area_vem_do_processo()
    {
        var tela = Nova();
        ProcurarProcesso(tela, "litoral");

        tela.Find("button.aj-resultados__item").Click();

        Despachante.Recebidas.OfType<ObterProcesso>().Should().ContainSingle().Which.Id.Should().Be(ProcessoId);
        tela.Find("div.aj-vinculo__escolhido .aj-vinculo__titulo").TextContent.Should().Be("Litoral — reclamação trabalhista");
        tela.Markup.Should().Contain("Área Trabalhista, definida pelo processo").And.Contain("O título aparece para o cliente no portal.");
        Rotulo(tela, "Cliente do processo").QuerySelector("option[selected]")!.TextContent.Should().Be("Transportes Litoral Ltda.");
        tela.FindAll("ul.aj-resultados").Should().BeEmpty();
        tela.FindAll("label.aj-rotulo").Should().NotContain(l => l.TextContent.StartsWith("Área de atuação"));
    }

    [Fact]
    public void Salvar_sem_titulo_descricao_nem_complexidade_mostra_os_erros_nos_campos_e_nao_envia()
    {
        var tela = Nova();

        tela.Find("form#formulario-da-atividade").Submit();

        tela.Markup.Should().Contain("Informe o título.").And.Contain("Informe a descrição.").And.Contain("Escolha a complexidade.");
        Rotulo(tela, "Título").ClassList.Should().Contain("aj-rotulo--erro");
        tela.Find("div.aj-rotulo.aj-rotulo--erro[role=group]").TextContent.Should().Contain("Escolha a complexidade.");
        Despachante.Recebidas.OfType<SalvarAtividade>().Should().BeEmpty();

        Complexidade(tela, "2");

        // Escolher o nível revalida o campo: o erro dele sai e os outros ficam.
        tela.Markup.Should().NotContain("Escolha a complexidade.").And.Contain("Informe o título.");
        tela.Find("div.aj-segmentado button[aria-pressed=true]").TextContent.Should().Be("2");
    }

    [Fact]
    public void Nova_atividade_salva_envia_os_campos_e_volta_para_a_lista_com_o_recado()
    {
        var tela = Nova();
        Digitar(tela, "Título", "Levantamento de certidões");
        Digitar(tela, "Descrição", "Pedir as certidões do imóvel.");
        Complexidade(tela, "3");
        Rotulo(tela, "Área de atuação").QuerySelector("select")!.Change(Area.ToString());

        tela.Find("form#formulario-da-atividade").Submit();

        tela.WaitForAssertion(() => Endereco.Should().EndWith("/atividades"));
        var enviada = Enviada;
        enviada.Id.Should().BeNull();
        enviada.Titulo.Should().Be("Levantamento de certidões");
        enviada.Descricao.Should().Be("Pedir as certidões do imóvel.");
        enviada.Complexidade.Should().Be(3);
        enviada.AreaAtuacaoId.Should().Be(Area);
        enviada.ClienteId.Should().BeNull();
        enviada.ProcessoId.Should().BeNull();
        enviada.VersaoEsperada.Should().BeNull();
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade salva.");
    }

    [Fact]
    public void Edicao_envia_a_versao_da_carga_e_volta_para_a_ficha_com_o_recado()
    {
        var tela = Edicao(versao: 7);
        Digitar(tela, "Título", "Minuta de acordo revisada");

        tela.Find("form#formulario-da-atividade").Submit();

        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/atividades/{AtividadeId}"));
        var enviada = Enviada;
        enviada.Id.Should().Be(AtividadeId);
        enviada.Titulo.Should().Be("Minuta de acordo revisada");
        enviada.DataEntrega.Should().Be(new DateOnly(2026, 10, 9));
        enviada.Complexidade.Should().Be(2);
        enviada.AreaAtuacaoId.Should().Be(Area);
        enviada.VersaoEsperada.Should().Be(7u);
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade salva.");
    }

    [Fact]
    public void Falha_ao_salvar_mostra_o_aviso_e_mantem_o_que_foi_digitado()
    {
        var tela = Edicao();
        Despachante.Responde<SalvarAtividade, Resultado<Guid>>(Resultado<Guid>.Falha(
            TipoErro.Conflito, "A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));
        Digitar(tela, "Título", "Minuta de acordo revisada");

        tela.Find("form#formulario-da-atividade").Submit();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("A atividade foi alterada por outra pessoa."));
        Rotulo(tela, "Título").QuerySelector("input")!.GetAttribute("value").Should().Be("Minuta de acordo revisada");
        tela.FindAll("button[form=formulario-da-atividade]").Should().HaveCount(3).And.OnlyContain(b => !b.HasAttribute("disabled"));
        Endereco.Should().NotEndWith($"/atividades/{AtividadeId}");
    }

    [Fact]
    public void Clique_duplo_em_salvar_envia_uma_vez_e_o_botao_fica_travado_ate_a_navegacao()
    {
        var resposta = new TaskCompletionSource<Resultado<Guid>>();
        var tela = Edicao();
        Despachante.RespondeDepois<SalvarAtividade, Resultado<Guid>>(resposta.Task);

        tela.Find("form#formulario-da-atividade").Submit();
        tela.Find("form#formulario-da-atividade").Submit();   // o segundo clique, com o primeiro envio em curso
        resposta.SetResult(Resultado<Guid>.Ok(AtividadeId));
        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/atividades/{AtividadeId}"));
        tela.Find("form#formulario-da-atividade").Submit();   // um clique tardio, com a navegação a caminho

        Despachante.Recebidas.OfType<SalvarAtividade>().Should().ContainSingle();
        tela.FindAll("button[form=formulario-da-atividade]").Should().HaveCount(3).And.OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Falha_ao_consultar_a_atividade_mostra_tentar_de_novo_e_o_clique_recarrega()
    {
        Respostas().Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
            Resultado<AtividadeDetalhe>.Falha(TipoErro.Validacao, "Falha de teste."));
        var tela = Desenhar<AtividadeFormulario>(p => p.Add(x => x.Id, (Guid?)AtividadeId));

        tela.Markup.Should().Contain("Falha de teste.");
        tela.FindAll("form#formulario-da-atividade").Should().BeEmpty();

        Despachante.Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(Resultado<AtividadeDetalhe>.Ok(Minuta(7)));
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.Find("form#formulario-da-atividade"));
        Rotulo(tela, "Título").QuerySelector("input")!.GetAttribute("value").Should().Be("Minuta de acordo trabalhista");
        tela.Markup.Should().NotContain("Falha de teste.");
    }

    [Fact]
    public void Producao_ve_acesso_negado_sem_consultar_nada_alem_do_proprio_acesso()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao);

        var tela = Desenhar<AtividadeFormulario>(p => p.Add(x => x.Id, (Guid?)AtividadeId));

        tela.Markup.Should().Contain("Acesso negado.").And.Contain("Editar atividade");
        tela.FindAll("form#formulario-da-atividade").Should().BeEmpty();
        Despachante.Recebidas.Should().ContainSingle().Which.Should().BeOfType<ObterMeuAcesso>();
    }
}
