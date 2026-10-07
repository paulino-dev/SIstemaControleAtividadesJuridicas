using AtividadesJuridicas.Infrastructure.Seguranca;

namespace AtividadesJuridicas.IntegrationTests;

public class HasherDeSenhaTests
{
    private readonly HasherDeSenhaPbkdf2 _hasher = new();

    [Fact]
    public void Hash_confere_com_a_senha_original_e_nao_a_contem()
    {
        var hash = _hasher.Gerar("senha-segura");

        hash.Should().StartWith("pbkdf2-sha256$").And.NotContain("senha-segura");
        _hasher.Verificar("senha-segura", hash).Should().BeTrue();
        _hasher.Verificar("senha-Segura", hash).Should().BeFalse();
    }

    [Fact]
    public void Mesma_senha_gera_hashes_diferentes()
    {
        _hasher.Gerar("senha-segura").Should().NotBe(_hasher.Gerar("senha-segura"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("texto-qualquer")]
    [InlineData("pbkdf2-sha256$abc$def$ghi")]
    [InlineData("pbkdf2-sha256$600000$%%%$%%%")]
    public void Hash_malformado_nao_confere_e_nao_lanca(string hash)
    {
        _hasher.Verificar("senha-segura", hash).Should().BeFalse();
    }
}
