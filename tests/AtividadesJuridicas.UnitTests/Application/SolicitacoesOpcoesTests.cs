using AtividadesJuridicas.Application.Solicitacoes;

namespace AtividadesJuridicas.UnitTests.Application;

public class SolicitacoesOpcoesTests
{
    [Fact]
    public void Trecho_curto_ou_do_tamanho_exato_volta_inteiro()
    {
        SolicitacoesOpcoes.Trecho("Preciso de uma certidão.").Should().Be("Preciso de uma certidão.");

        var exata = new string('a', SolicitacoesOpcoes.TamanhoDoTrecho);
        SolicitacoesOpcoes.Trecho(exata).Should().Be(exata);
    }

    [Fact]
    public void Trecho_longo_e_cortado_nos_primeiros_140_caracteres_com_reticencias()
    {
        var longa = new string('a', SolicitacoesOpcoes.TamanhoDoTrecho) + "bcd";

        SolicitacoesOpcoes.Trecho(longa).Should().Be(new string('a', SolicitacoesOpcoes.TamanhoDoTrecho) + "…");
    }

    [Fact]
    public void Trecho_nao_parte_um_emoji_ao_meio()
    {
        // O emoji ocupa dois caracteres, o 140º e o 141º: o corte o deixa de fora inteiro.
        var descricao = new string('a', SolicitacoesOpcoes.TamanhoDoTrecho - 1) + "😀" + "fim";

        var trecho = SolicitacoesOpcoes.Trecho(descricao);

        trecho.Should().Be(new string('a', SolicitacoesOpcoes.TamanhoDoTrecho - 1) + "…");
        trecho.Any(char.IsSurrogate).Should().BeFalse();
    }
}
