using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CS8632 // nullable reference types annotation

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMotorAssinaturas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "empresas_plano",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "planos_saa_s",
                schema: "gestor");

            migrationBuilder.DropColumn(
                name: "asaas_cliente_id_saa_s",
                schema: "gestor",
                table: "configuracoes_empresa");

            migrationBuilder.DropColumn(
                name: "assinatura_asaas_id",
                schema: "gestor",
                table: "configuracoes_empresa");

            migrationBuilder.DropColumn(
                name: "proxima_cobranca_em",
                schema: "gestor",
                table: "configuracoes_empresa");

            migrationBuilder.DropColumn(
                name: "status_assinatura",
                schema: "gestor",
                table: "configuracoes_empresa");

            migrationBuilder.AddColumn<Guid>(
                name: "assinatura_cliente_id",
                schema: "gestor",
                table: "contratos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "nicho_templates",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nicho = table.Column<string>(type: "text", nullable: false),
                    nome_plano = table.Column<string>(type: "text", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    preco_sugerido = table.Column<decimal>(type: "numeric", nullable: false),
                    mais_vendido = table.Column<bool>(type: "boolean", nullable: false),
                    periodicidade = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nicho_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "planos_assinatura",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    nicho = table.Column<string>(type: "text", nullable: false),
                    preco = table.Column<decimal>(type: "numeric", nullable: false),
                    periodicidade = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    mais_vendido = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planos_assinatura", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "NichoTemplateItens",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nicho_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    quantidade_por_ciclo = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    percentual_desconto = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nicho_template_itens", x => x.id);
                    table.ForeignKey(
                        name: "fk_nicho_template_itens_nicho_templates_nicho_template_id",
                        column: x => x.nicho_template_id,
                        principalSchema: "gestor",
                        principalTable: "nicho_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assinaturas_cliente",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plano_assinatura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    data_renovacao = table.Column<DateOnly>(type: "date", nullable: false),
                    ciclo_atual = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assinaturas_cliente", x => x.id);
                    table.ForeignKey(
                        name: "fk_assinaturas_cliente_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "gestor",
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assinaturas_cliente_contratos_contrato_id",
                        column: x => x.contrato_id,
                        principalSchema: "gestor",
                        principalTable: "contratos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assinaturas_cliente_planos_assinatura_plano_assinatura_id",
                        column: x => x.plano_assinatura_id,
                        principalSchema: "gestor",
                        principalTable: "planos_assinatura",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanoAssinaturaItens",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plano_assinatura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    servico_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantidade_por_ciclo = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    percentual_desconto = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plano_assinatura_itens", x => x.id);
                    table.ForeignKey(
                        name: "fk_plano_assinatura_itens_planos_assinatura_plano_assinatura_id",
                        column: x => x.plano_assinatura_id,
                        principalSchema: "gestor",
                        principalTable: "planos_assinatura",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assinaturas_cliente_cliente_id",
                schema: "gestor",
                table: "assinaturas_cliente",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_assinaturas_cliente_contrato_id",
                schema: "gestor",
                table: "assinaturas_cliente",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "ix_assinaturas_cliente_plano_assinatura_id",
                schema: "gestor",
                table: "assinaturas_cliente",
                column: "plano_assinatura_id");

            migrationBuilder.CreateIndex(
                name: "ix_nicho_template_itens_nicho_template_id",
                schema: "gestor",
                table: "NichoTemplateItens",
                column: "nicho_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_plano_assinatura_itens_plano_assinatura_id",
                schema: "gestor",
                table: "PlanoAssinaturaItens",
                column: "plano_assinatura_id");

            // Seed NichoTemplates
            var barbearia1 = Guid.Parse("a1000001-0000-0000-0000-000000000001");
            var barbearia2 = Guid.Parse("a1000001-0000-0000-0000-000000000002");
            var barbearia3 = Guid.Parse("a1000001-0000-0000-0000-000000000003");
            var salao1 = Guid.Parse("a2000001-0000-0000-0000-000000000001");
            var salao2 = Guid.Parse("a2000001-0000-0000-0000-000000000002");
            var salao3 = Guid.Parse("a2000001-0000-0000-0000-000000000003");
            var estetica1 = Guid.Parse("a3000001-0000-0000-0000-000000000001");
            var estetica2 = Guid.Parse("a3000001-0000-0000-0000-000000000002");
            var estetica3 = Guid.Parse("a3000001-0000-0000-0000-000000000003");
            var pet1 = Guid.Parse("a4000001-0000-0000-0000-000000000001");
            var pet2 = Guid.Parse("a4000001-0000-0000-0000-000000000002");
            var pet3 = Guid.Parse("a4000001-0000-0000-0000-000000000003");
            var pt1 = Guid.Parse("a5000001-0000-0000-0000-000000000001");
            var pt2 = Guid.Parse("a5000001-0000-0000-0000-000000000002");
            var pt3 = Guid.Parse("a5000001-0000-0000-0000-000000000003");

            migrationBuilder.InsertData(
                table: "nicho_templates",
                schema: "gestor",
                columns: new[] { "id", "nicho", "nome_plano", "descricao", "preco_sugerido", "mais_vendido", "periodicidade" },
                values: new object[,]
                {
                    { barbearia1, "Barbearia", "Básico", "2 cortes por mês", 79m, false, 0 },
                    { barbearia2, "Barbearia", "Premium", "4 cortes + 1 barba/mês + 10% desc. produtos", 129m, true, 0 },
                    { barbearia3, "Barbearia", "VIP", "Cortes ilimitados + 2 barbas + 1 hidratação + horário exclusivo", 199m, false, 0 },
                    { salao1, "Salão", "Mãos & Pés", "2 manicures + 2 pedicures/mês", 99m, false, 0 },
                    { salao2, "Salão", "Beleza Completa", "4 manicures + 4 pedicures + 1 escova/mês", 189m, true, 0 },
                    { salao3, "Salão", "Noiva & Premium", "Ilimitado mãos e pés + 2 escovas + 1 hidratação/mês", 299m, false, 0 },
                    { estetica1, "Estética", "Pele", "2 limpezas de pele/mês", 149m, false, 0 },
                    { estetica2, "Estética", "Corpo & Rosto", "4 procedimentos variados/mês", 249m, true, 0 },
                    { estetica3, "Estética", "Premium Spa", "8 procedimentos + drenagem + massagem/mês", 399m, false, 0 },
                    { pet1, "Pet Shop", "Porte Pequeno", "2 banhos + 1 tosa/mês", 89m, false, 0 },
                    { pet2, "Pet Shop", "Porte Médio", "2 banhos + 1 tosa/mês", 139m, true, 0 },
                    { pet3, "Pet Shop", "Porte Grande", "2 banhos + 1 tosa/mês", 199m, false, 0 },
                    { pt1, "Personal Trainer", "2x/semana", "8 sessões presenciais/mês", 299m, false, 0 },
                    { pt2, "Personal Trainer", "3x/semana", "12 sessões presenciais/mês", 419m, true, 0 },
                    { pt3, "Personal Trainer", "Diário + Online", "20 sessões + acompanhamento online ilimitado/mês", 599m, false, 0 },
                });

            migrationBuilder.InsertData(
                table: "NichoTemplateItens",
                schema: "gestor",
                columns: new[] { "id", "nicho_template_id", "descricao", "quantidade_por_ciclo", "tipo", "percentual_desconto" },
                values: new object?[,]
                {
                    { Guid.NewGuid(), barbearia1, "Corte de cabelo", 2, 0, null },
                    { Guid.NewGuid(), barbearia2, "Corte de cabelo", 4, 0, null },
                    { Guid.NewGuid(), barbearia2, "Barba", 1, 0, null },
                    { Guid.NewGuid(), barbearia2, "Desconto em produtos", 0, 1, 10m },
                    { Guid.NewGuid(), barbearia3, "Corte de cabelo", 0, 0, null },
                    { Guid.NewGuid(), barbearia3, "Barba", 2, 0, null },
                    { Guid.NewGuid(), barbearia3, "Hidratação capilar", 1, 0, null },
                    { Guid.NewGuid(), barbearia3, "Horário exclusivo", 0, 2, null },
                    { Guid.NewGuid(), salao1, "Manicure", 2, 0, null },
                    { Guid.NewGuid(), salao1, "Pedicure", 2, 0, null },
                    { Guid.NewGuid(), salao2, "Manicure", 4, 0, null },
                    { Guid.NewGuid(), salao2, "Pedicure", 4, 0, null },
                    { Guid.NewGuid(), salao2, "Escova", 1, 0, null },
                    { Guid.NewGuid(), salao3, "Manicure", 0, 0, null },
                    { Guid.NewGuid(), salao3, "Pedicure", 0, 0, null },
                    { Guid.NewGuid(), salao3, "Escova", 2, 0, null },
                    { Guid.NewGuid(), salao3, "Hidratação capilar", 1, 0, null },
                    { Guid.NewGuid(), estetica1, "Limpeza de pele", 2, 0, null },
                    { Guid.NewGuid(), estetica2, "Procedimentos estéticos", 4, 0, null },
                    { Guid.NewGuid(), estetica3, "Procedimentos premium", 8, 0, null },
                    { Guid.NewGuid(), estetica3, "Drenagem linfática", 1, 0, null },
                    { Guid.NewGuid(), estetica3, "Massagem relaxante", 1, 0, null },
                    { Guid.NewGuid(), pet1, "Banho", 2, 0, null },
                    { Guid.NewGuid(), pet1, "Tosa", 1, 0, null },
                    { Guid.NewGuid(), pet2, "Banho", 2, 0, null },
                    { Guid.NewGuid(), pet2, "Tosa", 1, 0, null },
                    { Guid.NewGuid(), pet3, "Banho", 2, 0, null },
                    { Guid.NewGuid(), pet3, "Tosa", 1, 0, null },
                    { Guid.NewGuid(), pt1, "Sessão presencial", 8, 0, null },
                    { Guid.NewGuid(), pt2, "Sessão presencial", 12, 0, null },
                    { Guid.NewGuid(), pt3, "Sessão presencial", 20, 0, null },
                    { Guid.NewGuid(), pt3, "Acompanhamento online", 0, 2, null },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assinaturas_cliente",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "NichoTemplateItens",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "PlanoAssinaturaItens",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "nicho_templates",
                schema: "gestor");

            migrationBuilder.DropTable(
                name: "planos_assinatura",
                schema: "gestor");

            migrationBuilder.DropColumn(
                name: "assinatura_cliente_id",
                schema: "gestor",
                table: "contratos");

            migrationBuilder.AddColumn<string>(
                name: "asaas_cliente_id_saa_s",
                schema: "gestor",
                table: "configuracoes_empresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "assinatura_asaas_id",
                schema: "gestor",
                table: "configuracoes_empresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "proxima_cobranca_em",
                schema: "gestor",
                table: "configuracoes_empresa",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_assinatura",
                schema: "gestor",
                table: "configuracoes_empresa",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "planos_saa_s",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    features = table.Column<string>(type: "text", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    preco = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planos_saa_s", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "empresas_plano",
                schema: "gestor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plano_saa_s_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fim_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    inicio_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empresas_plano", x => x.id);
                    table.ForeignKey(
                        name: "fk_empresas_plano_planos_saa_s_plano_saa_s_id",
                        column: x => x.plano_saa_s_id,
                        principalSchema: "gestor",
                        principalTable: "planos_saa_s",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "gestor",
                table: "planos_saa_s",
                columns: new[] { "id", "ativo", "criado_em", "descricao", "features", "nome", "preco" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), true, new DateTime(2026, 6, 10, 16, 15, 2, 245, DateTimeKind.Utc).AddTicks(7080), "Gestão essencial para pequenos negócios", "[\"asaas_cobrancas\",\"nota_fiscal\"]", "Básico", 97m },
                    { new Guid("10000000-0000-0000-0000-000000000002"), true, new DateTime(2026, 6, 10, 16, 15, 2, 245, DateTimeKind.Utc).AddTicks(7810), "Automações e integrações completas", "[\"asaas_cobrancas\",\"nota_fiscal\",\"automacoes_whatsapp\",\"assinatura_digital\",\"relatorios_avancados\"]", "Profissional", 197m },
                    { new Guid("10000000-0000-0000-0000-000000000003"), true, new DateTime(2026, 6, 10, 16, 15, 2, 245, DateTimeKind.Utc).AddTicks(7810), "Multi-profissional, sinal de reserva, tudo incluso", "[\"asaas_cobrancas\",\"nota_fiscal\",\"automacoes_whatsapp\",\"assinatura_digital\",\"relatorios_avancados\",\"sinal_reserva\",\"multi_profissional\"]", "Enterprise", 397m }
                });

            migrationBuilder.CreateIndex(
                name: "ix_empresas_plano_plano_saa_s_id",
                schema: "gestor",
                table: "empresas_plano",
                column: "plano_saa_s_id");
        }
    }
}
