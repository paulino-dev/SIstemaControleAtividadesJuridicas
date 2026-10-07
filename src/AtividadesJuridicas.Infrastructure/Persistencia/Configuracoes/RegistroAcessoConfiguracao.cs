using AtividadesJuridicas.Domain.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class RegistroAcessoConfiguracao : IEntityTypeConfiguration<RegistroAcesso>
{
    public void Configure(EntityTypeBuilder<RegistroAcesso> b)
    {
        b.ToTable("registros_acesso");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.TipoRecurso).HasMaxLength(100).IsRequired();
        b.Property(r => r.Acao).HasConversion<int>();
        b.Property(r => r.Detalhe).HasMaxLength(RegistroAcesso.TamanhoMaximoDetalhe);
        b.Property(r => r.Ip).HasMaxLength(45);

        b.HasIndex(r => r.DataHora);
        b.HasIndex(r => r.UsuarioId);
        b.HasIndex(r => new { r.TipoRecurso, r.RecursoId });
    }
}
