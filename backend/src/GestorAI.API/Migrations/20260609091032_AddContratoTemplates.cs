using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddContratoTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContratoTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Objeto = table.Column<string>(type: "text", nullable: false),
                    TipoCobranca = table.Column<int>(type: "integer", nullable: false),
                    Periodicidade = table.Column<int>(type: "integer", nullable: false),
                    DiaVencimento = table.Column<int>(type: "integer", nullable: false),
                    ValorPadrao = table.Column<decimal>(type: "numeric", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratoTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContratoTemplateItens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContratoTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratoTemplateItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContratoTemplateItens_ContratoTemplates_ContratoTemplateId",
                        column: x => x.ContratoTemplateId,
                        principalTable: "ContratoTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContratoTemplateItens_ContratoTemplateId",
                table: "ContratoTemplateItens",
                column: "ContratoTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContratoTemplateItens");

            migrationBuilder.DropTable(
                name: "ContratoTemplates");
        }
    }
}
