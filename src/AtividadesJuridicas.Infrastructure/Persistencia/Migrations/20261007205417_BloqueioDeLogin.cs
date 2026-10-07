using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class BloqueioDeLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "bloqueado_ate",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "falhas_de_login",
                table: "usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bloqueado_ate",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "falhas_de_login",
                table: "usuarios");
        }
    }
}
