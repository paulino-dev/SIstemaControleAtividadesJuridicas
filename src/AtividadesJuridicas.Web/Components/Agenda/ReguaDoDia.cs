using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.Web.Components.Agenda;

/// <summary>
/// Um evento posicionado na régua do Dia, em quartos de hora: <paramref name="InicioEmQuartos"/> conta a partir da
/// <see cref="ReguaDoDia.HoraInicial"/> (0 a 95) e <paramref name="Quartos"/> é a altura (2 a 96). Eventos que se
/// sobrepõem dividem a largura: o cartão ocupa a coluna <paramref name="Parte"/> de <paramref name="Partes"/> (1 a 4).
/// </summary>
public sealed record EventoNaRegua(EventoResumo Evento, int InicioEmQuartos, int Quartos, int Parte, int Partes);

/// <summary>
/// A régua de horas do Dia no desktop: das 08:00 às 19:00, estendida até a hora cheia do primeiro início e até a
/// hora cheia seguinte ao último fim do dia, com os eventos do dia posicionados por quartos de hora (a tela os põe
/// numa grade CSS por classes, sem atributo <c>style</c>).
/// </summary>
public sealed record ReguaDoDia(int HoraInicial, int HoraFinal, IReadOnlyList<EventoNaRegua> Eventos)
{
    public const int HoraInicialPadrao = 8;
    public const int HoraFinalPadrao = 19;

    /// <summary>Do quinto evento simultâneo em diante, o evento divide a última coluna com quem estiver nela.</summary>
    public const int MaximoDeColunas = 4;

    private const int QuartosPorHora = 4;
    private const int QuartosNoDia = 96;
    private const int QuartosMinimos = 2;
    private const int MinutosPorQuarto = 15;
    private const int MinutosNoDia = 24 * 60;

    /// <summary>Quantas horas a régua mostra (1 a 24): a classe <c>aj-regua__grade--horas-{n}</c>.</summary>
    public int Horas => HoraFinal - HoraInicial;

    /// <summary>
    /// Monta a régua do <paramref name="dia"/> com os eventos que o tocam (os outros são ignorados). Evento de outro dia
    /// entra recortado ao dia (de 00:00, ou até 24:00). O início arredonda para baixo e o fim para cima, com no mínimo
    /// dois quartos (o evento curto ganha altura para o cartão caber). Eventos que se sobrepõem, já arredondados, formam
    /// um grupo; cada um ocupa a primeira coluna livre do grupo, e o grupo divide a largura em até quatro colunas.
    /// </summary>
    public static ReguaDoDia Montar(FusoDoEscritorio fuso, DateOnly dia, IEnumerable<EventoResumo> eventos)
    {
        // A ordem de chegada (a do servidor: início, depois Id) desempata os que começam no mesmo quarto.
        var trechos = eventos
            .Where(evento => RotulosDeAgenda.TocaODia(fuso, evento, dia))
            .Select((evento, ordem) => Trecho.De(fuso, evento, dia, ordem))
            .OrderBy(trecho => trecho.Inicio).ThenBy(trecho => trecho.Ordem)
            .ToList();

        var horaInicial = trechos.Count == 0
            ? HoraInicialPadrao
            : Math.Min(HoraInicialPadrao, trechos.Min(trecho => trecho.Inicio) / QuartosPorHora);
        var horaFinal = trechos.Count == 0
            ? HoraFinalPadrao
            : Math.Max(HoraFinalPadrao, (trechos.Max(trecho => trecho.Fim) + QuartosPorHora - 1) / QuartosPorHora);

        var posicionados = new List<EventoNaRegua>(trechos.Count);
        var primeiro = 0;
        while (primeiro < trechos.Count)
        {
            // O grupo: o trecho e todos os seguintes que começam antes de o grupo terminar.
            var fimDoGrupo = trechos[primeiro].Fim;
            var depois = primeiro + 1;
            while (depois < trechos.Count && trechos[depois].Inicio < fimDoGrupo)
            {
                fimDoGrupo = Math.Max(fimDoGrupo, trechos[depois].Fim);
                depois++;
            }

            var grupo = trechos.GetRange(primeiro, depois - primeiro);
            var fimDaColuna = new List<int>(MaximoDeColunas);   // o fim do último trecho posto em cada coluna
            var colunas = new int[grupo.Count];
            for (var i = 0; i < grupo.Count; i++)
            {
                var livre = fimDaColuna.FindIndex(fim => fim <= grupo[i].Inicio);
                if (livre < 0 && fimDaColuna.Count < MaximoDeColunas)
                {
                    fimDaColuna.Add(0);
                    livre = fimDaColuna.Count - 1;
                }
                else if (livre < 0)
                {
                    livre = MaximoDeColunas - 1;
                }

                fimDaColuna[livre] = Math.Max(fimDaColuna[livre], grupo[i].Fim);
                colunas[i] = livre;
            }

            for (var i = 0; i < grupo.Count; i++)
                posicionados.Add(new EventoNaRegua(
                    grupo[i].Evento, grupo[i].Inicio - horaInicial * QuartosPorHora, grupo[i].Fim - grupo[i].Inicio,
                    colunas[i] + 1, fimDaColuna.Count));

            primeiro = depois;
        }

        return new ReguaDoDia(horaInicial, horaFinal, posicionados);
    }

    // O pedaço do evento dentro do dia, em quartos de hora desde a meia-noite (0 a 96), já arredondado.
    private sealed record Trecho(EventoResumo Evento, int Ordem, int Inicio, int Fim)
    {
        public static Trecho De(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia, int ordem)
        {
            var inicio = fuso.ParaLocal(evento.InicioEm);
            var fim = fuso.ParaLocal(evento.FimEm);

            // Começou noutro dia: entra da meia-noite. Termina noutro dia (ou à meia-noite em ponto): vai até 24:00.
            var minutoInicial = DateOnly.FromDateTime(inicio) < dia ? 0 : inicio.Hour * 60 + inicio.Minute;
            var minutoFinal = DateOnly.FromDateTime(fim) > dia ? MinutosNoDia : fim.Hour * 60 + fim.Minute;

            var quartoInicial = minutoInicial / MinutosPorQuarto;
            var quartoFinal = Math.Min(QuartosNoDia,
                Math.Max((minutoFinal + MinutosPorQuarto - 1) / MinutosPorQuarto, quartoInicial + QuartosMinimos));
            // O curto no fim do dia sobe para caber: 23:50–24:00 fica em 23:30–24:00.
            return new Trecho(evento, ordem, Math.Min(quartoInicial, quartoFinal - QuartosMinimos), quartoFinal);
        }
    }
}
