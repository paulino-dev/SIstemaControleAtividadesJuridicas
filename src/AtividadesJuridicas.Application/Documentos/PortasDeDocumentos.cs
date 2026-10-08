using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

public static class DocumentosOpcoes
{
    public const long TamanhoMaximoBytes = 50L * 1024 * 1024;
    public const int TamanhoDaPagina = 20;
    public const string TipoDeConteudoPadrao = "application/octet-stream";
}

/// <summary>Quem está consultando, quando a consulta precisa respeitar a lista de acesso.</summary>
public sealed record FiltroDeAcesso(Guid UsuarioId, Guid PerfilId);

public interface IDocumentoRepositorio
{
    /// <summary>O documento com as suas listas de acesso.</summary>
    Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Do mais recente para o mais antigo. Sem <paramref name="acesso"/>, todos os documentos; com ele,
    /// só os que o perfil ou o usuário alcançam pela lista de acesso, e nunca um contrato de honorários.
    /// </summary>
    Task<Pagina<DocumentoResumo>> ListarDoClienteAsync(
        Guid clienteId, CategoriaDocumento categoria, FiltroDeAcesso? acesso, int pagina, int tamanho, CancellationToken ct);

    void Adicionar(Documento documento);

    /// <summary>Exclusão lógica do documento e das entradas da lista de acesso; o arquivo permanece.</summary>
    void Remover(Documento documento);
}

/// <summary><paramref name="EnviadoPor"/> é o nome de quem enviou, quando conhecido.</summary>
public sealed record DocumentoResumo(
    Guid Id, string Nome, string NomeArquivoOriginal, long TamanhoBytes, CategoriaDocumento Categoria,
    DateTime EnviadoEm, string? EnviadoPor);

/// <summary>Quem recebe fecha <paramref name="Conteudo"/>.</summary>
public sealed record ArquivoParaBaixar(string NomeArquivo, long TamanhoBytes, Stream Conteudo);

public sealed record AcessoDoDocumento(
    Guid DocumentoId, string Nome, CategoriaDocumento Categoria, IReadOnlyList<Guid> Perfis, IReadOnlyList<Guid> Usuarios);
