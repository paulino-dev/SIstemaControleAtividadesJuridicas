using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Domain;

public class UsuarioTests
{
    private static readonly Perfil Diretor = Perfil.Criar("Diretor", TipoPerfil.Diretoria);

    private static Usuario Novo() =>
        Usuario.Criar("Ana Souza", Email.Criar("ana@x.com"), "hash", Diretor, " AL 1234 ");

    [Fact]
    public void Nasce_ativo_com_dados_aparados()
    {
        var usuario = Novo();

        usuario.Nome.Should().Be("Ana Souza");
        usuario.Email.Valor.Should().Be("ana@x.com");
        usuario.PerfilId.Should().Be(Diretor.Id);
        usuario.NumeroOab.Should().Be("AL 1234");
        usuario.Ativo.Should().BeTrue();
        usuario.UltimoAcessoEm.Should().BeNull();
    }

    [Fact]
    public void Oab_em_branco_vira_nulo()
    {
        var usuario = Usuario.Criar("Ana", Email.Criar("ana@x.com"), "hash", Diretor, "   ");

        usuario.NumeroOab.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_nome_vazio(string nome)
    {
        var acao = () => Usuario.Criar(nome, Email.Criar("ana@x.com"), "hash", Diretor);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_nome_maior_que_200_caracteres()
    {
        var acao = () => Usuario.Criar(new string('a', 201), Email.Criar("ana@x.com"), "hash", Diretor);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_perfil_inativo_na_criacao_e_na_troca()
    {
        var inativo = Perfil.Criar("Antigo", TipoPerfil.Producao);
        inativo.Inativar();

        var criar = () => Usuario.Criar("Ana", Email.Criar("ana@x.com"), "hash", inativo);
        var trocar = () => Novo().AlterarPerfil(inativo);

        criar.Should().Throw<ExcecaoDeDominio>();
        trocar.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_hash_de_senha_vazio()
    {
        var acao = () => Novo().DefinirSenha(" ");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Inativar_e_ativar_alternam_o_estado()
    {
        var usuario = Novo();

        usuario.Inativar();
        usuario.Ativo.Should().BeFalse();

        usuario.Ativar();
        usuario.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Registrar_acesso_guarda_o_momento()
    {
        var usuario = Novo();
        var agora = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

        usuario.RegistrarAcesso(agora);

        usuario.UltimoAcessoEm.Should().Be(agora);
    }

    private static readonly DateTime Agora = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Quinta_falha_seguida_bloqueia_por_quinze_minutos()
    {
        var usuario = Novo();

        for (var i = 1; i <= 4; i++)
            usuario.RegistrarFalhaDeLogin(Agora).Should().BeFalse($"a falha {i} ainda não bloqueia");
        usuario.EstaBloqueado(Agora).Should().BeFalse();

        usuario.RegistrarFalhaDeLogin(Agora).Should().BeTrue();

        usuario.EstaBloqueado(Agora).Should().BeTrue();
        usuario.EstaBloqueado(Agora.AddMinutes(14).AddSeconds(59)).Should().BeTrue();
        usuario.EstaBloqueado(Agora.AddMinutes(15)).Should().BeFalse();
    }

    [Fact]
    public void Depois_do_bloqueio_a_contagem_recomeca()
    {
        var usuario = Novo();
        for (var i = 0; i < 5; i++) usuario.RegistrarFalhaDeLogin(Agora);

        usuario.RegistrarFalhaDeLogin(Agora.AddMinutes(16)).Should().BeFalse();
    }

    [Fact]
    public void Acesso_bem_sucedido_zera_as_falhas()
    {
        var usuario = Novo();
        for (var i = 0; i < 4; i++) usuario.RegistrarFalhaDeLogin(Agora);

        usuario.RegistrarAcesso(Agora);

        usuario.FalhasDeLogin.Should().Be(0);
        for (var i = 0; i < 4; i++) usuario.RegistrarFalhaDeLogin(Agora).Should().BeFalse();
    }

    [Fact]
    public void Desbloquear_remove_o_bloqueio_e_as_falhas()
    {
        var usuario = Novo();
        for (var i = 0; i < 5; i++) usuario.RegistrarFalhaDeLogin(Agora);

        usuario.Desbloquear();

        usuario.EstaBloqueado(Agora).Should().BeFalse();
        usuario.FalhasDeLogin.Should().Be(0);
    }

    [Fact]
    public void Versao_de_sessao_nasce_preenchida_e_muda_ao_definir_senha_e_ao_inativar()
    {
        var usuario = Novo();
        var inicial = usuario.VersaoDeSessao;
        inicial.Should().NotBe(Guid.Empty);

        usuario.DefinirSenha("outro-hash");
        var aposSenha = usuario.VersaoDeSessao;
        aposSenha.Should().NotBe(inicial);

        usuario.Inativar();
        usuario.VersaoDeSessao.Should().NotBe(aposSenha);
    }

    [Fact]
    public void Versao_de_sessao_nao_muda_ao_editar_dados_trocar_perfil_ou_reativar()
    {
        var usuario = Novo();
        usuario.Inativar();
        var versao = usuario.VersaoDeSessao;

        usuario.Ativar();
        usuario.AlterarDados("Outro Nome", Email.Criar("outro@x.com"), null);
        usuario.AlterarPerfil(Perfil.Criar("Produção", TipoPerfil.Producao));

        usuario.VersaoDeSessao.Should().Be(versao, "reativar não revive nem renova sessões");
    }

    // ---- Usuário de cliente ----

    private static readonly Perfil PerfilCliente = Perfil.Criar("Cliente", TipoPerfil.Cliente);
    private static readonly Guid Maria = Guid.CreateVersion7();

    private static Usuario NovoDeCliente() =>
        Usuario.Criar("Carla Cliente", Email.Criar("carla@x.com"), "hash", PerfilCliente, " AL 1 ", Maria);

    [Fact]
    public void Usuario_de_cliente_nasce_vinculado_ao_cliente_e_sem_oab()
    {
        var usuario = NovoDeCliente();

        usuario.ClienteId.Should().Be(Maria);
        usuario.EhDeCliente.Should().BeTrue();
        usuario.PerfilId.Should().Be(PerfilCliente.Id);
        usuario.NumeroOab.Should().BeNull("OAB é da equipe");
        Novo().EhDeCliente.Should().BeFalse();
        Novo().ClienteId.Should().BeNull();
    }

    [Fact]
    public void Perfil_de_cliente_exige_cliente_e_perfil_interno_o_recusa()
    {
        var semCliente = () => Usuario.Criar("Carla", Email.Criar("carla@x.com"), "hash", PerfilCliente);
        var internoComCliente = () => Usuario.Criar("Ana", Email.Criar("ana@x.com"), "hash", Diretor, null, Maria);
        var clienteVazio = () => Usuario.Criar("Carla", Email.Criar("carla@x.com"), "hash", PerfilCliente, null, Guid.Empty);

        semCliente.Should().Throw<ExcecaoDeDominio>().WithMessage("Informe o cliente do usuário.");
        internoComCliente.Should().Throw<ExcecaoDeDominio>().WithMessage(Usuario.SoDeCliente);
        clienteVazio.Should().Throw<ExcecaoDeDominio>().WithMessage("Cliente inválido.");
    }

    [Fact]
    public void Usuario_nao_troca_entre_perfil_de_cliente_e_perfil_interno()
    {
        var deCliente = NovoDeCliente();
        var interno = Novo();

        var paraInterno = () => deCliente.AlterarPerfil(Diretor);
        var paraCliente = () => interno.AlterarPerfil(PerfilCliente);

        paraInterno.Should().Throw<ExcecaoDeDominio>().WithMessage(Usuario.TrocaEntreTipos);
        paraCliente.Should().Throw<ExcecaoDeDominio>().WithMessage(Usuario.TrocaEntreTipos);
        deCliente.PerfilId.Should().Be(PerfilCliente.Id);
        interno.PerfilId.Should().Be(Diretor.Id);
    }

    [Fact]
    public void Trocar_o_cliente_renova_a_sessao_e_manter_o_mesmo_nao()
    {
        var usuario = NovoDeCliente();
        var versao = usuario.VersaoDeSessao;
        var outro = Guid.CreateVersion7();

        usuario.VincularAoCliente(Maria).Should().BeFalse();
        usuario.VersaoDeSessao.Should().Be(versao);

        usuario.VincularAoCliente(outro).Should().BeTrue();
        usuario.ClienteId.Should().Be(outro);
        usuario.VersaoDeSessao.Should().NotBe(versao);
    }

    [Fact]
    public void So_usuario_de_cliente_e_vinculado_e_o_cliente_precisa_ser_valido()
    {
        var interno = () => Novo().VincularAoCliente(Maria);
        var vazio = () => NovoDeCliente().VincularAoCliente(Guid.Empty);

        interno.Should().Throw<ExcecaoDeDominio>().WithMessage(Usuario.SoDeCliente);
        vazio.Should().Throw<ExcecaoDeDominio>().WithMessage("Cliente inválido.");
    }

    [Fact]
    public void Editar_os_dados_de_usuario_de_cliente_mantem_a_oab_nula()
    {
        var usuario = NovoDeCliente();

        usuario.AlterarDados("Carla C.", Email.Criar("carla.c@x.com"), "AL 9");

        usuario.Nome.Should().Be("Carla C.");
        usuario.NumeroOab.Should().BeNull();
    }
}
