using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Domain.Documentos;

public enum CategoriaDocumento
{
    DocumentoGeral = 1,
    ContratoHonorarios = 2,
    DocumentoAtividade = 3,
    DocumentoDoCliente = 4,
    DocumentoComum = 5,
}

/// <summary>O arquivo já gravado no armazenamento: nome enviado, tipo, tamanho, hash e onde ficou.</summary>
public sealed record ArquivoDoDocumento(
    string NomeOriginal, string TipoConteudo, long TamanhoBytes, string HashSha256, string ChaveArmazenamento);

/// <summary>Um usuário com o perfil que ele tem agora (o tipo do perfil decide a regra do contrato).</summary>
public sealed record UsuarioComPerfil(Usuario Usuario, Perfil Perfil);

/// <summary>O que mudou na lista de acesso de um documento, pelos Ids de perfis e de usuários.</summary>
public sealed record AlteracaoDeAcesso(
    IReadOnlyList<Guid> PerfisAdicionados, IReadOnlyList<Guid> PerfisRemovidos,
    IReadOnlyList<Guid> UsuariosAdicionados, IReadOnlyList<Guid> UsuariosRemovidos)
{
    public bool Houve =>
        PerfisAdicionados.Count > 0 || PerfisRemovidos.Count > 0 ||
        UsuariosAdicionados.Count > 0 || UsuariosRemovidos.Count > 0;
}

public sealed class Documento : Entidade
{
    public const int TamanhoMaximoNome = 255;
    public const int TamanhoMaximoTipoConteudo = 255;
    public const int TamanhoMaximoChave = 200;

    public const string ContratoSoParaDiretoria =
        "Contrato de Honorários não pode ser liberado para perfil ou usuário de Produção.";

    public const string ClienteForaDasListas =
        "Usuário ou perfil de cliente não entra em lista de acesso de documento.";

    public const string SemListaDeAcesso = "Documento visível ao cliente não tem lista de acesso.";

    private const string ClienteObrigatorio = "O cliente do documento é obrigatório.";
    private const string PerfilInativo = "Perfil inativo não pode receber acesso a documento.";
    private const string UsuarioInativo = "Usuário inativo não pode receber acesso a documento.";

    private readonly List<DocumentoPerfilAcesso> _perfisComAcesso = [];
    private readonly List<DocumentoUsuarioAcesso> _usuariosComAcesso = [];

    private Documento() { }

    public string Nome { get; private set; } = null!;
    public string NomeArquivoOriginal { get; private set; } = null!;
    public string TipoConteudo { get; private set; } = null!;
    public long TamanhoBytes { get; private set; }
    public string HashSha256 { get; private set; } = null!;
    public string ChaveArmazenamento { get; private set; } = null!;
    public CategoriaDocumento Categoria { get; private set; }

    /// <summary>
    /// Obrigatório em documento geral, em contrato de honorários e em documento do cliente; em documento de
    /// atividade é o da atividade (nulo se avulsa); no documento comum não existe.
    /// </summary>
    public Guid? ClienteId { get; private set; }

    /// <summary>Obrigatório no documento de atividade e só nele.</summary>
    public Guid? AtividadeId { get; private set; }

    /// <summary>Verdadeiro nas categorias postas para o cliente: documento do cliente e documento comum.</summary>
    public bool VisivelAoCliente =>
        Categoria is CategoriaDocumento.DocumentoDoCliente or CategoriaDocumento.DocumentoComum;

    public IReadOnlyCollection<DocumentoPerfilAcesso> PerfisComAcesso => _perfisComAcesso;
    public IReadOnlyCollection<DocumentoUsuarioAcesso> UsuariosComAcesso => _usuariosComAcesso;

    /// <summary>Documento geral: nasce liberado para todos os perfis ativos informados.</summary>
    public static Documento CriarGeral(
        Guid clienteId, string? nome, ArquivoDoDocumento arquivo, IReadOnlyCollection<Perfil> perfisAtivos)
    {
        ArgumentNullException.ThrowIfNull(perfisAtivos);
        if (perfisAtivos.Any(p => p is null))
            throw new ArgumentException("A lista de perfis não pode conter perfil nulo.", nameof(perfisAtivos));
        if (perfisAtivos.Any(p => !p.Ativo))
            throw new ExcecaoDeDominio(PerfilInativo);
        if (perfisAtivos.Any(p => p.Tipo == TipoPerfil.Cliente))
            throw new ExcecaoDeDominio(ClienteForaDasListas);

        var documento = Criar(CategoriaDocumento.DocumentoGeral, clienteId, nome, arquivo);
        foreach (var perfil in perfisAtivos.DistinctBy(p => p.Id))
            documento._perfisComAcesso.Add(new DocumentoPerfilAcesso(documento.Id, perfil.Id));
        return documento;
    }

    /// <summary>Contrato de honorários: nasce com a lista de acesso vazia; só a Diretoria o alcança.</summary>
    public static Documento CriarContratoDeHonorarios(Guid clienteId, string? nome, ArquivoDoDocumento arquivo) =>
        Criar(CategoriaDocumento.ContratoHonorarios, clienteId, nome, arquivo);

    /// <summary>Documento do cliente, visível a ele no portal: nasce com as listas de acesso vazias.</summary>
    public static Documento CriarDoCliente(Guid clienteId, string? nome, ArquivoDoDocumento arquivo) =>
        Criar(CategoriaDocumento.DocumentoDoCliente, clienteId, nome, arquivo);

    /// <summary>Documento comum, visível a todos os clientes: não tem cliente e nasce com as listas vazias.</summary>
    public static Documento CriarComum(string? nome, ArquivoDoDocumento arquivo) =>
        Criar(CategoriaDocumento.DocumentoComum, null, nome, arquivo);

    /// <summary>
    /// Documento de atividade: nasce com as listas de acesso vazias, porque o acesso é o dos responsáveis atuais
    /// da atividade (decidido a cada consulta). <paramref name="clienteId"/> é o da atividade, nulo se ela for avulsa.
    /// </summary>
    public static Documento CriarDeAtividade(Guid atividadeId, Guid? clienteId, string? nome, ArquivoDoDocumento arquivo)
    {
        if (atividadeId == Guid.Empty)
            throw new ExcecaoDeDominio("A atividade do documento é obrigatória.");

        var documento = Criar(CategoriaDocumento.DocumentoAtividade, clienteId, nome, arquivo);
        documento.AtividadeId = atividadeId;
        return documento;
    }

    /// <summary>O cliente do documento de atividade é sempre o da atividade: ela mudou de cliente (ou ficou sem).</summary>
    public void AcompanharClienteDaAtividade(Guid? clienteId)
    {
        if (Categoria != CategoriaDocumento.DocumentoAtividade)
            throw new InvalidOperationException("Só o documento de atividade acompanha o cliente da atividade.");
        if (clienteId == Guid.Empty)
            throw new ExcecaoDeDominio(ClienteObrigatorio);

        ClienteId = clienteId;
    }

    public void Renomear(string nome) => Nome = NomeValido(nome);

    /// <summary>O nome do documento com a extensão do arquivo enviado, quando ele ainda não a tem.</summary>
    public string NomeParaBaixar()
    {
        var extensao = Path.GetExtension(NomeArquivoOriginal);
        return extensao.Length == 0 || Nome.EndsWith(extensao, StringComparison.OrdinalIgnoreCase)
            ? Nome
            : Nome + extensao;
    }

    /// <summary>
    /// A única regra de acesso a documentos. <paramref name="ehResponsavelDaAtividade"/> só conta no documento de
    /// atividade, e quem a responde é quem consulta a atividade. <paramref name="clienteDoUsuario"/> é o cliente
    /// de um usuário de perfil Cliente (nulo para a equipe).
    /// </summary>
    public bool PodeSerAcessadoPor(
        Guid usuarioId, Guid perfilId, TipoPerfil tipoPerfil, bool ehResponsavelDaAtividade = false,
        Guid? clienteDoUsuario = null)
    {
        if (tipoPerfil == TipoPerfil.Diretoria) return true;

        // O cliente alcança só o que foi posto para ele: os documentos do próprio cliente e os comuns. Nenhuma
        // outra categoria, ainda que ele conste em alguma lista.
        if (tipoPerfil == TipoPerfil.Cliente)
            return Categoria == CategoriaDocumento.DocumentoComum ||
                   (Categoria == CategoriaDocumento.DocumentoDoCliente && clienteDoUsuario is { } cliente && ClienteId == cliente);

        // Daqui em diante só a Produção: um tipo de perfil desconhecido não acessa nada.
        if (tipoPerfil != TipoPerfil.Producao) return false;

        // O que é visível ao cliente não é da Produção.
        if (VisivelAoCliente) return false;

        // Sem exceção: nem quem consta na lista (por ter sido da Diretoria) alcança um contrato.
        if (Categoria == CategoriaDocumento.ContratoHonorarios) return false;

        // As listas não contam: o acesso acompanha os responsáveis de hoje.
        if (Categoria == CategoriaDocumento.DocumentoAtividade) return ehResponsavelDaAtividade;

        return _perfisComAcesso.Any(a => a.PerfilId == perfilId) ||
               _usuariosComAcesso.Any(a => a.UsuarioId == usuarioId);
    }

    /// <summary>
    /// Deixa a lista de acesso com exatamente os perfis e usuários informados. A entrada de quem
    /// permanece é preservada; a de quem sai é removida da coleção.
    /// </summary>
    public AlteracaoDeAcesso DefinirAcesso(
        IReadOnlyCollection<Perfil> perfis, IReadOnlyCollection<UsuarioComPerfil> usuarios)
    {
        if (Categoria == CategoriaDocumento.DocumentoAtividade)
            throw new ExcecaoDeDominio("O acesso a um documento de atividade é o dos responsáveis da atividade.");
        if (VisivelAoCliente)
            throw new ExcecaoDeDominio(SemListaDeAcesso);

        ArgumentNullException.ThrowIfNull(perfis);
        ArgumentNullException.ThrowIfNull(usuarios);
        if (perfis.Any(p => p is null))
            throw new ArgumentException("A lista de perfis não pode conter perfil nulo.", nameof(perfis));
        if (usuarios.Any(u => u is null || u.Usuario is null || u.Perfil is null))
            throw new ArgumentException("Cada usuário informado deve vir com o usuário e o perfil.", nameof(usuarios));
        if (usuarios.Any(u => u.Usuario.PerfilId != u.Perfil.Id))
            throw new ArgumentException("O perfil informado não é o do usuário.", nameof(usuarios));

        var perfisDesejados = perfis.DistinctBy(p => p.Id).ToList();
        var usuariosDesejados = usuarios.DistinctBy(u => u.Usuario.Id).ToList();
        var perfisNovos = perfisDesejados.Where(p => _perfisComAcesso.All(a => a.PerfilId != p.Id)).ToList();
        var usuariosNovos = usuariosDesejados.Where(u => _usuariosComAcesso.All(a => a.UsuarioId != u.Usuario.Id)).ToList();

        // Tudo é verificado antes de qualquer mudança, para não deixar a lista pela metade.
        if (perfisDesejados.Any(p => p.Tipo == TipoPerfil.Cliente) ||
            usuariosDesejados.Any(u => u.Perfil.Tipo == TipoPerfil.Cliente))
            throw new ExcecaoDeDominio(ClienteForaDasListas);
        if (Categoria == CategoriaDocumento.ContratoHonorarios &&
            (perfisDesejados.Any(p => p.Tipo != TipoPerfil.Diretoria) ||
             usuariosDesejados.Any(u => u.Perfil.Tipo != TipoPerfil.Diretoria)))
            throw new ExcecaoDeDominio(ContratoSoParaDiretoria);
        if (perfisNovos.Any(p => !p.Ativo))
            throw new ExcecaoDeDominio(PerfilInativo);
        if (usuariosNovos.Any(u => !u.Usuario.Ativo))
            throw new ExcecaoDeDominio(UsuarioInativo);

        var idsDePerfis = perfisDesejados.Select(p => p.Id).ToHashSet();
        var idsDeUsuarios = usuariosDesejados.Select(u => u.Usuario.Id).ToHashSet();
        var perfisRemovidos = _perfisComAcesso.Where(a => !idsDePerfis.Contains(a.PerfilId)).ToList();
        var usuariosRemovidos = _usuariosComAcesso.Where(a => !idsDeUsuarios.Contains(a.UsuarioId)).ToList();

        foreach (var entrada in perfisRemovidos) _perfisComAcesso.Remove(entrada);
        foreach (var entrada in usuariosRemovidos) _usuariosComAcesso.Remove(entrada);
        foreach (var perfil in perfisNovos) _perfisComAcesso.Add(new DocumentoPerfilAcesso(Id, perfil.Id));
        foreach (var usuario in usuariosNovos) _usuariosComAcesso.Add(new DocumentoUsuarioAcesso(Id, usuario.Usuario.Id));

        return new AlteracaoDeAcesso(
            [.. perfisNovos.Select(p => p.Id)], [.. perfisRemovidos.Select(a => a.PerfilId)],
            [.. usuariosNovos.Select(u => u.Usuario.Id)], [.. usuariosRemovidos.Select(a => a.UsuarioId)]);
    }

    private static Documento Criar(CategoriaDocumento categoria, Guid? clienteId, string? nome, ArquivoDoDocumento arquivo)
    {
        ArgumentNullException.ThrowIfNull(arquivo);
        if (clienteId == Guid.Empty || (clienteId is null && categoria is CategoriaDocumento.DocumentoGeral or CategoriaDocumento.ContratoHonorarios
                or CategoriaDocumento.DocumentoDoCliente))
            throw new ExcecaoDeDominio(ClienteObrigatorio);
        if (string.IsNullOrWhiteSpace(arquivo.NomeOriginal))
            throw new ExcecaoDeDominio("O nome do arquivo é obrigatório.");

        var original = arquivo.NomeOriginal.Trim();
        if (original.Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome do arquivo deve ter até {TamanhoMaximoNome} caracteres.");
        if (arquivo.TamanhoBytes <= 0)
            throw new ExcecaoDeDominio("O arquivo está vazio.");

        // O que vem do armazenamento e do servidor, não do usuário: erro aqui é de programação.
        if (string.IsNullOrWhiteSpace(arquivo.TipoConteudo) || arquivo.TipoConteudo.Trim().Length > TamanhoMaximoTipoConteudo)
            throw new ArgumentException("Tipo de conteúdo inválido.", nameof(arquivo));
        if (!HashValido(arquivo.HashSha256))
            throw new ArgumentException("O hash deve ser SHA-256 em hexadecimal minúsculo.", nameof(arquivo));
        if (string.IsNullOrWhiteSpace(arquivo.ChaveArmazenamento) || arquivo.ChaveArmazenamento.Length > TamanhoMaximoChave)
            throw new ArgumentException("Chave de armazenamento inválida.", nameof(arquivo));

        return new Documento
        {
            Categoria = categoria,
            ClienteId = clienteId,
            Nome = NomeValido(string.IsNullOrWhiteSpace(nome) ? original : nome),
            NomeArquivoOriginal = original,
            TipoConteudo = arquivo.TipoConteudo.Trim(),
            TamanhoBytes = arquivo.TamanhoBytes,
            HashSha256 = arquivo.HashSha256,
            ChaveArmazenamento = arquivo.ChaveArmazenamento,
        };
    }

    private static string NomeValido(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ExcecaoDeDominio("O nome do documento é obrigatório.");
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome do documento deve ter até {TamanhoMaximoNome} caracteres.");
        return nome.Trim();
    }

    private static bool HashValido(string? hash) =>
        hash is { Length: 64 } && hash.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
