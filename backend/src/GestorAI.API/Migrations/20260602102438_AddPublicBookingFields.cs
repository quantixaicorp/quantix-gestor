using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicBookingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorPrimaria",
                table: "ConfiguracoesEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescricaoPublica",
                table: "ConfiguracoesEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "ConfiguracoesEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "ConfiguracoesEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesEmpresa_Slug",
                table: "ConfiguracoesEmpresa",
                column: "Slug",
                unique: true,
                filter: "\"Slug\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConfiguracoesEmpresa_Slug",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "CorPrimaria",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "DescricaoPublica",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "ConfiguracoesEmpresa");
        }
    }
}
