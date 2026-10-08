using AtividadesJuridicas.Domain.Atividades;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary><paramref name="UsuarioNome"/>: nulo quando o autor não é encontrado; o de usuário excluído continua vindo.</summary>
public sealed record ComentarioDetalhe(Guid Id, Guid UsuarioId, string? UsuarioNome, string Texto, DateTime DataHora);

public interface IComentarioRepositorio
{
    void Adicionar(AtividadeComentario comentario);

    /// <summary>Em ordem cronológica (data e hora, depois o identificador), com o nome do autor.</summary>
    Task<IReadOnlyList<ComentarioDetalhe>> ListarDaAtividadeAsync(Guid atividadeId, CancellationToken ct);
}
