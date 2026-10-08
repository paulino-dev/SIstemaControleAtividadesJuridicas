using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class DocumentosDeAtividade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "atividade_id",
                table: "documentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_documentos_atividade_id",
                table: "documentos",
                column: "atividade_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_atividade_so_na_categoria_3",
                table: "documentos",
                sql: "(categoria = 3) = (atividade_id IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_documentos_atividades_atividade_id",
                table: "documentos",
                column: "atividade_id",
                principalTable: "atividades",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_documentos_atividades_atividade_id",
                table: "documentos");

            migrationBuilder.DropIndex(
                name: "ix_documentos_atividade_id",
                table: "documentos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_atividade_so_na_categoria_3",
                table: "documentos");

            migrationBuilder.DropColumn(
                name: "atividade_id",
                table: "documentos");
        }
    }
}
