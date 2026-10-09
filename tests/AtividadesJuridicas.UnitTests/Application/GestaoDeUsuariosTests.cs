using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class GestaoDeUsuariosTests
{
    private readonly Cenario _c = new();

    private Task<Resultado<Guid>> Criar(Usuario? ator, CriarUsuario requisicao) =>
        new CriarUsuarioManipulador(_c.Como(ator), _c.Usuarios, _c.Perfis, _c.Hasher, _c.Unidade, _c.ClientesDaIdentidade)
            .ManipularAsync(requisicao, default);

    private Task<Resultado> Editar(Usuario? ator, EditarUsuario requisicao) =>
        new EditarUsuarioManipulador(_c.Como(ator), _c.Usuarios, _c.Perfis, _c.Unidade, _c.Auditoria, _c.ClientesDaIdentidade)
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

    private const string PerfilInativoNaAtivacao =
        "O perfil deste usuário está inativo. Reative o perfil ou troque o perfil do usuário antes de ativá-lo.";

    [Fact]
    public async Task Usuario_de_perfil_inativo_nao_e_reativado_e_nada_e_gravado_nem_auditado()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        alvo.Inativar();
        _c.PerfilProducao.Inativar();

        var resultado = await DefinirAtivo(_c.Diretor, alvo.Id, true);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be(PerfilInativoNaAtivacao);
        alvo.Ativo.Should().BeFalse();
        _c.Unidade.Salvamentos.Should().Be(0);
        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Saida_para_usuario_inativo_de_perfil_inativo_e_trocar_o_perfil_e_ativar()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        alvo.Inativar();
        _c.PerfilProducao.Inativar();
        var outro = Perfil.Criar("Estagiário", TipoPerfil.Producao);
        _c.Perfis.Itens.Add(outro);

        (await Editar(_c.Diretor, new EditarUsuario(alvo.Id, "Pedro", "pedro@x.com", outro.Id, null)))
            .Sucesso.Should().BeTrue();
        var ativou = await DefinirAtivo(_c.Diretor, alvo.Id, true);

        ativou.Sucesso.Should().BeTrue(ativou.Mensagem);
        alvo.Ativo.Should().BeTrue();
        alvo.PerfilId.Should().Be(outro.Id);
    }

    [Fact]
    public async Task Editar_para_perfil_inativo_e_erro_de_validacao()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _c.PerfilDiretor.Inativar();

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(alvo.Id, "Pedro", "pedro@x.com", _c.PerfilDiretor.Id, null));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        alvo.PerfilId.Should().Be(_c.PerfilProducao.Id);
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

    // ---- Usuários de cliente ----

    private const string InformeOCliente = "Informe o cliente do usuário.";
    private const string ClienteNaoEncontrado = "Cliente não encontrado.";
    private const string ClienteInativo = "O cliente está inativo.";

    private Perfil PerfilClienteNoCenario()
    {
        if (!_c.Perfis.Itens.Contains(_c.PerfilCliente)) _c.Perfis.Itens.Add(_c.PerfilCliente);
        return _c.PerfilCliente;
    }

    private CriarUsuario PedidoDeCliente(Guid? clienteId, string email = "carla@x.com", string? oab = null) =>
        new("Carla Cliente", email, "senha-da-carla", PerfilClienteNoCenario().Id, oab, clienteId);

    [Fact]
    public async Task Diretor_cria_usuario_de_cliente_vinculado_ao_cliente_e_sem_oab()
    {
        var maria = _c.NovoCliente();

        var resultado = await Criar(_c.Diretor, PedidoDeCliente(maria.Id, oab: "AL 99"));

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var criado = _c.Usuarios.Itens.Single(u => u.Id == resultado.Valor);
        criado.ClienteId.Should().Be(maria.Id);
        criado.EhDeCliente.Should().BeTrue();
        criado.PerfilId.Should().Be(_c.PerfilCliente.Id);
        criado.NumeroOab.Should().BeNull("a OAB informada para um usuário de cliente é ignorada");
        criado.SenhaHash.Should().Be("hash:senha-da-carla");
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Perfil_de_cliente_sem_cliente_com_cliente_inexistente_ou_inativo_e_recusado()
    {
        var inativa = _c.NovoCliente("José Souza", "111.444.777-35");
        inativa.Inativar();

        var sem = await Criar(_c.Diretor, PedidoDeCliente(null));
        var inexistente = await Criar(_c.Diretor, PedidoDeCliente(Guid.CreateVersion7()));
        var inativo = await Criar(_c.Diretor, PedidoDeCliente(inativa.Id));

        sem.Erro.Should().Be(TipoErro.Validacao);
        sem.Mensagem.Should().Be(InformeOCliente);
        inexistente.Erro.Should().Be(TipoErro.Validacao);
        inexistente.Mensagem.Should().Be(ClienteNaoEncontrado);
        inativo.Erro.Should().Be(TipoErro.Validacao);
        inativo.Mensagem.Should().Be(ClienteInativo);
        _c.Usuarios.Itens.Should().ContainSingle("só a Diretora existe");
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Perfil_interno_com_cliente_e_recusado()
    {
        var maria = _c.NovoCliente();

        var resultado = await Criar(_c.Diretor, NovoPedido() with { ClienteId = maria.Id });

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(Usuario.SoDeCliente);
        _c.Usuarios.Itens.Should().ContainSingle();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Editar_nao_troca_entre_perfil_de_cliente_e_perfil_interno()
    {
        var maria = _c.NovoCliente();
        var pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        var carla = _c.NovoUsuarioDeCliente("Carla", "carla@x.com", maria);
        var sessaoPedro = pedro.VersaoDeSessao;
        var sessaoCarla = carla.VersaoDeSessao;

        var internoParaCliente = await Editar(_c.Diretor,
            new EditarUsuario(pedro.Id, "Pedro", "pedro@x.com", _c.PerfilCliente.Id, null, maria.Id));
        var clienteParaInterno = await Editar(_c.Diretor,
            new EditarUsuario(carla.Id, "Carla", "carla@x.com", _c.PerfilProducao.Id, null));

        internoParaCliente.Erro.Should().Be(TipoErro.Validacao);
        internoParaCliente.Mensagem.Should().Be(Usuario.TrocaEntreTipos);
        clienteParaInterno.Erro.Should().Be(TipoErro.Validacao);
        clienteParaInterno.Mensagem.Should().Be(Usuario.TrocaEntreTipos);
        pedro.PerfilId.Should().Be(_c.PerfilProducao.Id);
        pedro.ClienteId.Should().BeNull();
        pedro.VersaoDeSessao.Should().Be(sessaoPedro);
        carla.PerfilId.Should().Be(_c.PerfilCliente.Id);
        carla.ClienteId.Should().Be(maria.Id);
        carla.VersaoDeSessao.Should().Be(sessaoCarla);
        _c.Auditoria.Eventos.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Trocar_o_cliente_do_usuario_gera_evento_e_renova_a_sessao()
    {
        var maria = _c.NovoCliente();
        var jose = _c.NovoCliente("José Souza", "111.444.777-35");
        var carla = _c.NovoUsuarioDeCliente("Carla", "carla@x.com", maria);
        var sessao = carla.VersaoDeSessao;

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(carla.Id, "Carla", "carla@x.com", _c.PerfilCliente.Id, null, jose.Id));

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        carla.ClienteId.Should().Be(jose.Id);
        carla.VersaoDeSessao.Should().NotBe(sessao);
        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.UsuarioClienteAlterado);
        evento.Evento.Should().Be("usuario_cliente_alterado");
        evento.Imediato.Should().BeFalse("o evento é gravado na mesma transação");
        evento.Entidade.Should().Be("Usuario");
        evento.EntidadeId.Should().Be(carla.Id);
        evento.Dados.Should().BeEquivalentTo(new { de = maria.Id, para = jose.Id });
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Manter_o_cliente_nao_gera_evento_nem_renova_a_sessao_mesmo_com_o_cliente_inativo()
    {
        var maria = _c.NovoCliente();
        var carla = _c.NovoUsuarioDeCliente("Carla", "carla@x.com", maria);
        maria.Inativar();
        var sessao = carla.VersaoDeSessao;

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(carla.Id, "Carla Cliente", "carla.cliente@x.com", _c.PerfilCliente.Id, null, maria.Id));

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        carla.Nome.Should().Be("Carla Cliente");
        carla.Email.Valor.Should().Be("carla.cliente@x.com");
        carla.ClienteId.Should().Be(maria.Id);
        carla.VersaoDeSessao.Should().Be(sessao);
        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Trocar_para_cliente_inexistente_ou_inativo_e_recusado()
    {
        var maria = _c.NovoCliente();
        var inativa = _c.NovoCliente("José Souza", "111.444.777-35");
        inativa.Inativar();
        var carla = _c.NovoUsuarioDeCliente("Carla", "carla@x.com", maria);
        var sessao = carla.VersaoDeSessao;

        var inexistente = await Editar(_c.Diretor,
            new EditarUsuario(carla.Id, "Carla", "carla@x.com", _c.PerfilCliente.Id, null, Guid.CreateVersion7()));
        var inativo = await Editar(_c.Diretor,
            new EditarUsuario(carla.Id, "Carla", "carla@x.com", _c.PerfilCliente.Id, null, inativa.Id));

        inexistente.Erro.Should().Be(TipoErro.Validacao);
        inexistente.Mensagem.Should().Be(ClienteNaoEncontrado);
        inativo.Erro.Should().Be(TipoErro.Validacao);
        inativo.Mensagem.Should().Be(ClienteInativo);
        carla.ClienteId.Should().Be(maria.Id);
        carla.VersaoDeSessao.Should().Be(sessao);
        _c.Auditoria.Eventos.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Editar_usuario_de_cliente_sem_cliente_e_recusado()
    {
        var maria = _c.NovoCliente();
        var carla = _c.NovoUsuarioDeCliente("Carla", "carla@x.com", maria);

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(carla.Id, "Carla", "carla@x.com", _c.PerfilCliente.Id, null));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(InformeOCliente);
        carla.ClienteId.Should().Be(maria.Id);
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Editar_usuario_interno_com_cliente_e_recusado()
    {
        var maria = _c.NovoCliente();
        var pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(pedro.Id, "Pedro", "pedro@x.com", _c.PerfilProducao.Id, null, maria.Id));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be(Usuario.SoDeCliente);
        pedro.ClienteId.Should().BeNull();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Lista_de_usuarios_traz_o_cliente_de_quem_e_de_cliente()
    {
        var maria = _c.NovoCliente();
        var carla = _c.NovoUsuarioDeCliente("Carla", "carla@x.com", maria);
        maria.Inativar();

        var lista = (await new ListarUsuariosManipulador(_c.Como(_c.Diretor), _c.Usuarios)
            .ManipularAsync(new ListarUsuarios(), default)).Valor!;

        var daCarla = lista.Single(u => u.Id == carla.Id);
        daCarla.ClienteId.Should().Be(maria.Id);
        daCarla.ClienteNome.Should().Be("Maria Silva", "o nome vem mesmo com o cliente inativo");
        var diana = lista.Single(u => u.Id == _c.Diretor.Id);
        diana.ClienteId.Should().BeNull();
        diana.ClienteNome.Should().BeNull();
    }

    [Fact]
    public async Task Usuarios_do_cliente_so_para_a_diretoria_e_so_os_daquele_cliente()
    {
        var maria = _c.NovoCliente();
        var jose = _c.NovoCliente("José Souza", "111.444.777-35");
        var zilda = _c.NovoUsuarioDeCliente("Zilda", "zilda@x.com", maria);
        var carla = _c.NovoUsuarioDeCliente("Carla", "carla@x.com", maria);
        carla.Inativar();
        _c.NovoUsuarioDeCliente("Jorge", "jorge@x.com", jose);
        var pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        Task<Resultado<IReadOnlyList<UsuarioDoCliente>>> Listar(Usuario? ator, Guid clienteId) =>
            new ListarUsuariosDoClienteManipulador(_c.Como(ator), _c.Usuarios, _c.ClientesDaIdentidade)
                .ManipularAsync(new ListarUsuariosDoCliente(clienteId), default);

        (await Listar(pedro, maria.Id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Listar(null, maria.Id)).Erro.Should().Be(TipoErro.AcessoNegado);

        var inexistente = await Listar(_c.Diretor, Guid.CreateVersion7());
        inexistente.Erro.Should().Be(TipoErro.NaoEncontrado);
        inexistente.Mensagem.Should().Be(ClienteNaoEncontrado);

        var lista = (await Listar(_c.Diretor, maria.Id)).Valor!;
        lista.Select(u => u.Nome).Should().Equal("Carla", "Zilda");
        lista.Select(u => u.Ativo).Should().Equal(false, true);
        lista[1].Should().Be(new UsuarioDoCliente(zilda.Id, "Zilda", "zilda@x.com", true, null));

        var consulta = new ListarUsuariosDoCliente(maria.Id);
        consulta.Should().BeAssignableTo<IConsultaAuditavel>();
        consulta.TipoRecurso.Should().Be("Usuario");
        consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
        consulta.Detalhe.Should().NotBeNull().And.Contain(maria.Id.ToString());
    }
}
