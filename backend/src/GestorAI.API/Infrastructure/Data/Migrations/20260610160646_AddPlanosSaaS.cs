using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanosSaaS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "planos_saa_s",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    preco = table.Column<decimal>(type: "numeric", nullable: false),
                    features = table.Column<string>(type: "text", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planos_saa_s", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "empresas_plano",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plano_saa_s_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    fim_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empresas_plano", x => x.id);
                    table.ForeignKey(
                        name: "fk_empresas_plano_planos_saa_s_plano_saa_s_id",
                        column: x => x.plano_saa_s_id,
                        principalTable: "planos_saa_s",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "planos_saa_s",
                columns: new[] { "id", "ativo", "criado_em", "descricao", "features", "nome", "preco" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), true, new DateTime(2026, 6, 10, 16, 6, 45, 837, DateTimeKind.Utc).AddTicks(120), "Gestão essencial para pequenos negócios", "[\"asaas_cobrancas\",\"nota_fiscal\"]", "Básico", 97m },
                    { new Guid("10000000-0000-0000-0000-000000000002"), true, new DateTime(2026, 6, 10, 16, 6, 45, 837, DateTimeKind.Utc).AddTicks(860), "Automações e integrações completas", "[\"asaas_cobrancas\",\"nota_fiscal\",\"automacoes_whatsapp\",\"assinatura_digital\",\"relatorios_avancados\"]", "Profissional", 197m },
                    { new Guid("10000000-0000-0000-0000-000000000003"), true, new DateTime(2026, 6, 10, 16, 6, 45, 837, DateTimeKind.Utc).AddTicks(860), "Multi-profissional, sinal de reserva, tudo incluso", "[\"asaas_cobrancas\",\"nota_fiscal\",\"automacoes_whatsapp\",\"assinatura_digital\",\"relatorios_avancados\",\"sinal_reserva\",\"multi_profissional\"]", "Enterprise", 397m }
                });

            migrationBuilder.CreateIndex(
                name: "ix_empresas_plano_plano_saa_s_id",
                table: "empresas_plano",
                column: "plano_saa_s_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "empresas_plano");

            migrationBuilder.DropTable(
                name: "planos_saa_s");
        }
    }
}
