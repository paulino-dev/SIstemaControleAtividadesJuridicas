using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

public class FusoDoEscritorioTests
{
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    [Fact]
    public void Formata_utc_no_horario_local_do_escritorio()
    {
        _fuso.Formatar(new DateTime(2026, 10, 6, 15, 0, 0, DateTimeKind.Utc)).Should().Be("06/10/2026 12:00");
    }

    [Fact]
    public void Logo_apos_a_meia_noite_utc_cai_no_dia_local_anterior()
    {
        _fuso.Formatar(new DateTime(2026, 10, 7, 0, 30, 0, DateTimeKind.Utc)).Should().Be("06/10/2026 21:30");
    }

    [Fact]
    public void Inicio_do_dia_local_vira_o_instante_utc_correspondente()
    {
        var fuso = new FusoDoEscritorio("America/Sao_Paulo");

        fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 7))
            .Should().Be(new DateTime(2026, 10, 7, 3, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Data_ausente_vira_travessao()
    {
        _fuso.Formatar(null).Should().Be("—");
    }
}
