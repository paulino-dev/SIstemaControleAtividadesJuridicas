using AtividadesJuridicas.Domain.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class RegistroAuditoriaConfiguracao : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> b)
    {
        b.ToTable("registros_auditoria");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.Operacao).HasConversion<int>();
        b.Property(r => r.Entidade).HasMaxLength(100);
        b.Property(r => r.Evento).HasMaxLength(100);
        b.Property(r => r.Dados).HasColumnType("jsonb");
        b.Property(r => r.Ip).HasMaxLength(45);

        b.HasIndex(r => r.DataHora);
        b.HasIndex(r => new { r.Entidade, r.EntidadeId });
        b.HasIndex(r => r.UsuarioId);
        // Consultas por registro (só EntidadeId) e por gravação (IdCorrelacao), sem o nome da entidade.
        b.HasIndex(r => r.EntidadeId);
        b.HasIndex(r => r.IdCorrelacao);
    }
}
