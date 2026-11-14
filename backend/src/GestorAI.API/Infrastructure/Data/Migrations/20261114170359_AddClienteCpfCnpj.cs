using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteCpfCnpj : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cnpj_cpf",
                schema: "gestor",
                table: "customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "person_type",
                schema: "gestor",
                table: "customers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cnpj_cpf",
                schema: "gestor",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "person_type",
                schema: "gestor",
                table: "customers");
        }
    }
}
