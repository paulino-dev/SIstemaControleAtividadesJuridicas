using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

/// <summary>Usada só pelo <c>dotnet ef</c> para gerar migrations; não abre conexão.</summary>
public sealed class AppDbContextFabrica : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time")
            .UseSnakeCaseNamingConvention()
            .Options);
}
