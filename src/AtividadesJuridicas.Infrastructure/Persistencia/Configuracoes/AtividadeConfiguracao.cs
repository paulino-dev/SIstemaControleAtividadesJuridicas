using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class AtividadeConfiguracao : IEntityTypeConfiguration<Atividade>
{
    public void Configure(EntityTypeBuilder<Atividade> b)
    {
        b.ToTable("atividades");

        b.Property(a => a.Titulo).HasMaxLength(Atividade.TamanhoMaximoTitulo).IsRequired();
        b.Property(a => a.Descricao).HasMaxLength(Atividade.TamanhoMaximoDescricao).IsRequired();
        b.Property(a => a.Status).HasConversion<int>();
        b.Property(a => a.TipoAutor).HasConversion<int>();
        b.Property(a => a.DataEntrega).HasColumnType("date");
        // xmin é coluna de sistema do PostgreSQL: não cria coluna, só dá o token de concorrência.
        b.Property(a => a.Versao).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        b.Property(a => a.VersaoDasPartes).HasDefaultValue(0);

        b.HasIndex(a => a.Status);
        b.HasIndex(a => a.DataEntrega);
        b.HasIndex(a => a.ClienteId);
        b.HasIndex(a => a.ProcessoId);

        b.HasOne<Cliente>().WithMany().HasForeignKey(a => a.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Processo>().WithMany().HasForeignKey(a => a.ProcessoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AreaAtuacao>().WithMany().HasForeignKey(a => a.AreaAtuacaoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(a => a.AutorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(a => a.RevisorId).OnDelete(DeleteBehavior.Restrict);

        // As filhas são gravadas pelos campos privados. ClientCascade (sem cascata no banco) faz o EF marcar como
        // excluída a filha retirada da coleção (ou a de uma atividade excluída); o interceptor de carimbos
        // converte essa exclusão em exclusão lógica.
        b.HasMany(a => a.Responsaveis).WithOne().HasForeignKey(r => r.AtividadeId).OnDelete(DeleteBehavior.ClientCascade);
        b.Navigation(a => a.Responsaveis).HasField("_responsaveis").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(a => a.Historico).WithOne().HasForeignKey(h => h.AtividadeId).OnDelete(DeleteBehavior.ClientCascade);
        b.Navigation(a => a.Historico).HasField("_historico").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class AtividadeResponsavelConfiguracao : IEntityTypeConfiguration<AtividadeResponsavel>
{
    public void Configure(EntityTypeBuilder<AtividadeResponsavel> b)
    {
        b.ToTable("atividades_responsaveis");
        b.Property(r => r.Direcionamento).HasMaxLength(AtividadeResponsavel.TamanhoMaximoDirecionamento);

        b.HasOne<Usuario>().WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        // Um usuário não se repete entre os responsáveis vivos da mesma atividade.
        b.HasIndex(r => new { r.AtividadeId, r.UsuarioId }).IsUnique().HasFilter("excluido_em IS NULL");
        b.HasIndex(r => r.UsuarioId);
    }
}

public sealed class AtividadeHistoricoStatusConfiguracao : IEntityTypeConfiguration<AtividadeHistoricoStatus>
{
    public void Configure(EntityTypeBuilder<AtividadeHistoricoStatus> b)
    {
        b.ToTable("atividades_historico_status");
        b.Property(h => h.StatusAnterior).HasConversion<int?>();
        b.Property(h => h.StatusNovo).HasConversion<int>();
        b.Property(h => h.Motivo).HasMaxLength(AtividadeHistoricoStatus.TamanhoMaximoMotivo);

        b.HasOne<Usuario>().WithMany().HasForeignKey(h => h.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        // A ordem identifica a transição dentro da atividade; duas vivas com a mesma ordem seriam histórico corrompido.
        b.HasIndex(h => new { h.AtividadeId, h.Ordem }).IsUnique().HasFilter("excluido_em IS NULL");
    }
}
