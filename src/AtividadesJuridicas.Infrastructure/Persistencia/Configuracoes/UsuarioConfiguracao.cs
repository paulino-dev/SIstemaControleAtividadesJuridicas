using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class UsuarioConfiguracao : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.Property(u => u.Nome).HasMaxLength(Usuario.TamanhoMaximoNome).IsRequired();
        b.Property(u => u.Email)
            .HasConversion(e => e.Valor, v => Email.Criar(v))
            .HasMaxLength(Email.TamanhoMaximo)
            .IsRequired();
        b.Property(u => u.SenhaHash).HasMaxLength(200).IsRequired();
        b.Property(u => u.NumeroOab).HasMaxLength(Usuario.TamanhoMaximoOab);

        // Filtrado: um e-mail de usuário excluído logicamente pode ser reutilizado.
        b.HasIndex(u => u.Email).IsUnique().HasFilter("excluido_em IS NULL");

        b.HasOne<Perfil>().WithMany().HasForeignKey(u => u.PerfilId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Cliente>().WithMany().HasForeignKey(u => u.ClienteId).OnDelete(DeleteBehavior.Restrict);
    }
}
