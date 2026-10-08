using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Atividades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "atividades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    data_entrega = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    concluida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    area_atuacao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo_autor = table.Column<int>(type: "integer", nullable: false),
                    autor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revisor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    versao_das_partes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    excluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atividades", x => x.id);
                    table.ForeignKey(
                        name: "fk_atividades_areas_atuacao_area_atuacao_id",
                        column: x => x.area_atuacao_id,
                        principalTable: "areas_atuacao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_atividades_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_atividades_processos_processo_id",
                        column: x => x.processo_id,
                        principalTable: "processos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_atividades_usuarios_autor_id",
                        column: x => x.autor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_atividades_usuarios_revisor_id",
                        column: x => x.revisor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "atividades_historico_status",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    atividade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    status_anterior = table.Column<int>(type: "integer", nullable: true),
                    status_novo = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    excluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atividades_historico_status", x => x.id);
                    table.ForeignKey(
                        name: "fk_atividades_historico_status_atividades_atividade_id",
                        column: x => x.atividade_id,
                        principalTable: "atividades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_atividades_historico_status_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "atividades_responsaveis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    atividade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direcionamento = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    concluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    excluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atividades_responsaveis", x => x.id);
                    table.ForeignKey(
                        name: "fk_atividades_responsaveis_atividades_atividade_id",
                        column: x => x.atividade_id,
                        principalTable: "atividades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_atividades_responsaveis_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_atividades_area_atuacao_id",
                table: "atividades",
                column: "area_atuacao_id");

            migrationBuilder.CreateIndex(
                name: "ix_atividades_autor_id",
                table: "atividades",
                column: "autor_id");

            migrationBuilder.CreateIndex(
                name: "ix_atividades_cliente_id",
                table: "atividades",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_atividades_data_entrega",
                table: "atividades",
                column: "data_entrega");

            migrationBuilder.CreateIndex(
                name: "ix_atividades_processo_id",
                table: "atividades",
                column: "processo_id");

            migrationBuilder.CreateIndex(
                name: "ix_atividades_revisor_id",
                table: "atividades",
                column: "revisor_id");

            migrationBuilder.CreateIndex(
                name: "ix_atividades_status",
                table: "atividades",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_atividades_historico_status_atividade_id_ordem",
                table: "atividades_historico_status",
                columns: new[] { "atividade_id", "ordem" },
                unique: true,
                filter: "excluido_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_atividades_historico_status_usuario_id",
                table: "atividades_historico_status",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_atividades_responsaveis_atividade_id_usuario_id",
                table: "atividades_responsaveis",
                columns: new[] { "atividade_id", "usuario_id" },
                unique: true,
                filter: "excluido_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_atividades_responsaveis_usuario_id",
                table: "atividades_responsaveis",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "atividades_historico_status");

            migrationBuilder.DropTable(
                name: "atividades_responsaveis");

            migrationBuilder.DropTable(
                name: "atividades");
        }
    }
}
