using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoNegocioEVendaOS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "observacao_os",
                schema: "gestor",
                table: "vendas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "profissional_id",
                schema: "gestor",
                table: "vendas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profissional_nome",
                schema: "gestor",
                table: "vendas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo_negocio",
                schema: "gestor",
                table: "configuracoes_empresa",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "observacao_os",
                schema: "gestor",
                table: "vendas");

            migrationBuilder.DropColumn(
                name: "profissional_id",
                schema: "gestor",
                table: "vendas");

            migrationBuilder.DropColumn(
                name: "profissional_nome",
                schema: "gestor",
                table: "vendas");

            migrationBuilder.DropColumn(
                name: "tipo_negocio",
                schema: "gestor",
                table: "configuracoes_empresa");
        }
    }
}
