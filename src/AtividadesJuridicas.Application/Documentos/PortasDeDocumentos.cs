using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

public static class DocumentosOpcoes
{
    public const long TamanhoMaximoBytes = 50L * 1024 * 1024;
    public const int TamanhoDaPagina = 20;
    public const string TipoDeConteudoPadrao = "application/octet-stream";
    public const string ConfirmacaoExigida = "Confirme que o documento ficará visível para todos os clientes.";

    /// <summary>
    /// A recusa da Diretoria ao renomear ou excluir um documento da solicitação: os anexos de uma solicitação só mudam
    /// pelas mãos do cliente que a abriu, enquanto ela aguarda.
    /// </summary>
    public const string DocumentoDaSolicitacaoSoPeloCliente = "Documento de solicitação só é alterado pelo cliente que a abriu.";
}

/// <summary>Quem está consultando, quando a consulta precisa respeitar a lista de acesso.</summary>
public sealed record FiltroDeAcesso(Guid UsuarioId, Guid PerfilId);

public interface IDocumentoRepositorio
{
    /// <summary>O documento com as suas listas de acesso.</summary>
    Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Do mais recente para o mais antigo. Sem <paramref name="acesso"/>, todos os documentos; com ele,
    /// só os que o perfil ou o usuário alcançam pela lista de acesso, e nunca um contrato de honorários, um
    /// documento do cliente, um documento comum nem um documento da solicitação (estes são do portal do
    /// cliente). Para a categoria dos documentos de atividade o filtro não é a lista de acesso: é ser
    /// responsável atual de uma atividade viva (não excluída) à qual o documento pertence.
    /// </summary>
    Task<Pagina<DocumentoResumo>> ListarDoClienteAsync(
        Guid clienteId, CategoriaDocumento categoria, FiltroDeAcesso? acesso, int pagina, int tamanho, CancellationToken ct);

    /// <summary>
    /// Quantos documentos do cliente o usuário lê em cada área, com o mesmo filtro de acesso de
    /// <see cref="ListarDoClienteAsync"/>. As categorias do portal que não são áreas da ficha (o documento comum e o
    /// documento da solicitação) nunca entram nas contagens por área. Área que ele não lê não aparece; as lidas e
    /// vazias vêm com zero: as quatro do cliente sem <paramref name="acesso"/> (Diretoria), só geral e atividade com ele.
    /// </summary>
    Task<IReadOnlyDictionary<CategoriaDocumento, int>> ContarDoClienteAsync(
        Guid clienteId, FiltroDeAcesso? acesso, CancellationToken ct);

    /// <summary>
    /// Os documentos do cliente (categoria <see cref="CategoriaDocumento.DocumentoDoCliente"/>) daquele cliente,
    /// do mais recente para o mais antigo, sem autor. Quem chama já decidiu que o usuário é o cliente.
    /// </summary>
    Task<Pagina<DocumentoDoPortal>> ListarDoPortalAsync(Guid clienteId, int pagina, int tamanho, CancellationToken ct);

    /// <summary>
    /// Todos os documentos comuns, do mais recente para o mais antigo. O nome de quem enviou só é buscado com
    /// <paramref name="comAutor"/>.
    /// </summary>
    Task<Pagina<DocumentoDoPortal>> ListarComunsAsync(bool comAutor, int pagina, int tamanho, CancellationToken ct);

    /// <summary>
    /// Os documentos vivos de uma solicitação, do mais antigo para o mais novo, sem autor; os gravados no mesmo
    /// instante (os de uma solicitação aberta com vários arquivos), pelo nome e depois pelo Id. Quem chama já decidiu
    /// que o usuário vê a solicitação.
    /// </summary>
    Task<IReadOnlyList<DocumentoDoPortal>> ListarDaSolicitacaoAsync(Guid solicitacaoId, CancellationToken ct);

    /// <summary>Quantos documentos vivos a solicitação tem.</summary>
    Task<int> ContarDaSolicitacaoAsync(Guid solicitacaoId, CancellationToken ct);

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

/// <summary><paramref name="PorCategoria"/>: quantos documentos o usuário lê em cada área do cliente.</summary>
public sealed record PaginaDeDocumentos(
    IReadOnlyList<DocumentoResumo> Itens, int Total, int Numero, int Tamanho,
    IReadOnlyDictionary<CategoriaDocumento, int> PorCategoria)
    : Pagina<DocumentoResumo>(Itens, Total, Numero, Tamanho);

/// <summary>
/// A linha de documento que o portal mostra: sem nome de arquivo original nem categoria.
/// <paramref name="EnviadoPor"/> só vem preenchido para a Diretoria.
/// </summary>
public sealed record DocumentoDoPortal(Guid Id, string Nome, long TamanhoBytes, DateTime EnviadoEm, string? EnviadoPor = null);

/// <summary>
/// Um arquivo que chega para ser gravado. <paramref name="NomeArquivo"/> é o nome que o navegador informou: só o
/// último segmento é guardado, e ele nunca é usado como caminho. <paramref name="AbrirConteudo"/> abre o conteúdo só
/// quando ele vai ser lido: quem recebe abre um arquivo por vez, lê e fecha antes de abrir o próximo (no Blazor Server
/// a transferência do navegador começa na abertura, e um fluxo aberto à espera estoura o tempo sem dados).
/// </summary>
public sealed record ArquivoParaEnviar(string NomeArquivo, string? TipoConteudo, Func<Stream> AbrirConteudo);

/// <summary>Quem recebe fecha <paramref name="Conteudo"/>.</summary>
public sealed record ArquivoParaBaixar(string NomeArquivo, long TamanhoBytes, Stream Conteudo) : IAsyncDisposable, IDisposable
{
    public void Dispose() => Conteudo.Dispose();

    public ValueTask DisposeAsync() => Conteudo.DisposeAsync();
}

public sealed record AcessoDoDocumento(
    Guid DocumentoId, string Nome, CategoriaDocumento Categoria, IReadOnlyList<Guid> Perfis, IReadOnlyList<Guid> Usuarios);
