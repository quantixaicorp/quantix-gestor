using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomacaoLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvolutionApiKey",
                table: "ConfiguracoesEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvolutionApiUrl",
                table: "ConfiguracoesEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvolutionInstance",
                table: "ConfiguracoesEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Lembrete1dAntes",
                table: "ConfiguracoesEmpresa",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Lembrete1dDepois",
                table: "ConfiguracoesEmpresa",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Lembrete3dAntes",
                table: "ConfiguracoesEmpresa",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Lembrete3dDepois",
                table: "ConfiguracoesEmpresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Lembrete7dDepois",
                table: "ConfiguracoesEmpresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LembreteNoDia",
                table: "ConfiguracoesEmpresa",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "AutomacaoLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CobrancaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoEvento = table.Column<int>(type: "integer", nullable: false),
                    EnviadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    ErroMsg = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomacaoLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomacaoLogs_Cobrancas_CobrancaId",
                        column: x => x.CobrancaId,
                        principalTable: "Cobrancas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomacaoLogs_CobrancaId_TipoEvento",
                table: "AutomacaoLogs",
                columns: new[] { "CobrancaId", "TipoEvento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomacaoLogs");

            migrationBuilder.DropColumn(
                name: "EvolutionApiKey",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "EvolutionApiUrl",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "EvolutionInstance",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "Lembrete1dAntes",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "Lembrete1dDepois",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "Lembrete3dAntes",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "Lembrete3dDepois",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "Lembrete7dDepois",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "LembreteNoDia",
                table: "ConfiguracoesEmpresa");
        }
    }
}
