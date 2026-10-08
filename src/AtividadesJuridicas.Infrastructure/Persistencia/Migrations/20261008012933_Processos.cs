using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Processos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "processos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_cnj = table.Column<string>(type: "character(20)", fixedLength: true, maxLength: 20, nullable: true),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    area_atuacao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    orgao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    comarca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    parte_contraria = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    data_distribuicao = table.Column<DateOnly>(type: "date", nullable: true),
                    valor_causa = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    excluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processos", x => x.id);
                    table.ForeignKey(
                        name: "fk_processos_areas_atuacao_area_atuacao_id",
                        column: x => x.area_atuacao_id,
                        principalTable: "areas_atuacao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "processos_clientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    processo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    polo = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    excluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processos_clientes", x => x.id);
                    table.ForeignKey(
                        name: "fk_processos_clientes_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_processos_clientes_processos_processo_id",
                        column: x => x.processo_id,
                        principalTable: "processos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_processos_area_atuacao_id",
                table: "processos",
                column: "area_atuacao_id");

            migrationBuilder.CreateIndex(
                name: "ix_processos_numero_cnj",
                table: "processos",
                column: "numero_cnj",
                unique: true,
                filter: "numero_cnj IS NOT NULL AND excluido_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_processos_titulo",
                table: "processos",
                column: "titulo");

            migrationBuilder.CreateIndex(
                name: "ix_processos_clientes_cliente_id",
                table: "processos_clientes",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_processos_clientes_processo_id_cliente_id",
                table: "processos_clientes",
                columns: new[] { "processo_id", "cliente_id" },
                unique: true,
                filter: "excluido_em IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processos_clientes");

            migrationBuilder.DropTable(
                name: "processos");
        }
    }
}
