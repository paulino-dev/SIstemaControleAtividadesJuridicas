using System.Text;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Application.Solicitacoes;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Domain.Solicitacoes;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadeDaSolicitacaoTests(BancoFixture banco) : TesteComBanco(banco)
{
    private const string Descricao = "Preciso de uma certidão atualizada.";
    private const string Alterada = "O cliente alterou a solicitação enquanto você criava a atividade. Confira os dados atualizados.";
    private const string JaVirouAtividade = "Esta solicitação já virou atividade.";
    private const string Cancelada = "Cliente cancelou a solicitação, mesmo assim deseja criar a atividade?";

    private Guid _admin, _pedro, _maria, _jose, _carla, _joao;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _pedro = (await ExecutarAsync(_admin, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null))).Valor;
        _maria = await ClienteAsync("Maria Silva", "529.982.247-25");
        _jose = await ClienteAsync("José Souza", "111.444.777-35");
        _carla = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _maria));
        _joao = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _jose, "João Cliente", "joao@x.com"));
    }

    private async Task<Guid> ClienteAsync(string nome, string cpf) =>
        (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada(nome, null, null, new EnderecoEntrada(), null), cpf, null, null))).Valor;

    private static ArquivoParaEnviar Arquivo(string nome, string texto = "conteúdo") =>
        new(nome, "application/pdf", () => new MemoryStream(Encoding.UTF8.GetBytes(texto)));

    private async Task<Guid> AbertaAsync(Guid ator, params ArquivoParaEnviar[] arquivos)
    {
        var resultado = await ExecutarAsync(ator, new AbrirSolicitacao(Descricao, UrgenciaDaSolicitacao.Media, arquivos));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    /// <summary>A ficha da Diretoria: o que o formulário de criação lê, inclusive a versão.</summary>
    private async Task<SolicitacaoDetalhe> FichaAsync(Guid id)
    {
        var ficha = await ExecutarAsync(_admin, new ObterSolicitacao(id));
        ficha.Sucesso.Should().BeTrue(ficha.Mensagem);
        return ficha.Valor!;
    }

    private Task<Resultado<Guid>> CriarAsync(
        Guid solicitacao, uint versao, IReadOnlyList<Guid>? documentos = null, bool comentario = false,
        bool confirmou = false, string titulo = "Preparar a certidão") =>
        ExecutarAsync(_admin, new CriarAtividadeDaSolicitacao(
            solicitacao, versao, titulo, "Prazo de 15 dias.", new DateOnly(2090, 6, 15), null, null, 1, comentario,
            documentos ?? [], confirmou));

    private async Task<Guid> CriadaAsync(Guid solicitacao, IReadOnlyList<Guid>? documentos = null)
    {
        var criada = await CriarAsync(solicitacao, (await FichaAsync(solicitacao)).Versao, documentos);
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        return criada.Valor;
    }

    private async Task CancelarAsync(Guid ator, Guid id)
    {
        var versao = (await ExecutarAsync(ator, new ObterMinhaSolicitacao(id))).Valor!.Versao;
        var cancelada = await ExecutarAsync(ator, new CancelarMinhaSolicitacao(id, versao));
        cancelada.Sucesso.Should().BeTrue(cancelada.Mensagem);
    }

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

    private Task<T> NoBancoAsync<T>(Func<AppDbContext, Task<T>> consulta) =>
        EmEscopoAsync(null, sp => consulta(sp.GetRequiredService<AppDbContext>()));

    /// <summary>As atividades vivas que nasceram da solicitação.</summary>
    private Task<List<Atividade>> AtividadesDaAsync(Guid solicitacao) =>
        NoBancoAsync(db => db.Atividades.AsNoTracking().Where(a => a.SolicitacaoId == solicitacao).ToListAsync());

    private Task<int> ComentariosNoBancoAsync() =>
        NoBancoAsync(db => db.Set<AtividadeComentario>().AsNoTracking().CountAsync());

    private Task<List<Documento>> DocumentosDeAtividadeNoBancoAsync() =>
        NoBancoAsync(db => db.Documentos.AsNoTracking()
            .Where(d => d.Categoria == CategoriaDocumento.DocumentoAtividade).ToListAsync());

    private async Task NadaFoiCriadoAsync(Guid solicitacao)
    {
        (await AtividadesDaAsync(solicitacao)).Should().BeEmpty();
        (await ComentariosNoBancoAsync()).Should().Be(0);
        (await DocumentosDeAtividadeNoBancoAsync()).Should().BeEmpty();
        (await FichaAsync(solicitacao)).AtividadeId.Should().BeNull();
    }

    private int ArquivosNoDisco() =>
        Directory.Exists(PastaDeArquivos) ? Directory.GetFiles(PastaDeArquivos, "*", SearchOption.AllDirectories).Length : 0;

    [Fact]
    public async Task Diretoria_cria_com_o_comentario_e_os_documentos_e_a_solicitacao_passa_a_em_atividade()
    {
        var id = await AbertaAsync(_carla, Arquivo("certidao.pdf"), Arquivo("procuracao.pdf"));
        var ficha = await FichaAsync(id);

        var criada = await CriarAsync(id, ficha.Versao, [.. ficha.Documentos.Select(d => d.Id)], comentario: true);

        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        var atividade = (await AtividadesDaAsync(id)).Should().ContainSingle().Subject;
        atividade.Id.Should().Be(criada.Valor);
        atividade.ClienteId.Should().Be(_maria);
        atividade.SolicitacaoId.Should().Be(id);
        atividade.AutorId.Should().Be(_admin);

        var depois = await FichaAsync(id);
        depois.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);
        depois.AtividadeId.Should().Be(criada.Valor);
        depois.AtividadeTitulo.Should().Be("Preparar a certidão");
        depois.Versao.Should().NotBe(ficha.Versao);

        var comentarios = (await ExecutarAsync(_admin, new ListarComentariosDaAtividade(criada.Valor))).Valor!;
        var comentario = comentarios.Should().ContainSingle().Subject;
        comentario.Texto.Should().Be(Descricao);
        comentario.UsuarioId.Should().Be(_admin);
        comentario.UsuarioNome.Should().Be("Diana Diretora");

        var documentos = await DocumentosDeAtividadeNoBancoAsync();
        documentos.Select(d => d.Nome).Should().BeEquivalentTo("certidao.pdf", "procuracao.pdf");
        documentos.Should().OnlyContain(d => d.AtividadeId == criada.Valor && d.ClienteId == _maria);

        var daSolicitacao = await ExecutarAsync(_carla, new ObterMinhaSolicitacao(id));
        daSolicitacao.Valor!.Situacao.Should().Be(SituacaoParaOCliente.EmAndamento);
        daSolicitacao.Valor.AtividadeTitulo.Should().Be("Preparar a certidão");
    }

    [Fact]
    public async Task Cliente_cancela_depois_de_o_diretor_carregar()
    {
        var id = await AbertaAsync(_carla);
        var lida = await FichaAsync(id);
        await CancelarAsync(_carla, id);

        var semConfirmar = await CriarAsync(id, lida.Versao);
        semConfirmar.Erro.Should().Be(TipoErro.ConfirmacaoNecessaria);
        semConfirmar.Mensagem.Should().Be(Cancelada);

        var versaoAntiga = await CriarAsync(id, lida.Versao, confirmou: true);
        versaoAntiga.Erro.Should().Be(TipoErro.Conflito);
        versaoAntiga.Mensagem.Should().Be(Alterada);

        await NadaFoiCriadoAsync(id);
        var relida = await FichaAsync(id);
        relida.Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);
        relida.Versao.Should().NotBe(lida.Versao, "o cancelamento muda a versão");

        var criada = await CriarAsync(id, relida.Versao, confirmou: true);

        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        var depois = await FichaAsync(id);
        depois.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);
        depois.AtividadeId.Should().Be(criada.Valor);
        depois.CanceladaEm.Should().NotBeNull("o cancelamento fica registrado");
    }

    [Fact]
    public async Task Cliente_remove_documento_depois_de_o_diretor_carregar()
    {
        var id = await AbertaAsync(_carla, Arquivo("um.pdf", "primeiro"), Arquivo("dois.pdf", "segundo"));
        var lida = await FichaAsync(id);
        var marcados = lida.Documentos.Select(d => d.Id).ToList();
        var removido = lida.Documentos.Single(d => d.Nome == "um.pdf").Id;
        var versaoDoCliente = (await ExecutarAsync(_carla, new ObterMinhaSolicitacao(id))).Valor!.Versao;
        (await ExecutarAsync(_carla, new RemoverDocumentoDaSolicitacao(removido, versaoDoCliente))).Sucesso.Should().BeTrue();

        var criada = await CriarAsync(id, lida.Versao, marcados);

        criada.Erro.Should().Be(TipoErro.Conflito, "a versão mudou: é alteração, não documento inválido");
        criada.Mensagem.Should().Be(Alterada);
        await NadaFoiCriadoAsync(id);

        // Depois de recarregar, a versão confere e o documento que saiu é o que a recusa aponta.
        var relida = await FichaAsync(id);
        relida.Documentos.Select(d => d.Nome).Should().Equal("dois.pdf");
        var comORemovido = await CriarAsync(id, relida.Versao, marcados);
        comORemovido.Erro.Should().Be(TipoErro.Validacao);
        comORemovido.Mensagem.Should().Be("Um documento marcado não está mais nesta solicitação.");
        await NadaFoiCriadoAsync(id);

        var certa = await CriarAsync(id, relida.Versao, [.. relida.Documentos.Select(d => d.Id)]);
        certa.Sucesso.Should().BeTrue(certa.Mensagem);
        (await DocumentosDeAtividadeNoBancoAsync()).Select(d => d.Nome).Should().Equal("dois.pdf");
    }

    [Fact]
    public async Task Duas_criacoes_simultaneas_so_uma_passa()
    {
        var id = await AbertaAsync(_carla, Arquivo("um.pdf", "primeiro"), Arquivo("dois.pdf", "segundo"));
        var ficha = await FichaAsync(id);
        var marcados = ficha.Documentos.Select(d => d.Id).ToList();

        var resultados = await Task.WhenAll(
            CriarAsync(id, ficha.Versao, marcados, comentario: true, titulo: "Primeira"),
            CriarAsync(id, ficha.Versao, marcados, comentario: true, titulo: "Segunda"));

        resultados.Count(r => r.Sucesso).Should().Be(1);
        var perdeu = resultados.Single(r => !r.Sucesso);
        perdeu.Erro.Should().Be(TipoErro.Conflito);
        perdeu.Mensagem.Should().BeOneOf(Alterada, JaVirouAtividade);
        var vencedora = resultados.Single(r => r.Sucesso).Valor;

        (await AtividadesDaAsync(id)).Should().ContainSingle().Which.Id.Should().Be(vencedora);
        (await ComentariosNoBancoAsync()).Should().Be(1, "o comentário é o de uma criação só");
        var levados = await DocumentosDeAtividadeNoBancoAsync();
        levados.Should().HaveCount(2, "os documentos são os de uma criação só").And.OnlyContain(d => d.AtividadeId == vencedora);
        (await FichaAsync(id)).AtividadeId.Should().Be(vencedora);
    }

    [Fact]
    public async Task Excluir_a_atividade_devolve_a_solicitacao_e_permite_criar_outra()
    {
        var id = await AbertaAsync(_carla);
        var primeira = await CriadaAsync(id);
        (await ExecutarAsync(_admin, new ObterAtividade(primeira))).Valor!.SolicitacaoId.Should().Be(id);

        var excluida = await ExecutarAsync(_admin, new ExcluirAtividade(primeira));

        excluida.Sucesso.Should().BeTrue(excluida.Mensagem);
        var devolvida = await FichaAsync(id);
        devolvida.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        devolvida.AtividadeId.Should().BeNull();
        var doCliente = await ExecutarAsync(_carla, new ObterMinhaSolicitacao(id));
        doCliente.Valor!.Situacao.Should().Be(SituacaoParaOCliente.AguardandoAnalise);
        (await AtividadesDaAsync(id)).Should().BeEmpty("a atividade excluída não conta mais");

        var segunda = await CriarAsync(id, devolvida.Versao, titulo: "Preparar a certidão de novo");

        segunda.Sucesso.Should().BeTrue(segunda.Mensagem);
        segunda.Valor.Should().NotBe(primeira);
        var depois = await FichaAsync(id);
        depois.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);
        depois.AtividadeId.Should().Be(segunda.Valor);
        (await AtividadesDaAsync(id)).Should().ContainSingle().Which.Id.Should().Be(segunda.Valor);
    }

    [Fact]
    public async Task Excluir_a_atividade_de_solicitacao_cancelada_devolve_a_solicitacao_cancelada()
    {
        var id = await AbertaAsync(_carla);
        await CancelarAsync(_carla, id);
        var versao = (await FichaAsync(id)).Versao;
        var criada = await CriarAsync(id, versao, confirmou: true);
        criada.Sucesso.Should().BeTrue(criada.Mensagem);

        var excluida = await ExecutarAsync(_admin, new ExcluirAtividade(criada.Valor));

        excluida.Sucesso.Should().BeTrue(excluida.Mensagem);
        var devolvida = await FichaAsync(id);
        devolvida.Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);
        devolvida.AtividadeId.Should().BeNull();
        devolvida.CanceladaEm.Should().NotBeNull("o cancelamento do cliente continua registrado");
        (await ExecutarAsync(_carla, new ObterMinhaSolicitacao(id))).Valor!.Situacao.Should().Be(SituacaoParaOCliente.Cancelada);
    }

    [Fact]
    public async Task Documento_levado_baixa_o_mesmo_conteudo()
    {
        var id = await AbertaAsync(_carla, Arquivo("laudo.pdf", "o laudo da Maria"));
        var daSolicitacao = (await FichaAsync(id)).Documentos.Single().Id;
        var arquivosAntes = ArquivosNoDisco();

        var atividade = await CriadaAsync(id, [daSolicitacao]);

        var levado = (await ExecutarAsync(_admin, new ListarDocumentosDaAtividade(atividade))).Valor!.Itens.Single().Id;
        levado.Should().NotBe(daSolicitacao);
        ArquivosNoDisco().Should().Be(arquivosAntes, "o documento levado usa o mesmo arquivo, não uma cópia");
        var documentos = await NoBancoAsync(db => db.Documentos.AsNoTracking()
            .Where(d => d.Id == daSolicitacao || d.Id == levado).ToListAsync());
        documentos.Select(d => d.ChaveArmazenamento).Distinct().Should().ContainSingle();
        documentos.Select(d => d.HashSha256).Distinct().Should().ContainSingle();

        var doLevado = await BaixarAsync(_admin, levado);
        var daOrigem = await BaixarAsync(_admin, daSolicitacao);
        doLevado.Texto.Should().Be("o laudo da Maria");
        daOrigem.Texto.Should().Be("o laudo da Maria");

        (await ExecutarAsync(_admin, new ExcluirDocumento(levado))).Sucesso.Should().BeTrue();

        (await BaixarAsync(_admin, levado)).Resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        (await BaixarAsync(_admin, daSolicitacao)).Texto.Should().Be("o laudo da Maria", "excluir um não afeta o outro");
        (await BaixarAsync(_carla, daSolicitacao)).Texto.Should().Be("o laudo da Maria");
        ArquivosNoDisco().Should().Be(arquivosAntes, "a exclusão é lógica e o arquivo permanece");
    }

    [Fact]
    public async Task Producao_responsavel_baixa_o_levado_e_nao_o_da_solicitacao()
    {
        var id = await AbertaAsync(_carla, Arquivo("laudo.pdf", "o laudo da Maria"));
        var daSolicitacao = (await FichaAsync(id)).Documentos.Single().Id;
        var atividade = await CriadaAsync(id, [daSolicitacao]);
        var levado = (await ExecutarAsync(_admin, new ListarDocumentosDaAtividade(atividade))).Valor!.Itens.Single().Id;
        var definido = await ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            atividade, [new ResponsavelEntrada(_pedro, "Redigir")], _admin));
        definido.Sucesso.Should().BeTrue(definido.Mensagem);

        var doLevado = await BaixarAsync(_pedro, levado);
        var daOrigem = await BaixarAsync(_pedro, daSolicitacao);

        doLevado.Texto.Should().Be("o laudo da Maria");
        daOrigem.Resultado.Erro.Should().Be(TipoErro.NaoEncontrado, "o documento da solicitação nunca é da Produção");
    }

    [Fact]
    public async Task Usuario_do_cliente_B_nao_alcanca_o_que_veio_do_cliente_A()
    {
        var id = await AbertaAsync(_carla, Arquivo("laudo.pdf", "o laudo da Maria"));
        var daSolicitacao = (await FichaAsync(id)).Documentos.Single().Id;
        var atividade = await CriadaAsync(id, [daSolicitacao]);
        var levado = (await ExecutarAsync(_admin, new ListarDocumentosDaAtividade(atividade))).Valor!.Itens.Single().Id;

        var ficha = await ExecutarAsync(_joao, new ObterMinhaSolicitacao(id));
        var baixaDoLevado = await BaixarAsync(_joao, levado);
        var baixaDaOrigem = await BaixarAsync(_joao, daSolicitacao);

        ficha.Erro.Should().Be(TipoErro.NaoEncontrado);
        baixaDoLevado.Resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        baixaDaOrigem.Resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        (await BaixarAsync(_carla, levado)).Resultado.Erro.Should().Be(TipoErro.NaoEncontrado,
            "o documento da atividade não é posto para o cliente, nem para o dono da solicitação");
    }
}
