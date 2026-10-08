using System.Globalization;
using System.Text;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public sealed class AreaAtuacaoRepositorioFalso : IAreaAtuacaoRepositorio
{
    public List<AreaAtuacao> Itens { get; } = [];

    public Task<AreaAtuacao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(a => a.Id == id));

    public Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.Any(a =>
            a.Id != excetoId && string.Equals(a.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<AreaAtuacaoResumo>> ListarAsync(bool somenteAtivas, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AreaAtuacaoResumo>>(
            [.. Itens.Where(a => !somenteAtivas || a.Ativo).OrderBy(a => a.Nome)
                .Select(a => new AreaAtuacaoResumo(a.Id, a.Nome, a.Ativo))]);

    public void Adicionar(AreaAtuacao area) => Itens.Add(area);
}

public sealed class PerfilRepositorioFalso : IPerfilRepositorio
{
    public List<Perfil> Itens { get; } = [];

    public Task<Perfil?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<PerfilResumo>> ListarAtivosAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PerfilResumo>>(
            [.. Itens.Where(p => p.Ativo).Select(p => new PerfilResumo(p.Id, p.Nome, p.Tipo))]);
}

public sealed class UsuarioRepositorioFalso(PerfilRepositorioFalso perfis) : IUsuarioRepositorio
{
    public List<Usuario> Itens { get; } = [];

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(u => u.Email == email));

    public Task<bool> ExisteEmailAsync(Email email, Guid? excetoUsuarioId, CancellationToken ct) =>
        Task.FromResult(Itens.Any(u => u.Email == email && u.Id != excetoUsuarioId));

    public Task<int> ContarDiretoresAtivosAsync(Guid? excetoUsuarioId, CancellationToken ct) =>
        Task.FromResult(Itens.Count(u =>
            u.Ativo && u.Id != excetoUsuarioId &&
            perfis.Itens.Any(p => p.Id == u.PerfilId && p.Tipo == TipoPerfil.Diretoria)));

    public Task<IReadOnlyList<UsuarioResumo>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UsuarioResumo>>(
            [.. Itens.OrderBy(u => u.Nome).Select(u =>
            {
                var perfil = perfis.Itens.First(p => p.Id == u.PerfilId);
                return new UsuarioResumo(u.Id, u.Nome, u.Email.Valor, perfil.Id, perfil.Nome,
                    perfil.Tipo, u.NumeroOab, u.Ativo, u.UltimoAcessoEm);
            })]);

    public void Adicionar(Usuario usuario) => Itens.Add(usuario);

    public int Reservas { get; private set; }

    public int LoginsRegistrados { get; private set; }

    public Task<TentativaDeLogin> ReservarTentativaDeLoginAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct)
    {
        Reservas++;
        var usuario = Itens.First(u => u.Id == usuarioId);
        if (usuario.EstaBloqueado(agoraUtc)) return Task.FromResult(TentativaDeLogin.Recusada);
        return Task.FromResult(usuario.RegistrarFalhaDeLogin(agoraUtc)
            ? TentativaDeLogin.AdmitidaEBloqueou
            : TentativaDeLogin.Admitida);
    }

    public Task RegistrarLoginBemSucedidoAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct)
    {
        LoginsRegistrados++;
        Itens.First(u => u.Id == usuarioId).RegistrarAcesso(agoraUtc);
        return Task.CompletedTask;
    }
}

public sealed class UnidadeDeTrabalhoFalsa : IUnidadeDeTrabalho
{
    public int Salvamentos { get; private set; }

    public Task SalvarAsync(CancellationToken ct)
    {
        Salvamentos++;
        return Task.CompletedTask;
    }
}

public sealed class HasherFalso : IHasherDeSenha
{
    public string Gerar(string senha) => "hash:" + senha;

    public int Verificacoes { get; private set; }

    public string HashDeDescarte => "hash:descarte-que-ninguem-conhece";

    public bool Verificar(string senha, string hash)
    {
        Verificacoes++;
        return hash == "hash:" + senha;
    }
}

public sealed class RelogioFixo : IRelogio
{
    public DateTime AgoraUtc { get; set; } = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
}

public sealed class ContextoDeAcessoFixo(UsuarioAutenticado? ator) : IContextoDeAcesso
{
    public Task<UsuarioAutenticado?> ObterAsync(CancellationToken ct) => Task.FromResult(ator);
}

public sealed class ClienteRepositorioFalso : IClienteRepositorio
{
    public List<Cliente> Itens { get; } = [];

    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(c => c.Id == id));

    public Task<bool> ExisteCpfAsync(Cpf cpf, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.OfType<ClientePessoaFisica>().Any(c => c.Cpf == cpf && c.Id != excetoId));

    public Task<bool> ExisteCnpjAsync(Cnpj cnpj, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.OfType<ClientePessoaJuridica>().Any(c => c.Cnpj == cnpj && c.Id != excetoId));

    public Task<Pagina<Cliente>> ListarAsync(string? busca, bool incluirInativos, int pagina, int tamanho, CancellationToken ct)
    {
        IEnumerable<Cliente> consulta = Itens.Where(c => incluirInativos || c.Ativo);

        if (Cpf.TentarCriar(busca, out var cpf))
            consulta = consulta.Where(c => c is ClientePessoaFisica pf && pf.Cpf == cpf);
        else if (Cnpj.TentarCriar(busca, out var cnpj))
            consulta = consulta.Where(c => c is ClientePessoaJuridica pj && pj.Cnpj == cnpj);
        else if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(c => SemAcentos(c.Nome).Contains(SemAcentos(busca.Trim()), StringComparison.OrdinalIgnoreCase));

        var todos = consulta.OrderBy(c => c.Nome).ToList();
        return Task.FromResult(new Pagina<Cliente>(
            [.. todos.Skip((pagina - 1) * tamanho).Take(tamanho)], todos.Count, pagina, tamanho));
    }

    public void Adicionar(Cliente cliente) => Itens.Add(cliente);

    public Task<IReadOnlyList<Cliente>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Cliente>>([.. Itens.Where(c => ids.Contains(c.Id))]);

    // Mesma semântica do unaccent do banco: remove os diacríticos antes de comparar.
    private static string SemAcentos(string texto) => new(
        [.. texto.Normalize(NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)]);
}

public sealed class ProcessoRepositorioFalso(AreaAtuacaoRepositorioFalso areas, ClienteRepositorioFalso clientes)
    : IProcessoRepositorio
{
    public List<Processo> Itens { get; } = [];

    public Task<Processo?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(p => p.Id == id));

    public Task<bool> ExisteNumeroCnjAsync(NumeroCnj numero, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.Any(p => p.NumeroCnj == numero && p.Id != excetoId));

    public Task<Pagina<ProcessoResumo>> ListarAsync(FiltroDeProcessos filtro, int pagina, int tamanho, CancellationToken ct)
    {
        IEnumerable<Processo> consulta = Itens;
        if (filtro.Status is { } status) consulta = consulta.Where(p => p.Status == status);
        if (filtro.ClienteId is { } clienteId) consulta = consulta.Where(p => p.Clientes.Any(c => c.ClienteId == clienteId));

        if (NumeroCnj.TentarCriar(filtro.Busca, out var numero))
            consulta = consulta.Where(p => p.NumeroCnj == numero);
        else if (!string.IsNullOrWhiteSpace(filtro.Busca))
            consulta = consulta.Where(p => Contem(p.Titulo, filtro.Busca) || Contem(p.ParteContraria, filtro.Busca));

        var todos = consulta.OrderBy(p => p.Titulo).ToList();
        return Task.FromResult(new Pagina<ProcessoResumo>(
            [.. todos.Skip((pagina - 1) * tamanho).Take(tamanho).Select(Resumo)], todos.Count, pagina, tamanho));
    }

    public Task<ProcessoDetalhe?> ObterDetalheAsync(Guid id, CancellationToken ct)
    {
        var p = Itens.FirstOrDefault(x => x.Id == id);
        if (p is null) return Task.FromResult<ProcessoDetalhe?>(null);

        return Task.FromResult<ProcessoDetalhe?>(new ProcessoDetalhe(
            p.Id, p.NumeroCnj?.Formatado, p.Titulo, p.AreaAtuacaoId, NomeDaArea(p.AreaAtuacaoId), p.Status,
            p.Orgao, p.Comarca, p.ParteContraria, p.DataDistribuicao, p.ValorCausa, p.Observacoes,
            [.. p.Clientes.Select(v =>
            {
                var c = clientes.Itens.First(x => x.Id == v.ClienteId);
                return new ClienteDoProcessoDetalhe(c.Id, c.Nome, Documento(c), v.Polo, c.Ativo);
            }).OrderBy(c => c.Nome)]));
    }

    public void Adicionar(Processo processo) => Itens.Add(processo);

    private ProcessoResumo Resumo(Processo p) => new(
        p.Id, p.NumeroCnj?.Formatado, p.Titulo, NomeDaArea(p.AreaAtuacaoId), p.Status,
        [.. p.Clientes.Select(v => clientes.Itens.First(c => c.Id == v.ClienteId).Nome).Order()], p.ParteContraria);

    private string NomeDaArea(Guid id) => areas.Itens.First(a => a.Id == id).Nome;

    private static string Documento(Cliente c) => c switch
    {
        ClientePessoaFisica pf => pf.Cpf.Formatado,
        ClientePessoaJuridica pj => pj.Cnpj.Formatado,
        _ => string.Empty,
    };

    // Mesma semântica da busca real: ignora maiúsculas e acentos.
    private static bool Contem(string? texto, string busca) =>
        texto is not null && SemAcentos(texto).Contains(SemAcentos(busca.Trim()), StringComparison.OrdinalIgnoreCase);

    private static string SemAcentos(string texto) => new([.. texto.Normalize(System.Text.NormalizationForm.FormD)
        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)]);
}

/// <summary>Monta um cenário com os dois perfis padrão e um diretor ativo.</summary>
public sealed class Cenario
{
    public PerfilRepositorioFalso Perfis { get; } = new();
    public UsuarioRepositorioFalso Usuarios { get; }
    public UnidadeDeTrabalhoFalsa Unidade { get; } = new();
    public AreaAtuacaoRepositorioFalso Areas { get; } = new();
    public ClienteRepositorioFalso Clientes { get; } = new();
    public ProcessoRepositorioFalso Processos { get; }
    public HasherFalso Hasher { get; } = new();
    public RelogioFixo Relogio { get; } = new();
    public AuditoriaFalsa Auditoria { get; } = new();
    public Perfil PerfilDiretor { get; } = Perfil.Criar("Diretor", TipoPerfil.Diretoria);
    public Perfil PerfilProducao { get; } = Perfil.Criar("Produção", TipoPerfil.Producao);
    public Usuario Diretor { get; }

    public Cenario()
    {
        Usuarios = new UsuarioRepositorioFalso(Perfis);
        Processos = new ProcessoRepositorioFalso(Areas, Clientes);
        Perfis.Itens.AddRange([PerfilDiretor, PerfilProducao]);
        Diretor = NovoUsuario("Diana Diretora", "diana@x.com", PerfilDiretor);
    }

    public Usuario NovoUsuario(string nome, string email, Perfil perfil, string senha = "senha-segura")
    {
        var usuario = Usuario.Criar(nome, Email.Criar(email), Hasher.Gerar(senha), perfil);
        Usuarios.Itens.Add(usuario);
        return usuario;
    }

    public IContextoDeAcesso Como(Usuario? usuario)
    {
        if (usuario is null) return new ContextoDeAcessoFixo(null);
        var perfil = Perfis.Itens.First(p => p.Id == usuario.PerfilId);
        return new ContextoDeAcessoFixo(new UsuarioAutenticado(usuario.Id, usuario.Nome, perfil.Tipo));
    }
}

public sealed record EventoAnotado(string Evento, string? Entidade, Guid? EntidadeId, object? Dados, bool Imediato, Guid? Ator = null);

public sealed class AuditoriaFalsa : IAuditoria
{
    public List<EventoAnotado> Eventos { get; } = [];

    public List<IConsultaAuditavel> Acessos { get; } = [];

    public Task RegistrarAcessoAsync(IConsultaAuditavel consulta, CancellationToken ct = default)
    {
        Acessos.Add(consulta);
        return Task.CompletedTask;
    }

    public void Anotar(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null) =>
        Eventos.Add(new EventoAnotado(evento, entidade, entidadeId, dados, Imediato: false));

    public Task RegistrarAsync(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null,
        Guid? ator = null, CancellationToken ct = default)
    {
        Eventos.Add(new EventoAnotado(evento, entidade, entidadeId, dados, Imediato: true, ator));
        return Task.CompletedTask;
    }
}
