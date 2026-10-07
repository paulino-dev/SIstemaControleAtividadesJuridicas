namespace AtividadesJuridicas.IntegrationTests;

public class ConteineresOrfaosTests
{
    private static readonly Func<int, bool> SoO100EstaVivo = pid => pid == 100;

    [Fact]
    public void Remove_so_os_conteineres_cujo_dono_morreu()
    {
        var saida = "aaa111 100\nbbb222 200\r\nccc333 300\n";

        BancoFixture.IdsOrfaos(saida, SoO100EstaVivo).Should().Equal("bbb222", "ccc333");
    }

    [Fact]
    public void Conteiner_de_uma_execucao_em_andamento_e_preservado()
    {
        BancoFixture.IdsOrfaos("aaa111 100\n", SoO100EstaVivo).Should().BeEmpty();
    }

    [Theory]
    [InlineData("aaa111")]
    [InlineData("aaa111 ")]
    [InlineData("aaa111 nao-e-numero")]
    public void Conteiner_sem_pid_legivel_e_orfao(string linha)
    {
        BancoFixture.IdsOrfaos(linha, SoO100EstaVivo).Should().Equal("aaa111");
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n\n")]
    public void Saida_vazia_nao_tem_orfaos(string saida)
    {
        BancoFixture.IdsOrfaos(saida, SoO100EstaVivo).Should().BeEmpty();
    }
}
