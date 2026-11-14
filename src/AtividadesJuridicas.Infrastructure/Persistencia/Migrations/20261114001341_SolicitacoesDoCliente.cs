using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class SolicitacoesDoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_cliente_conforme_a_categoria",
                table: "documentos");

            migrationBuilder.AddColumn<Guid>(
                name: "solicitacao_id",
                table: "documentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "solicitacoes_do_cliente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    solicitante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    urgencia = table.Column<int>(type: "integer", nullable: false),
                    cancelada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atividade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    versao_dos_documentos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    excluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solicitacoes_do_cliente", x => x.id);
                    table.CheckConstraint("ck_solicitacoes_urgencia", "urgencia IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "fk_solicitacoes_do_cliente_atividades_atividade_id",
                        column: x => x.atividade_id,
                        principalTable: "atividades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_solicitacoes_do_cliente_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_solicitacoes_do_cliente_usuarios_solicitante_id",
                        column: x => x.solicitante_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_documentos_solicitacao_id",
                table: "documentos",
                column: "solicitacao_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_cliente_conforme_a_categoria",
                table: "documentos",
                sql: "(categoria IN (1, 2, 4, 6) AND cliente_id IS NOT NULL) OR categoria = 3 OR (categoria = 5 AND cliente_id IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_solicitacao_so_na_categoria_6",
                table: "documentos",
                sql: "(categoria = 6) = (solicitacao_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_do_cliente_atividade_id",
                table: "solicitacoes_do_cliente",
                column: "atividade_id");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_do_cliente_cliente_id_criado_em",
                table: "solicitacoes_do_cliente",
                columns: new[] { "cliente_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_do_cliente_criado_em",
                table: "solicitacoes_do_cliente",
                column: "criado_em");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_do_cliente_solicitante_id",
                table: "solicitacoes_do_cliente",
                column: "solicitante_id");

            migrationBuilder.AddForeignKey(
                name: "fk_documentos_solicitacoes_do_cliente_solicitacao_id",
                table: "documentos",
                column: "solicitacao_id",
                principalTable: "solicitacoes_do_cliente",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_documentos_solicitacoes_do_cliente_solicitacao_id",
                table: "documentos");

            migrationBuilder.DropTable(
                name: "solicitacoes_do_cliente");

            migrationBuilder.DropIndex(
                name: "ix_documentos_solicitacao_id",
                table: "documentos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_cliente_conforme_a_categoria",
                table: "documentos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_solicitacao_so_na_categoria_6",
                table: "documentos");

            migrationBuilder.DropColumn(
                name: "solicitacao_id",
                table: "documentos");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_cliente_conforme_a_categoria",
                table: "documentos",
                sql: "(categoria IN (1, 2, 4) AND cliente_id IS NOT NULL) OR categoria = 3 OR (categoria = 5 AND cliente_id IS NULL)");
        }
    }
}
