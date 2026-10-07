using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class AutenticarUsuarioTests
{
    private readonly Cenario _c = new();

    private Task<Resultado<SessaoIniciada>> Autenticar(string email, string senha) =>
        new AutenticarUsuarioManipulador(_c.Usuarios, _c.Perfis, _c.Hasher, _c.Relogio)
            .ManipularAsync(new AutenticarUsuario(email, senha), default);

    [Fact]
    public async Task Credenciais_corretas_autenticam_e_registram_o_acesso()
    {
        var resultado = await Autenticar("diana@x.com", "senha-segura");

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor.Should().Be(new SessaoIniciada(
            _c.Diretor.Id, "Diana Diretora", TipoPerfil.Diretoria, _c.Diretor.VersaoDeSessao));
        _c.Diretor.UltimoAcessoEm.Should().Be(_c.Relogio.AgoraUtc);
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Email_com_maiusculas_e_espacos_encontra_o_mesmo_usuario()
    {
        (await Autenticar("  DIANA@X.com ", "senha-segura")).Sucesso.Should().BeTrue();
    }

    [Theory]
    [InlineData("diana@x.com", "senha-errada")]
    [InlineData("ninguem@x.com", "senha-segura")]
    [InlineData("nao-e-email", "senha-segura")]
    [InlineData("", "")]
    public async Task Falhas_devolvem_sempre_a_mesma_mensagem(string email, string senha)
    {
        var resultado = await Autenticar(email, senha);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        resultado.Mensagem.Should().Be("E-mail ou senha inválidos.");
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Usuario_inativo_nao_autentica_mesmo_com_a_senha_certa()
    {
        _c.Diretor.Inativar();

        var resultado = await Autenticar("diana@x.com", "senha-segura");

        resultado.Sucesso.Should().BeFalse();
        resultado.Mensagem.Should().Be("E-mail ou senha inválidos.");
    }
}
