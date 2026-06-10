using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingSaaS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "asaas_cliente_id_saa_s",
                table: "configuracoes_empresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "assinatura_asaas_id",
                table: "configuracoes_empresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dominio_customizado",
                table: "configuracoes_empresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "proxima_cobranca_em",
                table: "configuracoes_empresa",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_assinatura",
                table: "configuracoes_empresa",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "planos_saa_s",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 10, 16, 15, 2, 245, DateTimeKind.Utc).AddTicks(7080));

            migrationBuilder.UpdateData(
                table: "planos_saa_s",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 10, 16, 15, 2, 245, DateTimeKind.Utc).AddTicks(7810));

            migrationBuilder.UpdateData(
                table: "planos_saa_s",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 10, 16, 15, 2, 245, DateTimeKind.Utc).AddTicks(7810));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "asaas_cliente_id_saa_s",
                table: "configuracoes_empresa");

            migrationBuilder.DropColumn(
                name: "assinatura_asaas_id",
                table: "configuracoes_empresa");

            migrationBuilder.DropColumn(
                name: "dominio_customizado",
                table: "configuracoes_empresa");

            migrationBuilder.DropColumn(
                name: "proxima_cobranca_em",
                table: "configuracoes_empresa");

            migrationBuilder.DropColumn(
                name: "status_assinatura",
                table: "configuracoes_empresa");

            migrationBuilder.UpdateData(
                table: "planos_saa_s",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 10, 16, 6, 45, 837, DateTimeKind.Utc).AddTicks(120));

            migrationBuilder.UpdateData(
                table: "planos_saa_s",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 10, 16, 6, 45, 837, DateTimeKind.Utc).AddTicks(860));

            migrationBuilder.UpdateData(
                table: "planos_saa_s",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 10, 16, 6, 45, 837, DateTimeKind.Utc).AddTicks(860));
        }
    }
}
