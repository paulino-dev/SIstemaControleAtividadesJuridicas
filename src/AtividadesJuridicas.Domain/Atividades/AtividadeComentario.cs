using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Atividades;

/// <summary>
/// Um comentário numa atividade. É registro de conversa: nunca é editado nem apagado depois de criado.
/// Não faz parte do agregado <see cref="Atividade"/>: comentar não altera a atividade.
/// </summary>
public sealed class AtividadeComentario : Entidade
{
    public const int TamanhoMaximoTexto = 4000;

    private AtividadeComentario() { }

    public Guid AtividadeId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Texto { get; private set; } = null!;
    public DateTime DataHora { get; private set; }

    public static AtividadeComentario Criar(Guid atividadeId, Guid usuarioId, string texto, DateTime agoraUtc)
    {
        if (atividadeId == Guid.Empty) throw new ArgumentException("Informe a atividade.", nameof(atividadeId));
        if (usuarioId == Guid.Empty) throw new ArgumentException("Informe o autor.", nameof(usuarioId));
        if (agoraUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("A data deve estar em UTC.", nameof(agoraUtc));

        if (string.IsNullOrWhiteSpace(texto)) throw new ExcecaoDeDominio("Escreva o comentário.");
        var aparado = texto.Trim();
        if (aparado.Length > TamanhoMaximoTexto)
            throw new ExcecaoDeDominio($"O comentário deve ter até {TamanhoMaximoTexto} caracteres.");

        return new AtividadeComentario
        {
            AtividadeId = atividadeId,
            UsuarioId = usuarioId,
            Texto = aparado,
            DataHora = agoraUtc,
        };
    }
}
