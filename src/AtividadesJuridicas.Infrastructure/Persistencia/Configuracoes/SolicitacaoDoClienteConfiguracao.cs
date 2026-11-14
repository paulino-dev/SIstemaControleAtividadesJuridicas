using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Domain.Solicitacoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class SolicitacaoDoClienteConfiguracao : IEntityTypeConfiguration<SolicitacaoDoCliente>
{
    public void Configure(EntityTypeBuilder<SolicitacaoDoCliente> b)
    {
        b.ToTable("solicitacoes_do_cliente", t =>
            t.HasCheckConstraint("ck_solicitacoes_urgencia", "urgencia IN (1, 2, 3)"));

        b.Property(s => s.Descricao).HasMaxLength(SolicitacaoDoCliente.TamanhoMaximoDescricao).IsRequired();
        b.Property(s => s.Urgencia).HasConversion<int>();
        // xmin é coluna de sistema do PostgreSQL: não cria coluna, só dá o token de concorrência.
        b.Property(s => s.Versao).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        b.Property(s => s.VersaoDosDocumentos).HasDefaultValue(0);

        // A situação é calculada a partir dos campos e nunca é gravada.
        b.Ignore(s => s.Situacao);

        b.HasIndex(s => new { s.ClienteId, s.CriadoEm });
        b.HasIndex(s => s.CriadoEm);
        b.HasIndex(s => s.AtividadeId);

        b.HasOne<Cliente>().WithMany().HasForeignKey(s => s.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(s => s.SolicitanteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Atividade>().WithMany().HasForeignKey(s => s.AtividadeId).OnDelete(DeleteBehavior.Restrict);
    }
}
