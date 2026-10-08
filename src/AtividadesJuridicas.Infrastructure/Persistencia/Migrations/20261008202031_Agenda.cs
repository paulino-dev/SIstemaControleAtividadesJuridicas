using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtividadesJuridicas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Agenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    local = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    inicio_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duracao_estimada_minutos = table.Column<int>(type: "integer", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    atividade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    versao_dos_participantes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    excluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eventos", x => x.id);
                    table.ForeignKey(
                        name: "fk_eventos_atividades_atividade_id",
                        column: x => x.atividade_id,
                        principalTable: "atividades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_eventos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_eventos_processos_processo_id",
                        column: x => x.processo_id,
                        principalTable: "processos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_eventos_usuarios_criador_id",
                        column: x => x.criador_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eventos_participantes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    excluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eventos_participantes", x => x.id);
                    table.ForeignKey(
                        name: "fk_eventos_participantes_eventos_evento_id",
                        column: x => x.evento_id,
                        principalTable: "eventos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_eventos_participantes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_eventos_atividade_id",
                table: "eventos",
                column: "atividade_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_cliente_id",
                table: "eventos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_criador_id",
                table: "eventos",
                column: "criador_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_inicio_em",
                table: "eventos",
                column: "inicio_em");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_processo_id",
                table: "eventos",
                column: "processo_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_participantes_evento_id_usuario_id",
                table: "eventos_participantes",
                columns: new[] { "evento_id", "usuario_id" },
                unique: true,
                filter: "excluido_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_participantes_usuario_id",
                table: "eventos_participantes",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_participantes");

            migrationBuilder.DropTable(
                name: "eventos");
        }
    }
}
