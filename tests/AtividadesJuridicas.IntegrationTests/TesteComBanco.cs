using AtividadesJuridicas.Application;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Base para testes de Application + Infrastructure contra um banco novo, sem a camada Web.</summary>
public abstract class TesteComBanco(BancoFixture banco) : IAsyncLifetime
{
    protected static readonly AdminInicialOpcoes Admin = new("Diana Diretora", "diana@x.com", "senha-segura");

    private ServiceProvider _raiz = null!;
    private string _conexao = null!;

    public virtual async Task InitializeAsync()
    {
        _conexao = banco.NovaConexao();
        var conexao = _conexao;
        _raiz = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(_ => conexao)
            .BuildServiceProvider();

        await EmEscopoAsync(null, async sp =>
        {
            await sp.GetRequiredService<SemeadorInicial>().ExecutarAsync(Admin);
            return 0;
        });
    }

    public virtual async Task DisposeAsync()
    {
        await _raiz.DisposeAsync();
        await banco.RemoverAsync(_conexao);
    }

    /// <summary>Roda em um escopo novo, como o despachante faz, agindo como <paramref name="usuarioId"/>.</summary>
    protected async Task<T> EmEscopoAsync<T>(Guid? usuarioId, Func<IServiceProvider, Task<T>> acao)
    {
        await using var escopo = _raiz.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<IdentidadeAtual>().DefinirInterno(usuarioId, "127.0.0.9");
        return await acao(escopo.ServiceProvider);
    }

    protected Task<TResposta> ExecutarAsync<TResposta>(Guid? usuarioId, IRequisicao<TResposta> requisicao) =>
        EmEscopoAsync(usuarioId, sp => sp.GetRequiredService<ExecutorDeRequisicoes>().ExecutarAsync(requisicao, default));

    protected Task<Guid> IdDoAdminAsync() => EmEscopoAsync(null, async sp =>
        (await sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking()
            .SingleAsync(u => u.Email == Email.Criar(Admin.Email))).Id);

    protected Task<Guid> IdDoPerfilAsync(TipoPerfil tipo) => EmEscopoAsync(null, async sp =>
        (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking().SingleAsync(p => p.Tipo == tipo)).Id);

    protected Task<List<RegistroAuditoria>> AuditoriaAsync() => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAuditoria.AsNoTracking().OrderBy(r => r.Id).ToListAsync());
}
