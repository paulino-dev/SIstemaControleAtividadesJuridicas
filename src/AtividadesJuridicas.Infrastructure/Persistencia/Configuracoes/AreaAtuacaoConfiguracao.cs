using AtividadesJuridicas.Domain.Cadastros;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class AreaAtuacaoConfiguracao : IEntityTypeConfiguration<AreaAtuacao>
{
    public void Configure(EntityTypeBuilder<AreaAtuacao> b)
    {
        b.ToTable("areas_atuacao");
        b.Property(a => a.Nome).HasMaxLength(AreaAtuacao.TamanhoMaximoNome).IsRequired();
        b.HasIndex(a => a.Nome).IsUnique().HasFilter("excluido_em IS NULL");
    }
}
