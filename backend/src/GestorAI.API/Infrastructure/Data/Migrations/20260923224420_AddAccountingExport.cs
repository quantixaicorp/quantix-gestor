using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingExport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "default_cash_account_id",
                schema: "gestor",
                table: "company_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "preferred_accounting_system",
                schema: "gestor",
                table: "company_settings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "chart_of_accounts",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chart_of_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_chart_of_accounts_chart_of_accounts_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "gestor",
                        principalTable: "chart_of_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "account_mappings",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_name = table.Column<string>(type: "text", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_account_mappings_chart_of_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "gestor",
                        principalTable: "chart_of_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_mappings_account_id",
                schema: "gestor",
                table: "account_mappings",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_mappings_company_id_category_name",
                schema: "gestor",
                table: "account_mappings",
                columns: new[] { "company_id", "category_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chart_of_accounts_company_id_code",
                schema: "gestor",
                table: "chart_of_accounts",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chart_of_accounts_parent_id",
                schema: "gestor",
                table: "chart_of_accounts",
                column: "parent_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_mappings",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "chart_of_accounts",
                schema: "gestor");

            migrationBuilder.DropColumn(
                name: "default_cash_account_id",
                schema: "gestor",
                table: "company_settings");

            migrationBuilder.DropColumn(
                name: "preferred_accounting_system",
                schema: "gestor",
                table: "company_settings");
        }
    }
}
