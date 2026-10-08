using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class ClienteConfiguracao : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("clientes");

        // Uma tabela para os dois tipos; a coluna tipo decide a classe.
        b.HasDiscriminator(c => c.Tipo)
            .HasValue<ClientePessoaFisica>(TipoCliente.PessoaFisica)
            .HasValue<ClientePessoaJuridica>(TipoCliente.PessoaJuridica);
        b.Property(c => c.Tipo).HasConversion<int>();

        b.Property(c => c.Nome).HasMaxLength(Cliente.TamanhoMaximoNome).IsRequired();
        b.Property(c => c.Email)
            .HasConversion(e => e!.Valor, v => Email.Criar(v))
            .HasMaxLength(Email.TamanhoMaximo);
        b.Property(c => c.Telefone).HasMaxLength(Cliente.TamanhoMaximoTelefone);
        b.Property(c => c.Observacoes).HasMaxLength(Cliente.TamanhoMaximoObservacoes);

        // Objeto de valor: fica em colunas da própria tabela e é auditado campo a campo.
        b.ComplexProperty(c => c.Endereco, e =>
        {
            e.Property(x => x.Logradouro).HasMaxLength(200);
            e.Property(x => x.Numero).HasMaxLength(20);
            e.Property(x => x.Complemento).HasMaxLength(100);
            e.Property(x => x.Bairro).HasMaxLength(100);
            e.Property(x => x.Cidade).HasMaxLength(100);
            e.Property(x => x.Uf).HasMaxLength(2).IsFixedLength();
            e.Property(x => x.Cep).HasMaxLength(8).IsFixedLength();
        });

        b.HasIndex(c => c.Nome);
    }
}

public sealed class ClientePessoaFisicaConfiguracao : IEntityTypeConfiguration<ClientePessoaFisica>
{
    public void Configure(EntityTypeBuilder<ClientePessoaFisica> b)
    {
        b.Property(c => c.Cpf).HasConversion(c => c.Valor, v => Cpf.Criar(v)).HasMaxLength(11).IsFixedLength();
        b.Property(c => c.Rg).HasMaxLength(ClientePessoaFisica.TamanhoMaximoRg);
        b.HasIndex(c => c.Cpf).IsUnique().HasFilter("excluido_em IS NULL");
    }
}

public sealed class ClientePessoaJuridicaConfiguracao : IEntityTypeConfiguration<ClientePessoaJuridica>
{
    public void Configure(EntityTypeBuilder<ClientePessoaJuridica> b)
    {
        b.Property(c => c.Cnpj).HasConversion(c => c.Valor, v => Cnpj.Criar(v)).HasMaxLength(14).IsFixedLength();
        b.Property(c => c.NomeFantasia).HasMaxLength(Cliente.TamanhoMaximoNome);
        b.Property(c => c.NomeResponsavel).HasMaxLength(Cliente.TamanhoMaximoNome);
        b.Property(c => c.InscricaoEstadual).HasMaxLength(ClientePessoaJuridica.TamanhoMaximoInscricao);
        b.Property(c => c.InscricaoMunicipal).HasMaxLength(ClientePessoaJuridica.TamanhoMaximoInscricao);
        b.HasIndex(c => c.Cnpj).IsUnique().HasFilter("excluido_em IS NULL");
    }
}
