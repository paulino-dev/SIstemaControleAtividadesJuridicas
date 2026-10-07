using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class EntidadeTests
{
    private sealed class Exemplo : Entidade;

    private static readonly DateTime Agora = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Id_nasce_preenchido_com_guid_versao_7()
    {
        var entidade = new Exemplo();

        entidade.Id.Should().NotBe(Guid.Empty);
        entidade.Id.Version.Should().Be(7);
    }

    [Fact]
    public void Carimbar_criacao_registra_momento_e_usuario()
    {
        var entidade = new Exemplo();
        var usuario = Guid.CreateVersion7();

        entidade.CarimbarCriacao(Agora, usuario);

        entidade.CriadoEm.Should().Be(Agora);
        entidade.CriadoPor.Should().Be(usuario);
        entidade.AtualizadoEm.Should().BeNull();
    }

    [Fact]
    public void Carimbar_atualizacao_nao_altera_a_criacao()
    {
        var entidade = new Exemplo();
        entidade.CarimbarCriacao(Agora, null);

        entidade.CarimbarAtualizacao(Agora.AddHours(1), Guid.CreateVersion7());

        entidade.CriadoEm.Should().Be(Agora);
        entidade.AtualizadoEm.Should().Be(Agora.AddHours(1));
        entidade.AtualizadoPor.Should().NotBeNull();
    }

    [Fact]
    public void Marcar_excluido_e_idempotente()
    {
        var entidade = new Exemplo();
        var primeiro = Guid.CreateVersion7();

        entidade.MarcarExcluido(Agora, primeiro);
        entidade.MarcarExcluido(Agora.AddDays(1), Guid.CreateVersion7());

        entidade.Excluido.Should().BeTrue();
        entidade.ExcluidoEm.Should().Be(Agora);
        entidade.ExcluidoPor.Should().Be(primeiro);
    }

    [Fact]
    public void Carimbos_recusam_data_que_nao_seja_utc()
    {
        var entidade = new Exemplo();
        var local = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Local);

        var acao = () => entidade.CarimbarCriacao(local, null);

        acao.Should().Throw<ArgumentException>();
    }
}
