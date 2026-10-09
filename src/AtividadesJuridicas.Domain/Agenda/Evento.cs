using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Agenda;

public sealed class Evento : Entidade
{
    public const int TamanhoMaximoTitulo = 200;
    public const int TamanhoMaximoDescricao = 4000;
    public const int TamanhoMaximoLocal = 200;
    public const int DuracaoMaximaMinutos = 10080;

    /// <summary>O evento começa entre estes anos, inclusive.</summary>
    public const int AnoMinimoDeInicio = 2000;
    public const int AnoMaximoDeInicio = 2100;

    private const string Inativo = "Usuário inativo não pode ser incluído como participante.";

    private const string ClienteNoEvento = "Usuário de cliente não pode participar de evento.";

    private readonly List<EventoParticipante> _participantes = [];

    private Evento() { }

    public string Titulo { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public string? Local { get; private set; }
    public TipoEvento Tipo { get; private set; }
    public DateTime InicioEm { get; private set; }
    public int DuracaoEstimadaMinutos { get; private set; }
    public Guid? ClienteId { get; private set; }
    public Guid? ProcessoId { get; private set; }
    public Guid? AtividadeId { get; private set; }
    public Guid CriadorId { get; private set; }

    /// <summary>Versão da linha, mantida pelo banco (controle de concorrência otimista).</summary>
    [NaoAuditar]
    public uint Versao { get; private set; }

    /// <summary>
    /// Aumenta a cada mudança nos participantes. Faz essa mudança gravar também a linha do evento,
    /// acionando a verificação de concorrência.
    /// </summary>
    [NaoAuditar]
    public int VersaoDosParticipantes { get; private set; }

    public IReadOnlyCollection<EventoParticipante> Participantes => _participantes;

    public DateTime FimEm => InicioEm.AddMinutes(DuracaoEstimadaMinutos);

    /// <summary>
    /// O criador é sempre incluído entre os participantes, esteja ou não na lista informada. Ele e os demais
    /// participantes precisam estar ativos. O argumento <paramref name="criador"/> prevalece sobre a lista: se o mesmo
    /// usuário aparecer nela, vale o criador e a entrada da lista é ignorada.
    /// </summary>
    public static Evento Criar(
        DadosDoEvento dados, VinculoDoEvento vinculo, PessoaDoEvento criador,
        IReadOnlyCollection<PessoaDoEvento> participantes)
    {
        Validar(dados);
        ArgumentNullException.ThrowIfNull(vinculo);
        ArgumentNullException.ThrowIfNull(criador);
        ArgumentNullException.ThrowIfNull(participantes);
        ExigirSemNulos(participantes);
        if (criador.UsuarioId == Guid.Empty || participantes.Any(p => p.UsuarioId == Guid.Empty))
            throw new ExcecaoDeDominio("Usuário inválido.");
        ExigirSemRepeticao(participantes);
        if (!criador.Ativo || participantes.Any(p => p.UsuarioId != criador.UsuarioId && !p.Ativo))
            throw new ExcecaoDeDominio(Inativo);
        if (criador.EhCliente || participantes.Any(p => p.EhCliente))
            throw new ExcecaoDeDominio(ClienteNoEvento);

        var evento = new Evento { CriadorId = criador.UsuarioId };
        evento.Aplicar(dados, vinculo);
        evento._participantes.Add(new EventoParticipante(evento.Id, criador.UsuarioId));
        foreach (var p in participantes.Where(p => p.UsuarioId != criador.UsuarioId))
            evento._participantes.Add(new EventoParticipante(evento.Id, p.UsuarioId));
        return evento;
    }

    /// <summary>Não mexe nos participantes nem no criador.</summary>
    public void Alterar(DadosDoEvento dados, VinculoDoEvento vinculo)
    {
        Validar(dados);
        ArgumentNullException.ThrowIfNull(vinculo);
        Aplicar(dados, vinculo);
    }

    /// <summary>
    /// Altera só os dados e mantém o vínculo (cliente, processo e atividade) que o evento já tem, sem revalidá-lo: o
    /// vínculo foi válido quando gravado e pode ter deixado de ser por mudanças em outros cadastros. Não há como
    /// forjar um vínculo novo por aqui.
    /// </summary>
    public void Alterar(DadosDoEvento dados)
    {
        Validar(dados);
        AplicarDados(dados);
    }

    /// <summary>
    /// Deixa o evento com exatamente os participantes informados. Quem é novo precisa estar ativo; quem já
    /// participava pode permanecer mesmo inativo, com a entrada preservada. O criador pode ser retirado.
    /// </summary>
    public AlteracaoDeParticipantes DefinirParticipantes(IReadOnlyCollection<PessoaDoEvento> participantes)
    {
        ArgumentNullException.ThrowIfNull(participantes);
        ExigirSemNulos(participantes);

        // Tudo é verificado antes de qualquer mudança, para não deixar o evento pela metade.
        if (participantes.Any(p => p.UsuarioId == Guid.Empty))
            throw new ExcecaoDeDominio("Usuário inválido.");
        ExigirSemRepeticao(participantes);
        if (participantes.Count == 0)
            throw new ExcecaoDeDominio("O evento precisa de ao menos um participante.");
        if (participantes.Any(p => p.EhCliente))
            throw new ExcecaoDeDominio(ClienteNoEvento);

        var novos = participantes.Where(p => _participantes.All(e => e.UsuarioId != p.UsuarioId)).ToList();
        if (novos.Any(p => !p.Ativo))
            throw new ExcecaoDeDominio(Inativo);

        var ids = participantes.Select(p => p.UsuarioId).ToHashSet();
        var removidos = _participantes.Where(e => !ids.Contains(e.UsuarioId)).ToList();

        foreach (var entrada in removidos) _participantes.Remove(entrada);
        foreach (var novo in novos) _participantes.Add(new EventoParticipante(Id, novo.UsuarioId));

        var alteracao = new AlteracaoDeParticipantes(
            [.. novos.Select(p => p.UsuarioId)], [.. removidos.Select(e => e.UsuarioId)]);
        if (alteracao.Houve) VersaoDosParticipantes++;
        return alteracao;
    }

    public bool Participa(Guid usuarioId) => _participantes.Any(e => e.UsuarioId == usuarioId);

    /// <summary>Intervalos semiabertos <c>[InicioEm, FimEm)</c> e <c>[inicioUtc, fimUtc)</c>: encostados não se sobrepõem.</summary>
    public bool Sobrepoe(DateTime inicioUtc, DateTime fimUtc)
    {
        if (inicioUtc.Kind != DateTimeKind.Utc || fimUtc.Kind != DateTimeKind.Utc)
            throw new ExcecaoDeDominio("O intervalo deve estar em UTC.");
        if (fimUtc <= inicioUtc)
            throw new ExcecaoDeDominio("O fim do intervalo deve ser posterior ao início.");
        return InicioEm < fimUtc && inicioUtc < FimEm;
    }

    private static void ExigirSemNulos(IReadOnlyCollection<PessoaDoEvento> participantes)
    {
        if (participantes.Any(p => p is null))
            throw new ArgumentException("A lista de participantes não pode conter elemento nulo.", nameof(participantes));
    }

    private static void ExigirSemRepeticao(IReadOnlyCollection<PessoaDoEvento> participantes)
    {
        if (participantes.Select(p => p.UsuarioId).Distinct().Count() != participantes.Count)
            throw new ExcecaoDeDominio("Um usuário não pode se repetir entre os participantes.");
    }

    private void Aplicar(DadosDoEvento dados, VinculoDoEvento vinculo)
    {
        AplicarDados(dados);
        ClienteId = vinculo.ClienteId;
        ProcessoId = vinculo.ProcessoId;
        AtividadeId = vinculo.AtividadeId;
    }

    private void AplicarDados(DadosDoEvento dados)
    {
        Titulo = dados.Titulo.Trim();
        Descricao = Texto(dados.Descricao);
        Local = Texto(dados.Local);
        Tipo = dados.Tipo;
        InicioEm = dados.InicioEm;
        DuracaoEstimadaMinutos = dados.DuracaoEstimadaMinutos;
    }

    private static void Validar(DadosDoEvento dados)
    {
        ArgumentNullException.ThrowIfNull(dados);
        if (string.IsNullOrWhiteSpace(dados.Titulo))
            throw new ExcecaoDeDominio("O título do evento é obrigatório.");
        if (dados.Titulo.Trim().Length > TamanhoMaximoTitulo)
            throw new ExcecaoDeDominio($"O título do evento deve ter até {TamanhoMaximoTitulo} caracteres.");
        if (Texto(dados.Descricao) is { Length: > TamanhoMaximoDescricao })
            throw new ExcecaoDeDominio($"A descrição do evento deve ter até {TamanhoMaximoDescricao} caracteres.");
        if (Texto(dados.Local) is { Length: > TamanhoMaximoLocal })
            throw new ExcecaoDeDominio($"O local do evento deve ter até {TamanhoMaximoLocal} caracteres.");
        if (!Enum.IsDefined(dados.Tipo))
            throw new ExcecaoDeDominio("Tipo de evento inválido.");
        if (dados.InicioEm.Kind != DateTimeKind.Utc)
            throw new ArgumentException("O início do evento deve estar em UTC.", nameof(dados));
        if (dados.InicioEm.Year is < AnoMinimoDeInicio or > AnoMaximoDeInicio)
            throw new ExcecaoDeDominio($"O início do evento deve ser entre os anos {AnoMinimoDeInicio} e {AnoMaximoDeInicio}.");
        if (dados.DuracaoEstimadaMinutos <= 0)
            throw new ExcecaoDeDominio("A duração deve ser maior que zero.");
        if (dados.DuracaoEstimadaMinutos > DuracaoMaximaMinutos)
            throw new ExcecaoDeDominio("A duração deve ser de no máximo 7 dias.");
    }

    private static string? Texto(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
