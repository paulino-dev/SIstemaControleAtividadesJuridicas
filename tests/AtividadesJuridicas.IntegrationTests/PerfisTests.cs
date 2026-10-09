using System.Text;
using System.Text.Json;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class PerfisTests(BancoFixture banco) : TesteComBanco(banco)
{
    private Guid _admin, _maria, _perfilDiretor, _perfilProducao, _perfilCliente;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        _perfilCliente = await IdDoPerfilAsync(TipoPerfil.Cliente);
        _perfilDiretor = await IdDoPerfilAsync(TipoPerfil.Diretoria);
        _perfilProducao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _maria = (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null))).Valor;
    }

    private async Task<Guid> EnviarAsync(CategoriaDocumento categoria, string arquivo)
    {
        var resultado = await ExecutarAsync(_admin, new EnviarDocumento(
            categoria, _maria, null, arquivo, "application/pdf", new MemoryStream(Encoding.UTF8.GetBytes("conteúdo"))));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private async Task<Guid> CriarPerfilAsync(string nome, TipoPerfil tipo = TipoPerfil.Producao)
    {
        var resultado = await ExecutarAsync(_admin, new SalvarPerfil(null, nome, tipo));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private async Task<Guid> CriarUsuarioAsync(string nome, string email, Guid perfil) =>
        (await ExecutarAsync(_admin, new CriarUsuario(nome, email, "senha-bem-segura", perfil, null))).Valor;

    private Task<List<DocumentoPerfilAcesso>> EntradasDoPerfilAsync(Guid perfil) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Set<DocumentoPerfilAcesso>().IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.PerfilId == perfil).ToListAsync());

    private Task<List<Perfil>> PerfisNoBancoAsync() => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Perfis.IgnoreQueryFilters().AsNoTracking().OrderBy(p => p.Nome).ToListAsync());

    [Fact]
    public async Task Perfil_novo_entra_em_todo_documento_geral_vivo_e_em_mais_nenhum()
    {
        var geral1 = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "um.pdf");
        var geral2 = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "dois.pdf");
        var excluido = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "excluido.pdf");
        var contrato = await EnviarAsync(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");
        (await ExecutarAsync(_admin, new ExcluirDocumento(excluido))).Sucesso.Should().BeTrue();

        var estagio = await CriarPerfilAsync("Estagiário");

        var entradas = await EntradasDoPerfilAsync(estagio);
        entradas.Select(a => a.DocumentoId).Should().BeEquivalentTo([geral1, geral2]);
        entradas.Should().OnlyContain(a =>
            a.CriadoPor == _admin && a.CriadoEm.Kind == DateTimeKind.Utc && a.CriadoEm > DateTime.UtcNow.AddMinutes(-5) &&
            a.ExcluidoEm == null && a.Id != Guid.Empty);
        entradas.Select(a => a.Id).Should().OnlyHaveUniqueItems();
        entradas.Should().NotContain(a => a.DocumentoId == contrato || a.DocumentoId == excluido);
    }

    [Fact]
    public async Task Usuario_do_perfil_novo_passa_a_listar_e_baixar_os_documentos_gerais_e_nunca_os_contratos()
    {
        var geral = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "geral.pdf");
        var contrato = await EnviarAsync(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");
        var estagio = await CriarPerfilAsync("Estagiário");
        var ester = await CriarUsuarioAsync("Ester", "ester@x.com", estagio);

        var lista = (await ExecutarAsync(ester, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.DocumentoGeral))).Valor!;
        var baixou = await ExecutarAsync(ester, new BaixarDocumento(geral));
        var doContrato = await ExecutarAsync(ester, new BaixarDocumento(contrato));

        lista.Itens.Select(d => d.Id).Should().Equal(geral);
        baixou.Sucesso.Should().BeTrue(baixou.Mensagem);
        await baixou.Valor!.Conteudo.DisposeAsync();
        doContrato.Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    [Fact]
    public async Task Criacao_fica_na_auditoria_com_o_registro_do_perfil_e_o_evento_da_inclusao()
    {
        await EnviarAsync(CategoriaDocumento.DocumentoGeral, "um.pdf");
        await EnviarAsync(CategoriaDocumento.DocumentoGeral, "dois.pdf");

        var estagio = await CriarPerfilAsync("Estagiário");

        var registros = (await AuditoriaAsync()).Where(r => r.EntidadeId == estagio).ToList();
        var criacao = registros.Should().ContainSingle(r => r.Operacao == OperacaoAuditada.Criacao).Subject;
        criacao.Entidade.Should().Be("Perfil");
        criacao.UsuarioId.Should().Be(_admin);
        var evento = registros.Should().ContainSingle(r => r.Evento == EventosDeSeguranca.PerfilIncluidoNosDocumentosGerais).Subject;
        evento.UsuarioId.Should().Be(_admin);
        evento.IdCorrelacao.Should().Be(criacao.IdCorrelacao);
        using var dados = JsonDocument.Parse(evento.Dados!);
        dados.RootElement.GetProperty("documentos").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Sem_documentos_gerais_nao_ha_evento_de_inclusao()
    {
        var estagio = await CriarPerfilAsync("Estagiário");

        (await AuditoriaAsync()).Should().NotContain(r => r.Evento == EventosDeSeguranca.PerfilIncluidoNosDocumentosGerais);
        (await EntradasDoPerfilAsync(estagio)).Should().BeEmpty();
    }

    [Fact]
    public async Task Documento_enviado_depois_ja_nasce_com_o_perfil_novo_e_nao_com_o_inativo()
    {
        var estagio = await CriarPerfilAsync("Estagiário");
        var temporario = await CriarPerfilAsync("Temporário");
        (await ExecutarAsync(_admin, new DefinirAtivoPerfil(temporario, false))).Sucesso.Should().BeTrue();

        var documento = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "novo.pdf");

        var acesso = (await ExecutarAsync(_admin, new ObterAcessoDoDocumento(documento))).Valor!;
        acesso.Perfis.Should().BeEquivalentTo([_perfilDiretor, _perfilProducao, estagio]);
    }

    [Fact]
    public async Task Transacao_desfaz_tudo_quando_a_acao_falha()
    {
        var acao = () => EmEscopoAsync(_admin, async sp =>
        {
            var unidade = sp.GetRequiredService<IUnidadeDeTrabalho>();
            await unidade.EmTransacaoAsync(async () =>
            {
                sp.GetRequiredService<IPerfilRepositorio>().Adicionar(Perfil.Criar("Fantasma", TipoPerfil.Producao));
                await unidade.SalvarAsync(default);
                throw new InvalidOperationException("falhou depois de gravar");
            }, default);
            return 0;
        });

        await acao.Should().ThrowAsync<InvalidOperationException>().WithMessage("falhou depois de gravar");
        (await PerfisNoBancoAsync()).Select(p => p.Nome).Should().Equal("Cliente", "Diretor", "Produção");
        (await AuditoriaAsync()).Should().NotContain(r => r.Dados != null && r.Dados.Contains("Fantasma"));
    }

    [Fact]
    public async Task Transacao_confirma_o_que_a_acao_gravou()
    {
        await EmEscopoAsync(_admin, async sp =>
        {
            var unidade = sp.GetRequiredService<IUnidadeDeTrabalho>();
            await unidade.EmTransacaoAsync(async () =>
            {
                sp.GetRequiredService<IPerfilRepositorio>().Adicionar(Perfil.Criar("Confirmado", TipoPerfil.Producao));
                await unidade.SalvarAsync(default);
            }, default);
            return 0;
        });

        (await PerfisNoBancoAsync()).Select(p => p.Nome).Should().Contain("Confirmado");
    }

    [Theory]
    [InlineData("Produção")]
    [InlineData(" produção ")]
    [InlineData("PRODUÇÃO")]
    public async Task Nome_repetido_e_conflito_no_banco_real(string nome)
    {
        var resultado = await ExecutarAsync(_admin, new SalvarPerfil(null, nome, TipoPerfil.Producao));

        resultado.Erro.Should().Be(TipoErro.Conflito);
        (await PerfisNoBancoAsync()).Should().HaveCount(3);
    }

    [Fact]
    public async Task Renomear_gera_alteracao_so_do_nome_e_nao_mexe_nos_documentos()
    {
        await EnviarAsync(CategoriaDocumento.DocumentoGeral, "um.pdf");
        var antes = (await EntradasDoPerfilAsync(_perfilProducao)).Count;

        (await ExecutarAsync(_admin, new SalvarPerfil(_perfilProducao, "Advogados", TipoPerfil.Producao))).Sucesso.Should().BeTrue();

        var alteracao = (await AuditoriaAsync()).Should()
            .ContainSingle(r => r.Entidade == "Perfil" && r.Operacao == OperacaoAuditada.Alteracao).Subject;
        using var dados = JsonDocument.Parse(alteracao.Dados!);
        dados.RootElement.EnumerateObject().Select(p => p.Name).Should().Equal("Nome");
        dados.RootElement.GetProperty("Nome").GetProperty("para").GetString().Should().Be("Advogados");
        (await EntradasDoPerfilAsync(_perfilProducao)).Should().HaveCount(antes);
    }

    [Fact]
    public async Task Listagem_traz_ativos_e_inativos_com_a_contagem_de_usuarios_ativos()
    {
        await CriarUsuarioAsync("Pedro", "pedro@x.com", _perfilProducao);
        var paula = await CriarUsuarioAsync("Paula", "paula@x.com", _perfilProducao);
        (await ExecutarAsync(_admin, new DefinirAtivoUsuario(paula, false))).Sucesso.Should().BeTrue();
        var estagio = await CriarPerfilAsync("Estagiário");
        (await ExecutarAsync(_admin, new DefinirAtivoPerfil(estagio, false))).Sucesso.Should().BeTrue();

        var perfis = (await ExecutarAsync(_admin, new ListarPerfis())).Valor!;

        perfis.Should().BeEquivalentTo(
        [
            new PerfilDetalhe(_perfilCliente, "Cliente", TipoPerfil.Cliente, true, 0),
            new PerfilDetalhe(_perfilDiretor, "Diretor", TipoPerfil.Diretoria, true, 1),
            new PerfilDetalhe(estagio, "Estagiário", TipoPerfil.Producao, false, 0),
            new PerfilDetalhe(_perfilProducao, "Produção", TipoPerfil.Producao, true, 1),
        ], opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public async Task Perfil_em_uso_nao_e_inativado_e_perfil_inativo_nao_pode_ser_atribuido()
    {
        var pedro = await CriarUsuarioAsync("Pedro", "pedro@x.com", _perfilProducao);
        var estagio = await CriarPerfilAsync("Estagiário");

        var emUso = await ExecutarAsync(_admin, new DefinirAtivoPerfil(_perfilProducao, false));
        var livre = await ExecutarAsync(_admin, new DefinirAtivoPerfil(estagio, false));
        var atribuir = await ExecutarAsync(_admin, new CriarUsuario("Ester", "ester@x.com", "senha-bem-segura", estagio, null));

        emUso.Erro.Should().Be(TipoErro.Conflito);
        livre.Sucesso.Should().BeTrue(livre.Mensagem);
        atribuir.Sucesso.Should().BeFalse("perfil inativo não pode ser atribuído a usuário");
        atribuir.Erro.Should().Be(TipoErro.Validacao);
        var doPedro = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync(u => u.Id == pedro));
        doPedro.PerfilId.Should().Be(_perfilProducao);
        doPedro.Ativo.Should().BeTrue();
        (await ExecutarAsync(_admin, new ListarPerfisAtivos())).Valor!.Select(p => p.Id).Should().NotContain(estagio);
        pedro.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Perfil_inativado_permanece_nas_listas_em_que_ja_estava_e_pode_ser_retirado()
    {
        var estagio = await CriarPerfilAsync("Estagiário");
        var documento = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "um.pdf");
        (await ExecutarAsync(_admin, new DefinirAtivoPerfil(estagio, false))).Sucesso.Should().BeTrue();

        var manter = await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(documento, [_perfilDiretor, estagio], []));
        var aposManter = (await ExecutarAsync(_admin, new ObterAcessoDoDocumento(documento))).Valor!.Perfis;
        var retirar = await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(documento, [_perfilDiretor], []));
        var devolver = await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(documento, [_perfilDiretor, estagio], []));

        manter.Sucesso.Should().BeTrue(manter.Mensagem);
        aposManter.Should().BeEquivalentTo([_perfilDiretor, estagio]);
        retirar.Sucesso.Should().BeTrue(retirar.Mensagem);
        devolver.Erro.Should().Be(TipoErro.Validacao, "perfil inativo não entra em lista nova");
    }

    [Fact]
    public async Task Usuario_inativo_de_perfil_inativo_nao_e_reativado_e_nada_e_auditado()
    {
        var pedro = await CriarUsuarioAsync("Pedro", "pedro@x.com", _perfilProducao);
        (await ExecutarAsync(_admin, new DefinirAtivoUsuario(pedro, false))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_admin, new DefinirAtivoPerfil(_perfilProducao, false))).Sucesso.Should().BeTrue();

        var reativar = await ExecutarAsync(_admin, new DefinirAtivoUsuario(pedro, true));

        reativar.Erro.Should().Be(TipoErro.Conflito);
        reativar.Mensagem.Should().Be(
            "O perfil deste usuário está inativo. Reative o perfil ou troque o perfil do usuário antes de ativá-lo.");
        var noBanco = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync(u => u.Id == pedro));
        noBanco.Ativo.Should().BeFalse();
        (await AuditoriaAsync()).Should().NotContain(r => r.Evento == EventosDeSeguranca.UsuarioAtivado);
    }

    [Fact]
    public async Task Inclusao_em_massa_e_o_perfil_desfazem_juntos_quando_algo_falha_depois_do_insert()
    {
        await EnviarAsync(CategoriaDocumento.DocumentoGeral, "um.pdf");
        var perfilId = Guid.Empty;

        var acao = () => EmEscopoAsync(_admin, async sp =>
        {
            var unidade = sp.GetRequiredService<IUnidadeDeTrabalho>();
            await unidade.EmTransacaoAsync(async () =>
            {
                var perfil = Perfil.Criar("Fantasma", TipoPerfil.Producao);
                perfilId = perfil.Id;
                sp.GetRequiredService<IPerfilRepositorio>().Adicionar(perfil);
                await unidade.SalvarAsync(default);
                var incluidos = await sp.GetRequiredService<IAcessoInicialDePerfil>()
                    .IncluirNosDocumentosGeraisAsync(perfil.Id, default);
                incluidos.Should().BeGreaterThanOrEqualTo(1);
                throw new InvalidOperationException("falhou depois do insert em massa");
            }, default);
            return 0;
        });

        await acao.Should().ThrowAsync<InvalidOperationException>().WithMessage("falhou depois do insert em massa");
        perfilId.Should().NotBeEmpty();
        (await PerfisNoBancoAsync()).Should().NotContain(p => p.Id == perfilId);
        (await EntradasDoPerfilAsync(perfilId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Producao_nao_gerencia_perfis_e_a_tentativa_fica_registrada()
    {
        var pedro = await CriarUsuarioAsync("Pedro", "pedro@x.com", _perfilProducao);

        var criar = await ExecutarAsync(pedro, new SalvarPerfil(null, "Meu perfil", TipoPerfil.Diretoria));
        var listar = await ExecutarAsync(pedro, new ListarPerfis());

        criar.Erro.Should().Be(TipoErro.AcessoNegado);
        listar.Erro.Should().Be(TipoErro.AcessoNegado);
        (await PerfisNoBancoAsync()).Should().HaveCount(3);
        (await AuditoriaAsync()).Count(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == pedro).Should().Be(2);
    }

    [Fact]
    public async Task Renomear_para_o_nome_de_outro_perfil_e_conflito_no_banco_real_e_nada_muda()
    {
        var estagio = await CriarPerfilAsync("Estagiário");

        var resultado = await ExecutarAsync(_admin, new SalvarPerfil(estagio, "PRODUÇÃO", TipoPerfil.Producao));

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("Já existe um perfil com este nome.");
        (await PerfisNoBancoAsync()).Select(p => p.Nome).Should().Equal("Cliente", "Diretor", "Estagiário", "Produção");
    }

    [Fact]
    public async Task Reativar_o_perfil_tambem_libera_a_reativacao_do_usuario()
    {
        var pedro = await CriarUsuarioAsync("Pedro", "pedro@x.com", _perfilProducao);
        (await ExecutarAsync(_admin, new DefinirAtivoUsuario(pedro, false))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_admin, new DefinirAtivoPerfil(_perfilProducao, false))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_admin, new DefinirAtivoUsuario(pedro, true))).Erro.Should().Be(TipoErro.Conflito);

        (await ExecutarAsync(_admin, new DefinirAtivoPerfil(_perfilProducao, true))).Sucesso.Should().BeTrue();
        var reativar = await ExecutarAsync(_admin, new DefinirAtivoUsuario(pedro, true));

        reativar.Sucesso.Should().BeTrue(reativar.Mensagem);
        var noBanco = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync(u => u.Id == pedro));
        noBanco.Ativo.Should().BeTrue();
        (await AuditoriaAsync()).Should().ContainSingle(r => r.Evento == EventosDeSeguranca.UsuarioAtivado && r.EntidadeId == pedro);
    }

    [Fact]
    public async Task Usuario_de_perfil_novo_de_diretoria_acessa_contratos_e_conta_como_diretor()
    {
        var contrato = await EnviarAsync(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");
        var perfilSocio = await CriarPerfilAsync("Sócio", TipoPerfil.Diretoria);
        var socio = await CriarUsuarioAsync("Sócio da Silva", "socio@x.com", perfilSocio);

        var lista = (await ExecutarAsync(socio, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.ContratoHonorarios))).Valor!;
        var baixou = await ExecutarAsync(socio, new BaixarDocumento(contrato));

        lista.Itens.Select(d => d.Id).Should().Equal(contrato);
        baixou.Sucesso.Should().BeTrue(baixou.Mensagem);
        await baixou.Valor!.Conteudo.DisposeAsync();

        var inativarDiana = await ExecutarAsync(_admin, new DefinirAtivoUsuario(_admin, false));
        var inativarSocio = await ExecutarAsync(socio, new DefinirAtivoUsuario(socio, false));

        inativarDiana.Sucesso.Should().BeTrue(inativarDiana.Mensagem);
        inativarSocio.Erro.Should().Be(TipoErro.Conflito);
        inativarSocio.Mensagem.Should().Be("O sistema precisa de ao menos um usuário ativo de Diretoria.");
    }
}
