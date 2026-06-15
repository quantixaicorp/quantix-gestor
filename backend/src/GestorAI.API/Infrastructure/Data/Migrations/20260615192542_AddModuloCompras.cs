using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloCompras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "numero_parcela",
                schema: "gestor",
                table: "lancamentos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parcelamento_id",
                schema: "gestor",
                table: "lancamentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inscricao_estadual",
                schema: "gestor",
                table: "fornecedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nome_fantasia",
                schema: "gestor",
                table: "fornecedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "razao_social",
                schema: "gestor",
                table: "fornecedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "gestor",
                table: "fornecedores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "whatsapp",
                schema: "gestor",
                table: "fornecedores",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "pedidos_compra",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    fornecedor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    valor_estimado = table.Column<decimal>(type: "numeric", nullable: false),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedidos_compra", x => x.id);
                    table.ForeignKey(
                        name: "fk_pedidos_compra_fornecedores_fornecedor_id",
                        column: x => x.fornecedor_id,
                        principalSchema: "gestor",
                        principalTable: "fornecedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compras",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    fornecedor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_compra_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo_compra = table.Column<string>(type: "text", nullable: false),
                    numero_nota = table.Column<string>(type: "text", nullable: true),
                    condicao_pagamento = table.Column<string>(type: "text", nullable: false),
                    forma_pagamento = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric", nullable: false),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    criada_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_compras", x => x.id);
                    table.ForeignKey(
                        name: "fk_compras_fornecedores_fornecedor_id",
                        column: x => x.fornecedor_id,
                        principalSchema: "gestor",
                        principalTable: "fornecedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_compras_pedidos_compra_pedido_compra_id",
                        column: x => x.pedido_compra_id,
                        principalSchema: "gestor",
                        principalTable: "pedidos_compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensPedidoCompra",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_compra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_estimado = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itens_pedido_compra", x => x.id);
                    table.ForeignKey(
                        name: "fk_itens_pedido_compra_pedidos_compra_pedido_compra_id",
                        column: x => x.pedido_compra_id,
                        principalSchema: "gestor",
                        principalTable: "pedidos_compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_itens_pedido_compra_produtos_produto_id",
                        column: x => x.produto_id,
                        principalSchema: "gestor",
                        principalTable: "produtos",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ItensCompra",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    compra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    destino_compra = table.Column<int>(type: "integer", nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_unitario = table.Column<decimal>(type: "numeric", nullable: false),
                    desconto = table.Column<decimal>(type: "numeric", nullable: false),
                    frete_rateado = table.Column<decimal>(type: "numeric", nullable: false),
                    impostos = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric", nullable: false),
                    categoria_financeira = table.Column<string>(type: "text", nullable: true),
                    centro_custo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itens_compra", x => x.id);
                    table.ForeignKey(
                        name: "fk_itens_compra_compras_compra_id",
                        column: x => x.compra_id,
                        principalSchema: "gestor",
                        principalTable: "compras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_itens_compra_produtos_produto_id",
                        column: x => x.produto_id,
                        principalSchema: "gestor",
                        principalTable: "produtos",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "parcelamentos",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    compra_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric", nullable: false),
                    qtd_parcelas = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parcelamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_parcelamentos_compras_compra_id",
                        column: x => x.compra_id,
                        principalSchema: "gestor",
                        principalTable: "compras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lancamentos_parcelamento_id",
                schema: "gestor",
                table: "lancamentos",
                column: "parcelamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_compras_fornecedor_id",
                schema: "gestor",
                table: "compras",
                column: "fornecedor_id");

            migrationBuilder.CreateIndex(
                name: "ix_compras_pedido_compra_id",
                schema: "gestor",
                table: "compras",
                column: "pedido_compra_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_compra_compra_id",
                schema: "gestor",
                table: "ItensCompra",
                column: "compra_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_compra_produto_id",
                schema: "gestor",
                table: "ItensCompra",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_pedido_compra_pedido_compra_id",
                schema: "gestor",
                table: "ItensPedidoCompra",
                column: "pedido_compra_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_pedido_compra_produto_id",
                schema: "gestor",
                table: "ItensPedidoCompra",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "ix_parcelamentos_compra_id",
                schema: "gestor",
                table: "parcelamentos",
                column: "compra_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_compra_fornecedor_id",
                schema: "gestor",
                table: "pedidos_compra",
                column: "fornecedor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_lancamentos_parcelamentos_parcelamento_id",
                schema: "gestor",
                table: "lancamentos",
                column: "parcelamento_id",
                principalSchema: "gestor",
                principalTable: "parcelamentos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_lancamentos_parcelamentos_parcelamento_id",
                schema: "gestor",
                table: "lancamentos");

            migrationBuilder.DropTable(
                name: "ItensCompra",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "ItensPedidoCompra",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "parcelamentos",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "compras",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "pedidos_compra",
                schema: "gestor");

            migrationBuilder.DropIndex(
                name: "ix_lancamentos_parcelamento_id",
                schema: "gestor",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "numero_parcela",
                schema: "gestor",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "parcelamento_id",
                schema: "gestor",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "inscricao_estadual",
                schema: "gestor",
                table: "fornecedores");

            migrationBuilder.DropColumn(
                name: "nome_fantasia",
                schema: "gestor",
                table: "fornecedores");

            migrationBuilder.DropColumn(
                name: "razao_social",
                schema: "gestor",
                table: "fornecedores");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "gestor",
                table: "fornecedores");

            migrationBuilder.DropColumn(
                name: "whatsapp",
                schema: "gestor",
                table: "fornecedores");
        }
    }
}
