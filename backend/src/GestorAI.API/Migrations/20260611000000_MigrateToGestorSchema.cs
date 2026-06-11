using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToGestorSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS gestor;");

            migrationBuilder.Sql("ALTER TABLE public.agendamentos SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.automacao_logs SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.bloqueios_agenda SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.categorias SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.categorias_lancamento SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.clientes SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.cobrancas SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.configuracoes_empresa SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.contratos SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.\"ContratoItens\" SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.contrato_templates SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.\"ContratoTemplateItens\" SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.disponibilidade_semanais SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.fornecedores SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.itens_venda SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.lancamentos SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.movimentacoes_estoque SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.notas_fiscais SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.nota_fiscal_itens SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.orcamentos SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.orcamento_itens SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.produtos SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.profissionais SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE public.vendas SET SCHEMA gestor;");

            // Tabelas das migrations mais recentes (AddPlanosSaaS/AddBillingSaaS)
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.planos_saa_s SET SCHEMA gestor;");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.empresas_plano SET SCHEMA gestor;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE IF EXISTS gestor.empresas_plano SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS gestor.planos_saa_s SET SCHEMA public;");

            migrationBuilder.Sql("ALTER TABLE gestor.vendas SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.profissionais SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.produtos SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.orcamento_itens SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.orcamentos SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.nota_fiscal_itens SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.notas_fiscais SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.movimentacoes_estoque SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.lancamentos SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.itens_venda SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.fornecedores SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.disponibilidade_semanais SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.\"ContratoTemplateItens\" SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.contrato_templates SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.\"ContratoItens\" SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.contratos SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.configuracoes_empresa SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.cobrancas SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.clientes SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.categorias_lancamento SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.categorias SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.bloqueios_agenda SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.automacao_logs SET SCHEMA public;");
            migrationBuilder.Sql("ALTER TABLE gestor.agendamentos SET SCHEMA public;");

            migrationBuilder.Sql("DROP SCHEMA IF EXISTS gestor;");
        }
    }
}
