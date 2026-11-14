using System.Text;
using AtividadesJuridicas.Application.Atividades;
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
public class SolicitacoesDaDiretoriaTests(BancoFixture banco) : TesteComBanco(banco)
{
    private const string Descricao = "Preciso de uma certidão atualizada.";

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

    private async Task<Guid> AbertaAsync(
        Guid ator, string descricao = Descricao, UrgenciaDaSolicitacao urgencia = UrgenciaDaSolicitacao.Media,
        params ArquivoParaEnviar[] arquivos)
    {
        var resultado = await ExecutarAsync(ator, new AbrirSolicitacao(descricao, urgencia, arquivos));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private Task<Resultado<Pagina<SolicitacaoResumo>>> ListaAsync(
        Guid ator, SituacaoDaSolicitacao? situacao = null, Guid? cliente = null, int pagina = 1) =>
        ExecutarAsync(ator, new ListarSolicitacoes(situacao, cliente, pagina));

    private async Task<Pagina<SolicitacaoResumo>> ListadasAsync(
        SituacaoDaSolicitacao? situacao = null, Guid? cliente = null, int pagina = 1)
    {
        var lista = await ListaAsync(_admin, situacao, cliente, pagina);
        lista.Sucesso.Should().BeTrue(lista.Mensagem);
        return lista.Valor!;
    }

    private async Task<SolicitacaoDetalhe> FichaAsync(Guid id)
    {
        var ficha = await ExecutarAsync(_admin, new ObterSolicitacao(id));
        ficha.Sucesso.Should().BeTrue(ficha.Mensagem);
        return ficha.Valor!;
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

    private async Task CancelarAsync(Guid ator, Guid id)
    {
        var versao = (await ExecutarAsync(ator, new ObterMinhaSolicitacao(id))).Valor!.Versao;
        var cancelada = await ExecutarAsync(ator, new CancelarMinhaSolicitacao(id, versao));
        cancelada.Sucesso.Should().BeTrue(cancelada.Mensagem);
    }

    private Task ExecutarSqlAsync(FormattableString sql) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Database.ExecuteSqlAsync(sql));

    [Fact]
    public async Task Diretoria_lista_da_mais_antiga_para_a_mais_nova_com_filtros()
    {
        var primeira = await AbertaAsync(_carla, "Primeira, da Maria.", UrgenciaDaSolicitacao.Alta, Arquivo("um.pdf"), Arquivo("dois.pdf"));
        var segunda = await AbertaAsync(_joao, "Segunda, do José.", UrgenciaDaSolicitacao.Baixa);
        var terceira = await AbertaAsync(_carla, "Terceira, da Maria, cancelada.");
        await CancelarAsync(_carla, terceira);

        var todas = await ListadasAsync();

        todas.Itens.Select(s => s.Id).Should().Equal(primeira, segunda, terceira);
        todas.Total.Should().Be(3);
        todas.Numero.Should().Be(1);
        todas.Tamanho.Should().Be(SolicitacoesOpcoes.TamanhoDaPagina);

        var linha = todas.Itens[0];
        linha.AbertaEm.Kind.Should().Be(DateTimeKind.Utc);
        linha.AbertaEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        linha.ClienteId.Should().Be(_maria);
        linha.ClienteNome.Should().Be("Maria Silva");
        linha.SolicitanteNome.Should().Be("Carla Cliente");
        linha.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        linha.Trecho.Should().Be("Primeira, da Maria.");
        linha.Documentos.Should().Be(2);
        linha.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        todas.Itens[1].ClienteNome.Should().Be("José Souza");
        todas.Itens[1].SolicitanteNome.Should().Be("João Cliente");
        todas.Itens[1].Documentos.Should().Be(0);
        todas.Itens[2].Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);

        (await ListadasAsync(SituacaoDaSolicitacao.Aguardando)).Itens.Select(s => s.Id).Should().Equal(primeira, segunda);
        (await ListadasAsync(SituacaoDaSolicitacao.Cancelada)).Itens.Select(s => s.Id).Should().Equal(terceira);
        (await ListadasAsync(SituacaoDaSolicitacao.EmAtividade)).Itens.Should().BeEmpty();
        (await ListadasAsync(cliente: _maria)).Itens.Select(s => s.Id).Should().Equal(primeira, terceira);
        (await ListadasAsync(cliente: _jose)).Itens.Select(s => s.Id).Should().Equal(segunda);
        (await ListadasAsync(SituacaoDaSolicitacao.Cancelada, _maria)).Itens.Select(s => s.Id).Should().Equal(terceira);
        (await ListadasAsync(SituacaoDaSolicitacao.Cancelada, _jose)).Itens.Should().BeEmpty();
        (await ListadasAsync(cliente: Guid.CreateVersion7())).Itens.Should().BeEmpty();

        var segundaPagina = await ListadasAsync(pagina: 2);
        segundaPagina.Itens.Should().BeEmpty();
        segundaPagina.Total.Should().Be(3);
        segundaPagina.Numero.Should().Be(2);
    }

    [Fact]
    public async Task Lista_corta_a_descricao_em_um_trecho_e_pagina_de_vinte()
    {
        var longa = new string('a', SolicitacoesOpcoes.TamanhoDoTrecho + 50);
        var ids = new List<Guid> { await AbertaAsync(_carla, longa) };
        for (var i = 1; i <= SolicitacoesOpcoes.TamanhoDaPagina; i++) ids.Add(await AbertaAsync(_joao, $"Pedido {i}."));

        var primeira = await ListadasAsync();
        var segunda = await ListadasAsync(pagina: 2);

        primeira.Itens.Select(s => s.Id).Should().Equal(ids.Take(SolicitacoesOpcoes.TamanhoDaPagina));
        primeira.Total.Should().Be(SolicitacoesOpcoes.TamanhoDaPagina + 1);
        primeira.Itens[0].Trecho.Should().Be(new string('a', SolicitacoesOpcoes.TamanhoDoTrecho) + "…");
        segunda.Itens.Select(s => s.Id).Should().Equal(ids[^1]);
        (await ListadasAsync(pagina: 0)).Numero.Should().Be(1);
    }

    [Fact]
    public async Task Lista_e_ficha_nao_trazem_a_solicitacao_excluida_e_mantem_os_nomes_dos_excluidos()
    {
        var excluida = await AbertaAsync(_carla, "Esta será excluída.");
        var doJoao = await AbertaAsync(_joao, "Pedido do João.", UrgenciaDaSolicitacao.Alta, Arquivo("um.pdf"));
        var agora = DateTime.UtcNow;

        await ExecutarSqlAsync($"UPDATE solicitacoes_do_cliente SET excluido_em = {agora} WHERE id = {excluida}");

        (await ListadasAsync()).Itens.Select(s => s.Id).Should().Equal(doJoao);
        var inexistente = await ExecutarAsync(_admin, new ObterSolicitacao(excluida));
        inexistente.Erro.Should().Be(TipoErro.NaoEncontrado);
        inexistente.Mensagem.Should().Be("Solicitação não encontrada.");
        (await ExecutarAsync(_admin, new ObterSolicitacao(Guid.CreateVersion7()))).Erro.Should().Be(TipoErro.NaoEncontrado);

        // O José e o João saem do sistema; a solicitação deles continua dizendo quem são.
        await ExecutarSqlAsync($"UPDATE usuarios SET excluido_em = {agora} WHERE id = {_joao}");
        await ExecutarSqlAsync($"UPDATE clientes SET excluido_em = {agora} WHERE id = {_jose}");

        var linha = (await ListadasAsync()).Itens.Should().ContainSingle().Subject;
        linha.ClienteNome.Should().Be("José Souza");
        linha.SolicitanteNome.Should().Be("João Cliente");
        var ficha = await FichaAsync(doJoao);
        ficha.ClienteNome.Should().Be("José Souza");
        ficha.SolicitanteNome.Should().Be("João Cliente");
        ficha.Documentos.Should().ContainSingle();
    }

    [Fact]
    public async Task Diretoria_le_a_ficha_com_documentos_e_quem_abriu()
    {
        var id = await AbertaAsync(
            _carla, Descricao, UrgenciaDaSolicitacao.Alta, Arquivo("um.pdf", "primeiro conteúdo"), Arquivo("dois.pdf", "segundo conteúdo"));
        var versaoDoPortal = (await ExecutarAsync(_carla, new ObterMinhaSolicitacao(id))).Valor!.Versao;

        var ficha = await FichaAsync(id);

        ficha.Id.Should().Be(id);
        ficha.AbertaEm.Kind.Should().Be(DateTimeKind.Utc);
        ficha.AbertaEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        ficha.ClienteId.Should().Be(_maria);
        ficha.ClienteNome.Should().Be("Maria Silva");
        ficha.SolicitanteId.Should().Be(_carla);
        ficha.SolicitanteNome.Should().Be("Carla Cliente");
        ficha.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        ficha.Descricao.Should().Be(Descricao);
        ficha.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        ficha.CanceladaEm.Should().BeNull();
        ficha.AtividadeId.Should().BeNull();
        ficha.AtividadeTitulo.Should().BeNull();
        ficha.Versao.Should().Be(versaoDoPortal, "é a mesma versão que o cliente lê, e a Fase B a devolve ao agir");
        // Os dois foram gravados no mesmo instante: vêm pelo nome.
        ficha.Documentos.Select(d => d.Nome).Should().Equal("dois.pdf", "um.pdf");
        ficha.Documentos.Should().OnlyContain(d => d.TamanhoBytes > 0 && d.EnviadoPor == null);
    }

    [Fact]
    public async Task Ficha_da_cancelada_traz_a_data_e_acompanha_a_versao()
    {
        var id = await AbertaAsync(_carla);
        var antes = await FichaAsync(id);

        await CancelarAsync(_carla, id);
        var depois = await FichaAsync(id);

        depois.Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);
        depois.CanceladaEm.Should().NotBeNull();
        depois.CanceladaEm!.Value.Kind.Should().Be(DateTimeKind.Utc);
        depois.CanceladaEm.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        depois.Versao.Should().NotBe(antes.Versao);
    }

    [Fact]
    public async Task Ficha_e_lista_mostram_a_atividade_vinculada_pelo_que_esta_gravado()
    {
        var id = await AbertaAsync(_carla);
        var atividade = await ExecutarAsync(_admin, new SalvarAtividade(
            null, "Certidão da Maria", "Texto.", new DateOnly(2030, 1, 15), _maria, null, null, 1));
        atividade.Sucesso.Should().BeTrue(atividade.Mensagem);
        await ExecutarSqlAsync($"UPDATE solicitacoes_do_cliente SET atividade_id = {atividade.Valor} WHERE id = {id}");

        var ficha = await FichaAsync(id);

        ficha.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);
        ficha.AtividadeId.Should().Be(atividade.Valor);
        ficha.AtividadeTitulo.Should().Be("Certidão da Maria");
        (await ListadasAsync(SituacaoDaSolicitacao.EmAtividade)).Itens.Select(s => s.Id).Should().Equal(id);
        (await ListadasAsync(SituacaoDaSolicitacao.Aguardando)).Itens.Should().BeEmpty();

        // A atividade excluída não dá título; o vínculo continua como está gravado.
        await ExecutarSqlAsync($"UPDATE atividades SET excluido_em = {DateTime.UtcNow} WHERE id = {atividade.Valor}");

        var semAtividade = await FichaAsync(id);
        semAtividade.AtividadeId.Should().Be(atividade.Valor);
        semAtividade.AtividadeTitulo.Should().BeNull();
    }

    [Fact]
    public async Task Diretoria_baixa_documento_da_solicitacao()
    {
        var id = await AbertaAsync(_carla, Descricao, UrgenciaDaSolicitacao.Media, Arquivo("um.pdf", "texto do anexo"));
        var documento = (await FichaAsync(id)).Documentos.Single();

        var (baixado, texto) = await BaixarAsync(_admin, documento.Id);

        baixado.Sucesso.Should().BeTrue(baixado.Mensagem);
        baixado.Valor!.NomeArquivo.Should().Be("um.pdf");
        texto.Should().Be("texto do anexo");
    }

    [Fact]
    public async Task Diretoria_nao_renomeia_nem_exclui_documento_da_solicitacao()
    {
        var id = await AbertaAsync(_carla, Descricao, UrgenciaDaSolicitacao.Media, Arquivo("um.pdf", "texto do anexo"));
        var antes = await FichaAsync(id);
        var documento = antes.Documentos.Single();

        var renomear = await ExecutarAsync(_admin, new RenomearDocumento(documento.Id, "Outro nome"));
        var excluir = await ExecutarAsync(_admin, new ExcluirDocumento(documento.Id));

        renomear.Erro.Should().Be(TipoErro.Validacao);
        renomear.Mensagem.Should().Be("Documento de solicitação só é alterado pelo cliente que a abriu.");
        excluir.Erro.Should().Be(TipoErro.Validacao);
        excluir.Mensagem.Should().Be("Documento de solicitação só é alterado pelo cliente que a abriu.");
        var depois = await FichaAsync(id);
        depois.Versao.Should().Be(antes.Versao);
        depois.Documentos.Should().ContainSingle().Which.Should().Be(documento);
        (await BaixarAsync(_carla, documento.Id)).Texto.Should().Be("texto do anexo");
    }

    [Fact]
    public async Task Producao_e_cliente_nao_consultam_as_solicitacoes_da_diretoria()
    {
        var id = await AbertaAsync(_carla, Descricao, UrgenciaDaSolicitacao.Media, Arquivo("um.pdf"));

        foreach (var ator in new[] { _pedro, _carla, _joao })
        {
            var lista = await ExecutarAsync(ator, new ListarSolicitacoes());
            var ficha = await ExecutarAsync(ator, new ObterSolicitacao(id));

            lista.Erro.Should().Be(TipoErro.AcessoNegado);
            ficha.Erro.Should().Be(TipoErro.AcessoNegado);
            lista.Valor.Should().BeNull();
            ficha.Valor.Should().BeNull();
        }

        // A solicitação segue intacta, e as recusas ficam como tentativas de acesso negadas.
        (await FichaAsync(id)).Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        var negados = (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado).ToList();
        negados.Count(r => r.UsuarioId == _pedro).Should().Be(2);
        negados.Count(r => r.UsuarioId == _carla).Should().Be(2);
        negados.Count(r => r.UsuarioId == _joao).Should().Be(2);
    }

    [Fact]
    public async Task Situacao_invalida_e_recusada_com_o_banco()
    {
        await AbertaAsync(_carla);

        var resultado = await ListaAsync(_admin, (SituacaoDaSolicitacao)99);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Situação inválida.");
    }

    [Fact]
    public async Task Consultas_da_diretoria_ficam_registradas()
    {
        var id = await AbertaAsync(_carla);

        (await ListaAsync(_admin, SituacaoDaSolicitacao.Aguardando, _maria)).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_admin, new ObterSolicitacao(id))).Sucesso.Should().BeTrue();
        // Consulta recusada não é consulta feita.
        (await ExecutarAsync(_admin, new ObterSolicitacao(Guid.CreateVersion7()))).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await ListaAsync(_admin, (SituacaoDaSolicitacao)99)).Erro.Should().Be(TipoErro.Validacao);
        (await ListaAsync(_pedro)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await ExecutarAsync(_carla, new ListarSolicitacoes())).Erro.Should().Be(TipoErro.AcessoNegado);

        var registros = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(r => r.TipoRecurso == "Solicitacao" && r.UsuarioId != _carla).OrderBy(r => r.DataHora).ToListAsync());

        registros.Should().HaveCount(2);
        registros.Should().OnlyContain(r => r.UsuarioId == _admin);
        var listou = registros.Should().ContainSingle(r => r.Acao == AcaoDeAcesso.Listou).Subject;
        listou.RecursoId.Should().BeNull();
        listou.Detalhe.Should().Be($$"""{"Situacao":"Aguardando","ClienteId":"{{_maria}}","Pagina":1}""");
        registros.Should().ContainSingle(r => r.Acao == AcaoDeAcesso.Visualizou).Which.RecursoId.Should().Be(id);

        var daCarla = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(r => r.UsuarioId == _carla).ToListAsync());
        daCarla.Should().BeEmpty("o portão do cliente nega antes de qualquer consulta");
    }
}
