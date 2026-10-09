using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class ContextoDeAcessoTests
{
    private readonly Cenario _cenario = new();

    private ContextoDeAcesso Criar(Guid? usuarioId)
    {
        var identidade = new IdentidadeAtual();
        identidade.DefinirInterno(usuarioId);
        return new ContextoDeAcesso(identidade, _cenario.Usuarios, _cenario.Perfis, _cenario.ClientesDaIdentidade);
    }

    [Fact]
    public async Task Devolve_o_usuario_com_o_tipo_do_perfil_lido_do_repositorio()
    {
        var ator = await Criar(_cenario.Diretor.Id).ObterAsync(default);

        ator.Should().Be(new UsuarioAutenticado(
            _cenario.Diretor.Id, "Diana Diretora", TipoPerfil.Diretoria, _cenario.PerfilDiretor.Id));
        ator!.EhDiretoria.Should().BeTrue();
    }

    [Fact]
    public async Task Sem_identidade_devolve_nulo()
    {
        (await Criar(null).ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Usuario_inexistente_devolve_nulo()
    {
        (await Criar(Guid.CreateVersion7()).ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Usuario_inativado_devolve_nulo_mesmo_com_identidade_valida()
    {
        _cenario.Diretor.Inativar();

        (await Criar(_cenario.Diretor.Id).ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Troca_de_perfil_vale_na_consulta_seguinte()
    {
        var contexto = Criar(_cenario.Diretor.Id);
        (await contexto.ObterAsync(default))!.EhDiretoria.Should().BeTrue();

        _cenario.Diretor.AlterarPerfil(_cenario.PerfilProducao);

        (await contexto.ObterAsync(default))!.EhDiretoria.Should().BeFalse();
    }

    private ContextoDeAcesso CriarComVersao(Guid usuarioId, Guid? versao)
    {
        var identidade = new IdentidadeAtual();
        identidade.Definir(usuarioId, null, versao);
        return new ContextoDeAcesso(identidade, _cenario.Usuarios, _cenario.Perfis, _cenario.ClientesDaIdentidade);
    }

    [Fact]
    public async Task Sessao_com_a_versao_vigente_e_aceita()
    {
        var contexto = CriarComVersao(_cenario.Diretor.Id, _cenario.Diretor.VersaoDeSessao);

        (await contexto.ObterAsync(default)).Should().NotBeNull();
    }

    [Fact]
    public async Task Sessao_anterior_a_troca_de_senha_e_recusada_na_consulta_seguinte()
    {
        var contexto = CriarComVersao(_cenario.Diretor.Id, _cenario.Diretor.VersaoDeSessao);
        (await contexto.ObterAsync(default)).Should().NotBeNull();

        _cenario.Diretor.DefinirSenha("novo-hash");

        (await contexto.ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Sessao_antiga_nao_revive_ao_reativar_o_usuario()
    {
        var contexto = CriarComVersao(_cenario.Diretor.Id, _cenario.Diretor.VersaoDeSessao);

        _cenario.Diretor.Inativar();
        _cenario.Diretor.Ativar();

        (await contexto.ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Versao_vazia_nunca_e_aceita()
    {
        (await CriarComVersao(_cenario.Diretor.Id, Guid.Empty).ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Usuario_de_cliente_vem_com_o_cliente_e_e_reconhecido_como_cliente()
    {
        var cliente = _cenario.NovoCliente();
        var carla = _cenario.NovoUsuarioDeCliente("Carla Cliente", "carla@x.com", cliente);

        var ator = await Criar(carla.Id).ObterAsync(default);

        ator.Should().Be(new UsuarioAutenticado(
            carla.Id, "Carla Cliente", TipoPerfil.Cliente, _cenario.PerfilCliente.Id, cliente.Id));
        ator!.EhCliente.Should().BeTrue();
        ator.EhDiretoria.Should().BeFalse();
        ator.ClienteId.Should().Be(cliente.Id);
    }

    [Fact]
    public async Task Usuario_de_cliente_inativo_devolve_nulo_e_volta_ao_reativar()
    {
        var cliente = _cenario.NovoCliente();
        var carla = _cenario.NovoUsuarioDeCliente("Carla Cliente", "carla@x.com", cliente);
        var contexto = Criar(carla.Id);
        (await contexto.ObterAsync(default)).Should().NotBeNull();

        cliente.Inativar();
        (await contexto.ObterAsync(default)).Should().BeNull();

        cliente.Ativar();
        (await contexto.ObterAsync(default)).Should().NotBeNull();
    }

    [Fact]
    public async Task Usuario_de_cliente_que_nao_existe_devolve_nulo()
    {
        var cliente = _cenario.NovoCliente();
        var carla = _cenario.NovoUsuarioDeCliente("Carla Cliente", "carla@x.com", cliente);
        _cenario.Clientes.Itens.Remove(cliente);

        (await Criar(carla.Id).ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Par_perfil_cliente_incoerente_devolve_nulo()
    {
        var cliente = _cenario.NovoCliente();
        var carla = _cenario.NovoUsuarioDeCliente("Carla Cliente", "carla@x.com", cliente);
        var pedro = _cenario.NovoUsuario("Pedro", "pedro@x.com", _cenario.PerfilProducao);
        var clienteId = typeof(Usuario).GetProperty(nameof(Usuario.ClienteId))!;

        // Só possível por edição direta no banco: um usuário interno com cliente e um usuário de cliente sem ele.
        clienteId.SetValue(pedro, cliente.Id);
        clienteId.SetValue(carla, null);

        (await Criar(pedro.Id).ObterAsync(default)).Should().BeNull();
        (await Criar(carla.Id).ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Usuario_interno_nao_consulta_clientes()
    {
        await Criar(_cenario.Diretor.Id).ObterAsync(default);

        _cenario.ClientesDaIdentidade.Consultas.Should().Be(0);
    }

    [Fact]
    public async Task Usuario_com_versao_armazenada_vazia_nunca_aceita_sessao_vazia()
    {
        // Simula uma linha anterior à migração, cuja versão armazenada era só zeros.
        typeof(Usuario).GetProperty(nameof(Usuario.VersaoDeSessao))!.SetValue(_cenario.Diretor, Guid.Empty);

        (await CriarComVersao(_cenario.Diretor.Id, Guid.Empty).ObterAsync(default)).Should().BeNull();
    }
}
