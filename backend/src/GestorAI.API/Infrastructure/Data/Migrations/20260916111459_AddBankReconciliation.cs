using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBankReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "source",
                schema: "gestor",
                table: "transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "bank_accounts",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    bank_name = table.Column<string>(type: "text", nullable: false),
                    account_number = table.Column<string>(type: "text", nullable: true),
                    agency = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bank_statements",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    format = table.Column<int>(type: "integer", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    imported_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    item_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_statements", x => x.id);
                    table.ForeignKey(
                        name: "fk_bank_statements_bank_accounts_bank_account_id",
                        column: x => x.bank_account_id,
                        principalSchema: "gestor",
                        principalTable: "bank_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bank_statement_items",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_statement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    bank_transaction_id = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_statement_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_bank_statement_items_bank_statements_bank_statement_id",
                        column: x => x.bank_statement_id,
                        principalSchema: "gestor",
                        principalTable: "bank_statements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bank_reconciliations",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_statement_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    confidence_score = table.Column<int>(type: "integer", nullable: false),
                    match_type = table.Column<int>(type: "integer", nullable: false),
                    created_by_import = table.Column<bool>(type: "boolean", nullable: false),
                    reconciled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_reconciliations", x => x.id);
                    table.ForeignKey(
                        name: "fk_bank_reconciliations_bank_statement_items_bank_statement_it",
                        column: x => x.bank_statement_item_id,
                        principalSchema: "gestor",
                        principalTable: "bank_statement_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bank_reconciliations_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalSchema: "gestor",
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bank_reconciliations_bank_statement_item_id",
                schema: "gestor",
                table: "bank_reconciliations",
                column: "bank_statement_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bank_reconciliations_transaction_id",
                schema: "gestor",
                table: "bank_reconciliations",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_items_bank_statement_id",
                schema: "gestor",
                table: "bank_statement_items",
                column: "bank_statement_id");

            migrationBuilder.CreateIndex(
                name: "ix_bank_statements_bank_account_id",
                schema: "gestor",
                table: "bank_statements",
                column: "bank_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bank_reconciliations",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "bank_statement_items",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "bank_statements",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "bank_accounts",
                schema: "gestor");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "gestor",
                table: "transactions");
        }
    }
}
