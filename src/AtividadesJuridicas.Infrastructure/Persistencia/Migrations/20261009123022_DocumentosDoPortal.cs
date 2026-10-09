using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class DocumentosDoPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_cliente_obrigatorio",
                table: "documentos");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_cliente_conforme_a_categoria",
                table: "documentos",
                sql: "(categoria IN (1, 2, 4) AND cliente_id IS NOT NULL) OR categoria = 3 OR (categoria = 5 AND cliente_id IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_cliente_conforme_a_categoria",
                table: "documentos");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_cliente_obrigatorio",
                table: "documentos",
                sql: "categoria = 3 OR cliente_id IS NOT NULL");
        }
    }
}
