using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

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

    public int GravacoesDeLogin { get; private set; }

    public Task GravarEstadoDeLoginAsync(Usuario usuario, CancellationToken ct)
    {
        GravacoesDeLogin++;
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

/// <summary>Monta um cenário com os dois perfis padrão e um diretor ativo.</summary>
public sealed class Cenario
{
    public PerfilRepositorioFalso Perfis { get; } = new();
    public UsuarioRepositorioFalso Usuarios { get; }
    public UnidadeDeTrabalhoFalsa Unidade { get; } = new();
    public HasherFalso Hasher { get; } = new();
    public RelogioFixo Relogio { get; } = new();
    public AuditoriaFalsa Auditoria { get; } = new();
    public Perfil PerfilDiretor { get; } = Perfil.Criar("Diretor", TipoPerfil.Diretoria);
    public Perfil PerfilProducao { get; } = Perfil.Criar("Produção", TipoPerfil.Producao);
    public Usuario Diretor { get; }

    public Cenario()
    {
        Usuarios = new UsuarioRepositorioFalso(Perfis);
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

public sealed record EventoAnotado(string Evento, string? Entidade, Guid? EntidadeId, object? Dados, bool Imediato);

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
        CancellationToken ct = default)
    {
        Eventos.Add(new EventoAnotado(evento, entidade, entidadeId, dados, Imediato: true));
        return Task.CompletedTask;
    }
}
