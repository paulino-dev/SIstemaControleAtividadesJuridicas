using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class AtividadeDaSolicitacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_documentos_chave_armazenamento",
                table: "documentos");

            migrationBuilder.AddColumn<Guid>(
                name: "solicitacao_id",
                table: "atividades",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_documentos_chave_armazenamento",
                table: "documentos",
                column: "chave_armazenamento");

            migrationBuilder.CreateIndex(
                name: "ix_atividades_solicitacao_id",
                table: "atividades",
                column: "solicitacao_id",
                unique: true,
                filter: "excluido_em IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_atividades_solicitacoes_do_cliente_solicitacao_id",
                table: "atividades",
                column: "solicitacao_id",
                principalTable: "solicitacoes_do_cliente",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_atividades_solicitacoes_do_cliente_solicitacao_id",
                table: "atividades");

            migrationBuilder.DropIndex(
                name: "ix_documentos_chave_armazenamento",
                table: "documentos");

            migrationBuilder.DropIndex(
                name: "ix_atividades_solicitacao_id",
                table: "atividades");

            migrationBuilder.DropColumn(
                name: "solicitacao_id",
                table: "atividades");

            // Recriar o índice único falha, e o Down inteiro é desfeito pela transação, se já houver dois documentos
            // com a mesma chave de armazenamento (o da solicitação e o levado para a atividade). Antes de reverter,
            // é preciso tratar essas linhas (spec das solicitações, seção 8).
            migrationBuilder.CreateIndex(
                name: "ix_documentos_chave_armazenamento",
                table: "documentos",
                column: "chave_armazenamento",
                unique: true);
        }
    }
}
