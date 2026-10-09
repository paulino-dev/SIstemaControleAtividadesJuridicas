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
    /// só os que o perfil ou o usuário alcançam pela lista de acesso, e nunca um contrato de honorários, um documento do cliente nem um documento comum (estes são do
    /// portal do cliente). Para a
    /// categoria dos documentos de atividade o filtro não é a lista de acesso: é ser responsável atual de uma
    /// atividade viva (não excluída) à qual o documento pertence.
    /// </summary>
    Task<Pagina<DocumentoResumo>> ListarDoClienteAsync(
        Guid clienteId, CategoriaDocumento categoria, FiltroDeAcesso? acesso, int pagina, int tamanho, CancellationToken ct);

    /// <summary>Os documentos de uma atividade, do mais recente para o mais antigo. Quem chama já decidiu que o usuário vê a atividade.</summary>
    Task<Pagina<DocumentoResumo>> ListarDaAtividadeAsync(Guid atividadeId, int pagina, int tamanho, CancellationToken ct);

    /// <summary>Os documentos da atividade, rastreados, para o cliente deles acompanhar o da atividade.</summary>
    Task<IReadOnlyList<Documento>> ObterDaAtividadeAsync(Guid atividadeId, CancellationToken ct);

    void Adicionar(Documento documento);

    /// <summary>Exclusão lógica do documento e das entradas da lista de acesso; o arquivo permanece.</summary>
    void Remover(Documento documento);
}

/// <summary>
/// O que o controle de acesso a documentos precisa saber das atividades. Declarada aqui e implementada na
/// infraestrutura: Documentos não usa tipos de Atividades.
/// </summary>
public interface IResponsaveisDeAtividade
{
    /// <summary>Verdadeiro só para o responsável atual de uma atividade viva (não excluída).</summary>
    Task<bool> EhResponsavelAsync(Guid atividadeId, Guid usuarioId, CancellationToken ct);
}

/// <summary>
/// <paramref name="EnviadoPor"/> é o nome de quem enviou, quando conhecido. <paramref name="AtividadeId"/> e
/// <paramref name="AtividadeTitulo"/> só vêm no documento de atividade (o título, mesmo da atividade excluída).
/// </summary>
public sealed record DocumentoResumo(
    Guid Id, string Nome, string NomeArquivoOriginal, long TamanhoBytes, CategoriaDocumento Categoria,
    DateTime EnviadoEm, string? EnviadoPor, Guid? AtividadeId, string? AtividadeTitulo);

/// <summary>Quem recebe fecha <paramref name="Conteudo"/>.</summary>
public sealed record ArquivoParaBaixar(string NomeArquivo, long TamanhoBytes, Stream Conteudo) : IAsyncDisposable, IDisposable
{
    public void Dispose() => Conteudo.Dispose();

    public ValueTask DisposeAsync() => Conteudo.DisposeAsync();
}

public sealed record AcessoDoDocumento(
    Guid DocumentoId, string Nome, CategoriaDocumento Categoria, IReadOnlyList<Guid> Perfis, IReadOnlyList<Guid> Usuarios);
