using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Documentos;

/// <summary>
/// Um perfil na lista de acesso de um documento. É uma entidade, com Id próprio, para que a entrada
/// e a saída do perfil tenham carimbos, exclusão lógica e registro de auditoria.
/// </summary>
public sealed class DocumentoPerfilAcesso : Entidade
{
    private DocumentoPerfilAcesso() { }

    internal DocumentoPerfilAcesso(Guid documentoId, Guid perfilId)
    {
        DocumentoId = documentoId;
        PerfilId = perfilId;
    }

    public Guid DocumentoId { get; private set; }
    public Guid PerfilId { get; private set; }
}

/// <summary>Um usuário na lista de acesso de um documento.</summary>
public sealed class DocumentoUsuarioAcesso : Entidade
{
    private DocumentoUsuarioAcesso() { }

    internal DocumentoUsuarioAcesso(Guid documentoId, Guid usuarioId)
    {
        DocumentoId = documentoId;
        UsuarioId = usuarioId;
    }

    public Guid DocumentoId { get; private set; }
    public Guid UsuarioId { get; private set; }
}
