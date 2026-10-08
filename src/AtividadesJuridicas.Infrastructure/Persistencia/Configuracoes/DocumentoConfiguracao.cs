using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class DocumentoConfiguracao : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> b)
    {
        // Só o documento de atividade (categoria 3) pode existir sem cliente, e só ele tem atividade.
        b.ToTable("documentos", t =>
        {
            t.HasCheckConstraint("ck_documentos_cliente_obrigatorio", "categoria = 3 OR cliente_id IS NOT NULL");
            t.HasCheckConstraint("ck_documentos_atividade_so_na_categoria_3", "(categoria = 3) = (atividade_id IS NOT NULL)");
        });

        b.Property(d => d.Nome).HasMaxLength(Documento.TamanhoMaximoNome).IsRequired();
        b.Property(d => d.NomeArquivoOriginal).HasMaxLength(Documento.TamanhoMaximoNome).IsRequired();
        b.Property(d => d.TipoConteudo).HasMaxLength(Documento.TamanhoMaximoTipoConteudo).IsRequired();
        b.Property(d => d.HashSha256).HasMaxLength(64).IsFixedLength().IsRequired();
        b.Property(d => d.ChaveArmazenamento).HasMaxLength(Documento.TamanhoMaximoChave).IsRequired();
        b.Property(d => d.Categoria).HasConversion<int>();

        // Cada arquivo do armazenamento pertence a um único documento, mesmo depois de excluído.
        b.HasIndex(d => d.ChaveArmazenamento).IsUnique();
        b.HasIndex(d => new { d.ClienteId, d.Categoria });
        b.HasIndex(d => d.AtividadeId);

        b.HasOne<Cliente>().WithMany().HasForeignKey(d => d.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Atividade>().WithMany().HasForeignKey(d => d.AtividadeId).OnDelete(DeleteBehavior.Restrict);

        // As listas são gravadas pelos campos privados. ClientCascade (sem cascata no banco) faz o EF marcar
        // como excluída a entrada retirada da coleção (ou a de um documento excluído); o interceptor de
        // carimbos converte essa exclusão em exclusão lógica.
        b.HasMany(d => d.PerfisComAcesso).WithOne().HasForeignKey(a => a.DocumentoId).OnDelete(DeleteBehavior.ClientCascade);
        b.Navigation(d => d.PerfisComAcesso).HasField("_perfisComAcesso").UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(d => d.UsuariosComAcesso).WithOne().HasForeignKey(a => a.DocumentoId).OnDelete(DeleteBehavior.ClientCascade);
        b.Navigation(d => d.UsuariosComAcesso).HasField("_usuariosComAcesso").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class DocumentoPerfilAcessoConfiguracao : IEntityTypeConfiguration<DocumentoPerfilAcesso>
{
    public void Configure(EntityTypeBuilder<DocumentoPerfilAcesso> b)
    {
        b.ToTable("documentos_perfis_acesso");

        b.HasOne<Perfil>().WithMany().HasForeignKey(a => a.PerfilId).OnDelete(DeleteBehavior.Restrict);

        // Um perfil não se repete entre as entradas vivas do mesmo documento.
        b.HasIndex(a => new { a.DocumentoId, a.PerfilId }).IsUnique().HasFilter("excluido_em IS NULL");
        b.HasIndex(a => a.PerfilId);
    }
}

public sealed class DocumentoUsuarioAcessoConfiguracao : IEntityTypeConfiguration<DocumentoUsuarioAcesso>
{
    public void Configure(EntityTypeBuilder<DocumentoUsuarioAcesso> b)
    {
        b.ToTable("documentos_usuarios_acesso");

        b.HasOne<Usuario>().WithMany().HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(a => new { a.DocumentoId, a.UsuarioId }).IsUnique().HasFilter("excluido_em IS NULL");
        b.HasIndex(a => a.UsuarioId);
    }
}
