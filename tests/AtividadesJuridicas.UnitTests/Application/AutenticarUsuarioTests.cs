using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class AutenticarUsuarioTests
{
    private const string Invalido = "E-mail ou senha inválidos.";
    private const string Bloqueado = "Muitas tentativas de login. Aguarde 15 minutos e tente novamente.";

    private readonly Cenario _c = new();

    private Task<Resultado<SessaoIniciada>> Autenticar(string email, string senha) =>
        new AutenticarUsuarioManipulador(_c.Usuarios, _c.Perfis, _c.Hasher, _c.Relogio, _c.Auditoria,
            _c.ClientesDaIdentidade)
            .ManipularAsync(new AutenticarUsuario(email, senha), default);

    private string UltimoEvento() => _c.Auditoria.Eventos.Last().Evento;

    [Fact]
    public async Task Credenciais_corretas_autenticam_registram_o_acesso_e_o_evento()
    {
        var resultado = await Autenticar("diana@x.com", "senha-segura");

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor.Should().Be(new SessaoIniciada(
            _c.Diretor.Id, "Diana Diretora", TipoPerfil.Diretoria, _c.Diretor.VersaoDeSessao));
        _c.Diretor.UltimoAcessoEm.Should().Be(_c.Relogio.AgoraUtc);
        _c.Usuarios.Reservas.Should().Be(1);
        _c.Usuarios.LoginsRegistrados.Should().Be(1);
        _c.Unidade.Salvamentos.Should().Be(0, "registrar o acesso não é alteração de negócio");

        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Should().BeEquivalentTo(new
        {
            Evento = EventosDeSeguranca.LoginSucesso, Entidade = "Usuario", EntidadeId = _c.Diretor.Id, Imediato = true,
        });
    }

    [Fact]
    public async Task Email_com_maiusculas_e_espacos_encontra_o_mesmo_usuario()
    {
        (await Autenticar("  DIANA@X.com ", "senha-segura")).Sucesso.Should().BeTrue();
    }

    [Theory]
    [InlineData("ninguem@x.com", "senha-segura")]
    [InlineData("nao-e-email", "senha-segura")]
    [InlineData("", "")]
    public async Task Email_desconhecido_recebe_a_mensagem_generica_e_ainda_assim_gasta_uma_verificacao(
        string email, string senha)
    {
        var resultado = await Autenticar(email, senha);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        resultado.Mensagem.Should().Be(Invalido);
        _c.Hasher.Verificacoes.Should().Be(1, "o tempo de resposta não deve distinguir e-mail inexistente");
        UltimoEvento().Should().Be(EventosDeSeguranca.LoginFalha);
        _c.Auditoria.Eventos.Last().EntidadeId.Should().BeNull();
        _c.Usuarios.Reservas.Should().Be(0);
    }

    [Fact]
    public async Task Usuario_inativo_nao_autentica_mesmo_com_a_senha_certa()
    {
        _c.Diretor.Inativar();

        var resultado = await Autenticar("diana@x.com", "senha-segura");

        resultado.Mensagem.Should().Be(Invalido);
        UltimoEvento().Should().Be(EventosDeSeguranca.LoginFalha);
    }

    [Fact]
    public async Task Senha_errada_conta_a_falha_e_registra_o_evento_com_o_usuario()
    {
        var resultado = await Autenticar("diana@x.com", "senha-errada");

        resultado.Mensagem.Should().Be(Invalido);
        _c.Diretor.FalhasDeLogin.Should().Be(1);
        _c.Usuarios.Reservas.Should().Be(1);
        _c.Usuarios.LoginsRegistrados.Should().Be(0);
        _c.Auditoria.Eventos.Last().Should().BeEquivalentTo(new
        {
            Evento = EventosDeSeguranca.LoginFalha, Entidade = "Usuario", EntidadeId = _c.Diretor.Id, Imediato = true,
        });
    }

    [Fact]
    public async Task Quinta_senha_errada_bloqueia_e_avisa()
    {
        for (var i = 0; i < 4; i++)
            (await Autenticar("diana@x.com", "senha-errada")).Mensagem.Should().Be(Invalido);

        var quinta = await Autenticar("diana@x.com", "senha-errada");

        quinta.Erro.Should().Be(TipoErro.AcessoNegado);
        quinta.Mensagem.Should().Be(Bloqueado);
        UltimoEvento().Should().Be(EventosDeSeguranca.LoginBloqueado);
    }

    [Fact]
    public async Task Senha_correta_durante_o_bloqueio_e_recusada_sem_verificar_a_senha()
    {
        for (var i = 0; i < 5; i++) await Autenticar("diana@x.com", "senha-errada");
        var verificacoesAntes = _c.Hasher.Verificacoes;

        var resultado = await Autenticar("diana@x.com", "senha-segura");

        resultado.Sucesso.Should().BeFalse();
        resultado.Mensagem.Should().Be(Bloqueado);
        _c.Hasher.Verificacoes.Should().Be(verificacoesAntes);
        UltimoEvento().Should().Be(EventosDeSeguranca.LoginFalha);
    }

    [Fact]
    public async Task Passados_quinze_minutos_a_senha_correta_volta_a_funcionar()
    {
        for (var i = 0; i < 5; i++) await Autenticar("diana@x.com", "senha-errada");

        _c.Relogio.AgoraUtc = _c.Relogio.AgoraUtc.AddMinutes(15);

        (await Autenticar("diana@x.com", "senha-segura")).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Login_correto_zera_a_contagem_de_falhas()
    {
        for (var i = 0; i < 4; i++) await Autenticar("diana@x.com", "senha-errada");

        (await Autenticar("diana@x.com", "senha-segura")).Sucesso.Should().BeTrue();

        _c.Diretor.FalhasDeLogin.Should().Be(0);
        for (var i = 0; i < 4; i++)
            (await Autenticar("diana@x.com", "senha-errada")).Mensagem.Should().Be(Invalido);
    }

    [Fact]
    public async Task Senha_correta_com_perfil_ausente_recebe_a_mensagem_generica_e_registra_a_falha()
    {
        _c.Perfis.Itens.Clear();

        var resultado = await Autenticar("diana@x.com", "senha-segura");

        resultado.Mensagem.Should().Be(Invalido);
        _c.Auditoria.Eventos.Last().Should().BeEquivalentTo(new
        {
            Evento = EventosDeSeguranca.LoginFalha, Entidade = "Usuario", EntidadeId = _c.Diretor.Id, Imediato = true,
        });
    }

    [Fact]
    public async Task Nenhum_evento_de_login_carrega_a_senha()
    {
        await Autenticar("diana@x.com", "senha-errada-secreta");
        await Autenticar("ninguem@x.com", "outra-senha-secreta");

        _c.Auditoria.Eventos.Select(e => System.Text.Json.JsonSerializer.Serialize(e.Dados))
            .Should().NotContain(d => d.Contains("secreta"));
    }

    [Fact]
    public async Task Eventos_de_um_usuario_conhecido_tem_o_usuario_como_ator()
    {
        await Autenticar("diana@x.com", "senha-errada");
        _c.Auditoria.Eventos.Last().Ator.Should().Be(_c.Diretor.Id);

        await Autenticar("diana@x.com", "senha-segura");
        _c.Auditoria.Eventos.Last().Should().BeEquivalentTo(new
        {
            Evento = EventosDeSeguranca.LoginSucesso, Ator = _c.Diretor.Id,
        });
    }

    [Fact]
    public async Task Evento_de_bloqueio_e_de_tentativa_recusada_tem_o_usuario_como_ator()
    {
        for (var i = 0; i < 5; i++) await Autenticar("diana@x.com", "senha-errada");
        _c.Auditoria.Eventos.Last().Should().BeEquivalentTo(new
        {
            Evento = EventosDeSeguranca.LoginBloqueado, Ator = _c.Diretor.Id,
        });

        await Autenticar("diana@x.com", "senha-segura");
        _c.Auditoria.Eventos.Last().Ator.Should().Be(_c.Diretor.Id);
    }

    [Fact]
    public async Task Usuario_de_cliente_ativo_autentica()
    {
        var cliente = _c.NovoCliente();
        var carla = _c.NovoUsuarioDeCliente("Carla Cliente", "carla@x.com", cliente);

        var resultado = await Autenticar("carla@x.com", "senha-segura");

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor.Should().Be(new SessaoIniciada(
            carla.Id, "Carla Cliente", TipoPerfil.Cliente, carla.VersaoDeSessao));
    }

    [Fact]
    public async Task Usuario_de_cliente_inativo_recebe_credenciais_invalidas_mesmo_com_a_senha_certa()
    {
        var cliente = _c.NovoCliente();
        var carla = _c.NovoUsuarioDeCliente("Carla Cliente", "carla@x.com", cliente);
        cliente.Inativar();

        var resultado = await Autenticar("carla@x.com", "senha-segura");

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        resultado.Mensagem.Should().Be(Invalido);
        _c.Usuarios.LoginsRegistrados.Should().Be(0);
        _c.Usuarios.Reservas.Should().Be(1, "a tentativa continua contada");
        var evento = _c.Auditoria.Eventos.Last();
        evento.Should().BeEquivalentTo(new
        {
            Evento = EventosDeSeguranca.LoginFalha, Entidade = "Usuario", EntidadeId = carla.Id, Imediato = true,
        });
        evento.Dados.Should().BeEquivalentTo(new { motivo = "cliente_inativo" });
    }

    [Fact]
    public async Task Evento_de_email_desconhecido_nao_tem_ator()
    {
        await Autenticar("ninguem@x.com", "qualquer");

        _c.Auditoria.Eventos.Last().Ator.Should().BeNull();
    }
}
