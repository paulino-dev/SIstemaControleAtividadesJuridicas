using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class IndicesDeAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_registros_auditoria_entidade_id",
                table: "registros_auditoria",
                column: "entidade_id");

            migrationBuilder.CreateIndex(
                name: "ix_registros_auditoria_id_correlacao",
                table: "registros_auditoria",
                column: "id_correlacao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_registros_auditoria_entidade_id",
                table: "registros_auditoria");

            migrationBuilder.DropIndex(
                name: "ix_registros_auditoria_id_correlacao",
                table: "registros_auditoria");
        }
    }
}
