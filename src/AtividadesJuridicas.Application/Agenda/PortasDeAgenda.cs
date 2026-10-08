using AtividadesJuridicas.Domain.Agenda;

namespace AtividadesJuridicas.Application.Agenda;

public static class AgendaOpcoes
{
    public const int PeriodoMaximoEmDias = 92;
}

/// <summary><paramref name="Nome"/> é nulo quando o usuário não é mais encontrado.</summary>
public sealed record ParticipanteDetalhe(Guid UsuarioId, string? Nome, bool Ativo);

public sealed record EventoResumo(
    Guid Id, string Titulo, TipoEvento Tipo, DateTime InicioEm, DateTime FimEm, string? Local, string? ClienteNome,
    string? ProcessoTitulo, IReadOnlyList<string> Participantes);

/// <summary>O que o usuário pode fazer com o evento agora.</summary>
public sealed record AcoesDoEvento(bool Editar, bool Excluir);

/// <summary>
/// <paramref name="AtividadeTitulo"/> só vem quando o usuário pode ver a atividade. <paramref name="Versao"/>: a versão da
/// linha, que os formulários devolvem como versão esperada.
/// </summary>
public sealed record EventoDetalhe(
    Guid Id, string Titulo, string? Descricao, string? Local, TipoEvento Tipo, DateTime InicioEm,
    int DuracaoEstimadaMinutos, DateTime FimEm, Guid? ClienteId, string? ClienteNome, Guid? ProcessoId,
    string? ProcessoTitulo, Guid? AtividadeId, string? AtividadeTitulo, Guid CriadorId, string? CriadorNome,
    IReadOnlyList<ParticipanteDetalhe> Participantes, AcoesDoEvento Acoes, uint Versao);

/// <summary>Um participante que já tem outro evento no mesmo horário. Propositalmente sem o título do outro evento.</summary>
public sealed record SobreposicaoDeAgenda(Guid UsuarioId, string? UsuarioNome, DateTime InicioEm, DateTime FimEm);

public sealed record EventoSalvo(Guid Id, IReadOnlyList<SobreposicaoDeAgenda> Sobreposicoes);

/// <summary>
/// <paramref name="VisivelPara"/> não nulo restringe aos eventos em que esse usuário participa ou que ele criou.
/// </summary>
public sealed record FiltroDeEventos(DateTime DeUtc, DateTime AteUtc, Guid? ParticipanteId, TipoEvento? Tipo, Guid? VisivelPara);

public interface IEventoRepositorio
{
    /// <summary>O evento com os participantes.</summary>
    Task<Evento?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Os eventos que tocam o período (começam antes de <c>AteUtc</c> e terminam depois de <c>DeUtc</c>), em ordem de
    /// início e, no empate, de Id.
    /// </summary>
    Task<IReadOnlyList<EventoResumo>> ListarAsync(FiltroDeEventos filtro, CancellationToken ct);

    /// <summary>
    /// Para cada participante entre <paramref name="usuarioIds"/> e cada outro evento vivo que se sobrepõe ao intervalo
    /// <c>[inicioUtc, fimUtc)</c> (intervalos semiabertos: encostados não se sobrepõem), uma entrada. Desconsidera
    /// <paramref name="excetoEventoId"/> e participantes retirados.
    /// </summary>
    Task<IReadOnlyList<SobreposicaoDeAgenda>> SobreposicoesAsync(
        Guid? excetoEventoId, IReadOnlyCollection<Guid> usuarioIds, DateTime inicioUtc, DateTime fimUtc, CancellationToken ct);

    void Adicionar(Evento evento);

    /// <summary>Exclusão lógica do evento e dos participantes.</summary>
    void Remover(Evento evento);
}
