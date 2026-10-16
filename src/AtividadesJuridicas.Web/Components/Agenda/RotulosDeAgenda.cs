using System.Globalization;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.Web.Components.Agenda;

/// <summary>Os textos em português e o cálculo de dia que a agenda mostra.</summary>
public static class RotulosDeAgenda
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    // De segunda a domingo, a ordem da semana da agenda. Escritos aqui, e não pela cultura, para a maiúscula e o
    // "-feira" saírem como nos quadros.
    private static readonly string[] Siglas = ["Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom"];
    private static readonly string[] Nomes = ["Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado", "Domingo"];
    private static readonly string[] NomesLongos =
        ["Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado", "Domingo"];
    private static readonly string[] Meses =
        ["Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"];

    public static string Tipo(TipoEvento tipo) => tipo switch
    {
        TipoEvento.Audiencia => "Audiência",
        TipoEvento.Reuniao => "Reunião",
        TipoEvento.Prazo => "Prazo",
        TipoEvento.Outro => "Outro",
        _ => tipo.ToString(),
    };

    public const string AtividadeSemAcesso = "Atividade vinculada (sem acesso)";
    public const string AtividadeExcluida = "Atividade vinculada (excluída)";

    /// <summary>O nome de quem não é mais encontrado (nos participantes dos cartões).</summary>
    public const string UsuarioNaoEncontrado = "Usuário não encontrado";

    /// <summary>O texto da atividade vinculada: o título, ou o motivo de não haver título para mostrar.</summary>
    public static string Atividade(string? titulo, bool excluida) =>
        titulo ?? (excluida ? AtividadeExcluida : AtividadeSemAcesso);

    /// <summary>A segunda-feira da semana do dia (a semana vai de segunda a domingo).</summary>
    public static DateOnly SegundaDe(DateOnly dia) => dia.AddDays(-PosicaoNaSemana(dia));

    /// <summary>"dd/MM/yyyy" com algarismos e calendário fixos, qualquer que seja a cultura da conexão.</summary>
    public static string Data(DateOnly dia) => dia.ToString("dd/MM/yyyy", Cultura);

    /// <summary>"segunda-feira, 12/10/2026".</summary>
    public static string Dia(DateOnly dia) => dia.ToString("dddd, dd/MM/yyyy", Cultura);

    /// <summary>
    /// O evento aparece no dia se o intervalo <c>[início, fim)</c>, em horário do escritório, toca esse dia. O dia do
    /// fim é o do último instante do evento: quem termina à meia-noite em ponto não aparece no dia seguinte.
    /// </summary>
    public static bool TocaODia(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia)
    {
        var primeiro = DateOnly.FromDateTime(fuso.ParaLocal(evento.InicioEm));
        var ultimo = DateOnly.FromDateTime(fuso.ParaLocal(evento.FimEm.AddTicks(-1)));
        return primeiro <= dia && dia <= ultimo;
    }

    /// <summary>
    /// "HH:mm – HH:mm" no horário do escritório; o lado que cai em outro dia que não o listado leva a data
    /// ("23:30 – 08/10 00:30").
    /// </summary>
    public static string Intervalo(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia) =>
        IntervaloNoDia(fuso, evento.InicioEm, evento.FimEm, dia);

    // ---- Dias, semanas e meses ----

    /// <summary>"Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom".</summary>
    public static string SiglaDoDia(DateOnly dia) => Siglas[PosicaoNaSemana(dia)];

    /// <summary>"Segunda", "Terça"… "Domingo" (a faixa de dias e o título do dia do mobile).</summary>
    public static string NomeDoDia(DateOnly dia) => Nomes[PosicaoNaSemana(dia)];

    /// <summary>"Segunda-feira"… "Sábado", "Domingo" (o subtítulo e o cabeçalho do Dia).</summary>
    public static string NomeLongoDoDia(DateOnly dia) => NomesLongos[PosicaoNaSemana(dia)];

    /// <summary>O nome acessível do dia no desktop e no tablet: "quinta-feira, 08/10/2026, hoje".</summary>
    public static string RotuloLongo(DateOnly dia, bool hoje) => hoje ? $"{Dia(dia)}, hoje" : Dia(dia);

    /// <summary>O nome acessível do dia na faixa do mobile: "Quinta, 08/10, hoje".</summary>
    public static string RotuloCurto(DateOnly dia, bool hoje)
    {
        var rotulo = $"{NomeDoDia(dia)}, {dia.ToString("dd/MM", Cultura)}";
        return hoje ? $"{rotulo}, hoje" : rotulo;
    }

    /// <summary>O título do cabeçalho do Dia no desktop: "Quinta-feira" ou "Quinta-feira · hoje".</summary>
    public static string TituloDoDiaNoDesktop(DateOnly dia, bool hoje) =>
        hoje ? $"{NomeLongoDoDia(dia)} · hoje" : NomeLongoDoDia(dia);

    /// <summary>O título do dia escolhido no mobile (Dia e Mês): "Quinta, 08/10" ou "Quinta, 08/10 · hoje".</summary>
    public static string TituloDoDiaNoMobile(DateOnly dia, bool hoje)
    {
        var titulo = $"{NomeDoDia(dia)}, {dia.ToString("dd/MM", Cultura)}";
        return hoje ? $"{titulo} · hoje" : titulo;
    }

    /// <summary>
    /// "Semana de 05/10 a 11/10/2026" a partir da segunda-feira; com a semana entre dois anos, os dois anos:
    /// "Semana de 28/12/2026 a 03/01/2027".
    /// </summary>
    public static string SubtituloDaSemana(DateOnly segunda)
    {
        var domingo = segunda.AddDays(6);
        var de = segunda.Year == domingo.Year ? segunda.ToString("dd/MM", Cultura) : Data(segunda);
        return $"Semana de {de} a {Data(domingo)}";
    }

    /// <summary>"Quinta-feira, 08/10/2026".</summary>
    public static string SubtituloDoDia(DateOnly dia) => $"{NomeLongoDoDia(dia)}, {Data(dia)}";

    /// <summary>"Outubro de 2026".</summary>
    public static string SubtituloDoMes(DateOnly dia) => $"{Meses[dia.Month - 1]} de {dia.ToString("yyyy", Cultura)}";

    /// <summary>A linha do mês da barra do mobile: "Outubro 2026".</summary>
    public static string MesDoMobile(DateOnly dia) => $"{Meses[dia.Month - 1]} {dia.ToString("yyyy", Cultura)}";

    /// <summary>O número do dia com dois algarismos: "08", "31".</summary>
    public static string NumeroDoDia(DateOnly dia) => dia.ToString("dd", Cultura);

    /// <summary>Os eventos que tocam o <paramref name="dia"/> (pela regra de <see cref="TocaODia"/>), na ordem recebida.</summary>
    public static IReadOnlyList<EventoResumo> EventosDoDia(FusoDoEscritorio fuso, IEnumerable<EventoResumo> eventos, DateOnly dia) =>
        eventos.Where(evento => TocaODia(fuso, evento, dia)).ToArray();

    /// <summary>"0 eventos", "1 evento", "2 eventos".</summary>
    public static string TotalDeEventos(int quantidade) => Contagem.De(quantidade, "evento", "eventos");

    /// <summary>
    /// O nome acessível do "+N" da célula do Mês, com os eventos que não couberam: "Mais 2 eventos em 08/10",
    /// "Mais 1 evento em 09/10".
    /// </summary>
    public static string RotuloDoMais(DateOnly dia, int outros) =>
        $"Mais {TotalDeEventos(outros)} em {dia.ToString("dd/MM", Cultura)}";

    // ---- Cartões ----

    /// <summary>A hora de início na coluna de hora do cartão ("09:00"), ou "…" quando o evento começou em outro dia.</summary>
    public static string HoraDeInicio(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia)
    {
        var inicio = fuso.ParaLocal(evento.InicioEm);
        return DateOnly.FromDateTime(inicio) == dia ? inicio.ToString("HH:mm", Cultura) : "…";
    }

    /// <summary>
    /// A hora do fim na coluna de hora do cartão ("10:00"), ou "…" quando o evento termina em outro dia. Terminar à
    /// meia-noite em ponto é terminar no mesmo dia ("00:00").
    /// </summary>
    public static string HoraDeFim(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia)
    {
        var diaDoFim = DateOnly.FromDateTime(fuso.ParaLocal(evento.FimEm.AddTicks(-1)));
        return diaDoFim == dia ? fuso.ParaLocal(evento.FimEm).ToString("HH:mm", Cultura) : "…";
    }

    /// <summary>
    /// Os participantes separados por vírgula, na ordem do servidor; o usuário que não é mais encontrado (o servidor
    /// manda o nome vazio) aparece como "Usuário não encontrado". Nulo sem participantes.
    /// </summary>
    public static string? Participantes(EventoResumo evento) =>
        evento.Participantes.Count == 0
            ? null
            : string.Join(", ", evento.Participantes.Select(nome => string.IsNullOrWhiteSpace(nome) ? UsuarioNaoEncontrado : nome));

    /// <summary>A linha de apoio do cartão do tablet: o local ou, sem local, o cliente. Nula sem os dois.</summary>
    public static string? LinhaDoTablet(EventoResumo evento) => Preenchido(evento.Local) ?? Preenchido(evento.ClienteNome);

    /// <summary>A primeira linha de apoio do cartão do mobile: "local · cliente", o que houver. Nula sem os dois.</summary>
    public static string? LinhaDoMobile(EventoResumo evento) => Juntar(" · ", [evento.Local, evento.ClienteNome]);

    /// <summary>A linha de apoio do cartão da régua: "local · cliente · participantes", o que houver.</summary>
    public static string? LinhaDaRegua(EventoResumo evento) =>
        Juntar(" · ", [evento.Local, evento.ClienteNome, Participantes(evento)]);

    // ---- Ficha ----

    /// <summary>"90 minutos", "1 minuto".</summary>
    public static string Duracao(int minutos) => Contagem.De(minutos, "minuto", "minutos");

    /// <summary>O cliente vinculado que o usuário não pode ver (o servidor manda o nome nulo), na ficha e no formulário.</summary>
    public const string ClienteIndisponivel = "Cliente indisponível";

    /// <summary>O processo vinculado que o usuário não pode ver (o servidor manda o título nulo), na ficha e no formulário.</summary>
    public const string ProcessoIndisponivel = "Processo indisponível";

    /// <summary>
    /// A linha de contexto da ficha: "Audiência · 07/10/2026, 09:00 – 10:30 · Fórum Trabalhista, sala 4". Sem local (ou
    /// com <paramref name="comLocal"/> falso, o resumo do mobile), sem a última parte. O fim em outro dia leva a data
    /// ("23:30 – 08/10 00:30").
    /// </summary>
    public static string ResumoDoEvento(FusoDoEscritorio fuso, EventoDetalhe evento, bool comLocal)
    {
        var dia = DateOnly.FromDateTime(fuso.ParaLocal(evento.InicioEm));
        var resumo = $"{Tipo(evento.Tipo)} · {Data(dia)}, {IntervaloNoDia(fuso, evento.InicioEm, evento.FimEm, dia)}";
        return comLocal && Preenchido(evento.Local) is { } local ? $"{resumo} · {local}" : resumo;
    }

    // ---- Sobreposições ----

    /// <summary>
    /// O aviso de conflito do desktop, depois do "Evento salvo." em destaque: "Atenção: Rafael Teles já tem evento das
    /// 10:00 às 11:30 (07/10)." Várias pessoas, separadas por "; ". Só com ao menos uma sobreposição.
    /// </summary>
    public static string ConflitoNoDesktop(FusoDoEscritorio fuso, IReadOnlyList<SobreposicaoDeAgenda> sobreposicoes) =>
        $"Atenção: {Frases(fuso, sobreposicoes, noMobile: false)}.";

    /// <summary>O do tablet, sem "Atenção:": "Rafael Teles já tem evento das 10:00 às 11:30 (07/10)."</summary>
    public static string ConflitoNoTablet(FusoDoEscritorio fuso, IReadOnlyList<SobreposicaoDeAgenda> sobreposicoes) =>
        $"{Frases(fuso, sobreposicoes, noMobile: false)}.";

    /// <summary>
    /// O do mobile, sem "Evento salvo." e com "em dd/MM": "Rafael Teles já tem evento das 10:00 às 11:30 em 07/10."
    /// A sobreposição que atravessa a meia-noite tem a mesma forma nas três visões.
    /// </summary>
    public static string ConflitoNoMobile(FusoDoEscritorio fuso, IReadOnlyList<SobreposicaoDeAgenda> sobreposicoes) =>
        $"{Frases(fuso, sobreposicoes, noMobile: true)}.";

    private static int PosicaoNaSemana(DateOnly dia) => ((int)dia.DayOfWeek + 6) % 7;

    private static string IntervaloNoDia(FusoDoEscritorio fuso, DateTime inicioUtc, DateTime fimUtc, DateOnly dia)
    {
        var inicio = fuso.ParaLocal(inicioUtc);
        var fim = fuso.ParaLocal(fimUtc);
        var diaDoFim = DateOnly.FromDateTime(fuso.ParaLocal(fimUtc.AddTicks(-1)));

        var de = DateOnly.FromDateTime(inicio) == dia ? inicio.ToString("HH:mm", Cultura) : inicio.ToString("dd/MM HH:mm", Cultura);
        var ate = diaDoFim == dia ? fim.ToString("HH:mm", Cultura) : fim.ToString("dd/MM HH:mm", Cultura);
        return $"{de} – {ate}";
    }

    private static string Frases(FusoDoEscritorio fuso, IReadOnlyList<SobreposicaoDeAgenda> sobreposicoes, bool noMobile) =>
        string.Join("; ", sobreposicoes.Select(s => Frase(fuso, s, noMobile)));

    // "Fulano já tem evento das 10:00 às 11:30" e a data; se o intervalo atravessa a meia-noite, as duas datas aparecem:
    // "Fulano já tem evento de 10/03 22:00 a 11/03 01:00". Quem termina à meia-noite em ponto não atravessa.
    private static string Frase(FusoDoEscritorio fuso, SobreposicaoDeAgenda s, bool noMobile)
    {
        var nome = s.UsuarioNome ?? "Usuário";
        var inicio = fuso.ParaLocal(s.InicioEm);
        var fim = fuso.ParaLocal(s.FimEm);
        var diaDoFim = DateOnly.FromDateTime(fuso.ParaLocal(s.FimEm.AddTicks(-1)));

        if (DateOnly.FromDateTime(inicio) != diaDoFim)
            return $"{nome} já tem evento de {inicio.ToString("dd/MM HH:mm", Cultura)} a {fim.ToString("dd/MM HH:mm", Cultura)}";

        var horario = $"{nome} já tem evento das {inicio.ToString("HH:mm", Cultura)} às {fim.ToString("HH:mm", Cultura)}";
        var data = inicio.ToString("dd/MM", Cultura);
        return noMobile ? $"{horario} em {data}" : $"{horario} ({data})";
    }

    private static string? Preenchido(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto;

    private static string? Juntar(string separador, IEnumerable<string?> partes)
    {
        var preenchidas = partes.Where(parte => !string.IsNullOrWhiteSpace(parte)).ToList();
        return preenchidas.Count == 0 ? null : string.Join(separador, preenchidas);
    }
}
