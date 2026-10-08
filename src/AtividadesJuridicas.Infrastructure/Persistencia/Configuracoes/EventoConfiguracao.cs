using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class EventoConfiguracao : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> b)
    {
        b.ToTable("eventos");

        b.Property(e => e.Titulo).HasMaxLength(Evento.TamanhoMaximoTitulo).IsRequired();
        b.Property(e => e.Descricao).HasMaxLength(Evento.TamanhoMaximoDescricao);
        b.Property(e => e.Local).HasMaxLength(Evento.TamanhoMaximoLocal);
        b.Property(e => e.Tipo).HasConversion<int>();
        // O fim é calculado (início + duração) e não é gravado.
        b.Ignore(e => e.FimEm);
        // xmin é coluna de sistema do PostgreSQL: não cria coluna, só dá o token de concorrência.
        b.Property(e => e.Versao).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        b.Property(e => e.VersaoDosParticipantes).HasDefaultValue(0);

        b.HasIndex(e => e.InicioEm);

        b.HasOne<Cliente>().WithMany().HasForeignKey(e => e.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Processo>().WithMany().HasForeignKey(e => e.ProcessoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Atividade>().WithMany().HasForeignKey(e => e.AtividadeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(e => e.CriadorId).OnDelete(DeleteBehavior.Restrict);

        // As filhas são gravadas pelo campo privado. ClientCascade (sem cascata no banco) faz o EF marcar como
        // excluída a filha retirada da coleção (ou a de um evento excluído); o interceptor de carimbos converte
        // essa exclusão em exclusão lógica.
        b.HasMany(e => e.Participantes).WithOne().HasForeignKey(p => p.EventoId).OnDelete(DeleteBehavior.ClientCascade);
        b.Navigation(e => e.Participantes).HasField("_participantes").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class EventoParticipanteConfiguracao : IEntityTypeConfiguration<EventoParticipante>
{
    public void Configure(EntityTypeBuilder<EventoParticipante> b)
    {
        b.ToTable("eventos_participantes");

        b.HasOne<Usuario>().WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        // Um usuário não se repete entre os participantes vivos do mesmo evento.
        b.HasIndex(p => new { p.EventoId, p.UsuarioId }).IsUnique().HasFilter("excluido_em IS NULL");
        b.HasIndex(p => p.UsuarioId);
    }
}
