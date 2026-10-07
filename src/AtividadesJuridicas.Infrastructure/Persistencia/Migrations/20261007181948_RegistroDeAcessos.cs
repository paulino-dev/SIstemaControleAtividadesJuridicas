using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class RegistroDeAcessos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registros_acesso",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_recurso = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    recurso_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acao = table.Column<int>(type: "integer", nullable: false),
                    detalhe = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registros_acesso", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registros_acesso_data_hora",
                table: "registros_acesso",
                column: "data_hora");

            migrationBuilder.CreateIndex(
                name: "ix_registros_acesso_tipo_recurso_recurso_id",
                table: "registros_acesso",
                columns: new[] { "tipo_recurso", "recurso_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registros_acesso_usuario_id",
                table: "registros_acesso",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registros_acesso");
        }
    }
}
