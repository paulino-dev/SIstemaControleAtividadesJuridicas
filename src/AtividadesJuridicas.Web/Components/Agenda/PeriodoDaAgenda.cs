using System.Globalization;
using AtividadesJuridicas.Domain.Agenda;

namespace AtividadesJuridicas.Web.Components.Agenda;

/// <summary>
/// O período que a agenda mostra, lido do endereço (<c>/agenda?visao=dia|semana|mes&amp;data=yyyy-MM-dd</c>).
/// <paramref name="Visao"/> nula quer dizer "sem visao no endereço": cada largura mostra o seu padrão (Semana no
/// desktop e no tablet, Dia no mobile). <paramref name="DataNoEndereco"/> diz se a data veio do endereço (ou de uma
/// navegação), para o endereço gerado só levar o que o usuário escolheu.
/// </summary>
public sealed record PeriodoDaAgenda(VisaoDaAgenda? Visao, DateOnly Data, bool DataNoEndereco)
{
    /// <summary>
    /// Lê o endereço. Visão desconhecida conta como ausente; data inválida ou fora dos anos em que um evento pode
    /// começar (2000 a 2100) vira <paramref name="hoje"/>. Nada disso é erro.
    /// </summary>
    public static PeriodoDaAgenda Ler(string? visao, string? data, DateOnly hoje)
    {
        var lida = LerData(data);
        return new PeriodoDaAgenda(LerVisao(visao), lida ?? hoje, lida is not null);
    }

    /// <summary>A visão do desktop e do tablet: a do endereço ou, sem ela, a Semana.</summary>
    public VisaoDaAgenda NoDesktopETablet => Visao ?? VisaoDaAgenda.Semana;

    /// <summary>A visão do mobile: a do endereço ou, sem ela, o Dia.</summary>
    public VisaoDaAgenda NoMobile => Visao ?? VisaoDaAgenda.Dia;

    /// <summary>
    /// O primeiro dia consultado: a segunda-feira da semana de <see cref="Data"/> (Dia, Semana e sem visão, que mostram
    /// a mesma semana nas três larguras) ou o primeiro dia da grade do mês (Mês).
    /// </summary>
    public DateOnly InicioDaConsulta => Visao == VisaoDaAgenda.Mes ? InicioDaGradeDoMes : RotulosDeAgenda.SegundaDe(Data);

    /// <summary>O dia seguinte ao último consultado (exclusivo).</summary>
    public DateOnly FimDaConsulta => Visao == VisaoDaAgenda.Mes ? FimDaGradeDoMes : RotulosDeAgenda.SegundaDe(Data).AddDays(7);

    /// <summary>Os sete dias da semana de <see cref="Data"/>, de segunda a domingo.</summary>
    public IReadOnlyList<DateOnly> Semana => Dias(RotulosDeAgenda.SegundaDe(Data), 7);

    /// <summary>
    /// As datas da grade do mês de <see cref="Data"/>: semanas inteiras de segunda a domingo, do primeiro ao último dia
    /// do mês (4, 5 ou 6 semanas; 28 a 42 datas), qualquer que seja a visão.
    /// </summary>
    public IReadOnlyList<DateOnly> SemanasDoMes => Dias(InicioDaGradeDoMes, FimDaGradeDoMes.DayNumber - InicioDaGradeDoMes.DayNumber);

    /// <summary>O dia é do mês de <see cref="Data"/> (na grade do mês, o que não é fica apagado e sem eventos).</summary>
    public bool NoMes(DateOnly dia) => dia.Year == Data.Year && dia.Month == Data.Month;

    /// <summary>
    /// O que vai depois de <c>/agenda</c>: <c>""</c>, <c>?visao=semana</c>, <c>?data=2026-10-08</c> ou
    /// <c>?visao=dia&amp;data=2026-10-08</c>. A visão quando houver; a data quando estiver no endereço.
    /// </summary>
    public string Consulta
    {
        get
        {
            var partes = new List<string>(2);
            if (Visao is { } visao) partes.Add($"visao={NoEndereco(visao)}");
            if (DataNoEndereco) partes.Add($"data={Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
            return partes.Count == 0 ? string.Empty : "?" + string.Join("&", partes);
        }
    }

    /// <summary>O endereço da agenda neste período.</summary>
    public string Endereco => $"/agenda{Consulta}";

    /// <summary>A mesma data em outra visão (o controle Dia/Semana/Mês).</summary>
    public PeriodoDaAgenda ComVisao(VisaoDaAgenda visao) => this with { Visao = visao };

    /// <summary>Outra data, na mesma visão (a faixa de dias e a grade compacta do mobile).</summary>
    public PeriodoDaAgenda ComData(DateOnly data) => this with { Data = data, DataNoEndereco = true };

    /// <summary>
    /// O período anterior: um dia, uma semana ou um mês antes, conforme a <paramref name="unidade"/> (quem chama decide
    /// pela visão de cada largura). Um mês antes de 31/03 é o último dia de fevereiro.
    /// </summary>
    public PeriodoDaAgenda Anterior(VisaoDaAgenda unidade) => Andar(unidade, -1);

    /// <summary>O período seguinte: um dia, uma semana ou um mês depois. Um mês depois de 31/01 é o último dia de fevereiro.</summary>
    public PeriodoDaAgenda Proximo(VisaoDaAgenda unidade) => Andar(unidade, 1);

    /// <summary>O botão "Hoje": a data de hoje, na mesma visão.</summary>
    public PeriodoDaAgenda ParaHoje(DateOnly hoje) => this with { Data = hoje, DataNoEndereco = true };

    /// <summary>O endereço da ficha do evento com a origem: <c>/agenda/{id}?visao=…&amp;data=…</c>.</summary>
    public static string Ficha(Guid eventoId, string consulta) => $"/agenda/{eventoId}{consulta}";

    /// <summary>
    /// A consulta que a ficha e o formulário do evento repassam ao voltar: o que receberam no endereço, só o que
    /// <see cref="Ler"/> aceitaria (o resto some).
    /// </summary>
    public static string ConsultaDeOrigem(string? visao, string? data)
    {
        var lida = LerData(data);
        return new PeriodoDaAgenda(LerVisao(visao), lida ?? default, lida is not null).Consulta;
    }

    private DateOnly InicioDaGradeDoMes => RotulosDeAgenda.SegundaDe(new DateOnly(Data.Year, Data.Month, 1));

    private DateOnly FimDaGradeDoMes =>
        RotulosDeAgenda.SegundaDe(new DateOnly(Data.Year, Data.Month, 1).AddMonths(1).AddDays(-1)).AddDays(7);

    private PeriodoDaAgenda Andar(VisaoDaAgenda unidade, int sentido) => this with
    {
        Data = unidade switch
        {
            VisaoDaAgenda.Dia => Data.AddDays(sentido),
            VisaoDaAgenda.Semana => Data.AddDays(7 * sentido),
            // DateOnly.AddMonths leva ao último dia do mês quando o dia não existe nele (31/01 + 1 mês = 28/02).
            _ => Data.AddMonths(sentido),
        },
        DataNoEndereco = true,
    };

    private static IReadOnlyList<DateOnly> Dias(DateOnly inicio, int quantidade) =>
        Enumerable.Range(0, quantidade).Select(i => inicio.AddDays(i)).ToArray();

    private static VisaoDaAgenda? LerVisao(string? visao) => visao switch
    {
        "dia" => VisaoDaAgenda.Dia,
        "semana" => VisaoDaAgenda.Semana,
        "mes" => VisaoDaAgenda.Mes,
        _ => null,
    };

    private static string NoEndereco(VisaoDaAgenda visao) => visao switch
    {
        VisaoDaAgenda.Dia => "dia",
        VisaoDaAgenda.Semana => "semana",
        _ => "mes",
    };

    // Só "yyyy-MM-dd" com algarismos e calendário fixos, nos anos em que um evento pode começar.
    private static DateOnly? LerData(string? data) =>
        DateOnly.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lida)
        && lida.Year is >= Evento.AnoMinimoDeInicio and <= Evento.AnoMaximoDeInicio
            ? lida
            : null;
}
