using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class SenhaTests
{
    private readonly Cenario _c = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("        ")]
    public void Politica_recusa_senha_curta_ou_em_branco(string? senha)
    {
        PoliticaDeSenha.Validar(senha).Should().NotBeNull();
    }

    [Fact]
    public void Politica_recusa_senha_maior_que_128_caracteres()
    {
        PoliticaDeSenha.Validar(new string('a', 129)).Should().NotBeNull();
    }

    [Fact]
    public void Politica_aceita_senha_de_8_a_128_caracteres()
    {
        PoliticaDeSenha.Validar("12345678").Should().BeNull();
        PoliticaDeSenha.Validar(new string('a', 128)).Should().BeNull();
    }

    [Fact]
    public async Task Diretor_redefine_a_senha_de_outro_usuario()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await new RedefinirSenhaUsuarioManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Hasher, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new RedefinirSenhaUsuario(alvo.Id, "nova-senha-1"), default);

        resultado.Sucesso.Should().BeTrue();
        alvo.SenhaHash.Should().Be("hash:nova-senha-1");
    }

    [Fact]
    public async Task Producao_nao_redefine_senha_de_ninguem()
    {
        var producao = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await new RedefinirSenhaUsuarioManipulador(_c.Como(producao), _c.Usuarios, _c.Hasher, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new RedefinirSenhaUsuario(_c.Diretor.Id, "nova-senha-1"), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Diretor.SenhaHash.Should().Be("hash:senha-segura");
    }

    [Fact]
    public async Task Usuario_altera_a_propria_senha_informando_a_atual()
    {
        var producao = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await new AlterarMinhaSenhaManipulador(_c.Como(producao), _c.Usuarios, _c.Hasher, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new AlterarMinhaSenha("senha-segura", "nova-senha-1"), default);

        resultado.Sucesso.Should().BeTrue();
        producao.SenhaHash.Should().Be("hash:nova-senha-1");
    }

    [Fact]
    public async Task Senha_atual_errada_nao_altera_nada()
    {
        var resultado = await new AlterarMinhaSenhaManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Hasher, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new AlterarMinhaSenha("errada", "nova-senha-1"), default);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        _c.Diretor.SenhaHash.Should().Be("hash:senha-segura");
    }

    [Fact]
    public async Task Anonimo_nao_altera_senha()
    {
        var resultado = await new AlterarMinhaSenhaManipulador(_c.Como(null), _c.Usuarios, _c.Hasher, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new AlterarMinhaSenha("senha-segura", "nova-senha-1"), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Redefinir_a_senha_remove_o_bloqueio_de_login()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        for (var i = 0; i < 5; i++) alvo.RegistrarFalhaDeLogin(_c.Relogio.AgoraUtc);

        await new RedefinirSenhaUsuarioManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Hasher, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new RedefinirSenhaUsuario(alvo.Id, "nova-senha-1"), default);

        alvo.EstaBloqueado(_c.Relogio.AgoraUtc).Should().BeFalse();
    }
}
