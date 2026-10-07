using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class PerfilConfiguracao : IEntityTypeConfiguration<Perfil>
{
    public void Configure(EntityTypeBuilder<Perfil> b)
    {
        b.ToTable("perfis");
        b.Property(p => p.Nome).HasMaxLength(Perfil.TamanhoMaximoNome).IsRequired();
        b.Property(p => p.Tipo).HasConversion<int>();
        b.HasIndex(p => p.Nome).IsUnique().HasFilter("excluido_em IS NULL");
    }
}
