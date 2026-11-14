using System.Text;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Application.Solicitacoes;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Domain.Solicitacoes;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class SolicitacoesPersistenciaTests(BancoFixture banco) : TesteComBanco(banco)
{
    private Guid _admin, _pedro, _perfilProducao, _maria, _jose, _carla, _marcos, _joao;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        _perfilProducao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _pedro = (await ExecutarAsync(_admin, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", _perfilProducao, null))).Valor;
        _maria = await ClienteAsync("Maria Silva", "529.982.247-25");
        _jose = await ClienteAsync("José Souza", "111.444.777-35");
        _carla = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _maria));
        _marcos = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _maria, "Marcos Cliente", "marcos@x.com"));
        _joao = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _jose, "João Cliente", "joao@x.com"));
    }

    private async Task<Guid> ClienteAsync(string nome, string cpf) =>
        (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada(nome, null, null, new EnderecoEntrada(), null), cpf, null, null))).Valor;

    private static PostgresException PostgresDe(Exception erro)
    {
        for (var e = erro; e is not null; e = e.InnerException)
            if (e is PostgresException pg) return pg;
        throw new InvalidOperationException("Nenhuma PostgresException na cadeia de exceções: " + erro);
    }

    /// <summary>Abre a solicitação pelo domínio, como o usuário do cliente, e grava pelo repositório e pela unidade de trabalho.</summary>
    private Task<Guid> AbrirAsync(
        Guid cliente, Guid solicitante, string descricao = "Preciso de uma certidão atualizada.",
        UrgenciaDaSolicitacao urgencia = UrgenciaDaSolicitacao.Media) =>
        EmEscopoAsync(solicitante, async sp =>
        {
            var solicitacao = SolicitacaoDoCliente.Abrir(cliente, solicitante, descricao, urgencia);
            sp.GetRequiredService<ISolicitacaoRepositorio>().Adicionar(solicitacao);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return solicitacao.Id;
        });

    /// <summary>Altera a solicitação pelo domínio num escopo próprio e grava.</summary>
    private Task MudarAsync(Guid id, Guid ator, Action<SolicitacaoDoCliente> mudanca) =>
        EmEscopoAsync(ator, async sp =>
        {
            var solicitacao = (await sp.GetRequiredService<ISolicitacaoRepositorio>().ObterPorIdAsync(id, default))!;
            mudanca(solicitacao);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });

    private Task<SolicitacaoDoCliente> LerAsync(Guid id) =>
        EmEscopoAsync(null, async sp => (await sp.GetRequiredService<ISolicitacaoRepositorio>().ObterPorIdAsync(id, default))!);

    /// <summary>
    /// Grava o arquivo no armazenamento e o documento da solicitação pelo domínio, como o cliente que anexa.
    /// </summary>
    private Task<Guid> AnexarAsync(Guid solicitacao, Guid cliente, string nomeDoArquivo, Guid? ator = null) =>
        EmEscopoAsync(ator ?? _carla, async sp =>
        {
            await using var conteudo = new MemoryStream(Encoding.UTF8.GetBytes("conteúdo de " + nomeDoArquivo));
            var gravado = await sp.GetRequiredService<IArmazenamentoDeArquivos>()
                .GravarAsync(conteudo, DocumentosOpcoes.TamanhoMaximoBytes, default);
            var documento = Documento.CriarDaSolicitacao(
                solicitacao, cliente, null,
                new ArquivoDoDocumento(nomeDoArquivo, "application/pdf", gravado.TamanhoBytes, gravado.HashSha256, gravado.Chave));
            sp.GetRequiredService<IDocumentoRepositorio>().Adicionar(documento);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return documento.Id;
        });

    private Task RemoverDocumentoAsync(Guid documento) =>
        EmEscopoAsync(_carla, async sp =>
        {
            var repositorio = sp.GetRequiredService<IDocumentoRepositorio>();
            repositorio.Remover((await repositorio.ObterPorIdAsync(documento, default))!);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });

    private Task<Pagina<SolicitacaoDoPortal>> DoClienteAsync(
        Guid cliente, SituacaoParaOCliente? situacao = null, int pagina = 1, int tamanho = SolicitacoesOpcoes.TamanhoDaPagina) =>
        EmEscopoAsync(null, sp => sp.GetRequiredService<ISolicitacaoRepositorio>()
            .ListarDoClienteAsync(cliente, situacao, pagina, tamanho, default));

    private Task<Pagina<SolicitacaoResumo>> DaDiretoriaAsync(
        SituacaoDaSolicitacao? situacao = null, Guid? cliente = null, int pagina = 1, int tamanho = SolicitacoesOpcoes.TamanhoDaPagina) =>
        EmEscopoAsync(null, sp => sp.GetRequiredService<ISolicitacaoRepositorio>()
            .ListarAsync(situacao, cliente, pagina, tamanho, default));

    private Task<int> ComandoAsync(FormattableString sql) =>
        EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Database.ExecuteSqlAsync(sql));

    private Task ExcluirLogicamenteAsync(Guid solicitacao)
    {
        var agora = DateTime.UtcNow;
        return ComandoAsync($"UPDATE solicitacoes_do_cliente SET excluido_em = {agora} WHERE id = {solicitacao}");
    }

    private async Task<Guid> AtividadeAsync(string titulo)
    {
        var criada = await ExecutarAsync(_admin, new SalvarAtividade(
            null, titulo, "Texto da atividade.", new DateOnly(2090, 6, 15), _maria, null, null, 1));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        return criada.Valor;
    }

    /// <summary>
    /// Grava, pelo domínio, uma atividade da Maria criada a partir da solicitação. Não vincula a solicitação:
    /// o vínculo vai à parte, com a atividade devolvida.
    /// </summary>
    private Task<Atividade> AtividadeDaAsync(Guid solicitacao, string titulo) =>
        EmEscopoAsync(_admin, async sp =>
        {
            var atividade = Atividade.Criar(
                new DadosDaAtividade(titulo, "Texto da atividade.", new DateOnly(2090, 6, 15), 1),
                VinculoDaAtividade.Criar(_maria, null, null), TipoAutor.Usuario, _admin, DateTime.UtcNow, solicitacao);
            sp.GetRequiredService<IAtividadeRepositorio>().Adicionar(atividade);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return atividade;
        });

    /// <summary>A Diretoria é a única responsável e a revisora: ao concluir a parte, a atividade se conclui.</summary>
    private async Task ConcluirAtividadeAsync(Guid atividade)
    {
        var definido = await ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            atividade, [new ResponsavelEntrada(_admin, "Cuidar")], _admin));
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
        (await ExecutarAsync(_admin, new ConcluirMinhaParte(atividade))).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Solicitacao_grava_e_volta_com_todos_os_campos()
    {
        var descricao = new string('d', SolicitacaoDoCliente.TamanhoMaximoDescricao);
        var aberta = await AbrirAsync(_maria, _carla, descricao, UrgenciaDaSolicitacao.Alta);
        var cancelada = await AbrirAsync(_maria, _marcos);
        var atividade = await AtividadeDaAsync(cancelada, "Certidão da Maria");
        var agora = new DateTime(2026, 11, 13, 15, 30, 0, DateTimeKind.Utc);
        await MudarAsync(cancelada, _marcos, s => s.Cancelar(agora));
        await MudarAsync(cancelada, _admin, s => s.VincularAtividade(atividade, confirmouCancelada: true));

        var lida = await LerAsync(aberta);

        lida.ClienteId.Should().Be(_maria);
        lida.SolicitanteId.Should().Be(_carla);
        lida.Descricao.Should().Be(descricao);
        lida.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        lida.CanceladaEm.Should().BeNull();
        lida.AtividadeId.Should().BeNull();
        lida.VersaoDosDocumentos.Should().Be(0);
        lida.Versao.Should().NotBe(0u);
        lida.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        lida.CriadoEm.Kind.Should().Be(DateTimeKind.Utc);
        lida.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        lida.CriadoPor.Should().Be(_carla);
        lida.Excluido.Should().BeFalse();

        var vinculada = await LerAsync(cancelada);
        vinculada.CanceladaEm.Should().Be(agora);
        vinculada.AtividadeId.Should().Be(atividade.Id);
        vinculada.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);

        // A urgência fica como inteiro no banco, não como texto.
        var urgenciaGravada = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Database
            .SqlQuery<int>($"SELECT urgencia AS \"Value\" FROM solicitacoes_do_cliente WHERE id = {aberta}").SingleAsync());
        urgenciaGravada.Should().Be((int)UrgenciaDaSolicitacao.Alta);
    }

    [Fact]
    public async Task Solicitacao_inexistente_ou_excluida_nao_e_encontrada()
    {
        var excluida = await AbrirAsync(_maria, _carla);
        await ExcluirLogicamenteAsync(excluida);

        var repositorioLe = (Guid id) => EmEscopoAsync(null, sp => sp.GetRequiredService<ISolicitacaoRepositorio>().ObterPorIdAsync(id, default));

        (await repositorioLe(excluida)).Should().BeNull();
        (await repositorioLe(Guid.CreateVersion7())).Should().BeNull();
    }

    [Fact]
    public async Task Alterar_com_versao_antiga_falha_por_concorrencia()
    {
        var id = await AbrirAsync(_maria, _carla, "Descrição original.", UrgenciaDaSolicitacao.Baixa);

        await EmEscopoAsync(_carla, async spA =>
        {
            var daPrimeira = (await spA.GetRequiredService<ISolicitacaoRepositorio>().ObterPorIdAsync(id, default))!;

            // Outra pessoa do mesmo cliente altera e grava antes.
            await MudarAsync(id, _marcos, s => s.Alterar("Descrição da segunda.", UrgenciaDaSolicitacao.Alta));

            daPrimeira.Alterar("Descrição da primeira.", UrgenciaDaSolicitacao.Media);
            var salvar = () => spA.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            await salvar.Should().ThrowAsync<ExcecaoDeConcorrencia>();
            return 0;
        });

        var final = await LerAsync(id);
        final.Descricao.Should().Be("Descrição da segunda.");
        final.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);

        // A gravação que perdeu não deixou rastro na auditoria.
        var alteracoes = (await AuditoriaAsync())
            .Where(r => r.Entidade == "SolicitacaoDoCliente" && r.EntidadeId == id && r.Operacao == OperacaoAuditada.Alteracao);
        alteracoes.Should().ContainSingle();
    }

    [Fact]
    public async Task Mudanca_nos_documentos_muda_a_versao()
    {
        var id = await AbrirAsync(_maria, _carla);
        var antes = await LerAsync(id);

        await EmEscopoAsync(_carla, async spA =>
        {
            var desatualizada = (await spA.GetRequiredService<ISolicitacaoRepositorio>().ObterPorIdAsync(id, default))!;

            await MudarAsync(id, _carla, s => s.RegistrarMudancaNosDocumentos());

            // Quem abriu a tela antes da mudança nos documentos é avisado ao gravar.
            desatualizada.Alterar("Nova descrição.", UrgenciaDaSolicitacao.Alta);
            var salvar = () => spA.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            await salvar.Should().ThrowAsync<ExcecaoDeConcorrencia>();
            return 0;
        });

        var depois = await LerAsync(id);
        depois.VersaoDosDocumentos.Should().Be(1);
        depois.Versao.Should().NotBe(antes.Versao);
        depois.Descricao.Should().Be(antes.Descricao);

        // O contador e a versão são controle técnico: mexer só neles não vai para a auditoria.
        (await AuditoriaAsync()).Where(r => r.Entidade == "SolicitacaoDoCliente" && r.EntidadeId == id)
            .Select(r => r.Operacao).Should().Equal(OperacaoAuditada.Criacao);
    }

    [Fact]
    public async Task Listar_do_cliente_traz_so_as_do_cliente_da_mais_recente_para_a_mais_antiga()
    {
        var primeira = await AbrirAsync(_maria, _carla, "Primeira da Maria.", UrgenciaDaSolicitacao.Baixa);
        var doJose = await AbrirAsync(_jose, _joao, "Do José.");
        var segunda = await AbrirAsync(_maria, _marcos, "Segunda da Maria.", UrgenciaDaSolicitacao.Alta);
        var excluida = await AbrirAsync(_maria, _carla, "Excluída da Maria.");
        var longa = new string('a', SolicitacoesOpcoes.TamanhoDoTrecho + 60);
        var terceira = await AbrirAsync(_maria, _carla, longa);
        await ExcluirLogicamenteAsync(excluida);

        // Dois documentos vivos na primeira (um terceiro foi removido) e um na segunda.
        await AnexarAsync(primeira, _maria, "a.pdf");
        await AnexarAsync(primeira, _maria, "b.pdf");
        await RemoverDocumentoAsync(await AnexarAsync(primeira, _maria, "c.pdf"));
        await AnexarAsync(segunda, _maria, "d.pdf", _marcos);

        var daMaria = await DoClienteAsync(_maria);

        daMaria.Itens.Select(s => s.Id).Should().Equal(terceira, segunda, primeira);
        daMaria.Total.Should().Be(3);
        daMaria.Numero.Should().Be(1);
        daMaria.Tamanho.Should().Be(SolicitacoesOpcoes.TamanhoDaPagina);

        var linhaDaTerceira = daMaria.Itens[0];
        linhaDaTerceira.Trecho.Should().Be(new string('a', SolicitacoesOpcoes.TamanhoDoTrecho) + "…");
        linhaDaTerceira.SolicitanteNome.Should().Be("Carla Cliente");
        linhaDaTerceira.Urgencia.Should().Be(UrgenciaDaSolicitacao.Media);
        linhaDaTerceira.Situacao.Should().Be(SituacaoParaOCliente.AguardandoAnalise);
        linhaDaTerceira.Documentos.Should().Be(0);
        linhaDaTerceira.AbertaEm.Should().Be((await LerAsync(terceira)).CriadoEm);

        var linhaDaSegunda = daMaria.Itens[1];
        linhaDaSegunda.Trecho.Should().Be("Segunda da Maria.");
        linhaDaSegunda.SolicitanteNome.Should().Be("Marcos Cliente");
        linhaDaSegunda.Urgencia.Should().Be(UrgenciaDaSolicitacao.Alta);
        linhaDaSegunda.Documentos.Should().Be(1);

        var linhaDaPrimeira = daMaria.Itens[2];
        linhaDaPrimeira.Urgencia.Should().Be(UrgenciaDaSolicitacao.Baixa);
        linhaDaPrimeira.Documentos.Should().Be(2);

        // As páginas respeitam a ordem e o total.
        var primeiraPagina = await DoClienteAsync(_maria, pagina: 1, tamanho: 2);
        var segundaPagina = await DoClienteAsync(_maria, pagina: 2, tamanho: 2);
        primeiraPagina.Itens.Select(s => s.Id).Should().Equal(terceira, segunda);
        segundaPagina.Itens.Select(s => s.Id).Should().Equal(primeira);
        primeiraPagina.Total.Should().Be(3);
        segundaPagina.Total.Should().Be(3);

        var doJoseLista = await DoClienteAsync(_jose);
        doJoseLista.Itens.Select(s => s.Id).Should().Equal(doJose);
        doJoseLista.Itens.Single().SolicitanteNome.Should().Be("João Cliente");
        (await DoClienteAsync(Guid.CreateVersion7())).Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Situacao_para_o_cliente_segue_a_atividade_viva()
    {
        var aguardando = await AbrirAsync(_maria, _carla, "Aguardando.");
        var cancelada = await AbrirAsync(_maria, _carla, "Cancelada.");
        var vinculada = await AbrirAsync(_maria, _carla, "Vinculada.");
        var canceladaEVinculada = await AbrirAsync(_maria, _carla, "Cancelada e vinculada.");
        var agora = new DateTime(2026, 11, 13, 15, 30, 0, DateTimeKind.Utc);
        await MudarAsync(cancelada, _carla, s => s.Cancelar(agora));
        await MudarAsync(canceladaEVinculada, _carla, s => s.Cancelar(agora));

        async Task<Dictionary<Guid, SituacaoParaOCliente>> SituacoesAsync() =>
            (await DoClienteAsync(_maria)).Itens.ToDictionary(s => s.Id, s => s.Situacao);

        async Task ConferirAsync(
            SituacaoParaOCliente daAguardando, SituacaoParaOCliente daCancelada,
            SituacaoParaOCliente daVinculada, SituacaoParaOCliente daCanceladaEVinculada)
        {
            var esperado = new Dictionary<Guid, SituacaoParaOCliente>
            {
                [aguardando] = daAguardando,
                [cancelada] = daCancelada,
                [vinculada] = daVinculada,
                [canceladaEVinculada] = daCanceladaEVinculada,
            };
            (await SituacoesAsync()).Should().BeEquivalentTo(esperado);

            // O filtro usa a mesma regra da situação mostrada, para cada uma das quatro.
            foreach (var situacao in Enum.GetValues<SituacaoParaOCliente>())
            {
                var filtrada = await DoClienteAsync(_maria, situacao);
                var ids = esperado.Where(e => e.Value == situacao).Select(e => e.Key);
                filtrada.Itens.Select(s => s.Id).Should().BeEquivalentTo(ids, $"o filtro {situacao}");
                filtrada.Total.Should().Be(ids.Count(), $"o total do filtro {situacao}");
            }
        }

        // Sem atividade: vale a situação da própria solicitação.
        await ConferirAsync(
            SituacaoParaOCliente.AguardandoAnalise, SituacaoParaOCliente.Cancelada,
            SituacaoParaOCliente.AguardandoAnalise, SituacaoParaOCliente.Cancelada);

        // Atividade viva e não concluída: em andamento, mesmo para a que o cliente cancelou.
        var atividadeDaVinculada = await AtividadeDaAsync(vinculada, "Atividade da vinculada");
        var atividadeDaCancelada = await AtividadeDaAsync(canceladaEVinculada, "Atividade da cancelada");
        var daVinculada = atividadeDaVinculada.Id;
        var daCanceladaEVinculada = atividadeDaCancelada.Id;
        await MudarAsync(vinculada, _admin, s => s.VincularAtividade(atividadeDaVinculada, confirmouCancelada: false));
        await MudarAsync(canceladaEVinculada, _admin, s => s.VincularAtividade(atividadeDaCancelada, confirmouCancelada: true));
        await ConferirAsync(
            SituacaoParaOCliente.AguardandoAnalise, SituacaoParaOCliente.Cancelada,
            SituacaoParaOCliente.EmAndamento, SituacaoParaOCliente.EmAndamento);
        var andamento = await EmEscopoAsync(null, sp => sp.GetRequiredService<ISolicitacaoRepositorio>()
            .ObterAtividadeAsync(daVinculada, default));
        andamento.Should().NotBeNull();
        andamento!.Titulo.Should().Be("Atividade da vinculada");
        andamento.Status.Should().Be(StatusAtividade.Criada);
        andamento.ConcluidaEm.Should().BeNull();

        // Concluída.
        await ConcluirAtividadeAsync(daVinculada);
        await ConcluirAtividadeAsync(daCanceladaEVinculada);
        await ConferirAsync(
            SituacaoParaOCliente.AguardandoAnalise, SituacaoParaOCliente.Cancelada,
            SituacaoParaOCliente.Concluida, SituacaoParaOCliente.Concluida);
        var concluida = await EmEscopoAsync(null, sp => sp.GetRequiredService<ISolicitacaoRepositorio>()
            .ObterAtividadeAsync(daVinculada, default));
        concluida!.Status.Should().Be(StatusAtividade.Concluida);
        concluida.ConcluidaEm.Should().NotBeNull();

        // A atividade excluída deixa de valer: volta a situação da própria solicitação.
        (await ExecutarAsync(_admin, new ExcluirAtividade(daVinculada))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_admin, new ExcluirAtividade(daCanceladaEVinculada))).Sucesso.Should().BeTrue();
        await ConferirAsync(
            SituacaoParaOCliente.AguardandoAnalise, SituacaoParaOCliente.Cancelada,
            SituacaoParaOCliente.AguardandoAnalise, SituacaoParaOCliente.Cancelada);
        (await EmEscopoAsync(null, sp => sp.GetRequiredService<ISolicitacaoRepositorio>()
            .ObterAtividadeAsync(daVinculada, default))).Should().BeNull("só a atividade viva é devolvida");
    }

    [Fact]
    public async Task Listar_da_diretoria_vai_da_mais_antiga_para_a_mais_nova_e_filtra()
    {
        var primeira = await AbrirAsync(_maria, _carla, "Primeira da Maria.", UrgenciaDaSolicitacao.Baixa);
        var doJose = await AbrirAsync(_jose, _joao, "Do José.", UrgenciaDaSolicitacao.Alta);
        var terceira = await AbrirAsync(_maria, _marcos, new string('b', SolicitacoesOpcoes.TamanhoDoTrecho + 1));
        var cancelada = await AbrirAsync(_maria, _carla, "Cancelada.");
        var vinculada = await AbrirAsync(_maria, _carla, "Vinculada.");
        var excluida = await AbrirAsync(_maria, _carla, "Excluída.");
        await ExcluirLogicamenteAsync(excluida);
        await MudarAsync(cancelada, _carla, s => s.Cancelar(new DateTime(2026, 11, 13, 15, 30, 0, DateTimeKind.Utc)));
        var atividade = await AtividadeDaAsync(vinculada, "Certidão da Maria");
        await MudarAsync(vinculada, _admin, s => s.VincularAtividade(atividade, confirmouCancelada: false));
        await AnexarAsync(primeira, _maria, "a.pdf");
        await AnexarAsync(primeira, _maria, "b.pdf");

        var todas = await DaDiretoriaAsync();

        todas.Itens.Select(s => s.Id).Should().Equal(primeira, doJose, terceira, cancelada, vinculada);
        todas.Total.Should().Be(5);

        var linhaDaPrimeira = todas.Itens[0];
        linhaDaPrimeira.ClienteId.Should().Be(_maria);
        linhaDaPrimeira.ClienteNome.Should().Be("Maria Silva");
        linhaDaPrimeira.SolicitanteNome.Should().Be("Carla Cliente");
        linhaDaPrimeira.Urgencia.Should().Be(UrgenciaDaSolicitacao.Baixa);
        linhaDaPrimeira.Trecho.Should().Be("Primeira da Maria.");
        linhaDaPrimeira.Documentos.Should().Be(2);
        linhaDaPrimeira.Situacao.Should().Be(SituacaoDaSolicitacao.Aguardando);
        linhaDaPrimeira.AbertaEm.Should().Be((await LerAsync(primeira)).CriadoEm);

        todas.Itens[1].ClienteNome.Should().Be("José Souza");
        todas.Itens[1].SolicitanteNome.Should().Be("João Cliente");
        todas.Itens[2].Trecho.Should().Be(new string('b', SolicitacoesOpcoes.TamanhoDoTrecho) + "…");
        todas.Itens[3].Situacao.Should().Be(SituacaoDaSolicitacao.Cancelada);
        todas.Itens[4].Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);

        (await DaDiretoriaAsync(SituacaoDaSolicitacao.Aguardando)).Itens.Select(s => s.Id)
            .Should().Equal(primeira, doJose, terceira);
        (await DaDiretoriaAsync(SituacaoDaSolicitacao.Cancelada)).Itens.Select(s => s.Id).Should().Equal(cancelada);
        (await DaDiretoriaAsync(SituacaoDaSolicitacao.EmAtividade)).Itens.Select(s => s.Id).Should().Equal(vinculada);
        (await DaDiretoriaAsync(cliente: _jose)).Itens.Select(s => s.Id).Should().Equal(doJose);
        (await DaDiretoriaAsync(SituacaoDaSolicitacao.Aguardando, _maria)).Itens.Select(s => s.Id)
            .Should().Equal(primeira, terceira);
        (await DaDiretoriaAsync(SituacaoDaSolicitacao.Cancelada, _jose)).Itens.Should().BeEmpty();

        var primeiraPagina = await DaDiretoriaAsync(pagina: 1, tamanho: 2);
        var ultimaPagina = await DaDiretoriaAsync(pagina: 3, tamanho: 2);
        primeiraPagina.Itens.Select(s => s.Id).Should().Equal(primeira, doJose);
        ultimaPagina.Itens.Select(s => s.Id).Should().Equal(vinculada);
        ultimaPagina.Total.Should().Be(5);
    }

    [Fact]
    public async Task Nomes_do_usuario_e_do_cliente_valem_mesmo_para_os_excluidos()
    {
        var id = await AbrirAsync(_jose, _joao);
        var agora = DateTime.UtcNow;
        await ComandoAsync($"UPDATE usuarios SET excluido_em = {agora} WHERE id = {_joao}");
        await ComandoAsync($"UPDATE clientes SET excluido_em = {agora} WHERE id = {_jose}");

        var repositorio = (Func<IServiceProvider, ISolicitacaoRepositorio>)(sp => sp.GetRequiredService<ISolicitacaoRepositorio>());

        (await EmEscopoAsync(null, sp => repositorio(sp).NomeDoUsuarioAsync(_joao, default))).Should().Be("João Cliente");
        (await EmEscopoAsync(null, sp => repositorio(sp).NomeDoClienteAsync(_jose, default))).Should().Be("José Souza");
        (await EmEscopoAsync(null, sp => repositorio(sp).NomeDoUsuarioAsync(Guid.CreateVersion7(), default))).Should().BeNull();
        (await EmEscopoAsync(null, sp => repositorio(sp).NomeDoClienteAsync(Guid.CreateVersion7(), default))).Should().BeNull();

        // As listas continuam identificando quem abriu e o cliente.
        var daDiretoria = (await DaDiretoriaAsync()).Itens.Single(s => s.Id == id);
        daDiretoria.ClienteNome.Should().Be("José Souza");
        daDiretoria.SolicitanteNome.Should().Be("João Cliente");
        (await DoClienteAsync(_jose)).Itens.Single(s => s.Id == id).SolicitanteNome.Should().Be("João Cliente");
    }

    [Fact]
    public async Task Banco_recusa_documento_da_solicitacao_sem_solicitacao_e_solicitacao_fora_da_categoria_6()
    {
        var solicitacao = await AbrirAsync(_maria, _carla);
        var atividade = await AtividadeAsync("Certidão da Maria");

        // Categoria 6 sem solicitação, e solicitação em documento de outra categoria.
        var semSolicitacao = () => InserirDocumentoCruAsync(CategoriaDocumento.DocumentoDaSolicitacao, _maria, null, null);
        var geralComSolicitacao = () => InserirDocumentoCruAsync(CategoriaDocumento.DocumentoGeral, _maria, null, solicitacao);
        var doClienteComSolicitacao = () => InserirDocumentoCruAsync(CategoriaDocumento.DocumentoDoCliente, _maria, null, solicitacao);
        var deAtividadeComSolicitacao = () => InserirDocumentoCruAsync(CategoriaDocumento.DocumentoAtividade, null, atividade, solicitacao);
        foreach (var acao in new[] { semSolicitacao, geralComSolicitacao, doClienteComSolicitacao, deAtividadeComSolicitacao })
        {
            var pg = PostgresDe((await acao.Should().ThrowAsync<Exception>()).Which);
            pg.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
            pg.ConstraintName.Should().Be("ck_documentos_solicitacao_so_na_categoria_6");
        }

        // A categoria 6 também exige cliente.
        var semCliente = () => InserirDocumentoCruAsync(CategoriaDocumento.DocumentoDaSolicitacao, null, null, solicitacao);
        var erroSemCliente = PostgresDe((await semCliente.Should().ThrowAsync<Exception>()).Which);
        erroSemCliente.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        erroSemCliente.ConstraintName.Should().Be("ck_documentos_cliente_conforme_a_categoria");

        // A solicitação precisa existir.
        var inexistente = () => InserirDocumentoCruAsync(CategoriaDocumento.DocumentoDaSolicitacao, _maria, null, Guid.CreateVersion7());
        PostgresDe((await inexistente.Should().ThrowAsync<Exception>()).Which).SqlState
            .Should().Be(PostgresErrorCodes.ForeignKeyViolation);

        await InserirDocumentoCruAsync(CategoriaDocumento.DocumentoDaSolicitacao, _maria, null, solicitacao);
        (await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Documentos.AsNoTracking()
            .CountAsync(d => d.SolicitacaoId == solicitacao))).Should().Be(1);
    }

    [Fact]
    public async Task Banco_recusa_urgencia_fora_de_1_a_3()
    {
        var invalida = () => ComandoAsync(
            $"""
            INSERT INTO solicitacoes_do_cliente (id, cliente_id, solicitante_id, descricao, urgencia, versao_dos_documentos, criado_em)
            VALUES ({Guid.CreateVersion7()}, {_maria}, {_carla}, 'x', 4, 0, {DateTime.UtcNow})
            """);

        var pg = PostgresDe((await invalida.Should().ThrowAsync<Exception>()).Which);
        pg.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        pg.ConstraintName.Should().Be("ck_solicitacoes_urgencia");
    }

    [Fact]
    public async Task Banco_nao_apaga_solicitacao_que_tem_documento()
    {
        var solicitacao = await AbrirAsync(_maria, _carla);
        await AnexarAsync(solicitacao, _maria, "a.pdf");

        // Sem cascata: o banco recusa apagar fisicamente uma solicitação que tem documento.
        var apagar = () => ComandoAsync($"DELETE FROM solicitacoes_do_cliente WHERE id = {solicitacao}");

        PostgresDe((await apagar.Should().ThrowAsync<Exception>()).Which).SqlState
            .Should().Be(PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task Atividade_criada_e_vinculada_na_mesma_gravacao()
    {
        var solicitacao = await AbrirAsync(_maria, _carla);

        // Uma só gravação: a atividade nova (que aponta para a solicitação) e a solicitação que já existe (que passa
        // a apontar para a atividade). As duas chaves estrangeiras se cruzam, e o EF precisa achar a ordem.
        var atividade = await EmEscopoAsync(_admin, async sp =>
        {
            var existente = (await sp.GetRequiredService<ISolicitacaoRepositorio>().ObterPorIdAsync(solicitacao, default))!;
            var nova = Atividade.Criar(
                new DadosDaAtividade("Certidão da Maria", "Texto da atividade.", new DateOnly(2090, 6, 15), 1),
                VinculoDaAtividade.Criar(_maria, null, null), TipoAutor.Usuario, _admin, DateTime.UtcNow, solicitacao);
            sp.GetRequiredService<IAtividadeRepositorio>().Adicionar(nova);
            existente.VincularAtividade(nova, confirmouCancelada: false);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return nova.Id;
        });

        var lidaDaAtividade = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<IAtividadeRepositorio>().ObterPorIdAsync(atividade, default));
        lidaDaAtividade.Should().NotBeNull();
        lidaDaAtividade!.SolicitacaoId.Should().Be(solicitacao);
        var lidaDaSolicitacao = await LerAsync(solicitacao);
        lidaDaSolicitacao.AtividadeId.Should().Be(atividade);
        lidaDaSolicitacao.Situacao.Should().Be(SituacaoDaSolicitacao.EmAtividade);
    }

    [Fact]
    public async Task Duas_atividades_vivas_da_mesma_solicitacao_sao_recusadas()
    {
        var solicitacao = await AbrirAsync(_maria, _carla);
        var primeira = await AtividadeDaAsync(solicitacao, "Primeira atividade");

        var segunda = () => AtividadeDaAsync(solicitacao, "Segunda atividade");

        (await segunda.Should().ThrowAsync<ExcecaoDeConflito>()).Which.Restricao
            .Should().Be("ix_atividades_solicitacao_id");
        var daSolicitacao = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Atividades
            .IgnoreQueryFilters().AsNoTracking().Where(a => a.SolicitacaoId == solicitacao).Select(a => a.Id).ToListAsync());
        daSolicitacao.Should().Equal(primeira.Id);
    }

    [Fact]
    public async Task Atividade_excluida_nao_impede_outra_da_mesma_solicitacao()
    {
        var solicitacao = await AbrirAsync(_maria, _carla);
        var primeira = await AtividadeDaAsync(solicitacao, "Primeira atividade");
        (await ExecutarAsync(_admin, new ExcluirAtividade(primeira.Id))).Sucesso.Should().BeTrue();

        var segunda = await AtividadeDaAsync(solicitacao, "Segunda atividade");

        var daSolicitacao = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Atividades
            .IgnoreQueryFilters().AsNoTracking().Where(a => a.SolicitacaoId == solicitacao)
            .Select(a => new { a.Id, Excluida = a.ExcluidoEm != null }).ToListAsync());
        daSolicitacao.Should().BeEquivalentTo([new { primeira.Id, Excluida = true }, new { segunda.Id, Excluida = false }]);
    }

    [Fact]
    public async Task Dois_documentos_com_a_mesma_chave_gravam()
    {
        var solicitacao = await AbrirAsync(_maria, _carla);
        var atividade = await AtividadeDaAsync(solicitacao, "Certidão da Maria");
        var daSolicitacao = await AnexarAsync(solicitacao, _maria, "certidao.pdf");

        // O documento da solicitação levado para a atividade é outro documento, com o mesmo arquivo.
        var levado = await EmEscopoAsync(_admin, async sp =>
        {
            var repositorio = sp.GetRequiredService<IDocumentoRepositorio>();
            var novo = (await repositorio.ObterPorIdAsync(daSolicitacao, default))!.LevarParaAtividade(atividade.Id, _maria);
            repositorio.Adicionar(novo);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return novo.Id;
        });

        var documentos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Documentos
            .AsNoTracking().Where(d => d.Id == daSolicitacao || d.Id == levado).ToListAsync());
        documentos.Should().HaveCount(2);
        documentos.Select(d => d.ChaveArmazenamento).Distinct().Should().ContainSingle();
        documentos.Select(d => d.HashSha256).Distinct().Should().ContainSingle();
        documentos.Single(d => d.Id == daSolicitacao).Categoria.Should().Be(CategoriaDocumento.DocumentoDaSolicitacao);
        documentos.Single(d => d.Id == levado).Categoria.Should().Be(CategoriaDocumento.DocumentoAtividade);
        documentos.Single(d => d.Id == levado).AtividadeId.Should().Be(atividade.Id);
    }

    [Fact]
    public async Task Filtro_da_producao_nunca_traz_documento_da_solicitacao()
    {
        var solicitacao = await AbrirAsync(_maria, _carla);
        var daSolicitacao = await AnexarAsync(solicitacao, _maria, "da-solicitacao.pdf");
        var geral = await GravarGeralAsync();

        // Pela porta do domínio ninguém dá acesso a um documento do cliente; aqui as entradas das listas são postas
        // direto no banco, como um dado antigo ou uma gravação por fora poderia deixá-las.
        foreach (var documento in new[] { daSolicitacao, geral })
        {
            var agora = DateTime.UtcNow;
            await ComandoAsync(
                $"""
                INSERT INTO documentos_perfis_acesso (id, documento_id, perfil_id, criado_em)
                VALUES ({Guid.CreateVersion7()}, {documento}, {_perfilProducao}, {agora})
                """);
            await ComandoAsync(
                $"""
                INSERT INTO documentos_usuarios_acesso (id, documento_id, usuario_id, criado_em)
                VALUES ({Guid.CreateVersion7()}, {documento}, {_pedro}, {agora})
                """);
        }

        var filtro = new FiltroDeAcesso(_pedro, _perfilProducao);
        var repositorio = (Func<IServiceProvider, IDocumentoRepositorio>)(sp => sp.GetRequiredService<IDocumentoRepositorio>());

        var daProducao = await EmEscopoAsync(_pedro, sp => repositorio(sp).ListarDoClienteAsync(
            _maria, CategoriaDocumento.DocumentoDaSolicitacao, filtro, 1, 20, default));
        var contagem = await EmEscopoAsync(_pedro, sp => repositorio(sp).ContarDoClienteAsync(_maria, filtro, default));

        daProducao.Itens.Should().BeEmpty();
        daProducao.Total.Should().Be(0);
        contagem.Should().NotContainKey(CategoriaDocumento.DocumentoDaSolicitacao);
        // O documento geral com as mesmas entradas aparece: é o filtro, e não a falta das entradas, que esconde o outro.
        contagem[CategoriaDocumento.DocumentoGeral].Should().Be(1);
        (await EmEscopoAsync(_pedro, sp => repositorio(sp).ListarDoClienteAsync(
            _maria, CategoriaDocumento.DocumentoGeral, filtro, 1, 20, default))).Itens.Select(d => d.Id).Should().Equal(geral);

        // A Diretoria (sem filtro de acesso) lista o documento da solicitação: ele existe.
        var daDiretoria = await EmEscopoAsync(_admin, sp => repositorio(sp).ListarDoClienteAsync(
            _maria, CategoriaDocumento.DocumentoDaSolicitacao, null, 1, 20, default));
        daDiretoria.Itens.Select(d => d.Id).Should().Equal(daSolicitacao);
        (await EmEscopoAsync(_admin, sp => repositorio(sp).ContarDoClienteAsync(_maria, null, default)))
            .Should().NotContainKey(CategoriaDocumento.DocumentoDaSolicitacao);
    }

    [Fact]
    public async Task Documentos_da_solicitacao_vem_do_mais_antigo_para_o_mais_novo_sem_os_excluidos()
    {
        var solicitacao = await AbrirAsync(_maria, _carla);
        var outra = await AbrirAsync(_maria, _marcos);
        var primeiro = await AnexarAsync(solicitacao, _maria, "primeiro.pdf");
        var removido = await AnexarAsync(solicitacao, _maria, "removido.pdf");
        var terceiro = await AnexarAsync(solicitacao, _maria, "terceiro.pdf");
        var deOutra = await AnexarAsync(outra, _maria, "da-outra.pdf", _marcos);
        await GravarGeralAsync();
        await RemoverDocumentoAsync(removido);

        var repositorio = (Func<IServiceProvider, IDocumentoRepositorio>)(sp => sp.GetRequiredService<IDocumentoRepositorio>());
        var documentos = await EmEscopoAsync(_carla, sp => repositorio(sp).ListarDaSolicitacaoAsync(solicitacao, default));
        var quantos = await EmEscopoAsync(_carla, sp => repositorio(sp).ContarDaSolicitacaoAsync(solicitacao, default));

        documentos.Select(d => d.Id).Should().Equal(primeiro, terceiro);
        documentos.Select(d => d.Nome).Should().Equal("primeiro.pdf", "terceiro.pdf");
        documentos.Should().OnlyContain(d => d.EnviadoPor == null && d.TamanhoBytes > 0);
        documentos.Select(d => d.EnviadoEm).Should().BeInAscendingOrder();
        quantos.Should().Be(2);

        (await EmEscopoAsync(_marcos, sp => repositorio(sp).ListarDaSolicitacaoAsync(outra, default)))
            .Select(d => d.Id).Should().Equal(deOutra);
        (await EmEscopoAsync(_marcos, sp => repositorio(sp).ContarDaSolicitacaoAsync(outra, default))).Should().Be(1);
        (await EmEscopoAsync(_carla, sp => repositorio(sp).ListarDaSolicitacaoAsync(Guid.CreateVersion7(), default))).Should().BeEmpty();
        (await EmEscopoAsync(_carla, sp => repositorio(sp).ContarDaSolicitacaoAsync(Guid.CreateVersion7(), default))).Should().Be(0);
    }

    [Fact]
    public async Task Documentos_gravados_no_mesmo_instante_vem_pelo_nome_e_depois_pelo_id()
    {
        // Os documentos de uma solicitação aberta com vários arquivos são gravados juntos e têm o mesmo CriadoEm.
        var solicitacao = Guid.Empty;
        var ids = await EmEscopoAsync(_carla, async sp =>
        {
            var aberta = SolicitacaoDoCliente.Abrir(_maria, _carla, "Preciso de uma certidão atualizada.", UrgenciaDaSolicitacao.Media);
            solicitacao = aberta.Id;
            sp.GetRequiredService<ISolicitacaoRepositorio>().Adicionar(aberta);
            var documentos = new List<Documento>();
            foreach (var nome in new[] { "c.pdf", "mesmo.pdf", "a.pdf", "mesmo.pdf", "b.pdf", "mesmo.pdf" })
            {
                var documento = Documento.CriarDaSolicitacao(aberta.Id, _maria, null,
                    new ArquivoDoDocumento(nome, "application/pdf", 10, new string('a', 64), $"2026/11/{Guid.NewGuid():N}"));
                sp.GetRequiredService<IDocumentoRepositorio>().Adicionar(documento);
                documentos.Add(documento);
            }
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return documentos.Where(d => d.Nome == "mesmo.pdf").Select(d => d.Id).ToList();
        });

        var listados = await EmEscopoAsync(_carla, sp =>
            sp.GetRequiredService<IDocumentoRepositorio>().ListarDaSolicitacaoAsync(solicitacao, default));

        listados.Select(d => d.EnviadoEm).Distinct().Should().ContainSingle("todos foram gravados no mesmo instante");
        listados.Select(d => d.Nome).Should().Equal("a.pdf", "b.pdf", "c.pdf", "mesmo.pdf", "mesmo.pdf", "mesmo.pdf");
        // Mesmo instante e mesmo nome: a ordem do Id no banco, que compara os bytes como o texto do Guid.
        listados.Skip(3).Select(d => d.Id).Should().Equal(ids.OrderBy(id => id.ToString(), StringComparer.Ordinal));
    }

    private Task<Guid> GravarGeralAsync() =>
        EmEscopoAsync(_admin, async sp =>
        {
            var documento = Documento.CriarGeral(
                _maria, null,
                new ArquivoDoDocumento("geral.pdf", "application/pdf", 10, new string('c', 64), $"2026/11/{Guid.NewGuid():N}"), []);
            sp.GetRequiredService<IDocumentoRepositorio>().Adicionar(documento);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return documento.Id;
        });

    private Task<int> InserirDocumentoCruAsync(CategoriaDocumento categoria, Guid? cliente, Guid? atividade, Guid? solicitacao)
    {
        var id = Guid.CreateVersion7();
        var chave = $"2026/11/{Guid.NewGuid():N}";
        var hash = new string('a', 64);
        var agora = DateTime.UtcNow;
        var codigo = (int)categoria;
        return ComandoAsync(
            $"""
            INSERT INTO documentos (id, nome, nome_arquivo_original, tipo_conteudo, tamanho_bytes, hash_sha256,
                                    chave_armazenamento, categoria, cliente_id, atividade_id, solicitacao_id, criado_em)
            VALUES ({id}, 'x', 'x.pdf', 'application/pdf', 1, {hash}, {chave}, {codigo}, {cliente}, {atividade}, {solicitacao}, {agora})
            """);
    }
}
