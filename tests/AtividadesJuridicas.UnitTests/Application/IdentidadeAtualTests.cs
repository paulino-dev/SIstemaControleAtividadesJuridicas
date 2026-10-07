using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class IdentidadeAtualTests
{
    [Fact]
    public void Definir_guarda_usuario_e_ip()
    {
        var identidade = new IdentidadeAtual();
        var usuario = Guid.CreateVersion7();

        identidade.Definir(usuario, "10.0.0.7");

        identidade.UsuarioId.Should().Be(usuario);
        identidade.Ip.Should().Be("10.0.0.7");
    }

    [Fact]
    public void Cada_instancia_tem_um_id_de_correlacao_proprio()
    {
        var a = new IdentidadeAtual();
        var b = new IdentidadeAtual();

        a.IdCorrelacao.Should().NotBe(Guid.Empty);
        a.IdCorrelacao.Should().NotBe(b.IdCorrelacao);
    }
}
