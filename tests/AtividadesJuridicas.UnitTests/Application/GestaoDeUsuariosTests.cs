using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class GestaoDeUsuariosTests
{
    private readonly Cenario _c = new();

    private Task<Resultado<Guid>> Criar(Usuario? ator, CriarUsuario requisicao) =>
        new CriarUsuarioManipulador(_c.Como(ator), _c.Usuarios, _c.Perfis, _c.Hasher, _c.Unidade)
            .ManipularAsync(requisicao, default);

    private Task<Resultado> Editar(Usuario? ator, EditarUsuario requisicao) =>
        new EditarUsuarioManipulador(_c.Como(ator), _c.Usuarios, _c.Perfis, _c.Unidade, _c.Auditoria)
            .ManipularAsync(requisicao, default);

    private Task<Resultado> DefinirAtivo(Usuario? ator, Guid id, bool ativo) =>
        new DefinirAtivoUsuarioManipulador(_c.Como(ator), _c.Usuarios, _c.Perfis, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirAtivoUsuario(id, ativo), default);

    private CriarUsuario NovoPedido(string email = "paulo@x.com") =>
        new("Paulo Produção", email, "senha-segura", _c.PerfilProducao.Id, "AL 99");

    // ---- Criar ----

    [Fact]
    public async Task Diretor_cria_usuario_com_senha_em_hash()
    {
        var resultado = await Criar(_c.Diretor, NovoPedido());

        resultado.Sucesso.Should().BeTrue();
        var criado = _c.Usuarios.Itens.Single(u => u.Id == resultado.Valor);
        criado.Email.Valor.Should().Be("paulo@x.com");
        criado.PerfilId.Should().Be(_c.PerfilProducao.Id);
        criado.SenhaHash.Should().Be("hash:senha-segura");
        criado.Ativo.Should().BeTrue();
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Producao_e_anonimo_nao_criam_usuario()
    {
        var producao = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        (await Criar(producao, NovoPedido())).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Criar(null, NovoPedido())).Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Usuarios.Itens.Should().HaveCount(2);
    }

    [Fact]
    public async Task Email_ja_usado_em_outra_caixa_e_conflito()
    {
        var resultado = await Criar(_c.Diretor, NovoPedido(" DIANA@x.com "));

        resultado.Erro.Should().Be(TipoErro.Conflito);
    }

    [Theory]
    [InlineData("", "paulo@x.com", "senha-segura")]
    [InlineData("Paulo", "invalido", "senha-segura")]
    [InlineData("Paulo", "paulo@x.com", "curta")]
    public async Task Dados_invalidos_sao_erro_de_validacao(string nome, string email, string senha)
    {
        var resultado = await Criar(_c.Diretor, new CriarUsuario(nome, email, senha, _c.PerfilProducao.Id, null));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Perfil_inexistente_ou_inativo_e_erro_de_validacao()
    {
        _c.PerfilProducao.Inativar();

        (await Criar(_c.Diretor, NovoPedido())).Erro.Should().Be(TipoErro.Validacao);
        (await Criar(_c.Diretor, NovoPedido() with { PerfilId = Guid.CreateVersion7() }))
            .Erro.Should().Be(TipoErro.Validacao);
    }

    // ---- Editar ----

    [Fact]
    public async Task Diretor_edita_dados_e_perfil_de_outro_usuario()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(alvo.Id, "Pedro Silva", "pedro.silva@x.com", _c.PerfilDiretor.Id, null));

        resultado.Sucesso.Should().BeTrue();
        alvo.Nome.Should().Be("Pedro Silva");
        alvo.Email.Valor.Should().Be("pedro.silva@x.com");
        alvo.PerfilId.Should().Be(_c.PerfilDiretor.Id);
    }

    [Fact]
    public async Task Editar_mantendo_o_proprio_email_nao_e_conflito()
    {
        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(_c.Diretor.Id, "Diana D.", "diana@x.com", _c.PerfilDiretor.Id, null));

        resultado.Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Editar_para_email_de_outro_usuario_e_conflito()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(alvo.Id, "Pedro", "diana@x.com", _c.PerfilProducao.Id, null));

        resultado.Erro.Should().Be(TipoErro.Conflito);
    }

    [Fact]
    public async Task Editar_usuario_inexistente_e_nao_encontrado()
    {
        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(Guid.CreateVersion7(), "X", "x@x.com", _c.PerfilProducao.Id, null));

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    [Fact]
    public async Task Ultimo_diretor_nao_pode_trocar_o_proprio_perfil_para_producao()
    {
        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(_c.Diretor.Id, "Diana", "diana@x.com", _c.PerfilProducao.Id, null));

        resultado.Erro.Should().Be(TipoErro.Conflito);
        _c.Diretor.PerfilId.Should().Be(_c.PerfilDiretor.Id);
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Havendo_outro_diretor_ativo_a_troca_de_perfil_e_permitida()
    {
        _c.NovoUsuario("Dora", "dora@x.com", _c.PerfilDiretor);

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(_c.Diretor.Id, "Diana", "diana@x.com", _c.PerfilProducao.Id, null));

        resultado.Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Outro_diretor_inativo_nao_conta_para_liberar_a_troca()
    {
        _c.NovoUsuario("Dora", "dora@x.com", _c.PerfilDiretor).Inativar();

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(_c.Diretor.Id, "Diana", "diana@x.com", _c.PerfilProducao.Id, null));

        resultado.Erro.Should().Be(TipoErro.Conflito);
    }

    // ---- Ativar / inativar ----

    [Fact]
    public async Task Ultimo_diretor_nao_pode_inativar_a_si_mesmo()
    {
        var resultado = await DefinirAtivo(_c.Diretor, _c.Diretor.Id, false);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        _c.Diretor.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Diretor_inativa_e_reativa_usuario_de_producao()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        (await DefinirAtivo(_c.Diretor, alvo.Id, false)).Sucesso.Should().BeTrue();
        alvo.Ativo.Should().BeFalse();

        (await DefinirAtivo(_c.Diretor, alvo.Id, true)).Sucesso.Should().BeTrue();
        alvo.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Producao_nao_inativa_ninguem()
    {
        var producao = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        (await DefinirAtivo(producao, _c.Diretor.Id, false)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    // ---- Listar ----

    [Fact]
    public async Task Diretor_lista_usuarios_e_perfis_ativos()
    {
        _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var usuarios = await new ListarUsuariosManipulador(_c.Como(_c.Diretor), _c.Usuarios)
            .ManipularAsync(new ListarUsuarios(), default);
        var perfis = await new ListarPerfisAtivosManipulador(_c.Como(_c.Diretor), _c.Perfis)
            .ManipularAsync(new ListarPerfisAtivos(), default);

        usuarios.Valor!.Select(u => u.Nome).Should().Equal("Diana Diretora", "Pedro");
        perfis.Valor!.Select(p => p.Nome).Should().BeEquivalentTo("Diretor", "Produção");
    }

    [Fact]
    public async Task Producao_nao_lista_usuarios()
    {
        var producao = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await new ListarUsuariosManipulador(_c.Como(producao), _c.Usuarios)
            .ManipularAsync(new ListarUsuarios(), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        resultado.Valor.Should().BeNull();
    }
}
