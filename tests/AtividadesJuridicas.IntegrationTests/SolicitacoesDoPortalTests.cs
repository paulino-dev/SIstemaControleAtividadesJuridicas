using System.Text;
using System.Text.Json;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Application.Solicitacoes;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Domain.Solicitacoes;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class SolicitacoesDoPortalTests(BancoFixture banco) : TesteComBanco(banco)
{
    private const string Descricao = "Preciso de uma certidão atualizada.";

    private Guid _admin, _pedro, _maria, _jose, _carla, _ana, _joao;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _pedro = (await ExecutarAsync(_admin, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null))).Valor;
        _maria = await ClienteAsync("Maria Silva", "529.982.247-25");
        _jose = await ClienteAsync("José Souza", "111.444.777-35");
        _carla = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _maria));
        _ana = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _maria, "Ana Cliente", "ana@x.com"));
        _joao = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _jose, "João Cliente", "joao@x.com"));
    }

    private async Task<Guid> ClienteAsync(string nome, string cpf) =>
        (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada(nome, null, null, new EnderecoEntrada(), null), cpf, null, null))).Valor;

    private static ArquivoParaEnviar Arquivo(string nome, string texto = "conteúdo") =>
        new(nome, "application/pdf", () => new MemoryStream(Encoding.UTF8.GetBytes(texto)));

    private Task<Resultado<Guid>> AbrirAsync(
        Guid ator, string descricao = Descricao, UrgenciaDaSolicitacao urgencia = UrgenciaDaSolicitacao.Media,
        params ArquivoParaEnviar[] arquivos) =>
        ExecutarAsync(ator, new AbrirSolicitacao(descricao, urgencia, arquivos));

    private async Task<Guid> AbertaAsync(Guid ator, params ArquivoParaEnviar[] arquivos)
    {
        var resultado = await AbrirAsync(ator, Descricao, UrgenciaDaSolicitacao.Media, arquivos);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private Task<Resultado<MinhaSolicitacaoDetalhe>> FichaAsync(Guid ator, Guid id) =>
        ExecutarAsync(ator, new ObterMinhaSolicitacao(id));

    private async Task<MinhaSolicitacaoDetalhe> LerAsync(Guid ator, Guid id)
    {
        var ficha = await FichaAsync(ator, id);
        ficha.Sucesso.Should().BeTrue(ficha.Mensagem);
        return ficha.Valor!;
    }

    private async Task<uint> VersaoAsync(Guid ator, Guid id) => (await LerAsync(ator, id)).Versao;

    private Task<Resultado<Pagina<SolicitacaoDoPortal>>> ListaAsync(Guid ator, SituacaoParaOCliente? situacao = null) =>
        ExecutarAsync(ator, new ListarMinhasSolicitacoes(situacao));

    private Task<Resultado> AlterarAsync(
        Guid ator, Guid id, uint versao, string descricao = "Descrição nova.", UrgenciaDaSolicitacao urgencia = UrgenciaDaSolicitacao.Alta) =>
        ExecutarAsync(ator, new AlterarMinhaSolicitacao(id, descricao, urgencia, versao));

    private Task<Resultado<Guid>> AnexarAsync(Guid ator, Guid id, uint versao, string arquivo = "extra.pdf") =>
        ExecutarAsync(ator, new AnexarDocumentoASolicitacao(
            id, arquivo, "application/pdf", new MemoryStream(Encoding.UTF8.GetBytes("conteúdo de " + arquivo)), versao));

    private Task<Resultado> RemoverAsync(Guid ator, Guid documento, uint versao) =>
        ExecutarAsync(ator, new RemoverDocumentoDaSolicitacao(documento, versao));

    private Task<Resultado> CancelarAsync(Guid ator, Guid id, uint versao) =>
        ExecutarAsync(ator, new CancelarMinhaSolicitacao(id, versao));

    private async Task<(Resultado<ArquivoParaBaixar> Resultado, string? Texto)> BaixarAsync(Guid usuario, Guid documento)
    {
        var resultado = await ExecutarAsync(usuario, new BaixarDocumento(documento));
        if (resultado.Valor is not { } arquivo) return (resultado, null);

        await using (arquivo)
        {
            using var leitor = new StreamReader(arquivo.Conteudo, Encoding.UTF8);
            return (resultado, await leitor.ReadToEndAsync());
        }
    }

    private async Task<List<RegistroAuditoria>> NegadosDeDownloadAsync(Guid usuario) =>
        [.. (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == usuario)];

    private Task<int> SolicitacoesNoBancoAsync() => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().SolicitacoesDoCliente.IgnoreQueryFilters().CountAsync());

    private Task<int> DocumentosNoBancoAsync() => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Documentos.IgnoreQueryFilters().CountAsync());

    private int ArquivosNoDisco() =>
        Directory.Exists(PastaDeArquivos) ? Directory.GetFiles(PastaDeArquivos, "*", SearchOption.AllDirectories).Length : 0;

    [Fact]
    public async Task Carla_abre_com_dois_arquivos_e_ve_na_lista_e_na_ficha()
    {
        var resultado = await AbrirAsync(
            _carla, Descricao, UrgenciaDaSolicitacao.Alta, Arquivo("um.pdf", "primeiro conteúdo"), Arquivo("dois.pdf", "segundo conteúdo"));

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var id = resultado.Valor;

        var lista = (await ListaAsync(_carla)).Valor!;
        var linha = lista.Itens.Should().ContainSingle().Subject;
        linha.Id.Should().Be(id);
        linha.Trecho.Should().Be(Descricao);
        linha.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        linha.Situacao.Should().Be(SituacaoParaOCliente.AguardandoAnalise);
        linha.SolicitanteNome.Should().Be("Carla Cliente");
        linha.Documentos.Should().Be(2);
        linha.AbertaEm.Kind.Should().Be(DateTimeKind.Utc);
        linha.AbertaEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

        var ficha = await LerAsync(_carla, id);
        ficha.AbertaEm.Should().Be(linha.AbertaEm);
        ficha.Descricao.Should().Be(Descricao);
        ficha.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        ficha.Situacao.Should().Be(SituacaoParaOCliente.AguardandoAnalise);
        ficha.SolicitanteNome.Should().Be("Carla Cliente");
        ficha.PodeAlterar.Should().BeTrue();
        ficha.AtividadeTitulo.Should().BeNull();
        ficha.AtividadeConcluidaEm.Should().BeNull();
        ficha.Documentos.Should().HaveCount(2);
        ficha.Documentos.Select(d => d.Nome).Should().BeEquivalentTo("um.pdf", "dois.pdf");

        var textos = new List<string?>();
        foreach (var documento in ficha.Documentos)
        {
            var (baixado, texto) = await BaixarAsync(_carla, documento.Id);
            baixado.Sucesso.Should().BeTrue(baixado.Mensagem);
            textos.Add(texto);
        }

        textos.Should().BeEquivalentTo("primeiro conteúdo", "segundo conteúdo");
        ArquivosNoDisco().Should().Be(2);
        (await SolicitacoesNoBancoAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Abrir_sem_arquivos_e_valido_e_o_limite_de_dez_vale_para_a_abertura()
    {
        var semArquivos = await AbrirAsync(_carla);
        semArquivos.Sucesso.Should().BeTrue(semArquivos.Mensagem);
        var dez = await AbrirAsync(_carla, Descricao, UrgenciaDaSolicitacao.Baixa, [.. Enumerable.Range(1, 10).Select(i => Arquivo($"d{i}.pdf", $"c{i}"))]);
        dez.Sucesso.Should().BeTrue(dez.Mensagem);
        var arquivosAntes = ArquivosNoDisco();
        var documentosAntes = await DocumentosNoBancoAsync();
        var solicitacoesAntes = await SolicitacoesNoBancoAsync();

        var onze = await AbrirAsync(_carla, Descricao, UrgenciaDaSolicitacao.Baixa, [.. Enumerable.Range(1, 11).Select(i => Arquivo($"d{i}.pdf", $"c{i}"))]);

        onze.Erro.Should().Be(TipoErro.Validacao);
        onze.Mensagem.Should().Be("Anexe até 10 documentos por solicitação.");
        ArquivosNoDisco().Should().Be(arquivosAntes);
        (await DocumentosNoBancoAsync()).Should().Be(documentosAntes);
        (await SolicitacoesNoBancoAsync()).Should().Be(solicitacoesAntes);
        (await LerAsync(_carla, dez.Valor)).Documentos.Should().HaveCount(10);
    }

    [Fact]
    public async Task Abrir_com_um_arquivo_recusado_nao_grava_nada_nem_deixa_arquivo()
    {
        var resultado = await AbrirAsync(
            _carla, Descricao, UrgenciaDaSolicitacao.Media,
            Arquivo("pequeno.pdf"),
            new ArquivoParaEnviar("enorme.pdf", "application/pdf", () => new FluxoDeZeros(DocumentosOpcoes.TamanhoMaximoBytes + 1)));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O arquivo passa do limite de 50 MB.");
        (await SolicitacoesNoBancoAsync()).Should().Be(0);
        (await DocumentosNoBancoAsync()).Should().Be(0);
        ArquivosNoDisco().Should().Be(0, "o primeiro arquivo, já gravado, é removido, e o segundo não deixa temporário");
        (await ListaAsync(_carla)).Valor!.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Abrir_com_descricao_invalida_nao_grava_arquivo()
    {
        var resultado = await AbrirAsync(_carla, "   ", UrgenciaDaSolicitacao.Media, Arquivo("um.pdf"));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("A descrição da solicitação é obrigatória.");
        (await SolicitacoesNoBancoAsync()).Should().Be(0);
        ArquivosNoDisco().Should().Be(0);
    }

    [Fact]
    public async Task Cliente_A_nao_lista_nao_abre_e_nao_baixa_nada_do_cliente_B()
    {
        var daMaria = await AbertaAsync(_carla, Arquivo("da-maria.pdf"));
        var documentoDaMaria = (await LerAsync(_carla, daMaria)).Documentos.Single().Id;
        var doJose = await AbertaAsync(_joao, Arquivo("do-jose.pdf"));
        var documentoDoJose = (await LerAsync(_joao, doJose)).Documentos.Single().Id;
        var arquivosAntes = ArquivosNoDisco();

        (await ListaAsync(_joao)).Valor!.Itens.Select(s => s.Id).Should().Equal(doJose);
        (await ListaAsync(_carla)).Valor!.Itens.Select(s => s.Id).Should().Equal(daMaria);

        // O João não alcança nada da Maria: a solicitação não existe para ele, nem o documento dela.
        var ficha = await FichaAsync(_joao, daMaria);
        ficha.Erro.Should().Be(TipoErro.NaoEncontrado);
        ficha.Mensagem.Should().Be("Solicitação não encontrada.");
        (await FichaAsync(_joao, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);

        var versaoDaMaria = await VersaoAsync(_carla, daMaria);
        var comandos = new[]
        {
            await AlterarAsync(_joao, daMaria, versaoDaMaria),
            await AnexarAsync(_joao, daMaria, versaoDaMaria),
            await RemoverAsync(_joao, documentoDaMaria, versaoDaMaria),
            await CancelarAsync(_joao, daMaria, versaoDaMaria),
        };
        comandos.Should().OnlyContain(r => r.Erro == TipoErro.NaoEncontrado && r.Mensagem == "Solicitação não encontrada.");

        var baixado = await BaixarAsync(_joao, documentoDaMaria);
        baixado.Resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        baixado.Texto.Should().BeNull();
        var negado = (await NegadosDeDownloadAsync(_joao)).Should().ContainSingle().Subject;
        negado.Entidade.Should().Be("Documento");
        negado.EntidadeId.Should().Be(documentoDaMaria);

        // E o inverso: a Carla não alcança nada do José.
        (await FichaAsync(_carla, doJose)).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await RemoverAsync(_carla, documentoDoJose, await VersaoAsync(_joao, doJose))).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await BaixarAsync(_carla, documentoDoJose)).Resultado.Erro.Should().Be(TipoErro.NaoEncontrado);

        // Nada mudou, de nenhum dos lados.
        var daMariaDepois = await LerAsync(_carla, daMaria);
        daMariaDepois.Descricao.Should().Be(Descricao);
        daMariaDepois.Situacao.Should().Be(SituacaoParaOCliente.AguardandoAnalise);
        daMariaDepois.Documentos.Should().ContainSingle();
        daMariaDepois.Versao.Should().Be(versaoDaMaria);
        (await LerAsync(_joao, doJose)).Documentos.Should().ContainSingle();
        ArquivosNoDisco().Should().Be(arquivosAntes);
    }

    [Fact]
    public async Task Solicitacao_excluida_nao_existe_para_o_cliente()
    {
        var id = await AbertaAsync(_carla, Arquivo("um.pdf"));
        var documento = (await LerAsync(_carla, id)).Documentos.Single().Id;
        var versao = await VersaoAsync(_carla, id);
        var agora = DateTime.UtcNow;
        await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Database
            .ExecuteSqlAsync($"UPDATE solicitacoes_do_cliente SET excluido_em = {agora} WHERE id = {id}"));

        (await ListaAsync(_carla)).Valor!.Itens.Should().BeEmpty();
        (await FichaAsync(_carla, id)).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await AlterarAsync(_carla, id, versao)).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await AnexarAsync(_carla, id, versao)).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await CancelarAsync(_carla, id, versao)).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await RemoverAsync(_carla, documento, versao)).Erro.Should().Be(TipoErro.NaoEncontrado);
        ArquivosNoDisco().Should().Be(1);
    }

    [Fact]
    public async Task Colega_do_mesmo_cliente_ve_mas_nao_altera()
    {
        var id = await AbertaAsync(_carla, Arquivo("da-carla.pdf", "texto da carla"));
        var versao = await VersaoAsync(_carla, id);
        var documento = (await LerAsync(_carla, id)).Documentos.Single().Id;
        var arquivosAntes = ArquivosNoDisco();

        var lista = (await ListaAsync(_ana)).Valor!;
        lista.Itens.Should().ContainSingle().Which.SolicitanteNome.Should().Be("Carla Cliente");
        var ficha = await LerAsync(_ana, id);
        ficha.PodeAlterar.Should().BeFalse();
        ficha.SolicitanteNome.Should().Be("Carla Cliente");
        ficha.Documentos.Should().ContainSingle();
        var (baixado, texto) = await BaixarAsync(_ana, documento);
        baixado.Sucesso.Should().BeTrue(baixado.Mensagem);
        texto.Should().Be("texto da carla");

        var comandos = new[]
        {
            await AlterarAsync(_ana, id, versao),
            await AnexarAsync(_ana, id, versao),
            await RemoverAsync(_ana, documento, versao),
            await CancelarAsync(_ana, id, versao),
        };

        comandos.Should().OnlyContain(r => r.Erro == TipoErro.Validacao && r.Mensagem == "Só quem abriu a solicitação pode alterá-la.");
        var depois = await LerAsync(_carla, id);
        depois.Descricao.Should().Be(Descricao);
        depois.Situacao.Should().Be(SituacaoParaOCliente.AguardandoAnalise);
        depois.Documentos.Should().ContainSingle();
        depois.Versao.Should().Be(versao);
        ArquivosNoDisco().Should().Be(arquivosAntes);
    }

    [Fact]
    public async Task Alterar_com_versao_antiga_responde_conflito_e_mantem_a_primeira()
    {
        var id = await AbertaAsync(_carla);
        var versaoLida = await VersaoAsync(_carla, id);

        var primeira = await AlterarAsync(_carla, id, versaoLida, "Primeira alteração.", UrgenciaDaSolicitacao.Baixa);
        var segunda = await AlterarAsync(_carla, id, versaoLida, "Segunda alteração.", UrgenciaDaSolicitacao.Alta);

        primeira.Sucesso.Should().BeTrue(primeira.Mensagem);
        segunda.Erro.Should().Be(TipoErro.Conflito);
        segunda.Mensagem.Should().Be("A solicitação foi alterada. Recarregue e tente de novo.");
        var ficha = await LerAsync(_carla, id);
        ficha.Descricao.Should().Be("Primeira alteração.");
        ficha.Urgencia.Should().Be(UrgenciaDaSolicitacao.Baixa);
        ficha.Versao.Should().NotBe(versaoLida);

        // Com a versão que ela leu depois, o mesmo pedido passa.
        (await AlterarAsync(_carla, id, ficha.Versao, "Segunda alteração.")).Sucesso.Should().BeTrue();
        (await LerAsync(_carla, id)).Descricao.Should().Be("Segunda alteração.");
    }

    [Fact]
    public async Task Alterar_com_dados_invalidos_responde_validacao_e_nao_muda_nada()
    {
        var id = await AbertaAsync(_carla);
        var versao = await VersaoAsync(_carla, id);

        var vazia = await AlterarAsync(_carla, id, versao, "  ");
        var urgencia = await AlterarAsync(_carla, id, versao, urgencia: (UrgenciaDaSolicitacao)99);

        vazia.Erro.Should().Be(TipoErro.Validacao);
        vazia.Mensagem.Should().Be("A descrição da solicitação é obrigatória.");
        urgencia.Mensagem.Should().Be("Urgência inválida.");
        var ficha = await LerAsync(_carla, id);
        ficha.Descricao.Should().Be(Descricao);
        ficha.Versao.Should().Be(versao);
    }

    [Fact]
    public async Task Anexar_e_remover_mudam_a_versao()
    {
        var id = await AbertaAsync(_carla, Arquivo("um.pdf"));
        var versaoLida = await VersaoAsync(_carla, id);
        var arquivosAntes = ArquivosNoDisco();

        var anexo = await AnexarAsync(_carla, id, versaoLida, "dois.pdf");

        anexo.Sucesso.Should().BeTrue(anexo.Mensagem);
        var aposAnexo = await LerAsync(_carla, id);
        aposAnexo.Versao.Should().NotBe(versaoLida);
        aposAnexo.Documentos.Select(d => d.Nome).Should().BeEquivalentTo("um.pdf", "dois.pdf");
        ArquivosNoDisco().Should().Be(arquivosAntes + 1);

        // Quem leu antes do anexo é avisado, e o arquivo recusado não fica no disco.
        var antigo = await AnexarAsync(_carla, id, versaoLida, "tres.pdf");
        antigo.Erro.Should().Be(TipoErro.Conflito);
        antigo.Mensagem.Should().Be("A solicitação foi alterada. Recarregue e tente de novo.");
        (await AlterarAsync(_carla, id, versaoLida)).Erro.Should().Be(TipoErro.Conflito);
        (await CancelarAsync(_carla, id, versaoLida)).Erro.Should().Be(TipoErro.Conflito);
        (await RemoverAsync(_carla, anexo.Valor, versaoLida)).Erro.Should().Be(TipoErro.Conflito);
        ArquivosNoDisco().Should().Be(arquivosAntes + 1);

        var remocao = await RemoverAsync(_carla, anexo.Valor, aposAnexo.Versao);

        remocao.Sucesso.Should().BeTrue(remocao.Mensagem);
        var aposRemocao = await LerAsync(_carla, id);
        aposRemocao.Versao.Should().NotBe(aposAnexo.Versao);
        aposRemocao.Documentos.Select(d => d.Nome).Should().Equal("um.pdf");
        (await RemoverAsync(_carla, anexo.Valor, aposAnexo.Versao)).Erro.Should().Be(TipoErro.NaoEncontrado, "o documento já saiu");
        (await BaixarAsync(_carla, anexo.Valor)).Resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        ArquivosNoDisco().Should().Be(arquivosAntes + 1, "a exclusão é lógica e o arquivo permanece");
    }

    [Fact]
    public async Task Anexos_simultaneos_com_a_mesma_versao_so_um_vence_e_o_outro_nao_deixa_arquivo()
    {
        var id = await AbertaAsync(_carla, Arquivo("um.pdf"));
        var versao = await VersaoAsync(_carla, id);
        var arquivosAntes = ArquivosNoDisco();

        var resultados = await Task.WhenAll(
            AnexarAsync(_carla, id, versao, "a.pdf"), AnexarAsync(_carla, id, versao, "b.pdf"));

        resultados.Count(r => r.Sucesso).Should().Be(1);
        var perdeu = resultados.Single(r => !r.Sucesso);
        perdeu.Erro.Should().Be(TipoErro.Conflito);
        perdeu.Mensagem.Should().Be("A solicitação foi alterada. Recarregue e tente de novo.");
        (await LerAsync(_carla, id)).Documentos.Should().HaveCount(2);
        ArquivosNoDisco().Should().Be(arquivosAntes + 1);
    }

    [Fact]
    public async Task Anexar_alem_do_limite_e_recusado_e_o_removido_libera_a_vaga()
    {
        var id = await AbertaAsync(_carla, [.. Enumerable.Range(1, 10).Select(i => Arquivo($"d{i}.pdf", $"c{i}"))]);
        var ficha = await LerAsync(_carla, id);
        var arquivosAntes = ArquivosNoDisco();

        var recusado = await AnexarAsync(_carla, id, ficha.Versao);

        recusado.Erro.Should().Be(TipoErro.Validacao);
        recusado.Mensagem.Should().Be("Anexe até 10 documentos por solicitação.");
        ArquivosNoDisco().Should().Be(arquivosAntes);
        (await RemoverAsync(_carla, ficha.Documentos[0].Id, ficha.Versao)).Sucesso.Should().BeTrue();
        (await AnexarAsync(_carla, id, await VersaoAsync(_carla, id))).Sucesso.Should().BeTrue();
        (await LerAsync(_carla, id)).Documentos.Should().HaveCount(10);
    }

    [Fact]
    public async Task Anexar_arquivo_acima_do_limite_de_tamanho_e_recusado_sem_deixar_arquivo()
    {
        var id = await AbertaAsync(_carla);
        var versao = await VersaoAsync(_carla, id);

        var resultado = await ExecutarAsync(_carla, new AnexarDocumentoASolicitacao(
            id, "enorme.pdf", "application/pdf", new FluxoDeZeros(DocumentosOpcoes.TamanhoMaximoBytes + 1), versao));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O arquivo passa do limite de 50 MB.");
        ArquivosNoDisco().Should().Be(0);
        (await DocumentosNoBancoAsync()).Should().Be(0);
        (await VersaoAsync(_carla, id)).Should().Be(versao);
    }

    [Fact]
    public async Task Depois_de_cancelada_nada_mais_muda()
    {
        var id = await AbertaAsync(_carla, Arquivo("um.pdf"));
        var documento = (await LerAsync(_carla, id)).Documentos.Single().Id;

        var cancelamento = await CancelarAsync(_carla, id, await VersaoAsync(_carla, id));

        cancelamento.Sucesso.Should().BeTrue(cancelamento.Mensagem);
        var ficha = await LerAsync(_carla, id);
        ficha.Situacao.Should().Be(SituacaoParaOCliente.Cancelada);
        ficha.PodeAlterar.Should().BeFalse();
        (await ListaAsync(_carla)).Valor!.Itens.Single().Situacao.Should().Be(SituacaoParaOCliente.Cancelada);
        (await ListaAsync(_carla, SituacaoParaOCliente.Cancelada)).Valor!.Itens.Select(s => s.Id).Should().Equal(id);
        var arquivosAntes = ArquivosNoDisco();

        // Com a versão nova, o que recusa é o estado da solicitação.
        var comandos = new[]
        {
            await AlterarAsync(_carla, id, ficha.Versao),
            await AnexarAsync(_carla, id, ficha.Versao),
            await RemoverAsync(_carla, documento, ficha.Versao),
            await CancelarAsync(_carla, id, ficha.Versao),
        };

        comandos.Should().OnlyContain(r => r.Erro == TipoErro.Validacao && r.Mensagem == "Esta solicitação não pode mais ser alterada.");
        var depois = await LerAsync(_carla, id);
        depois.Descricao.Should().Be(Descricao);
        depois.Documentos.Should().ContainSingle();
        depois.Situacao.Should().Be(SituacaoParaOCliente.Cancelada);
        ArquivosNoDisco().Should().Be(arquivosAntes);
    }

    [Fact]
    public async Task Producao_nao_baixa_documento_de_solicitacao()
    {
        var id = await AbertaAsync(_carla, Arquivo("um.pdf"));
        var documento = (await LerAsync(_carla, id)).Documentos.Single().Id;

        var (baixado, texto) = await BaixarAsync(_pedro, documento);

        baixado.Erro.Should().Be(TipoErro.NaoEncontrado);
        texto.Should().BeNull();
        var negado = (await NegadosDeDownloadAsync(_pedro)).Should().ContainSingle().Subject;
        negado.Entidade.Should().Be("Documento");
        negado.EntidadeId.Should().Be(documento);

        // A Produção também não usa as requisições do portal.
        (await ExecutarAsync(_pedro, new ListarMinhasSolicitacoes())).Erro.Should().Be(TipoErro.AcessoNegado);
        (await ExecutarAsync(_pedro, new ObterMinhaSolicitacao(id))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await ExecutarAsync(_pedro, new CancelarMinhaSolicitacao(id, 0))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await ExecutarAsync(_pedro, new RemoverDocumentoDaSolicitacao(documento, 0))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await LerAsync(_carla, id)).Situacao.Should().Be(SituacaoParaOCliente.AguardandoAnalise);
    }

    [Fact]
    public async Task Diretoria_baixa_documento_de_solicitacao_mas_nao_usa_as_requisicoes_do_portal()
    {
        var id = await AbertaAsync(_carla, Arquivo("um.pdf", "texto do anexo"));
        var documento = (await LerAsync(_carla, id)).Documentos.Single().Id;

        var (baixado, texto) = await BaixarAsync(_admin, documento);

        baixado.Sucesso.Should().BeTrue(baixado.Mensagem);
        texto.Should().Be("texto do anexo");
        (await ExecutarAsync(_admin, new ListarMinhasSolicitacoes())).Erro.Should().Be(TipoErro.AcessoNegado);
        (await ExecutarAsync(_admin, new ObterMinhaSolicitacao(id))).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Consultas_do_portal_ficam_registradas()
    {
        var id = await AbertaAsync(_carla);

        (await ListaAsync(_carla, SituacaoParaOCliente.AguardandoAnalise)).Sucesso.Should().BeTrue();
        (await FichaAsync(_carla, id)).Sucesso.Should().BeTrue();
        // Consulta recusada não é consulta feita.
        (await FichaAsync(_joao, id)).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await ListaAsync(_joao, (SituacaoParaOCliente)99)).Erro.Should().Be(TipoErro.Validacao);

        var daCarla = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(r => r.UsuarioId == _carla).OrderBy(r => r.DataHora).ToListAsync());
        daCarla.Should().HaveCount(2);
        daCarla.Should().OnlyContain(r => r.TipoRecurso == "Solicitacao");
        var listou = daCarla.Should().ContainSingle(r => r.Acao == AcaoDeAcesso.Listou).Subject;
        listou.RecursoId.Should().BeNull();
        listou.Detalhe.Should().Be("""{"Situacao":"AguardandoAnalise","Pagina":1}""");
        daCarla.Should().ContainSingle(r => r.Acao == AcaoDeAcesso.Visualizou).Which.RecursoId.Should().Be(id);

        var doJoao = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(r => r.UsuarioId == _joao).ToListAsync());
        doJoao.Should().BeEmpty();
    }

    [Fact]
    public async Task Gravacoes_ficam_na_auditoria()
    {
        var id = await AbertaAsync(_carla, Arquivo("um.pdf"));
        var primeiro = (await LerAsync(_carla, id)).Documentos.Single().Id;
        (await AlterarAsync(_carla, id, await VersaoAsync(_carla, id), "Descrição auditada.")).Sucesso.Should().BeTrue();
        var anexo = await AnexarAsync(_carla, id, await VersaoAsync(_carla, id), "dois.pdf");
        anexo.Sucesso.Should().BeTrue(anexo.Mensagem);
        (await RemoverAsync(_carla, primeiro, await VersaoAsync(_carla, id))).Sucesso.Should().BeTrue();
        (await CancelarAsync(_carla, id, await VersaoAsync(_carla, id))).Sucesso.Should().BeTrue();

        var registros = await AuditoriaAsync();

        // Abertura, alteração e cancelamento; anexar e remover só mexem no controle técnico e não geram registro da solicitação.
        var daSolicitacao = registros.Where(r => r.Entidade == "SolicitacaoDoCliente" && r.EntidadeId == id).ToList();
        daSolicitacao.Select(r => r.Operacao).Should().BeEquivalentTo(
            [OperacaoAuditada.Criacao, OperacaoAuditada.Alteracao, OperacaoAuditada.Alteracao]);
        daSolicitacao.Should().OnlyContain(r => r.UsuarioId == _carla);

        var criacao = JsonDocument.Parse(daSolicitacao.Single(r => r.Operacao == OperacaoAuditada.Criacao).Dados!).RootElement;
        criacao.GetProperty("Descricao").GetString().Should().Be(Descricao);
        criacao.GetProperty("ClienteId").GetGuid().Should().Be(_maria);
        criacao.GetProperty("SolicitanteId").GetGuid().Should().Be(_carla);

        var alteracoes = daSolicitacao.Where(r => r.Operacao == OperacaoAuditada.Alteracao)
            .Select(r => JsonDocument.Parse(r.Dados!).RootElement).ToList();
        var alteracao = alteracoes.Single(d => d.TryGetProperty("Descricao", out _));
        alteracao.GetProperty("Descricao").GetProperty("de").GetString().Should().Be(Descricao);
        alteracao.GetProperty("Descricao").GetProperty("para").GetString().Should().Be("Descrição auditada.");
        alteracao.GetProperty("Urgencia").GetProperty("para").GetString().Should().Be("Alta");
        alteracoes.Count(d => d.TryGetProperty("CanceladaEm", out _)).Should().Be(1, "o cancelamento é uma alteração só dele");

        var dosDocumentos = registros.Where(r => r.Entidade == "Documento").ToList();
        dosDocumentos.Where(r => r.Operacao == OperacaoAuditada.Criacao).Select(r => r.EntidadeId)
            .Should().BeEquivalentTo([primeiro, anexo.Valor]);
        dosDocumentos.Should().ContainSingle(r => r.Operacao == OperacaoAuditada.Exclusao).Which.EntidadeId.Should().Be(primeiro);
        dosDocumentos.Should().OnlyContain(r => r.UsuarioId == _carla);
    }

    /// <summary>Um fluxo de zeros do tamanho pedido, sem alocar o conteúdo.</summary>
    private sealed class FluxoDeZeros(long tamanho) : Stream
    {
        private long _lidos;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => tamanho;

        public override long Position
        {
            get => _lidos;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => Ler(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer) => Ler(buffer);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Ler(buffer.Span));

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int Ler(Span<byte> destino)
        {
            var quantos = (int)Math.Min(destino.Length, tamanho - _lidos);
            destino[..quantos].Clear();
            _lidos += quantos;
            return quantos;
        }
    }
}
