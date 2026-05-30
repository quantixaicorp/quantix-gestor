using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Migrations
{
    /// <inheritdoc />
    public partial class FixClienteFkRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename ContratoItens table (DbSet removed, EF now uses entity class name)
            migrationBuilder.DropForeignKey(
                name: "FK_ContratoItens_Contratos_ContratoId",
                table: "ContratoItens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContratoItens",
                table: "ContratoItens");

            migrationBuilder.RenameTable(
                name: "ContratoItens",
                newName: "ContratoItem");

            migrationBuilder.RenameIndex(
                name: "IX_ContratoItens_ContratoId",
                table: "ContratoItem",
                newName: "IX_ContratoItem_ContratoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContratoItem",
                table: "ContratoItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContratoItem_Contratos_ContratoId",
                table: "ContratoItem",
                column: "ContratoId",
                principalTable: "Contratos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Fix Contratos -> Clientes FK: Cascade -> Restrict
            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_Clientes_ClienteId",
                table: "Contratos");

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_Clientes_ClienteId",
                table: "Contratos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Fix Cobrancas -> Clientes FK: Cascade -> Restrict
            migrationBuilder.DropForeignKey(
                name: "FK_Cobrancas_Clientes_ClienteId",
                table: "Cobrancas");

            migrationBuilder.AddForeignKey(
                name: "FK_Cobrancas_Clientes_ClienteId",
                table: "Cobrancas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert Cobrancas -> Clientes FK: Restrict -> Cascade
            migrationBuilder.DropForeignKey(
                name: "FK_Cobrancas_Clientes_ClienteId",
                table: "Cobrancas");

            migrationBuilder.AddForeignKey(
                name: "FK_Cobrancas_Clientes_ClienteId",
                table: "Cobrancas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Revert Contratos -> Clientes FK: Restrict -> Cascade
            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_Clientes_ClienteId",
                table: "Contratos");

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_Clientes_ClienteId",
                table: "Contratos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Revert table rename: ContratoItem -> ContratoItens
            migrationBuilder.DropForeignKey(
                name: "FK_ContratoItem_Contratos_ContratoId",
                table: "ContratoItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContratoItem",
                table: "ContratoItem");

            migrationBuilder.RenameTable(
                name: "ContratoItem",
                newName: "ContratoItens");

            migrationBuilder.RenameIndex(
                name: "IX_ContratoItem_ContratoId",
                table: "ContratoItens",
                newName: "IX_ContratoItens_ContratoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContratoItens",
                table: "ContratoItens",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContratoItens_Contratos_ContratoId",
                table: "ContratoItens",
                column: "ContratoId",
                principalTable: "Contratos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
