using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesEmpresa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    RazaoSocial = table.Column<string>(type: "text", nullable: true),
                    NomeFantasia = table.Column<string>(type: "text", nullable: true),
                    Cnpj = table.Column<string>(type: "text", nullable: true),
                    InscricaoEstadual = table.Column<string>(type: "text", nullable: true),
                    InscricaoMunicipal = table.Column<string>(type: "text", nullable: true),
                    Logradouro = table.Column<string>(type: "text", nullable: true),
                    Numero = table.Column<string>(type: "text", nullable: true),
                    Complemento = table.Column<string>(type: "text", nullable: true),
                    Bairro = table.Column<string>(type: "text", nullable: true),
                    CodigoMunicipio = table.Column<string>(type: "text", nullable: true),
                    Municipio = table.Column<string>(type: "text", nullable: true),
                    Uf = table.Column<string>(type: "text", nullable: true),
                    Cep = table.Column<string>(type: "text", nullable: true),
                    RegimeTributario = table.Column<int>(type: "integer", nullable: true),
                    CscId = table.Column<string>(type: "text", nullable: true),
                    CscToken = table.Column<string>(type: "text", nullable: true),
                    Ambiente = table.Column<int>(type: "integer", nullable: true),
                    SerieNfe = table.Column<int>(type: "integer", nullable: true),
                    SerieNfce = table.Column<int>(type: "integer", nullable: true),
                    FocusNfeToken = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesEmpresa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotasFiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Modelo = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: true),
                    Serie = table.Column<int>(type: "integer", nullable: true),
                    ChaveAcesso = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FocusNfeId = table.Column<string>(type: "text", nullable: true),
                    FocusNfeRef = table.Column<string>(type: "text", nullable: true),
                    Protocolo = table.Column<string>(type: "text", nullable: true),
                    ProtocoloCancelamento = table.Column<string>(type: "text", nullable: true),
                    XmlUrl = table.Column<string>(type: "text", nullable: true),
                    PdfUrl = table.Column<string>(type: "text", nullable: true),
                    MensagemErro = table.Column<string>(type: "text", nullable: true),
                    AutorizadaEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CanceladaEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotaFiscalItens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotaFiscalId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeProduto = table.Column<string>(type: "text", nullable: false),
                    Ncm = table.Column<string>(type: "text", nullable: true),
                    Cfop = table.Column<string>(type: "text", nullable: true),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaFiscalItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaFiscalItens_NotasFiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "NotasFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalItens_NotaFiscalId",
                table: "NotaFiscalItens",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_VendaId",
                table: "NotasFiscais",
                column: "VendaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesEmpresa");

            migrationBuilder.DropTable(
                name: "NotaFiscalItens");

            migrationBuilder.DropTable(
                name: "NotasFiscais");
        }
    }
}
