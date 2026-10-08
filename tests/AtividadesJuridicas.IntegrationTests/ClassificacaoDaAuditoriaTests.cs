using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Infrastructure.Persistencia.Interceptores;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A regra que decide o que vai para a auditoria, testada num modelo descartável construído aqui
/// (não abre conexão): os nomes de controle da entidade só valem no nível dela, e [NaoAuditar] numa
/// propriedade complexa inteira tira a subárvore.
/// </summary>
public class ClassificacaoDaAuditoriaTests
{
    public sealed class Marca
    {
        public int Id { get; set; }
        public string? Descricao { get; set; }
        public DateTime? CriadoEm { get; set; }
    }

    public sealed class Segredo
    {
        public string? Chave { get; set; }
    }

    public sealed class EntidadeDeTeste : Entidade
    {
        public string Nome { get; set; } = "";
        public Marca Marca { get; set; } = new();

        [NaoAuditar]
        public Segredo Segredo { get; set; } = new();
    }

    private sealed class ContextoDeTeste(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entidade = modelBuilder.Entity<EntidadeDeTeste>();
            entidade.HasKey(e => e.Id);
            entidade.ComplexProperty(e => e.Marca);
            entidade.ComplexProperty(e => e.Segredo);
        }
    }

    [Fact]
    public void Nomes_de_controle_so_valem_na_entidade_e_NaoAuditar_em_complexa_tira_a_subarvore()
    {
        using var db = new ContextoDeTeste(
            new DbContextOptionsBuilder<ContextoDeTeste>().UseNpgsql("Host=localhost;Database=design_time").Options);
        var tipo = db.Model.FindEntityType(typeof(EntidadeDeTeste))!;

        var auditadas = AuditoriaInterceptor.PropriedadesAuditadas(tipo);

        auditadas.Should().BeEquivalentTo(["Nome", "Marca.Id", "Marca.Descricao", "Marca.CriadoEm"]);
    }
}
