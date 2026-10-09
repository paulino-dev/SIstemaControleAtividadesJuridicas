using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class ComplexidadeDaAtividade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // As atividades que já existem recebem complexidade 1. O valor padrão serve só a esse preenchimento e é
            // retirado em seguida: toda atividade nova informa a sua.
            migrationBuilder.AddColumn<int>(
                name: "complexidade",
                table: "atividades",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("ALTER TABLE atividades ALTER COLUMN complexidade DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "complexidade",
                table: "atividades");
        }
    }
}
