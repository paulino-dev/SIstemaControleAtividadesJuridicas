using AtividadesJuridicas.Domain.Cadastros;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class ProcessoConfiguracao : IEntityTypeConfiguration<Processo>
{
    public void Configure(EntityTypeBuilder<Processo> b)
    {
        b.ToTable("processos");

        b.Property(p => p.NumeroCnj)
            .HasConversion(n => n!.Valor, v => NumeroCnj.Criar(v))
            .HasMaxLength(20).IsFixedLength();
        b.Property(p => p.Titulo).HasMaxLength(Processo.TamanhoMaximoTitulo).IsRequired();
        b.Property(p => p.Status).HasConversion<int>();
        b.Property(p => p.Orgao).HasMaxLength(Processo.TamanhoMaximoOrgao);
        b.Property(p => p.Comarca).HasMaxLength(Processo.TamanhoMaximoComarca);
        b.Property(p => p.ParteContraria).HasMaxLength(Processo.TamanhoMaximoParteContraria);
        b.Property(p => p.ValorCausa).HasPrecision(18, 2);
        // xmin é coluna de sistema do PostgreSQL: não cria coluna, só dá o token de concorrência.
        b.Property(p => p.Versao).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        b.Property(p => p.VersaoDosVinculos).HasDefaultValue(0);
        b.Property(p => p.Observacoes).HasMaxLength(Processo.TamanhoMaximoObservacoes);

        // Único só entre os processos que têm número e não foram excluídos.
        b.HasIndex(p => p.NumeroCnj).IsUnique().HasFilter("numero_cnj IS NOT NULL AND excluido_em IS NULL");
        b.HasIndex(p => p.Titulo);

        b.HasOne<AreaAtuacao>().WithMany().HasForeignKey(p => p.AreaAtuacaoId).OnDelete(DeleteBehavior.Restrict);

        // Os vínculos são gravados pelo campo privado da coleção. O comportamento ClientCascade (sem cascata no banco) faz o EF
        // marcar como excluído o vínculo retirado da coleção (ou o de um processo excluído); o
        // interceptor de carimbos converte essa exclusão em exclusão lógica.
        b.HasMany(p => p.Clientes).WithOne().HasForeignKey(v => v.ProcessoId).OnDelete(DeleteBehavior.ClientCascade);
        b.Navigation(p => p.Clientes).HasField("_clientes").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProcessoClienteConfiguracao : IEntityTypeConfiguration<ProcessoCliente>
{
    public void Configure(EntityTypeBuilder<ProcessoCliente> b)
    {
        b.ToTable("processos_clientes");
        b.Property(v => v.Polo).HasConversion<int>();

        b.HasOne<Cliente>().WithMany().HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.Restrict);

        // Um cliente não se repete entre os vínculos vivos do mesmo processo.
        b.HasIndex(v => new { v.ProcessoId, v.ClienteId }).IsUnique().HasFilter("excluido_em IS NULL");
        b.HasIndex(v => v.ClienteId);
    }
}
