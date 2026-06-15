using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dashboard_layouts",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    widgets_json = table.Column<string>(type: "text", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dashboard_layouts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_layouts_empresa_id",
                schema: "gestor",
                table: "dashboard_layouts",
                column: "empresa_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dashboard_layouts",
                schema: "gestor");
        }
    }
}
