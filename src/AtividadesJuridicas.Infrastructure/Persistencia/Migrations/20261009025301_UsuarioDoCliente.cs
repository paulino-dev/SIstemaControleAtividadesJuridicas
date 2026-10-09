using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioDoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "cliente_id",
                table: "usuarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_cliente_id",
                table: "usuarios",
                column: "cliente_id");

            migrationBuilder.AddForeignKey(
                name: "fk_usuarios_clientes_cliente_id",
                table: "usuarios",
                column: "cliente_id",
                principalTable: "clientes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_usuarios_clientes_cliente_id",
                table: "usuarios");

            migrationBuilder.DropIndex(
                name: "ix_usuarios_cliente_id",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "cliente_id",
                table: "usuarios");
        }
    }
}
