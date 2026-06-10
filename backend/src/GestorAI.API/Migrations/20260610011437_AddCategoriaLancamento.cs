using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaLancamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias_lancamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categorias_lancamento", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categorias_lancamento_empresa_id_tipo_nome",
                table: "categorias_lancamento",
                columns: new[] { "empresa_id", "tipo", "nome" },
                unique: true);

            migrationBuilder.Sql(@"
    INSERT INTO categorias_lancamento (id, empresa_id, nome, tipo)
    SELECT gen_random_uuid(), l.empresa_id, cat.nome, cat.tipo_val
    FROM (SELECT DISTINCT empresa_id FROM lancamentos) l
    CROSS JOIN (VALUES
        ('Aluguel',    1),
        ('Fornecedor', 1),
        ('Utilidades', 1),
        ('Salários',   1),
        ('Marketing',  1),
        ('Outros',     1),
        ('Venda',      0),
        ('Serviço',    0),
        ('Outros',     0)
    ) AS cat(nome, tipo_val)
    ON CONFLICT (empresa_id, tipo, nome) DO NOTHING;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categorias_lancamento");
        }
    }
}
