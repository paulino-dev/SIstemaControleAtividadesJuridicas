using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class IdentidadeAtualTests
{
    [Fact]
    public void Definir_guarda_usuario_e_ip()
    {
        var identidade = new IdentidadeAtual();
        var usuario = Guid.CreateVersion7();

        identidade.DefinirInterno(usuario, "10.0.0.7");

        identidade.UsuarioId.Should().Be(usuario);
        identidade.Ip.Should().Be("10.0.0.7");
    }

    [Fact]
    public void Definir_com_usuario_sem_versao_de_sessao_e_recusado()
    {
        var identidade = new IdentidadeAtual();

        var acao = () => identidade.Definir(Guid.CreateVersion7(), null, null);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Definir_sem_usuario_aceita_versao_nula_e_com_usuario_guarda_a_versao()
    {
        var identidade = new IdentidadeAtual();
        var versao = Guid.CreateVersion7();

        identidade.Definir(null, "10.0.0.7", null);
        identidade.UsuarioId.Should().BeNull();

        identidade.Definir(Guid.CreateVersion7(), "10.0.0.7", versao);
        identidade.VersaoDeSessao.Should().Be(versao);
    }

    [Fact]
    public void DefinirInterno_deixa_a_versao_de_sessao_nula()
    {
        var identidade = new IdentidadeAtual();

        identidade.DefinirInterno(Guid.CreateVersion7());

        identidade.VersaoDeSessao.Should().BeNull();
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
