using System.Linq.Expressions;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

public sealed class AppDbContext(DbContextOptions<AppDbContext> opcoes) : DbContext(opcoes)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<AreaAtuacao> AreasAtuacao => Set<AreaAtuacao>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Processo> Processos => Set<Processo>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<RegistroAcesso> RegistrosAcesso => Set<RegistroAcesso>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Convenções de toda Entidade: chave gerada no domínio e filtro de exclusão lógica.
        var raizes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(Entidade).IsAssignableFrom(t.ClrType) && t.BaseType is null)
            .Select(t => t.ClrType)
            .ToList();

        foreach (var tipo in raizes)
        {
            var entidade = modelBuilder.Entity(tipo);
            entidade.HasKey(nameof(Entidade.Id));
            entidade.Property(nameof(Entidade.Id)).ValueGeneratedNever();

            var parametro = Expression.Parameter(tipo, "e");
            var naoExcluido = Expression.Equal(
                Expression.Property(parametro, nameof(Entidade.ExcluidoEm)),
                Expression.Constant(null, typeof(DateTime?)));
            entidade.HasQueryFilter(Expression.Lambda(naoExcluido, parametro));
        }
    }
}
