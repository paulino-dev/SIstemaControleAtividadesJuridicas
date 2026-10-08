using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Atividades;

public sealed class Atividade : Entidade
{
    public const int TamanhoMaximoTitulo = 200;
    public const int TamanhoMaximoDescricao = 10000;

    public const string SoORevisor = "Somente o revisor da atividade pode aprová-la ou devolvê-la.";
    public const string RevisorTravado =
        "O revisor desta atividade é o seu único responsável de Diretoria e não pode ser outro.";

    private const string SoAlteraResponsaveis =
        "Os responsáveis só podem ser alterados enquanto a atividade está criada, direcionada, em correção ou reaberta.";
    private const string SoAguardandoRevisao =
        "A atividade só pode ser aprovada ou devolvida quando está feita ou corrigida.";

    private readonly List<AtividadeResponsavel> _responsaveis = [];
    private readonly List<AtividadeHistoricoStatus> _historico = [];

    private Atividade() { }

    public string Titulo { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;

    /// <summary>Obrigatória a partir de Direcionada.</summary>
    public DateOnly? DataEntrega { get; private set; }

    public StatusAtividade Status { get; private set; }

    /// <summary>Preenchido ao entrar em Concluida; limpo ao reabrir.</summary>
    public DateTime? ConcluidaEm { get; private set; }

    public Guid? ClienteId { get; private set; }
    public Guid? ProcessoId { get; private set; }
    public Guid? AreaAtuacaoId { get; private set; }
    public TipoAutor TipoAutor { get; private set; }
    public Guid? AutorId { get; private set; }
    public Guid? RevisorId { get; private set; }

    /// <summary>Versão da linha, mantida pelo banco (controle de concorrência otimista).</summary>
    [NaoAuditar]
    public uint Versao { get; private set; }

    /// <summary>
    /// Aumenta a cada mudança nos responsáveis ou nas partes. Faz essa mudança gravar também a linha da
    /// atividade, acionando a verificação de concorrência: duas conclusões simultâneas não passam as duas.
    /// </summary>
    [NaoAuditar]
    public int VersaoDasPartes { get; private set; }

    public IReadOnlyCollection<AtividadeResponsavel> Responsaveis => _responsaveis;
    public IReadOnlyCollection<AtividadeHistoricoStatus> Historico => _historico;

    public bool PodeAlterarResponsaveis => Status is
        StatusAtividade.Criada or StatusAtividade.Direcionada or StatusAtividade.ParaCorrecao or StatusAtividade.Reaberta;

    public bool PodeTrocarRevisor => Status != StatusAtividade.Concluida;

    private bool AguardaPartes => Status is
        StatusAtividade.Direcionada or StatusAtividade.ParaCorrecao or StatusAtividade.Reaberta;

    private bool AguardaRevisao => Status is StatusAtividade.Feita or StatusAtividade.Corrigida;

    public static Atividade Criar(
        DadosDaAtividade dados, VinculoDaAtividade vinculo, TipoAutor tipoAutor, Guid? autorId, DateTime agoraUtc)
    {
        Validar(dados, StatusAtividade.Criada);
        ArgumentNullException.ThrowIfNull(vinculo);
        if (!Enum.IsDefined(tipoAutor))
            throw new ExcecaoDeDominio("Tipo de autor inválido.");
        if (tipoAutor == TipoAutor.Usuario && (autorId is null || autorId == Guid.Empty))
            throw new ExcecaoDeDominio("O autor da atividade é obrigatório.");
        if (tipoAutor == TipoAutor.Sistema && autorId is not null)
            throw new ExcecaoDeDominio("Atividade criada pelo sistema não tem autor.");

        var atividade = new Atividade { TipoAutor = tipoAutor, AutorId = autorId, Status = StatusAtividade.Criada };
        atividade.Aplicar(dados, vinculo);
        atividade._historico.Add(new AtividadeHistoricoStatus(
            atividade.Id, 1, null, StatusAtividade.Criada, autorId, agoraUtc, null));
        return atividade;
    }

    public void Alterar(DadosDaAtividade dados, VinculoDaAtividade vinculo)
    {
        Validar(dados, Status);
        ArgumentNullException.ThrowIfNull(vinculo);
        Aplicar(dados, vinculo);
    }

    /// <summary>
    /// Deixa a atividade com exatamente os responsáveis informados e reavalia o revisor. A entrada de quem
    /// permanece é preservada (com a conclusão da sua parte). Em atividade criada, o primeiro responsável a
    /// direciona; nos demais status, se todos os que ficaram já concluíram, a atividade avança.
    /// </summary>
    public AlteracaoDeResponsaveis DefinirResponsaveis(
        IReadOnlyCollection<ResponsavelInformado> responsaveis, PessoaDaAtividade? revisor, DateTime agoraUtc, Guid? atorId)
    {
        ArgumentNullException.ThrowIfNull(responsaveis);
        if (responsaveis.Any(r => r is null || r.Pessoa is null))
            throw new ArgumentException("A lista de responsáveis não pode conter elemento nulo.", nameof(responsaveis));

        // Tudo é verificado antes de qualquer mudança, para não deixar a atividade pela metade.
        if (!PodeAlterarResponsaveis)
            throw new ExcecaoDeDominio(SoAlteraResponsaveis);
        if (responsaveis.Select(r => r.Pessoa.UsuarioId).Distinct().Count() != responsaveis.Count)
            throw new ExcecaoDeDominio("Um usuário não pode se repetir entre os responsáveis.");
        if (responsaveis.Any(r => Texto(r.Direcionamento) is { Length: > AtividadeResponsavel.TamanhoMaximoDirecionamento }))
            throw new ExcecaoDeDominio(
                $"O direcionamento deve ter até {AtividadeResponsavel.TamanhoMaximoDirecionamento} caracteres.");

        var novos = responsaveis.Where(r => _responsaveis.All(e => e.UsuarioId != r.Pessoa.UsuarioId)).ToList();
        if (novos.Any(r => !r.Pessoa.Ativo))
            throw new ExcecaoDeDominio("Usuário inativo não pode ser incluído como responsável.");
        if (Status != StatusAtividade.Criada && responsaveis.Count == 0)
            throw new ExcecaoDeDominio("A atividade precisa de ao menos um responsável.");

        var revisorId = ResolverRevisor([.. responsaveis.Select(r => r.Pessoa)], revisor);
        if (responsaveis.Count > 0)
        {
            if (revisorId is null)
                throw new ExcecaoDeDominio("Informe o revisor da atividade.");
            if (DataEntrega is null)
                throw new ExcecaoDeDominio("Informe a data de entrega antes de direcionar a atividade.");
        }

        var ids = responsaveis.Select(r => r.Pessoa.UsuarioId).ToHashSet();
        var removidos = _responsaveis.Where(e => !ids.Contains(e.UsuarioId)).ToList();
        var direcionamentoAlterado = new List<Guid>();

        foreach (var entrada in removidos) _responsaveis.Remove(entrada);
        foreach (var informado in responsaveis)
        {
            var direcionamento = Texto(informado.Direcionamento);
            var existente = _responsaveis.FirstOrDefault(e => e.UsuarioId == informado.Pessoa.UsuarioId);
            if (existente is null)
            {
                _responsaveis.Add(new AtividadeResponsavel(Id, informado.Pessoa.UsuarioId, direcionamento));
            }
            else if (existente.Direcionamento != direcionamento)
            {
                existente.AlterarDirecionamento(direcionamento);
                direcionamentoAlterado.Add(existente.UsuarioId);
            }
        }

        RevisorId = revisorId;

        var alteracao = new AlteracaoDeResponsaveis(
            [.. novos.Select(r => r.Pessoa.UsuarioId)], [.. removidos.Select(e => e.UsuarioId)], direcionamentoAlterado);
        if (alteracao.Houve) VersaoDasPartes++;

        if (Status == StatusAtividade.Criada && _responsaveis.Count > 0)
            Transicionar(StatusAtividade.Direcionada, atorId, agoraUtc, null);
        else
            AvancarSeTodosConcluiram(atorId, agoraUtc);

        return alteracao;
    }

    /// <summary><paramref name="responsaveisAtuais"/>: os responsáveis de agora, para reavaliar o travamento.</summary>
    public void TrocarRevisor(PessoaDaAtividade revisor, IReadOnlyCollection<PessoaDaAtividade> responsaveisAtuais)
    {
        ArgumentNullException.ThrowIfNull(revisor);
        ArgumentNullException.ThrowIfNull(responsaveisAtuais);
        if (!responsaveisAtuais.Select(p => p.UsuarioId).ToHashSet().SetEquals(_responsaveis.Select(e => e.UsuarioId)))
            throw new ArgumentException("A lista informada não é a dos responsáveis atuais.", nameof(responsaveisAtuais));
        if (!PodeTrocarRevisor)
            throw new ExcecaoDeDominio("O revisor não pode ser trocado depois que a atividade foi concluída.");

        RevisorId = ResolverRevisor(responsaveisAtuais, revisor);
    }

    public void ConcluirParte(Guid usuarioId, DateTime agoraUtc)
    {
        if (!AguardaPartes)
            throw new ExcecaoDeDominio(
                "A parte só pode ser concluída enquanto a atividade está direcionada, em correção ou reaberta.");

        var parte = _responsaveis.FirstOrDefault(e => e.UsuarioId == usuarioId)
            ?? throw new ExcecaoDeDominio("Você não é responsável por esta atividade.");
        if (!parte.Pendente)
            throw new ExcecaoDeDominio("Sua parte já está concluída.");

        parte.Concluir(agoraUtc);
        VersaoDasPartes++;
        AvancarSeTodosConcluiram(usuarioId, agoraUtc);
    }

    public void Aprovar(Guid revisorId, DateTime agoraUtc)
    {
        ExigirRevisao(revisorId);
        Transicionar(StatusAtividade.Concluida, revisorId, agoraUtc, null);
    }

    /// <summary>Devolve para correção: só as partes dos responsáveis selecionados voltam a ficar pendentes.</summary>
    public void Devolver(Guid revisorId, string motivo, IReadOnlyCollection<Guid> responsaveis, DateTime agoraUtc)
    {
        ExigirRevisao(revisorId);
        var motivoValido = MotivoValido(motivo);
        ReabrirPartes(responsaveis);
        Transicionar(StatusAtividade.ParaCorrecao, revisorId, agoraUtc, motivoValido);
    }

    public void Reabrir(string motivo, IReadOnlyCollection<Guid> responsaveis, DateTime agoraUtc, Guid? atorId)
    {
        if (Status != StatusAtividade.Concluida)
            throw new ExcecaoDeDominio("Só uma atividade concluída pode ser reaberta.");

        var motivoValido = MotivoValido(motivo);
        ReabrirPartes(responsaveis);
        Transicionar(StatusAtividade.Reaberta, atorId, agoraUtc, motivoValido);
    }

    /// <summary>"Atrasada" não é status: é a data de entrega anterior a hoje em atividade não concluída.</summary>
    public bool EstaAtrasada(DateOnly hoje) => Status != StatusAtividade.Concluida && DataEntrega < hoje;

    public bool EhResponsavel(Guid usuarioId) => _responsaveis.Any(e => e.UsuarioId == usuarioId);

    // Exatamente um responsável ativo de Diretoria: ele é o revisor e não pode ser outro. Caso contrário,
    // vale o informado (que precisa ser diretor ativo) ou, sem informado, o revisor atual é mantido.
    private Guid? ResolverRevisor(IReadOnlyCollection<PessoaDaAtividade> responsaveis, PessoaDaAtividade? informado)
    {
        var diretores = responsaveis.Where(p => p.EhDiretoriaAtiva).ToList();
        if (diretores.Count == 1)
        {
            var travado = diretores[0].UsuarioId;
            if (informado is not null && informado.UsuarioId != travado)
                throw new ExcecaoDeDominio(RevisorTravado);
            return travado;
        }

        if (informado is null || informado.UsuarioId == RevisorId) return RevisorId;
        if (!informado.EhDiretoriaAtiva)
            throw new ExcecaoDeDominio("O revisor deve ser um usuário ativo de Diretoria.");
        return informado.UsuarioId;
    }

    private void ExigirRevisao(Guid revisorId)
    {
        if (!AguardaRevisao)
            throw new ExcecaoDeDominio(SoAguardandoRevisao);
        if (RevisorId != revisorId)
            throw new ExcecaoDeDominio(SoORevisor);
    }

    // Valida tudo antes de reabrir a primeira parte.
    private void ReabrirPartes(IReadOnlyCollection<Guid> responsaveis)
    {
        ArgumentNullException.ThrowIfNull(responsaveis);
        var selecionados = responsaveis.Distinct().ToList();
        if (selecionados.Count == 0)
            throw new ExcecaoDeDominio("Selecione ao menos um responsável.");
        if (selecionados.Any(id => _responsaveis.All(e => e.UsuarioId != id)))
            throw new ExcecaoDeDominio("Responsável selecionado não faz parte da atividade.");

        foreach (var parte in _responsaveis.Where(e => selecionados.Contains(e.UsuarioId))) parte.Reabrir();
        VersaoDasPartes++;
    }

    private void AvancarSeTodosConcluiram(Guid? usuarioId, DateTime agoraUtc)
    {
        if (!AguardaPartes || _responsaveis.Count == 0 || _responsaveis.Any(e => e.Pendente)) return;

        Transicionar(
            Status == StatusAtividade.ParaCorrecao ? StatusAtividade.Corrigida : StatusAtividade.Feita,
            usuarioId, agoraUtc, null);

        // O revisor é o único responsável: não há a quem submeter, a atividade se conclui.
        if (_responsaveis.Count == 1 && _responsaveis[0].UsuarioId == RevisorId)
            Transicionar(StatusAtividade.Concluida, usuarioId, agoraUtc, null);
    }

    private void Transicionar(StatusAtividade novo, Guid? usuarioId, DateTime agoraUtc, string? motivo)
    {
        _historico.Add(new AtividadeHistoricoStatus(Id, _historico.Count + 1, Status, novo, usuarioId, agoraUtc, motivo));
        Status = novo;
        ConcluidaEm = novo == StatusAtividade.Concluida ? agoraUtc : null;
    }

    private void Aplicar(DadosDaAtividade dados, VinculoDaAtividade vinculo)
    {
        Titulo = dados.Titulo.Trim();
        Descricao = dados.Descricao.Trim();
        DataEntrega = dados.DataEntrega;
        ClienteId = vinculo.ClienteId;
        ProcessoId = vinculo.ProcessoId;
        AreaAtuacaoId = vinculo.AreaAtuacaoId;
    }

    private static void Validar(DadosDaAtividade dados, StatusAtividade status)
    {
        ArgumentNullException.ThrowIfNull(dados);
        if (string.IsNullOrWhiteSpace(dados.Titulo))
            throw new ExcecaoDeDominio("O título da atividade é obrigatório.");
        if (dados.Titulo.Trim().Length > TamanhoMaximoTitulo)
            throw new ExcecaoDeDominio($"O título da atividade deve ter até {TamanhoMaximoTitulo} caracteres.");
        if (string.IsNullOrWhiteSpace(dados.Descricao))
            throw new ExcecaoDeDominio("A descrição da atividade é obrigatória.");
        if (dados.Descricao.Trim().Length > TamanhoMaximoDescricao)
            throw new ExcecaoDeDominio($"A descrição da atividade deve ter até {TamanhoMaximoDescricao} caracteres.");
        if (status != StatusAtividade.Criada && dados.DataEntrega is null)
            throw new ExcecaoDeDominio("A data de entrega é obrigatória depois que a atividade é direcionada.");
    }

    private static string MotivoValido(string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ExcecaoDeDominio("Informe o motivo.");
        if (motivo.Trim().Length > AtividadeHistoricoStatus.TamanhoMaximoMotivo)
            throw new ExcecaoDeDominio($"O motivo deve ter até {AtividadeHistoricoStatus.TamanhoMaximoMotivo} caracteres.");
        return motivo.Trim();
    }

    private static string? Texto(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
