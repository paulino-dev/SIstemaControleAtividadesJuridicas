using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

/// <summary>O comentário não é filho do agregado Atividade: sem navegação, comentar não grava a linha da atividade.</summary>
public sealed class AtividadeComentarioConfiguracao : IEntityTypeConfiguration<AtividadeComentario>
{
    public void Configure(EntityTypeBuilder<AtividadeComentario> b)
    {
        b.ToTable("atividades_comentarios");
        b.Property(c => c.Texto).HasMaxLength(AtividadeComentario.TamanhoMaximoTexto).IsRequired();

        b.HasOne<Atividade>().WithMany().HasForeignKey(c => c.AtividadeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(c => new { c.AtividadeId, c.DataHora });
    }
}
