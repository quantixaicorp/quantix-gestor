using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCobrancaAsaasFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AsaasApiKey",
                table: "ConfiguracoesEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AsaasSandbox",
                table: "ConfiguracoesEmpresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AsaasBoletoUrl",
                table: "Cobrancas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AsaasId",
                table: "Cobrancas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AsaasPaymentLink",
                table: "Cobrancas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AsaasPixQrCode",
                table: "Cobrancas",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AsaasApiKey",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "AsaasSandbox",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "AsaasBoletoUrl",
                table: "Cobrancas");

            migrationBuilder.DropColumn(
                name: "AsaasId",
                table: "Cobrancas");

            migrationBuilder.DropColumn(
                name: "AsaasPaymentLink",
                table: "Cobrancas");

            migrationBuilder.DropColumn(
                name: "AsaasPixQrCode",
                table: "Cobrancas");
        }
    }
}
