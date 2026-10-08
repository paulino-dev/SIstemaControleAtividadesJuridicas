using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class PerfisTests
{
    private readonly Cenario _c = new();

    private Usuario Producao() => _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

    private Task<Resultado<Guid>> Salvar(Usuario? ator, Guid? id, string nome, TipoPerfil tipo = TipoPerfil.Producao) =>
        new SalvarPerfilManipulador(_c.Como(ator), _c.Perfis, _c.AcessoInicial, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new SalvarPerfil(id, nome, tipo), default);

    private Task<Resultado> DefinirAtivo(Usuario? ator, Guid id, bool ativo) =>
        new DefinirAtivoPerfilManipulador(_c.Como(ator), _c.Perfis, _c.Usuarios, _c.Unidade)
            .ManipularAsync(new DefinirAtivoPerfil(id, ativo), default);

    private Task<Resultado<IReadOnlyList<PerfilDetalhe>>> Listar(Usuario? ator) =>
        new ListarPerfisManipulador(_c.Como(ator), _c.Perfis).ManipularAsync(new ListarPerfis(), default);

    // ---- Criar ----

    [Fact]
    public async Task Diretor_cria_perfil_e_ele_e_incluido_nos_documentos_gerais_na_mesma_transacao()
    {
        _c.AcessoInicial.DocumentosGerais = 7;

        var resultado = await Salvar(_c.Diretor, null, "  Estagiário ");

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var perfil = _c.Perfis.Itens.Single(p => p.Id == resultado.Valor);
        perfil.Nome.Should().Be("Estagiário");
        perfil.Tipo.Should().Be(TipoPerfil.Producao);
        perfil.Ativo.Should().BeTrue();
        _c.Unidade.Transacoes.Should().Be(1);
        _c.AcessoInicial.Inclusoes.Should().Equal((perfil.Id, true));

        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.PerfilIncluidoNosDocumentosGerais);
        evento.Entidade.Should().Be("Perfil");
        evento.EntidadeId.Should().Be(perfil.Id);
        evento.Imediato.Should().BeFalse("o evento é gravado na mesma transação");
        evento.Dados.Should().BeEquivalentTo(new { documentos = 7 });
    }

    [Fact]
    public async Task Sem_documentos_gerais_o_perfil_e_criado_sem_evento()
    {
        var resultado = await Salvar(_c.Diretor, null, "Estagiário");

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _c.AcessoInicial.Inclusoes.Should().ContainSingle();
        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Perfil_de_diretoria_tambem_pode_ser_criado()
    {
        var resultado = await Salvar(_c.Diretor, null, "Sócio", TipoPerfil.Diretoria);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _c.Perfis.Itens.Single(p => p.Id == resultado.Valor).Tipo.Should().Be(TipoPerfil.Diretoria);
    }

    [Fact]
    public async Task Falha_na_inclusao_sobe_para_a_transacao_ser_desfeita()
    {
        _c.AcessoInicial.Falha = new InvalidOperationException("banco fora do ar");

        var acao = () => Salvar(_c.Diretor, null, "Estagiário");

        await acao.Should().ThrowAsync<InvalidOperationException>().WithMessage("banco fora do ar");
        _c.Unidade.Transacoes.Should().Be(1, "a falha aconteceu dentro da transação, que a desfaz");
    }

    [Theory]
    [InlineData("Produção")]
    [InlineData("  PRODUÇÃO ")]
    [InlineData("produção")]
    public async Task Nome_repetido_e_conflito_sem_diferenciar_maiusculas(string nome)
    {
        var resultado = await Salvar(_c.Diretor, null, nome);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("Já existe um perfil com este nome.");
        _c.Perfis.Itens.Should().HaveCount(2);
        _c.Unidade.Transacoes.Should().Be(0);
    }

    [Fact]
    public async Task Conflito_vindo_do_banco_tambem_e_nome_em_uso()
    {
        _c.Unidade.Falha = new ExcecaoDeConflito("duplicado");

        var resultado = await Salvar(_c.Diretor, null, "Estagiário");

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("Já existe um perfil com este nome.");
    }

    [Fact]
    public async Task Nome_vazio_ou_tipo_invalido_e_erro_de_validacao()
    {
        (await Salvar(_c.Diretor, null, "   ")).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_c.Diretor, null, "Estagiário", (TipoPerfil)99)).Erro.Should().Be(TipoErro.Validacao);
        _c.Perfis.Itens.Should().HaveCount(2);
        _c.AcessoInicial.Inclusoes.Should().BeEmpty();
    }

    // ---- Renomear ----

    [Fact]
    public async Task Diretor_renomeia_sem_mexer_nos_documentos()
    {
        var resultado = await Salvar(_c.Diretor, _c.PerfilProducao.Id, "Advogados");

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _c.PerfilProducao.Nome.Should().Be("Advogados");
        _c.AcessoInicial.Inclusoes.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Renomear_para_o_nome_de_outro_perfil_e_conflito_e_nada_muda()
    {
        var estagio = (await Salvar(_c.Diretor, null, "Estagiário")).Valor;
        var salvamentos = _c.Unidade.Salvamentos;

        var resultado = await Salvar(_c.Diretor, estagio, "PRODUÇÃO");

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("Já existe um perfil com este nome.");
        _c.Perfis.Itens.Single(p => p.Id == estagio).Nome.Should().Be("Estagiário");
        _c.PerfilProducao.Nome.Should().Be("Produção");
        _c.Unidade.Salvamentos.Should().Be(salvamentos);
    }

    [Fact]
    public async Task Salvar_mantendo_o_proprio_nome_nao_e_conflito()
    {
        (await Salvar(_c.Diretor, _c.PerfilProducao.Id, "produção")).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task O_tipo_do_perfil_nao_muda()
    {
        var resultado = await Salvar(_c.Diretor, _c.PerfilProducao.Id, "Produção", TipoPerfil.Diretoria);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O tipo do perfil não pode ser alterado.");
        _c.PerfilProducao.Tipo.Should().Be(TipoPerfil.Producao);
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Perfil_inexistente_e_nao_encontrado()
    {
        (await Salvar(_c.Diretor, Guid.CreateVersion7(), "X")).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    // ---- Ativar e inativar ----

    [Fact]
    public async Task Perfil_sem_usuario_ativo_e_inativado_e_reativado()
    {
        (await DefinirAtivo(_c.Diretor, _c.PerfilProducao.Id, false)).Sucesso.Should().BeTrue();
        _c.PerfilProducao.Ativo.Should().BeFalse();

        (await DefinirAtivo(_c.Diretor, _c.PerfilProducao.Id, true)).Sucesso.Should().BeTrue();
        _c.PerfilProducao.Ativo.Should().BeTrue();
        _c.Unidade.Salvamentos.Should().Be(2);
    }

    [Fact]
    public async Task Perfil_com_usuario_ativo_nao_e_inativado()
    {
        Producao();

        var resultado = await DefinirAtivo(_c.Diretor, _c.PerfilProducao.Id, false);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("Há usuários ativos com este perfil. Troque o perfil deles antes de inativar.");
        _c.PerfilProducao.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Perfil_so_com_usuarios_inativos_pode_ser_inativado()
    {
        Producao().Inativar();

        (await DefinirAtivo(_c.Diretor, _c.PerfilProducao.Id, false)).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task O_perfil_do_diretor_em_uso_nao_e_inativado()
    {
        (await DefinirAtivo(_c.Diretor, _c.PerfilDiretor.Id, false)).Erro.Should().Be(TipoErro.Conflito);
        _c.PerfilDiretor.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Definir_a_mesma_situacao_nao_grava()
    {
        (await DefinirAtivo(_c.Diretor, _c.PerfilProducao.Id, true)).Sucesso.Should().BeTrue();

        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Ativar_ou_inativar_perfil_inexistente_e_nao_encontrado()
    {
        (await DefinirAtivo(_c.Diretor, Guid.CreateVersion7(), false)).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    // ---- Acesso e listagem ----

    [Fact]
    public async Task Producao_e_anonimo_nao_gerenciam_nem_listam_perfis()
    {
        var pedro = Producao();

        (await Salvar(pedro, null, "Novo")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(null, null, "Novo")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(pedro, _c.PerfilProducao.Id, "Outro nome")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await DefinirAtivo(pedro, _c.PerfilDiretor.Id, false)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await DefinirAtivo(null, _c.PerfilDiretor.Id, false)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Listar(pedro)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Perfis.Itens.Should().HaveCount(2);
        _c.PerfilProducao.Nome.Should().Be("Produção");
    }

    [Fact]
    public async Task Listagem_traz_todos_os_perfis_com_a_contagem_de_usuarios_ativos()
    {
        Producao();
        _c.NovoUsuario("Paula", "paula@x.com", _c.PerfilProducao).Inativar();
        var estagio = (await Salvar(_c.Diretor, null, "Estagiário")).Valor;
        (await DefinirAtivo(_c.Diretor, estagio, false)).Sucesso.Should().BeTrue();

        var perfis = (await Listar(_c.Diretor)).Valor!;

        perfis.Should().BeEquivalentTo(
        [
            new PerfilDetalhe(_c.PerfilDiretor.Id, "Diretor", TipoPerfil.Diretoria, true, 1),
            new PerfilDetalhe(estagio, "Estagiário", TipoPerfil.Producao, false, 0),
            new PerfilDetalhe(_c.PerfilProducao.Id, "Produção", TipoPerfil.Producao, true, 1),
        ], opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public void Listagem_e_uma_consulta_auditavel()
    {
        IConsultaAuditavel consulta = new ListarPerfis();

        consulta.TipoRecurso.Should().Be("Perfil");
        consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
    }
}
