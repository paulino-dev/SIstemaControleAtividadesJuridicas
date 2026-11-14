using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Application.Solicitacoes;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Solicitacoes;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O que o cliente e a Diretoria fazem nas telas das solicitações, clicando: abrir com arquivo, cancelar com a
/// confirmação na página, a ficha de quem não abriu e o filtro da lista da Diretoria.
/// </summary>
public class SolicitacoesInterativasTests : TelaInterativa
{
    private static readonly Guid SolicitacaoId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000005a1");
    private static readonly Guid DocumentoId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000005d1");
    private static readonly Guid MariaId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000005c1");
    private static readonly DateTime AbertaEm = new(2026, 11, 10, 15, 30, 0, DateTimeKind.Utc);

    private const string Confirmacao = "Cancelar esta solicitação? Ela não poderá ser reaberta.";

    private string Endereco => Services.GetRequiredService<NavigationManager>().Uri;

    private static MinhaSolicitacaoDetalhe Detalhe(bool podeAlterar, uint versao = 4) => new(
        SolicitacaoId, AbertaEm, "Preciso de uma certidão atualizada.", UrgenciaDaSolicitacao.Alta,
        SituacaoParaOCliente.AguardandoAnalise, "Carla Cliente", podeAlterar,
        [new DocumentoDoPortal(DocumentoId, "rg.pdf", 2048, AbertaEm)], null, null, versao);

    private IRenderedComponent<MinhaSolicitacaoFicha> Ficha(bool podeAlterar, string consulta = "")
    {
        Despachante.Responde<ObterMinhaSolicitacao, Resultado<MinhaSolicitacaoDetalhe>>(
            Resultado<MinhaSolicitacaoDetalhe>.Ok(Detalhe(podeAlterar)));
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/minhas-solicitacoes/{SolicitacaoId}{consulta}");
        return Desenhar<MinhaSolicitacaoFicha>(p => p.Add(x => x.Id, SolicitacaoId));
    }

    [Fact]
    public void Cliente_abre_solicitacao_com_arquivo_e_cai_na_ficha_com_o_aviso()
    {
        List<string> nomes = [];
        Despachante.Responde<AbrirSolicitacao, Resultado<Guid>>(abrir =>
        {
            // Os arquivos são lidos enquanto o envio dura: o teste guarda os nomes antes de os fluxos se fecharem.
            nomes.AddRange(abrir.Arquivos.Select(a => a.NomeArquivo));
            return Resultado<Guid>.Ok(SolicitacaoId);
        });
        var tela = Desenhar<MinhaSolicitacaoFormulario>();

        tela.Find("textarea").Change("Preciso de uma certidão atualizada.");
        tela.FindAll("div.aj-segmentado button").Single(b => b.TextContent == "Alta").Click();
        tela.FindComponents<InputFile>()[^1].Instance.Should().NotBeNull();
        tela.FindComponents<InputFile>()[^1].UploadFiles(
            InputFileContent.CreateFromText("conteúdo", "rg.pdf"), InputFileContent.CreateFromText("outro", "cpf.pdf"));

        // A lista dos escolhidos, com "Remover" em cada um: remover tira só aquele.
        tela.FindAll(".aj-linha .aj-linha__titulo").Select(t => t.TextContent).Should().Equal("rg.pdf", "cpf.pdf");
        tela.FindAll(".aj-linha button").Should().OnlyContain(b => b.TextContent == "Remover");
        tela.FindAll(".aj-linha")[1].QuerySelector("button")!.Click();
        tela.FindAll(".aj-linha .aj-linha__titulo").Select(t => t.TextContent).Should().Equal("rg.pdf");

        tela.Find("form#formulario-da-solicitacao").Submit();

        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/minhas-solicitacoes/{SolicitacaoId}?enviada=1"));
        var enviada = Despachante.Recebidas.OfType<AbrirSolicitacao>().Should().ContainSingle().Subject;
        enviada.Descricao.Should().Be("Preciso de uma certidão atualizada.");
        enviada.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        nomes.Should().Equal("rg.pdf");

        // A ficha, aberta com ?enviada=1, avisa que a solicitação foi enviada.
        var ficha = Ficha(podeAlterar: true, "?enviada=1");
        ficha.Markup.Should().Contain("Solicitação enviada. A equipe verifica sua solicitação em até 24 horas úteis.");
    }

    [Fact]
    public void Enviar_sem_descricao_nem_urgencia_mostra_os_erros_e_nao_envia()
    {
        var tela = Desenhar<MinhaSolicitacaoFormulario>();

        tela.Find("form#formulario-da-solicitacao").Submit();

        tela.Markup.Should().Contain("A descrição da solicitação é obrigatória.").And.Contain("Escolha a urgência.");
        Despachante.Recebidas.OfType<AbrirSolicitacao>().Should().BeEmpty();
    }

    [Fact]
    public void Mais_de_dez_arquivos_sao_recusados_na_tela()
    {
        var tela = Desenhar<MinhaSolicitacaoFormulario>();

        tela.FindComponents<InputFile>()[^1].UploadFiles(Enumerable.Range(1, 11)
            .Select(n => InputFileContent.CreateFromText("conteúdo", $"arquivo-{n}.pdf")).ToArray());

        tela.Markup.Should().Contain("Anexe até 10 documentos por solicitação.");
        tela.FindAll(".aj-linha").Should().BeEmpty();
    }

    private static InputFileContent[] Arquivos(int quantos, string prefixo) =>
        Enumerable.Range(1, quantos).Select(n => InputFileContent.CreateFromText("conteúdo", $"{prefixo}-{n}.pdf")).ToArray();

    private static string[] Escolhidos(IRenderedComponent<MinhaSolicitacaoFormulario> tela) =>
        tela.FindAll(".aj-linha .aj-linha__titulo").Select(t => t.TextContent).ToArray();

    [Fact]
    public void Arquivo_acima_de_50_mb_e_recusado_na_tela_e_os_demais_da_escolha_ficam()
    {
        var grande = InputFileContent.CreateFromBinary(new byte[DocumentosOpcoes.TamanhoMaximoBytes + 1], "grande.pdf");
        var tela = Desenhar<MinhaSolicitacaoFormulario>();

        // Sozinho, é recusado e a lista continua vazia.
        tela.FindComponents<InputFile>()[^1].UploadFiles(grande);

        tela.Markup.Should().Contain("O arquivo grande.pdf passa do limite de 50 MB.");
        Escolhidos(tela).Should().BeEmpty();

        // Numa escolha mista, só ele sai: o outro entra na lista.
        tela.FindComponents<InputFile>()[^1].UploadFiles(grande, InputFileContent.CreateFromText("conteúdo", "rg.pdf"));

        tela.Markup.Should().Contain("O arquivo grande.pdf passa do limite de 50 MB.");
        Escolhidos(tela).Should().Equal("rg.pdf");
    }

    [Fact]
    public void Limite_de_dez_arquivos_vale_para_a_soma_das_escolhas()
    {
        var tela = Desenhar<MinhaSolicitacaoFormulario>();
        tela.FindComponents<InputFile>()[^1].UploadFiles(Arquivos(6, "primeira"));
        Escolhidos(tela).Should().HaveCount(6);

        // 6 + 5 passa de 10: a segunda escolha é recusada inteira, e a primeira fica.
        tela.FindComponents<InputFile>()[^1].UploadFiles(Arquivos(5, "segunda"));

        tela.Markup.Should().Contain("Anexe até 10 documentos por solicitação.");
        Escolhidos(tela).Should().HaveCount(6).And.OnlyContain(nome => nome.StartsWith("primeira-"));

        // 6 + 4 chega a 10: entra.
        tela.FindComponents<InputFile>()[^1].UploadFiles(Arquivos(4, "terceira"));

        Escolhidos(tela).Should().HaveCount(10);
        tela.Markup.Should().NotContain("Anexe até 10 documentos por solicitação.");
    }

    [Fact]
    public void Falha_ao_enviar_mantem_o_formulario_para_tentar_de_novo()
    {
        var tentativas = new List<string[]>();
        Despachante.Responde<AbrirSolicitacao, Resultado<Guid>>(abrir =>
        {
            tentativas.Add(abrir.Arquivos.Select(a => a.NomeArquivo).ToArray());
            return tentativas.Count == 1
                ? Resultado<Guid>.Falha(TipoErro.Validacao, "Falha de teste.")
                : Resultado<Guid>.Ok(SolicitacaoId);
        });
        var tela = Desenhar<MinhaSolicitacaoFormulario>();
        tela.Find("textarea").Change("Preciso de uma certidão atualizada.");
        tela.FindAll("div.aj-segmentado button").Single(b => b.TextContent == "Média").Click();
        tela.FindComponents<InputFile>()[^1].UploadFiles(InputFileContent.CreateFromText("conteúdo", "rg.pdf"));

        tela.Find("form#formulario-da-solicitacao").Submit();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Falha de teste."));
        Endereco.Should().NotContain("/minhas-solicitacoes/");
        tela.Find("textarea").GetAttribute("value").Should().Be("Preciso de uma certidão atualizada.");
        tela.FindAll("div.aj-segmentado button").Single(b => b.TextContent == "Média").GetAttribute("aria-pressed").Should().Be("true");
        Escolhidos(tela).Should().Equal("rg.pdf");
        tela.FindAll("button[type=submit]").Should().OnlyContain(b => !b.HasAttribute("disabled"));

        // A nova tentativa manda o mesmo arquivo, lido de novo do campo em que ele foi escolhido.
        tela.Find("form#formulario-da-solicitacao").Submit();

        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/minhas-solicitacoes/{SolicitacaoId}?enviada=1"));
        tentativas.Should().HaveCount(2);
        tentativas[1].Should().Equal("rg.pdf");
    }

    // A solicitação como o servidor a tem depois de outra tela mexer nela: outro texto e outra versão.
    private static MinhaSolicitacaoDetalhe MudadaPorOutraTela(bool podeAlterar = true) =>
        Detalhe(podeAlterar, versao: 9) with { Descricao = "Texto que outra tela salvou.", Urgencia = UrgenciaDaSolicitacao.Baixa };

    [Fact]
    public void Conflito_ao_salvar_a_edicao_rele_os_campos_e_a_versao_e_mostra_a_mensagem()
    {
        var leituras = 0;
        Despachante
            .Responde<ObterMinhaSolicitacao, Resultado<MinhaSolicitacaoDetalhe>>(_ => Resultado<MinhaSolicitacaoDetalhe>.Ok(
                ++leituras == 1 ? Detalhe(podeAlterar: true) : MudadaPorOutraTela()))
            .Responde<AlterarMinhaSolicitacao, Resultado>(alterar => alterar.VersaoEsperada == 9
                ? Resultado.Ok()
                : Resultado.Falha(TipoErro.Conflito, SolicitacoesOpcoes.Alterada));
        var tela = Desenhar<MinhaSolicitacaoFormulario>(p => p.Add(x => x.Id, (Guid?)SolicitacaoId));
        tela.Find("textarea").Change("Texto digitado sobre a versão antiga.");

        tela.Find("form#formulario-da-solicitacao").Submit();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain(SolicitacoesOpcoes.Alterada));
        tela.Find("textarea").GetAttribute("value").Should().Be("Texto que outra tela salvou.");
        tela.FindAll("div.aj-segmentado button").Single(b => b.TextContent == "Baixa").GetAttribute("aria-pressed").Should().Be("true");

        // Salvar de novo vai com a versão relida.
        tela.Find("form#formulario-da-solicitacao").Submit();

        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/minhas-solicitacoes/{SolicitacaoId}"));
        Despachante.Recebidas.OfType<AlterarMinhaSolicitacao>().Last().Should().Be(new AlterarMinhaSolicitacao(
            SolicitacaoId, "Texto que outra tela salvou.", UrgenciaDaSolicitacao.Baixa, 9));
    }

    [Fact]
    public void Conflito_em_operacao_de_documento_rele_os_campos_e_nao_salva_o_texto_antigo_com_a_versao_nova()
    {
        var leituras = 0;
        Despachante
            .Responde<ObterMinhaSolicitacao, Resultado<MinhaSolicitacaoDetalhe>>(_ => Resultado<MinhaSolicitacaoDetalhe>.Ok(
                ++leituras == 1 ? Detalhe(podeAlterar: true) : MudadaPorOutraTela()))
            .Responde<RemoverDocumentoDaSolicitacao, Resultado>(Resultado.Falha(TipoErro.Conflito, SolicitacoesOpcoes.Alterada))
            .Responde<AlterarMinhaSolicitacao, Resultado>(Resultado.Ok());
        var tela = Desenhar<MinhaSolicitacaoFormulario>(p => p.Add(x => x.Id, (Guid?)SolicitacaoId));
        tela.Find("textarea").Change("Texto digitado sobre a versão antiga.");

        tela.FindAll(".aj-linha button").Single(b => b.TextContent == "Remover").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain(SolicitacoesOpcoes.Alterada));
        tela.Find("textarea").GetAttribute("value").Should().Be("Texto que outra tela salvou.");

        tela.Find("form#formulario-da-solicitacao").Submit();

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<AlterarMinhaSolicitacao>().Should().ContainSingle()
            .Which.Should().Be(new AlterarMinhaSolicitacao(
                SolicitacaoId, "Texto que outra tela salvou.", UrgenciaDaSolicitacao.Baixa, 9)));
    }

    [Fact]
    public void Documento_removido_com_sucesso_mantem_o_que_foi_digitado()
    {
        Despachante
            .Responde<ObterMinhaSolicitacao, Resultado<MinhaSolicitacaoDetalhe>>(
                Resultado<MinhaSolicitacaoDetalhe>.Ok(Detalhe(podeAlterar: true)))
            .Responde<RemoverDocumentoDaSolicitacao, Resultado>(Resultado.Ok());
        var tela = Desenhar<MinhaSolicitacaoFormulario>(p => p.Add(x => x.Id, (Guid?)SolicitacaoId));
        tela.Find("textarea").Change("Texto ainda não salvo.");

        tela.FindAll(".aj-linha button").Single(b => b.TextContent == "Remover").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento removido."));
        tela.Find("textarea").GetAttribute("value").Should().Be("Texto ainda não salvo.");
    }

    [Fact]
    public void Conflito_ao_salvar_que_tira_a_edicao_leva_a_mensagem_para_a_ficha()
    {
        var leituras = 0;
        Despachante
            .Responde<ObterMinhaSolicitacao, Resultado<MinhaSolicitacaoDetalhe>>(_ => Resultado<MinhaSolicitacaoDetalhe>.Ok(
                ++leituras == 1 ? Detalhe(podeAlterar: true) : MudadaPorOutraTela(podeAlterar: false)))
            .Responde<AlterarMinhaSolicitacao, Resultado>(Resultado.Falha(TipoErro.Conflito, SolicitacoesOpcoes.Alterada));
        var tela = Desenhar<MinhaSolicitacaoFormulario>(p => p.Add(x => x.Id, (Guid?)SolicitacaoId));

        tela.Find("form#formulario-da-solicitacao").Submit();

        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/minhas-solicitacoes/{SolicitacaoId}"));
        Services.GetRequiredService<Recados>().Retirar().Should()
            .Be(new Recado(SolicitacoesOpcoes.Alterada, TipoDeAviso.Erro));
    }

    [Fact]
    public void Cancelar_pede_confirmacao_e_voltar_nao_cancela()
    {
        Despachante.Responde<CancelarMinhaSolicitacao, Resultado>(Resultado.Ok());
        var tela = Ficha(podeAlterar: true);

        tela.FindAll("button").Single(b => b.TextContent == "Cancelar solicitação").Click();

        var dialogo = tela.Find("[role=alertdialog]");
        dialogo.QuerySelector("strong")!.TextContent.Should().Be(Confirmacao);
        dialogo.QuerySelectorAll("button").Select(b => b.TextContent).Should().Equal("Voltar", "Sim, cancelar");

        dialogo.QuerySelectorAll("button").Single(b => b.TextContent == "Voltar").Click();

        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        Despachante.Recebidas.OfType<CancelarMinhaSolicitacao>().Should().BeEmpty();

        // Confirmado, cancela com a versão com que a ficha foi lida.
        tela.FindAll("button").Single(b => b.TextContent == "Cancelar solicitação").Click();
        tela.Find("[role=alertdialog]").QuerySelectorAll("button").Single(b => b.TextContent == "Sim, cancelar").Click();

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<CancelarMinhaSolicitacao>().Should().ContainSingle()
            .Which.Should().Be(new CancelarMinhaSolicitacao(SolicitacaoId, 4)));
    }

    [Fact]
    public void Conflito_ao_cancelar_recarrega_a_ficha_e_mostra_a_mensagem()
    {
        Despachante.Responde<CancelarMinhaSolicitacao, Resultado>(
            Resultado.Falha(TipoErro.Conflito, SolicitacoesOpcoes.Alterada));
        var tela = Ficha(podeAlterar: true);

        tela.FindAll("button").Single(b => b.TextContent == "Cancelar solicitação").Click();
        tela.Find("[role=alertdialog]").QuerySelectorAll("button").Single(b => b.TextContent == "Sim, cancelar").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain(SolicitacoesOpcoes.Alterada));
        Despachante.Recebidas.OfType<ObterMinhaSolicitacao>().Should().HaveCount(2);
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
    }

    [Fact]
    public void Ficha_de_colega_nao_mostra_editar_nem_cancelar()
    {
        var tela = Ficha(podeAlterar: false);

        tela.Markup.Should().Contain("Preciso de uma certidão atualizada.").And.Contain("rg.pdf")
            .And.NotContain("Cancelar solicitação").And.NotContain("/editar");
        tela.FindAll("a, button").Should().NotContain(e => e.TextContent == "Editar");
    }

    [Fact]
    public void Edicao_sem_poder_alterar_volta_para_a_ficha()
    {
        Despachante.Responde<ObterMinhaSolicitacao, Resultado<MinhaSolicitacaoDetalhe>>(
            Resultado<MinhaSolicitacaoDetalhe>.Ok(Detalhe(podeAlterar: false)));

        Desenhar<MinhaSolicitacaoFormulario>(p => p.Add(x => x.Id, (Guid?)SolicitacaoId));

        Endereco.Should().EndWith($"/minhas-solicitacoes/{SolicitacaoId}");
    }

    [Fact]
    public void Edicao_salva_com_a_versao_lida_e_remover_documento_recarrega_a_versao()
    {
        var versao = 4u;
        Despachante
            .Responde<ObterMinhaSolicitacao, Resultado<MinhaSolicitacaoDetalhe>>(_ =>
                Resultado<MinhaSolicitacaoDetalhe>.Ok(Detalhe(podeAlterar: true, versao)))
            .Responde<RemoverDocumentoDaSolicitacao, Resultado>(_ =>
            {
                versao = 5;
                return Resultado.Ok();
            })
            .Responde<AlterarMinhaSolicitacao, Resultado>(Resultado.Ok());
        var tela = Desenhar<MinhaSolicitacaoFormulario>(p => p.Add(x => x.Id, (Guid?)SolicitacaoId));

        tela.Find("h1").TextContent.Should().Be("Editar solicitação");
        tela.FindAll(".aj-linha button").Single(b => b.TextContent == "Remover").Click();

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<ObterMinhaSolicitacao>().Should().HaveCount(2));
        Despachante.Recebidas.OfType<RemoverDocumentoDaSolicitacao>().Should().ContainSingle()
            .Which.Should().Be(new RemoverDocumentoDaSolicitacao(DocumentoId, 4));

        tela.Find("textarea").Change("Descrição nova.");
        tela.Find("form#formulario-da-solicitacao").Submit();

        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/minhas-solicitacoes/{SolicitacaoId}"));
        Despachante.Recebidas.OfType<AlterarMinhaSolicitacao>().Should().ContainSingle().Which.Should().Be(
            new AlterarMinhaSolicitacao(SolicitacaoId, "Descrição nova.", UrgenciaDaSolicitacao.Alta, 5));
    }

    [Fact]
    public void Anexar_documento_envia_na_hora_com_a_versao_lida_e_recarrega()
    {
        string? nome = null;
        Despachante
            .Responde<ObterMinhaSolicitacao, Resultado<MinhaSolicitacaoDetalhe>>(
                Resultado<MinhaSolicitacaoDetalhe>.Ok(Detalhe(podeAlterar: true)))
            .Responde<AnexarDocumentoASolicitacao, Resultado<Guid>>(anexar =>
            {
                nome = anexar.NomeArquivo;
                return Resultado<Guid>.Ok(Guid.NewGuid());
            });
        var tela = Desenhar<MinhaSolicitacaoFormulario>(p => p.Add(x => x.Id, (Guid?)SolicitacaoId));

        tela.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", "comprovante.pdf"));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento anexado."));
        nome.Should().Be("comprovante.pdf");
        Despachante.Recebidas.OfType<AnexarDocumentoASolicitacao>().Should().ContainSingle().Which.VersaoEsperada.Should().Be(4);
        Despachante.Recebidas.OfType<ObterMinhaSolicitacao>().Should().HaveCount(2);
        tela.Markup.Should().Contain(">Anexar documento<");
    }

    // ---- Diretoria ----

    private static Resultado<Pagina<SolicitacaoResumo>> ListaDaDiretoria(SituacaoDaSolicitacao? situacao) =>
        Resultado<Pagina<SolicitacaoResumo>>.Ok(new(
            new[]
            {
                new SolicitacaoResumo(SolicitacaoId, AbertaEm, MariaId, "Maria Silva", "Carla Cliente",
                    UrgenciaDaSolicitacao.Alta, "Preciso de uma certidão.", 1, SituacaoDaSolicitacao.Aguardando),
                new SolicitacaoResumo(Guid.NewGuid(), AbertaEm.AddDays(1), MariaId, "Maria Silva", "Carla Cliente",
                    UrgenciaDaSolicitacao.Baixa, "Pedido cancelado.", 0, SituacaoDaSolicitacao.Cancelada),
            }.Where(s => situacao is null || s.Situacao == situacao).ToList(), situacao is null ? 2 : 1, 1, 20));

    [Fact]
    public void Lista_da_diretoria_abre_em_aguardando_e_traz_todas_com_o_filtro_todas()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarSolicitacoes, Resultado<Pagina<SolicitacaoResumo>>>(r => ListaDaDiretoria(r.Situacao));
        var tela = Desenhar<Solicitacoes>();

        Despachante.Recebidas.OfType<ListarSolicitacoes>().Should().ContainSingle()
            .Which.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        tela.Find("button.aj-chip[aria-pressed=true]").TextContent.Should().Be("Aguardando");
        tela.Markup.Should().Contain("Preciso de uma certidão.").And.NotContain("Pedido cancelado.");

        tela.FindAll("button.aj-chip").Single(b => b.TextContent == "Todas").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Pedido cancelado."));
        Despachante.Recebidas.OfType<ListarSolicitacoes>().Last().Should().Be(new ListarSolicitacoes(null, null, 1));
        tela.Find("button.aj-chip[aria-pressed=true]").TextContent.Should().Be("Todas");
    }

    [Fact]
    public void Lista_da_diretoria_filtra_pelo_cliente_escolhido_na_busca()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarSolicitacoes, Resultado<Pagina<SolicitacaoResumo>>>(r => ListaDaDiretoria(r.Situacao))
            .Responde<ListarClientes, Resultado<PaginaDeClientes>>(Resultado<PaginaDeClientes>.Ok(new(
                [new ClienteResumo(MariaId, TipoCliente.PessoaFisica, "Maria Silva", "529.982.247-25", null, null, null, true)],
                1, 1, 20, 1, 0)));
        var tela = Desenhar<Solicitacoes>();

        tela.Find("input[form=busca-de-cliente]").Input("maria");
        tela.Find("form#busca-de-cliente").Submit();
        tela.Find("button.aj-resultados__item").Click();

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<ListarSolicitacoes>().Last()
            .Should().Be(new ListarSolicitacoes(SituacaoDaSolicitacao.Aguardando, MariaId, 1)));
        tela.Find("div.aj-vinculo__escolhido .aj-vinculo__titulo").TextContent.Should().Be("Maria Silva");
    }

    [Fact]
    public void Producao_na_lista_da_diretoria_ve_nao_encontrada_sem_consultar()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao);

        var tela = Desenhar<Solicitacoes>();

        tela.Markup.Should().Contain("Página não encontrada");
        Despachante.Recebidas.OfType<ListarSolicitacoes>().Should().BeEmpty();
    }

    [Fact]
    public void Ficha_da_diretoria_com_atividade_excluida_nao_leva_a_atividade()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterSolicitacao, Resultado<SolicitacaoDetalhe>>(Resultado<SolicitacaoDetalhe>.Ok(new(
                SolicitacaoId, AbertaEm, MariaId, "Maria Silva", Guid.NewGuid(), "Carla Cliente", UrgenciaDaSolicitacao.Media,
                "Preciso de uma certidão.", SituacaoDaSolicitacao.EmAtividade, null, [], Guid.NewGuid(), null, 3)));

        var tela = Desenhar<SolicitacaoFicha>(p => p.Add(x => x.Id, SolicitacaoId));

        tela.Markup.Should().Contain("Virou atividade").And.Contain("Preciso de uma certidão.")
            .And.NotContain("Ver atividade").And.NotContain("/atividades/");
        tela.Find($"a[href='/clientes/{MariaId}']").TextContent.Should().Be("Maria Silva");
    }

    [Fact]
    public void Ficha_da_diretoria_com_atividade_viva_leva_a_ela()
    {
        var atividade = Guid.NewGuid();
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterSolicitacao, Resultado<SolicitacaoDetalhe>>(Resultado<SolicitacaoDetalhe>.Ok(new(
                SolicitacaoId, AbertaEm, MariaId, "Maria Silva", Guid.NewGuid(), "Carla Cliente", UrgenciaDaSolicitacao.Media,
                "Preciso de uma certidão.", SituacaoDaSolicitacao.EmAtividade, null, [], atividade, "Emitir certidão", 3)));

        var tela = Desenhar<SolicitacaoFicha>(p => p.Add(x => x.Id, SolicitacaoId));

        tela.Find($"a[href='/atividades/{atividade}']").TextContent.Should().Be("Ver atividade");
        tela.Markup.Should().Contain("Emitir certidão");
    }
}
