using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Auditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registros_auditoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operacao = table.Column<int>(type: "integer", nullable: false),
                    entidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entidade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dados = table.Column<string>(type: "jsonb", nullable: true),
                    ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    id_correlacao = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registros_auditoria", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registros_auditoria_data_hora",
                table: "registros_auditoria",
                column: "data_hora");

            migrationBuilder.CreateIndex(
                name: "ix_registros_auditoria_entidade_entidade_id",
                table: "registros_auditoria",
                columns: new[] { "entidade", "entidade_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registros_auditoria_usuario_id",
                table: "registros_auditoria",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registros_auditoria");
        }
    }
}
