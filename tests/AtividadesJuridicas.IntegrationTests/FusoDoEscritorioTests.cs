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

    // Em Havana, 2026-03-08 00:00 não existe (relógio vai de 00:00 para 01:00) e 2026-11-01 00:00 se repete.
    private readonly FusoDoEscritorio _havana = new("America/Havana");

    [Fact]
    public void Dia_cuja_meia_noite_nao_existe_comeca_na_primeira_hora_que_existe()
    {
        var inicio = () => _havana.InicioDoDiaEmUtc(new DateOnly(2026, 3, 8));

        inicio.Should().NotThrow().Which.Should().Be(new DateTime(2026, 3, 8, 5, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Dia_comum_no_mesmo_fuso_comeca_a_meia_noite_local()
    {
        _havana.InicioDoDiaEmUtc(new DateOnly(2026, 3, 7))
            .Should().Be(new DateTime(2026, 3, 7, 5, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Dia_com_lacuna_na_meia_noite_tem_vinte_e_tres_horas()
    {
        var dia = new DateOnly(2026, 3, 8);

        (_havana.InicioDoDiaEmUtc(dia.AddDays(1)) - _havana.InicioDoDiaEmUtc(dia))
            .Should().Be(TimeSpan.FromHours(23));
    }

    [Fact]
    public void Meia_noite_repetida_usa_o_instante_mais_cedo()
    {
        _havana.InicioDoDiaEmUtc(new DateOnly(2026, 11, 1))
            .Should().Be(new DateTime(2026, 11, 1, 4, 0, 0, DateTimeKind.Utc));
    }

    // Em Nova York, 2026-03-08 02:00-03:00 não existe e 2026-11-01 01:00-02:00 se repete.
    private readonly FusoDoEscritorio _novaYork = new("America/New_York");

    [Fact]
    public void ParaUtc_converte_horario_normal_em_Sao_Paulo()
    {
        var utc = _fuso.ParaUtc(new DateTime(2026, 10, 7, 9, 0, 0));

        utc.Should().Be(new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc));
        utc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ParaUtc_converte_horario_normal_em_fuso_com_horario_de_verao()
    {
        _novaYork.ParaUtc(new DateTime(2026, 7, 1, 12, 0, 0))
            .Should().Be(new DateTime(2026, 7, 1, 16, 0, 0, DateTimeKind.Utc));
        _novaYork.ParaUtc(new DateTime(2026, 1, 15, 12, 0, 0))
            .Should().Be(new DateTime(2026, 1, 15, 17, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ParaUtc_empurra_horario_inexistente_para_o_primeiro_instante_valido()
    {
        // 02:30 não existe; o primeiro instante válido é 03:00 EDT (07:00 UTC).
        var utc = _novaYork.ParaUtc(new DateTime(2026, 3, 8, 2, 30, 0));

        utc.Should().Be(new DateTime(2026, 3, 8, 7, 0, 0, DateTimeKind.Utc));
        utc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ParaUtc_vale_a_primeira_ocorrencia_de_horario_ambiguo()
    {
        // 01:30 acontece duas vezes; a primeira é em EDT (05:30 UTC), não em EST (06:30 UTC).
        _novaYork.ParaUtc(new DateTime(2026, 11, 1, 1, 30, 0))
            .Should().Be(new DateTime(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ParaUtc_aceita_hora_de_Havana_como_o_inicio_do_dia()
    {
        // Mesma política de InicioDoDiaEmUtc: a meia-noite repetida vale a primeira ocorrência.
        _havana.ParaUtc(new DateTime(2026, 11, 1, 0, 0, 0))
            .Should().Be(_havana.InicioDoDiaEmUtc(new DateOnly(2026, 11, 1)));
    }

    [Theory]
    [InlineData("America/Sao_Paulo", 2026, 10, 7, 9, 15)]
    [InlineData("America/New_York", 2026, 7, 1, 12, 0)]
    [InlineData("America/New_York", 2026, 1, 15, 23, 59)]
    public void Ida_e_volta_com_ParaLocal_devolve_o_mesmo_horario(string id, int ano, int mes, int dia, int hora, int minuto)
    {
        var fuso = new FusoDoEscritorio(id);
        var local = new DateTime(ano, mes, dia, hora, minuto, 0);

        fuso.ParaLocal(fuso.ParaUtc(local)).Should().Be(local);
    }

    [Fact]
    public void Data_ausente_vira_travessao()
    {
        _fuso.Formatar(null).Should().Be("—");
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    public void ParaUtc_recusa_data_que_nao_e_do_relogio_do_escritorio(DateTimeKind tipo)
    {
        var recusa = () => _fuso.ParaUtc(new DateTime(2026, 10, 7, 9, 0, 0, tipo));

        recusa.Should().Throw<ArgumentException>();
    }
}
