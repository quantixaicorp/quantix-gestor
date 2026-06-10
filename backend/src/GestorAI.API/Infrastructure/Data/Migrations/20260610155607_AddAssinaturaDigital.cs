using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssinaturaDigital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "click_sign_doc_key",
                table: "contratos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "click_sign_status",
                table: "contratos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "click_sign_viewer_url",
                table: "contratos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "click_sign_api_key",
                table: "configuracoes_empresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "click_sign_sandbox",
                table: "configuracoes_empresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "click_sign_doc_key",
                table: "contratos");

            migrationBuilder.DropColumn(
                name: "click_sign_status",
                table: "contratos");

            migrationBuilder.DropColumn(
                name: "click_sign_viewer_url",
                table: "contratos");

            migrationBuilder.DropColumn(
                name: "click_sign_api_key",
                table: "configuracoes_empresa");

            migrationBuilder.DropColumn(
                name: "click_sign_sandbox",
                table: "configuracoes_empresa");
        }
    }
}
