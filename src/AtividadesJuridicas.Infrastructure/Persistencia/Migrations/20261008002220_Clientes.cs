using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Clientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    telefone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    endereco_bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    endereco_cep = table.Column<string>(type: "character(8)", fixedLength: true, maxLength: 8, nullable: true),
                    endereco_cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    endereco_complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    endereco_logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    endereco_numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    endereco_uf = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: true),
                    cpf = table.Column<string>(type: "character(11)", fixedLength: true, maxLength: 11, nullable: true),
                    rg = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    data_nascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    cnpj = table.Column<string>(type: "character(14)", fixedLength: true, maxLength: 14, nullable: true),
                    nome_fantasia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    nome_responsavel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    inscricao_estadual = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    inscricao_municipal = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    excluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clientes_cnpj",
                table: "clientes",
                column: "cnpj",
                unique: true,
                filter: "excluido_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_cpf",
                table: "clientes",
                column: "cpf",
                unique: true,
                filter: "excluido_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_nome",
                table: "clientes",
                column: "nome");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clientes");
        }
    }
}
