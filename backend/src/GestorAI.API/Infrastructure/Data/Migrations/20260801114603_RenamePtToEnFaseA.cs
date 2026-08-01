using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamePtToEnFaseA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"appointments\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"billings\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"categories\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"company_settings\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"contract_item\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"contracts\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"customers\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"financial_entries\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"fiscal_invoice_items\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"fiscal_invoices\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"products\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"professionals\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"quote_items\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"quotes\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"sale_items\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"sales\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"schedule_blocks\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"stock_movements\" CASCADE;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS gestor.\"weekly_availabilities\" CASCADE;");

            migrationBuilder.RenameTable(
                name: "disponibilidade_semanais",
                schema: "gestor",
                newName: "weekly_availabilities");

            migrationBuilder.RenameTable(
                name: "ContratoTemplateItens",
                schema: "gestor",
                newName: "contract_template_items");

            migrationBuilder.RenameTable(
                name: "PlanoAssinaturaItens",
                schema: "gestor",
                newName: "subscription_plan_items");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "customers",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "nome",
                schema: "gestor",
                table: "customers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "whatsapp",
                schema: "gestor",
                table: "customers",
                newName: "whats_app");

            migrationBuilder.RenameColumn(
                name: "observacoes",
                schema: "gestor",
                table: "customers",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "data_cadastro",
                schema: "gestor",
                table: "customers",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "sales",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                schema: "gestor",
                table: "sales",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "data_hora",
                schema: "gestor",
                table: "sales",
                newName: "sale_date");

            migrationBuilder.RenameColumn(
                name: "desconto",
                schema: "gestor",
                table: "sales",
                newName: "discount");

            migrationBuilder.RenameColumn(
                name: "forma_pagamento",
                schema: "gestor",
                table: "sales",
                newName: "payment_method");

            migrationBuilder.RenameColumn(
                name: "parcelas",
                schema: "gestor",
                table: "sales",
                newName: "installments");

            migrationBuilder.RenameColumn(
                name: "observacao",
                schema: "gestor",
                table: "sales",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "profissional_id",
                schema: "gestor",
                table: "sales",
                newName: "professional_id");

            migrationBuilder.RenameColumn(
                name: "profissional_nome",
                schema: "gestor",
                table: "sales",
                newName: "professional_name");

            migrationBuilder.RenameColumn(
                name: "venda_id",
                schema: "gestor",
                table: "sale_items",
                newName: "sale_id");

            migrationBuilder.RenameColumn(
                name: "produto_id",
                schema: "gestor",
                table: "sale_items",
                newName: "product_id");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                schema: "gestor",
                table: "sale_items",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "preco_unitario",
                schema: "gestor",
                table: "sale_items",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "desconto",
                schema: "gestor",
                table: "sale_items",
                newName: "discount");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "products",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "categoria_id",
                schema: "gestor",
                table: "products",
                newName: "category_id");

            migrationBuilder.RenameColumn(
                name: "nome",
                schema: "gestor",
                table: "products",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "products",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "preco_venda",
                schema: "gestor",
                table: "products",
                newName: "sale_price");

            migrationBuilder.RenameColumn(
                name: "custo_medio",
                schema: "gestor",
                table: "products",
                newName: "average_cost");

            migrationBuilder.RenameColumn(
                name: "estoque_atual",
                schema: "gestor",
                table: "products",
                newName: "current_stock");

            migrationBuilder.RenameColumn(
                name: "estoque_minimo",
                schema: "gestor",
                table: "products",
                newName: "minimum_stock");

            migrationBuilder.RenameColumn(
                name: "codigo_barras",
                schema: "gestor",
                table: "products",
                newName: "barcode");

            migrationBuilder.RenameColumn(
                name: "ativo",
                schema: "gestor",
                table: "products",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                schema: "gestor",
                table: "products",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "atualizado_em",
                schema: "gestor",
                table: "products",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "duracao_minutos",
                schema: "gestor",
                table: "products",
                newName: "duration_minutes");

            migrationBuilder.RenameColumn(
                name: "tipo",
                schema: "gestor",
                table: "products",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "categories",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "nome",
                schema: "gestor",
                table: "categories",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "transaction_categories",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "nome",
                schema: "gestor",
                table: "transaction_categories",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "tipo",
                schema: "gestor",
                table: "transaction_categories",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "professionals",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "nome",
                schema: "gestor",
                table: "professionals",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "telefone",
                schema: "gestor",
                table: "professionals",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "ativo",
                schema: "gestor",
                table: "professionals",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                schema: "gestor",
                table: "professionals",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "profissional_id",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "professional_id");

            migrationBuilder.RenameColumn(
                name: "dia_semana",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "week_day");

            migrationBuilder.RenameColumn(
                name: "hora_inicio",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "start_time");

            migrationBuilder.RenameColumn(
                name: "hora_fim",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "end_time");

            migrationBuilder.RenameColumn(
                name: "data_inicio",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "data_fim",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "end_date");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "schedule_blocks",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "profissional_id",
                schema: "gestor",
                table: "schedule_blocks",
                newName: "professional_id");

            migrationBuilder.RenameColumn(
                name: "data_inicio",
                schema: "gestor",
                table: "schedule_blocks",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "data_fim",
                schema: "gestor",
                table: "schedule_blocks",
                newName: "end_date");

            migrationBuilder.RenameColumn(
                name: "motivo",
                schema: "gestor",
                table: "schedule_blocks",
                newName: "reason");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "appointments",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "profissional_id",
                schema: "gestor",
                table: "appointments",
                newName: "professional_id");

            migrationBuilder.RenameColumn(
                name: "cliente_nome",
                schema: "gestor",
                table: "appointments",
                newName: "customer_name");

            migrationBuilder.RenameColumn(
                name: "cliente_telefone",
                schema: "gestor",
                table: "appointments",
                newName: "customer_phone");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                schema: "gestor",
                table: "appointments",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "servico_id",
                schema: "gestor",
                table: "appointments",
                newName: "service_id");

            migrationBuilder.RenameColumn(
                name: "data_hora_inicio",
                schema: "gestor",
                table: "appointments",
                newName: "start_at");

            migrationBuilder.RenameColumn(
                name: "data_hora_fim",
                schema: "gestor",
                table: "appointments",
                newName: "end_at");

            migrationBuilder.RenameColumn(
                name: "observacao",
                schema: "gestor",
                table: "appointments",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "venda_id",
                schema: "gestor",
                table: "appointments",
                newName: "sale_id");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                schema: "gestor",
                table: "appointments",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "sinal_asaas_id",
                schema: "gestor",
                table: "appointments",
                newName: "deposit_asaas_id");

            migrationBuilder.RenameColumn(
                name: "sinal_pago",
                schema: "gestor",
                table: "appointments",
                newName: "deposit_paid");

            migrationBuilder.RenameColumn(
                name: "sinal_pix_qr_code",
                schema: "gestor",
                table: "appointments",
                newName: "deposit_pix_qr_code");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "invoices",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "venda_id",
                schema: "gestor",
                table: "invoices",
                newName: "sale_id");

            migrationBuilder.RenameColumn(
                name: "modelo",
                schema: "gestor",
                table: "invoices",
                newName: "model");

            migrationBuilder.RenameColumn(
                name: "numero",
                schema: "gestor",
                table: "invoices",
                newName: "number");

            migrationBuilder.RenameColumn(
                name: "serie",
                schema: "gestor",
                table: "invoices",
                newName: "series");

            migrationBuilder.RenameColumn(
                name: "chave_acesso",
                schema: "gestor",
                table: "invoices",
                newName: "access_key");

            migrationBuilder.RenameColumn(
                name: "protocolo",
                schema: "gestor",
                table: "invoices",
                newName: "protocol");

            migrationBuilder.RenameColumn(
                name: "protocolo_cancelamento",
                schema: "gestor",
                table: "invoices",
                newName: "cancellation_protocol");

            migrationBuilder.RenameColumn(
                name: "mensagem_erro",
                schema: "gestor",
                table: "invoices",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "autorizada_em",
                schema: "gestor",
                table: "invoices",
                newName: "authorized_at");

            migrationBuilder.RenameColumn(
                name: "cancelada_em",
                schema: "gestor",
                table: "invoices",
                newName: "canceled_at");

            migrationBuilder.RenameColumn(
                name: "criada_em",
                schema: "gestor",
                table: "invoices",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "invoice_items",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "nota_fiscal_id",
                schema: "gestor",
                table: "invoice_items",
                newName: "invoice_id");

            migrationBuilder.RenameColumn(
                name: "nome_produto",
                schema: "gestor",
                table: "invoice_items",
                newName: "product_name");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                schema: "gestor",
                table: "invoice_items",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "preco_unitario",
                schema: "gestor",
                table: "invoice_items",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "company_settings",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "cor_primaria",
                schema: "gestor",
                table: "company_settings",
                newName: "primary_color");

            migrationBuilder.RenameColumn(
                name: "descricao_publica",
                schema: "gestor",
                table: "company_settings",
                newName: "public_description");

            migrationBuilder.RenameColumn(
                name: "lembrete1d_antes",
                schema: "gestor",
                table: "company_settings",
                newName: "reminder1day_before");

            migrationBuilder.RenameColumn(
                name: "lembrete1d_depois",
                schema: "gestor",
                table: "company_settings",
                newName: "reminder1day_after");

            migrationBuilder.RenameColumn(
                name: "lembrete3d_antes",
                schema: "gestor",
                table: "company_settings",
                newName: "reminder3days_before");

            migrationBuilder.RenameColumn(
                name: "lembrete3d_depois",
                schema: "gestor",
                table: "company_settings",
                newName: "reminder3days_after");

            migrationBuilder.RenameColumn(
                name: "lembrete7d_depois",
                schema: "gestor",
                table: "company_settings",
                newName: "reminder7days_after");

            migrationBuilder.RenameColumn(
                name: "lembrete_no_dia",
                schema: "gestor",
                table: "company_settings",
                newName: "reminder_on_due_date");

            migrationBuilder.RenameColumn(
                name: "aprovar_automaticamente",
                schema: "gestor",
                table: "company_settings",
                newName: "auto_approve");

            migrationBuilder.RenameColumn(
                name: "horas_limite_cancelamento",
                schema: "gestor",
                table: "company_settings",
                newName: "cancellation_limit_hours");

            migrationBuilder.RenameColumn(
                name: "valor_sinal",
                schema: "gestor",
                table: "company_settings",
                newName: "deposit_amount");

            migrationBuilder.RenameColumn(
                name: "dominio_customizado",
                schema: "gestor",
                table: "company_settings",
                newName: "custom_domain");

            migrationBuilder.RenameColumn(
                name: "telefone",
                schema: "gestor",
                table: "company_settings",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "contracts",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "numero",
                schema: "gestor",
                table: "contracts",
                newName: "number");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                schema: "gestor",
                table: "contracts",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "titulo",
                schema: "gestor",
                table: "contracts",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "objeto",
                schema: "gestor",
                table: "contracts",
                newName: "subject");

            migrationBuilder.RenameColumn(
                name: "tipo_cobranca",
                schema: "gestor",
                table: "contracts",
                newName: "charge_type");

            migrationBuilder.RenameColumn(
                name: "valor",
                schema: "gestor",
                table: "contracts",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "data_inicio",
                schema: "gestor",
                table: "contracts",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "data_fim",
                schema: "gestor",
                table: "contracts",
                newName: "end_date");

            migrationBuilder.RenameColumn(
                name: "periodicidade",
                schema: "gestor",
                table: "contracts",
                newName: "frequency");

            migrationBuilder.RenameColumn(
                name: "dia_vencimento",
                schema: "gestor",
                table: "contracts",
                newName: "due_day");

            migrationBuilder.RenameColumn(
                name: "observacao",
                schema: "gestor",
                table: "contracts",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                schema: "gestor",
                table: "contracts",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "assinatura_cliente_id",
                schema: "gestor",
                table: "contracts",
                newName: "customer_subscription_id");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "charges",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                schema: "gestor",
                table: "charges",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "contrato_id",
                schema: "gestor",
                table: "charges",
                newName: "contract_id");

            migrationBuilder.RenameColumn(
                name: "referencia",
                schema: "gestor",
                table: "charges",
                newName: "reference");

            migrationBuilder.RenameColumn(
                name: "valor",
                schema: "gestor",
                table: "charges",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "data_vencimento",
                schema: "gestor",
                table: "charges",
                newName: "due_date");

            migrationBuilder.RenameColumn(
                name: "data_pagamento",
                schema: "gestor",
                table: "charges",
                newName: "payment_date");

            migrationBuilder.RenameColumn(
                name: "forma_pagamento",
                schema: "gestor",
                table: "charges",
                newName: "payment_method");

            migrationBuilder.RenameColumn(
                name: "observacao",
                schema: "gestor",
                table: "charges",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                schema: "gestor",
                table: "charges",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "suppliers",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "nome",
                schema: "gestor",
                table: "suppliers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "telefone",
                schema: "gestor",
                table: "suppliers",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "cidade",
                schema: "gestor",
                table: "suppliers",
                newName: "city");

            migrationBuilder.RenameColumn(
                name: "contato",
                schema: "gestor",
                table: "suppliers",
                newName: "contact_person");

            migrationBuilder.RenameColumn(
                name: "observacoes",
                schema: "gestor",
                table: "suppliers",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "data_cadastro",
                schema: "gestor",
                table: "suppliers",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "whatsapp",
                schema: "gestor",
                table: "suppliers",
                newName: "whats_app");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "contract_templates",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "nome",
                schema: "gestor",
                table: "contract_templates",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "objeto",
                schema: "gestor",
                table: "contract_templates",
                newName: "subject");

            migrationBuilder.RenameColumn(
                name: "tipo_cobranca",
                schema: "gestor",
                table: "contract_templates",
                newName: "charge_type");

            migrationBuilder.RenameColumn(
                name: "periodicidade",
                schema: "gestor",
                table: "contract_templates",
                newName: "frequency");

            migrationBuilder.RenameColumn(
                name: "dia_vencimento",
                schema: "gestor",
                table: "contract_templates",
                newName: "due_day");

            migrationBuilder.RenameColumn(
                name: "valor_padrao",
                schema: "gestor",
                table: "contract_templates",
                newName: "default_amount");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                schema: "gestor",
                table: "contract_templates",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "automation_logs",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "cobranca_id",
                schema: "gestor",
                table: "automation_logs",
                newName: "charge_id");

            migrationBuilder.RenameColumn(
                name: "tipo_evento",
                schema: "gestor",
                table: "automation_logs",
                newName: "event_type");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                schema: "gestor",
                table: "automation_logs",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "sucesso",
                schema: "gestor",
                table: "automation_logs",
                newName: "success");

            migrationBuilder.RenameColumn(
                name: "erro_msg",
                schema: "gestor",
                table: "automation_logs",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "subscription_plans",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "nome",
                schema: "gestor",
                table: "subscription_plans",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "subscription_plans",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "nicho",
                schema: "gestor",
                table: "subscription_plans",
                newName: "niche");

            migrationBuilder.RenameColumn(
                name: "preco",
                schema: "gestor",
                table: "subscription_plans",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "periodicidade",
                schema: "gestor",
                table: "subscription_plans",
                newName: "frequency");

            migrationBuilder.RenameColumn(
                name: "ativo",
                schema: "gestor",
                table: "subscription_plans",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "mais_vendido",
                schema: "gestor",
                table: "subscription_plans",
                newName: "best_seller");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                schema: "gestor",
                table: "subscription_plans",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "plano_assinatura_id",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "subscription_plan_id");

            migrationBuilder.RenameColumn(
                name: "contrato_id",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "contract_id");

            migrationBuilder.RenameColumn(
                name: "data_inicio",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "data_renovacao",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "renewal_date");

            migrationBuilder.RenameColumn(
                name: "ciclo_atual",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "current_cycle");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "nicho",
                schema: "gestor",
                table: "niche_templates",
                newName: "niche");

            migrationBuilder.RenameColumn(
                name: "nome_plano",
                schema: "gestor",
                table: "niche_templates",
                newName: "plan_name");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "niche_templates",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "preco_sugerido",
                schema: "gestor",
                table: "niche_templates",
                newName: "suggested_price");

            migrationBuilder.RenameColumn(
                name: "mais_vendido",
                schema: "gestor",
                table: "niche_templates",
                newName: "best_seller");

            migrationBuilder.RenameColumn(
                name: "periodicidade",
                schema: "gestor",
                table: "niche_templates",
                newName: "frequency");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "dashboard_layouts",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "atualizado_em",
                schema: "gestor",
                table: "dashboard_layouts",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "report_layouts",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "atualizado_em",
                schema: "gestor",
                table: "report_layouts",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "installment_plans",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "compra_id",
                schema: "gestor",
                table: "installment_plans",
                newName: "purchase_id");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "installment_plans",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "valor_total",
                schema: "gestor",
                table: "installment_plans",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "qtd_parcelas",
                schema: "gestor",
                table: "installment_plans",
                newName: "installment_count");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "purchases",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "numero",
                schema: "gestor",
                table: "purchases",
                newName: "number");

            migrationBuilder.RenameColumn(
                name: "data",
                schema: "gestor",
                table: "purchases",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "fornecedor_id",
                schema: "gestor",
                table: "purchases",
                newName: "supplier_id");

            migrationBuilder.RenameColumn(
                name: "pedido_compra_id",
                schema: "gestor",
                table: "purchases",
                newName: "purchase_order_id");

            migrationBuilder.RenameColumn(
                name: "tipo_compra",
                schema: "gestor",
                table: "purchases",
                newName: "purchase_type");

            migrationBuilder.RenameColumn(
                name: "numero_nota",
                schema: "gestor",
                table: "purchases",
                newName: "note_number");

            migrationBuilder.RenameColumn(
                name: "condicao_pagamento",
                schema: "gestor",
                table: "purchases",
                newName: "payment_terms");

            migrationBuilder.RenameColumn(
                name: "forma_pagamento",
                schema: "gestor",
                table: "purchases",
                newName: "payment_method");

            migrationBuilder.RenameColumn(
                name: "valor_total",
                schema: "gestor",
                table: "purchases",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "observacoes",
                schema: "gestor",
                table: "purchases",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "criada_em",
                schema: "gestor",
                table: "purchases",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "purchase_orders",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "numero",
                schema: "gestor",
                table: "purchase_orders",
                newName: "number");

            migrationBuilder.RenameColumn(
                name: "data",
                schema: "gestor",
                table: "purchase_orders",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "fornecedor_id",
                schema: "gestor",
                table: "purchase_orders",
                newName: "supplier_id");

            migrationBuilder.RenameColumn(
                name: "valor_estimado",
                schema: "gestor",
                table: "purchase_orders",
                newName: "estimated_amount");

            migrationBuilder.RenameColumn(
                name: "observacoes",
                schema: "gestor",
                table: "purchase_orders",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                schema: "gestor",
                table: "purchase_orders",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "transactions",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "tipo",
                schema: "gestor",
                table: "transactions",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "transactions",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "valor",
                schema: "gestor",
                table: "transactions",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "data_vencimento",
                schema: "gestor",
                table: "transactions",
                newName: "due_date");

            migrationBuilder.RenameColumn(
                name: "data_pagamento",
                schema: "gestor",
                table: "transactions",
                newName: "payment_date");

            migrationBuilder.RenameColumn(
                name: "categoria",
                schema: "gestor",
                table: "transactions",
                newName: "category");

            migrationBuilder.RenameColumn(
                name: "venda_id",
                schema: "gestor",
                table: "transactions",
                newName: "sale_id");

            migrationBuilder.RenameColumn(
                name: "observacao",
                schema: "gestor",
                table: "transactions",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "numero_parcela",
                schema: "gestor",
                table: "transactions",
                newName: "installment_number");

            migrationBuilder.RenameColumn(
                name: "parcelamento_id",
                schema: "gestor",
                table: "transactions",
                newName: "installment_plan_id");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "stock_movements",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "produto_id",
                schema: "gestor",
                table: "stock_movements",
                newName: "product_id");

            migrationBuilder.RenameColumn(
                name: "tipo",
                schema: "gestor",
                table: "stock_movements",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                schema: "gestor",
                table: "stock_movements",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "origem",
                schema: "gestor",
                table: "stock_movements",
                newName: "source");

            migrationBuilder.RenameColumn(
                name: "data_hora",
                schema: "gestor",
                table: "stock_movements",
                newName: "movement_date");

            migrationBuilder.RenameColumn(
                name: "observacao",
                schema: "gestor",
                table: "stock_movements",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "quotes",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                schema: "gestor",
                table: "quotes",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "numero",
                schema: "gestor",
                table: "quotes",
                newName: "number");

            migrationBuilder.RenameColumn(
                name: "titulo",
                schema: "gestor",
                table: "quotes",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "data_validade",
                schema: "gestor",
                table: "quotes",
                newName: "expiration_date");

            migrationBuilder.RenameColumn(
                name: "observacao",
                schema: "gestor",
                table: "quotes",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "venda_id",
                schema: "gestor",
                table: "quotes",
                newName: "sale_id");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                schema: "gestor",
                table: "quotes",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "token_publico",
                schema: "gestor",
                table: "quotes",
                newName: "public_token");

            migrationBuilder.RenameColumn(
                name: "orcamento_id",
                schema: "gestor",
                table: "quote_items",
                newName: "quote_id");

            migrationBuilder.RenameColumn(
                name: "tipo",
                schema: "gestor",
                table: "quote_items",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "produto_id",
                schema: "gestor",
                table: "quote_items",
                newName: "product_id");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "quote_items",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                schema: "gestor",
                table: "quote_items",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "valor_unitario",
                schema: "gestor",
                table: "quote_items",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "contrato_id",
                schema: "gestor",
                table: "contract_item",
                newName: "contract_id");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "contract_item",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                schema: "gestor",
                table: "contract_item",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "valor_unitario",
                schema: "gestor",
                table: "contract_item",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "contrato_template_id",
                schema: "gestor",
                table: "contract_template_items",
                newName: "contract_template_id");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "contract_template_items",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                schema: "gestor",
                table: "contract_template_items",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "valor_unitario",
                schema: "gestor",
                table: "contract_template_items",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "purchase_items",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "compra_id",
                schema: "gestor",
                table: "purchase_items",
                newName: "purchase_id");

            migrationBuilder.RenameColumn(
                name: "produto_id",
                schema: "gestor",
                table: "purchase_items",
                newName: "product_id");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "purchase_items",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "destino_compra",
                schema: "gestor",
                table: "purchase_items",
                newName: "destination");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                schema: "gestor",
                table: "purchase_items",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "valor_unitario",
                schema: "gestor",
                table: "purchase_items",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "desconto",
                schema: "gestor",
                table: "purchase_items",
                newName: "discount");

            migrationBuilder.RenameColumn(
                name: "frete_rateado",
                schema: "gestor",
                table: "purchase_items",
                newName: "allocated_freight");

            migrationBuilder.RenameColumn(
                name: "impostos",
                schema: "gestor",
                table: "purchase_items",
                newName: "taxes");

            migrationBuilder.RenameColumn(
                name: "valor_total",
                schema: "gestor",
                table: "purchase_items",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "categoria_financeira",
                schema: "gestor",
                table: "purchase_items",
                newName: "financial_category");

            migrationBuilder.RenameColumn(
                name: "centro_custo",
                schema: "gestor",
                table: "purchase_items",
                newName: "cost_center");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "company_id");

            migrationBuilder.RenameColumn(
                name: "pedido_compra_id",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "purchase_order_id");

            migrationBuilder.RenameColumn(
                name: "produto_id",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "product_id");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "valor_estimado",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "estimated_amount");

            migrationBuilder.RenameColumn(
                name: "nicho_template_id",
                schema: "gestor",
                table: "niche_template_items",
                newName: "niche_template_id");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "niche_template_items",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "quantidade_por_ciclo",
                schema: "gestor",
                table: "niche_template_items",
                newName: "quantity_per_cycle");

            migrationBuilder.RenameColumn(
                name: "tipo",
                schema: "gestor",
                table: "niche_template_items",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "percentual_desconto",
                schema: "gestor",
                table: "niche_template_items",
                newName: "discount_percentage");

            migrationBuilder.RenameColumn(
                name: "plano_assinatura_id",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "subscription_plan_id");

            migrationBuilder.RenameColumn(
                name: "descricao",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "servico_id",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "service_id");

            migrationBuilder.RenameColumn(
                name: "quantidade_por_ciclo",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "quantity_per_cycle");

            migrationBuilder.RenameColumn(
                name: "tipo",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "percentual_desconto",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "discount_percentage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "discount_percentage",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "percentual_desconto");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "quantity_per_cycle",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "quantidade_por_ciclo");

            migrationBuilder.RenameColumn(
                name: "service_id",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "servico_id");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "subscription_plan_id",
                schema: "gestor",
                table: "subscription_plan_items",
                newName: "plano_assinatura_id");

            migrationBuilder.RenameColumn(
                name: "discount_percentage",
                schema: "gestor",
                table: "niche_template_items",
                newName: "percentual_desconto");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "gestor",
                table: "niche_template_items",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "quantity_per_cycle",
                schema: "gestor",
                table: "niche_template_items",
                newName: "quantidade_por_ciclo");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "niche_template_items",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "niche_template_id",
                schema: "gestor",
                table: "niche_template_items",
                newName: "nicho_template_id");

            migrationBuilder.RenameColumn(
                name: "estimated_amount",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "valor_estimado");

            migrationBuilder.RenameColumn(
                name: "quantity",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "product_id",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "produto_id");

            migrationBuilder.RenameColumn(
                name: "purchase_order_id",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "pedido_compra_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "purchase_order_items",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "cost_center",
                schema: "gestor",
                table: "purchase_items",
                newName: "centro_custo");

            migrationBuilder.RenameColumn(
                name: "financial_category",
                schema: "gestor",
                table: "purchase_items",
                newName: "categoria_financeira");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                schema: "gestor",
                table: "purchase_items",
                newName: "valor_total");

            migrationBuilder.RenameColumn(
                name: "taxes",
                schema: "gestor",
                table: "purchase_items",
                newName: "impostos");

            migrationBuilder.RenameColumn(
                name: "allocated_freight",
                schema: "gestor",
                table: "purchase_items",
                newName: "frete_rateado");

            migrationBuilder.RenameColumn(
                name: "discount",
                schema: "gestor",
                table: "purchase_items",
                newName: "desconto");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                schema: "gestor",
                table: "purchase_items",
                newName: "valor_unitario");

            migrationBuilder.RenameColumn(
                name: "quantity",
                schema: "gestor",
                table: "purchase_items",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "destination",
                schema: "gestor",
                table: "purchase_items",
                newName: "destino_compra");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "purchase_items",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "product_id",
                schema: "gestor",
                table: "purchase_items",
                newName: "produto_id");

            migrationBuilder.RenameColumn(
                name: "purchase_id",
                schema: "gestor",
                table: "purchase_items",
                newName: "compra_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "purchase_items",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                schema: "gestor",
                table: "contract_template_items",
                newName: "valor_unitario");

            migrationBuilder.RenameColumn(
                name: "quantity",
                schema: "gestor",
                table: "contract_template_items",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "contract_template_items",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "contract_template_id",
                schema: "gestor",
                table: "contract_template_items",
                newName: "contrato_template_id");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                schema: "gestor",
                table: "contract_item",
                newName: "valor_unitario");

            migrationBuilder.RenameColumn(
                name: "quantity",
                schema: "gestor",
                table: "contract_item",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "contract_item",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "contract_id",
                schema: "gestor",
                table: "contract_item",
                newName: "contrato_id");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                schema: "gestor",
                table: "quote_items",
                newName: "valor_unitario");

            migrationBuilder.RenameColumn(
                name: "quantity",
                schema: "gestor",
                table: "quote_items",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "quote_items",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "product_id",
                schema: "gestor",
                table: "quote_items",
                newName: "produto_id");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "gestor",
                table: "quote_items",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "quote_id",
                schema: "gestor",
                table: "quote_items",
                newName: "orcamento_id");

            migrationBuilder.RenameColumn(
                name: "public_token",
                schema: "gestor",
                table: "quotes",
                newName: "token_publico");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "quotes",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "sale_id",
                schema: "gestor",
                table: "quotes",
                newName: "venda_id");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "gestor",
                table: "quotes",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "expiration_date",
                schema: "gestor",
                table: "quotes",
                newName: "data_validade");

            migrationBuilder.RenameColumn(
                name: "title",
                schema: "gestor",
                table: "quotes",
                newName: "titulo");

            migrationBuilder.RenameColumn(
                name: "number",
                schema: "gestor",
                table: "quotes",
                newName: "numero");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                schema: "gestor",
                table: "quotes",
                newName: "cliente_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "quotes",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "gestor",
                table: "stock_movements",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "movement_date",
                schema: "gestor",
                table: "stock_movements",
                newName: "data_hora");

            migrationBuilder.RenameColumn(
                name: "source",
                schema: "gestor",
                table: "stock_movements",
                newName: "origem");

            migrationBuilder.RenameColumn(
                name: "quantity",
                schema: "gestor",
                table: "stock_movements",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "gestor",
                table: "stock_movements",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "product_id",
                schema: "gestor",
                table: "stock_movements",
                newName: "produto_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "stock_movements",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "installment_plan_id",
                schema: "gestor",
                table: "transactions",
                newName: "parcelamento_id");

            migrationBuilder.RenameColumn(
                name: "installment_number",
                schema: "gestor",
                table: "transactions",
                newName: "numero_parcela");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "gestor",
                table: "transactions",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "sale_id",
                schema: "gestor",
                table: "transactions",
                newName: "venda_id");

            migrationBuilder.RenameColumn(
                name: "category",
                schema: "gestor",
                table: "transactions",
                newName: "categoria");

            migrationBuilder.RenameColumn(
                name: "payment_date",
                schema: "gestor",
                table: "transactions",
                newName: "data_pagamento");

            migrationBuilder.RenameColumn(
                name: "due_date",
                schema: "gestor",
                table: "transactions",
                newName: "data_vencimento");

            migrationBuilder.RenameColumn(
                name: "amount",
                schema: "gestor",
                table: "transactions",
                newName: "valor");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "transactions",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "gestor",
                table: "transactions",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "transactions",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "purchase_orders",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "gestor",
                table: "purchase_orders",
                newName: "observacoes");

            migrationBuilder.RenameColumn(
                name: "estimated_amount",
                schema: "gestor",
                table: "purchase_orders",
                newName: "valor_estimado");

            migrationBuilder.RenameColumn(
                name: "supplier_id",
                schema: "gestor",
                table: "purchase_orders",
                newName: "fornecedor_id");

            migrationBuilder.RenameColumn(
                name: "date",
                schema: "gestor",
                table: "purchase_orders",
                newName: "data");

            migrationBuilder.RenameColumn(
                name: "number",
                schema: "gestor",
                table: "purchase_orders",
                newName: "numero");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "purchase_orders",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "purchases",
                newName: "criada_em");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "gestor",
                table: "purchases",
                newName: "observacoes");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                schema: "gestor",
                table: "purchases",
                newName: "valor_total");

            migrationBuilder.RenameColumn(
                name: "payment_method",
                schema: "gestor",
                table: "purchases",
                newName: "forma_pagamento");

            migrationBuilder.RenameColumn(
                name: "payment_terms",
                schema: "gestor",
                table: "purchases",
                newName: "condicao_pagamento");

            migrationBuilder.RenameColumn(
                name: "note_number",
                schema: "gestor",
                table: "purchases",
                newName: "numero_nota");

            migrationBuilder.RenameColumn(
                name: "purchase_type",
                schema: "gestor",
                table: "purchases",
                newName: "tipo_compra");

            migrationBuilder.RenameColumn(
                name: "purchase_order_id",
                schema: "gestor",
                table: "purchases",
                newName: "pedido_compra_id");

            migrationBuilder.RenameColumn(
                name: "supplier_id",
                schema: "gestor",
                table: "purchases",
                newName: "fornecedor_id");

            migrationBuilder.RenameColumn(
                name: "date",
                schema: "gestor",
                table: "purchases",
                newName: "data");

            migrationBuilder.RenameColumn(
                name: "number",
                schema: "gestor",
                table: "purchases",
                newName: "numero");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "purchases",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "installment_count",
                schema: "gestor",
                table: "installment_plans",
                newName: "qtd_parcelas");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                schema: "gestor",
                table: "installment_plans",
                newName: "valor_total");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "installment_plans",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "purchase_id",
                schema: "gestor",
                table: "installment_plans",
                newName: "compra_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "installment_plans",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "gestor",
                table: "report_layouts",
                newName: "atualizado_em");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "report_layouts",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "gestor",
                table: "dashboard_layouts",
                newName: "atualizado_em");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "dashboard_layouts",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "frequency",
                schema: "gestor",
                table: "niche_templates",
                newName: "periodicidade");

            migrationBuilder.RenameColumn(
                name: "best_seller",
                schema: "gestor",
                table: "niche_templates",
                newName: "mais_vendido");

            migrationBuilder.RenameColumn(
                name: "suggested_price",
                schema: "gestor",
                table: "niche_templates",
                newName: "preco_sugerido");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "niche_templates",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "plan_name",
                schema: "gestor",
                table: "niche_templates",
                newName: "nome_plano");

            migrationBuilder.RenameColumn(
                name: "niche",
                schema: "gestor",
                table: "niche_templates",
                newName: "nicho");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "current_cycle",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "ciclo_atual");

            migrationBuilder.RenameColumn(
                name: "renewal_date",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "data_renovacao");

            migrationBuilder.RenameColumn(
                name: "start_date",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "data_inicio");

            migrationBuilder.RenameColumn(
                name: "contract_id",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "contrato_id");

            migrationBuilder.RenameColumn(
                name: "subscription_plan_id",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "plano_assinatura_id");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "cliente_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "customer_subscriptions",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "subscription_plans",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "best_seller",
                schema: "gestor",
                table: "subscription_plans",
                newName: "mais_vendido");

            migrationBuilder.RenameColumn(
                name: "is_active",
                schema: "gestor",
                table: "subscription_plans",
                newName: "ativo");

            migrationBuilder.RenameColumn(
                name: "frequency",
                schema: "gestor",
                table: "subscription_plans",
                newName: "periodicidade");

            migrationBuilder.RenameColumn(
                name: "price",
                schema: "gestor",
                table: "subscription_plans",
                newName: "preco");

            migrationBuilder.RenameColumn(
                name: "niche",
                schema: "gestor",
                table: "subscription_plans",
                newName: "nicho");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "subscription_plans",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "gestor",
                table: "subscription_plans",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "subscription_plans",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "error_message",
                schema: "gestor",
                table: "automation_logs",
                newName: "erro_msg");

            migrationBuilder.RenameColumn(
                name: "success",
                schema: "gestor",
                table: "automation_logs",
                newName: "sucesso");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "automation_logs",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "event_type",
                schema: "gestor",
                table: "automation_logs",
                newName: "tipo_evento");

            migrationBuilder.RenameColumn(
                name: "charge_id",
                schema: "gestor",
                table: "automation_logs",
                newName: "cobranca_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "automation_logs",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "contract_templates",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "default_amount",
                schema: "gestor",
                table: "contract_templates",
                newName: "valor_padrao");

            migrationBuilder.RenameColumn(
                name: "due_day",
                schema: "gestor",
                table: "contract_templates",
                newName: "dia_vencimento");

            migrationBuilder.RenameColumn(
                name: "frequency",
                schema: "gestor",
                table: "contract_templates",
                newName: "periodicidade");

            migrationBuilder.RenameColumn(
                name: "charge_type",
                schema: "gestor",
                table: "contract_templates",
                newName: "tipo_cobranca");

            migrationBuilder.RenameColumn(
                name: "subject",
                schema: "gestor",
                table: "contract_templates",
                newName: "objeto");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "gestor",
                table: "contract_templates",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "contract_templates",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "whats_app",
                schema: "gestor",
                table: "suppliers",
                newName: "whatsapp");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "suppliers",
                newName: "data_cadastro");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "gestor",
                table: "suppliers",
                newName: "observacoes");

            migrationBuilder.RenameColumn(
                name: "contact_person",
                schema: "gestor",
                table: "suppliers",
                newName: "contato");

            migrationBuilder.RenameColumn(
                name: "city",
                schema: "gestor",
                table: "suppliers",
                newName: "cidade");

            migrationBuilder.RenameColumn(
                name: "phone",
                schema: "gestor",
                table: "suppliers",
                newName: "telefone");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "gestor",
                table: "suppliers",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "suppliers",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "charges",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "gestor",
                table: "charges",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "payment_method",
                schema: "gestor",
                table: "charges",
                newName: "forma_pagamento");

            migrationBuilder.RenameColumn(
                name: "payment_date",
                schema: "gestor",
                table: "charges",
                newName: "data_pagamento");

            migrationBuilder.RenameColumn(
                name: "due_date",
                schema: "gestor",
                table: "charges",
                newName: "data_vencimento");

            migrationBuilder.RenameColumn(
                name: "amount",
                schema: "gestor",
                table: "charges",
                newName: "valor");

            migrationBuilder.RenameColumn(
                name: "reference",
                schema: "gestor",
                table: "charges",
                newName: "referencia");

            migrationBuilder.RenameColumn(
                name: "contract_id",
                schema: "gestor",
                table: "charges",
                newName: "contrato_id");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                schema: "gestor",
                table: "charges",
                newName: "cliente_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "charges",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "customer_subscription_id",
                schema: "gestor",
                table: "contracts",
                newName: "assinatura_cliente_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "contracts",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "gestor",
                table: "contracts",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "due_day",
                schema: "gestor",
                table: "contracts",
                newName: "dia_vencimento");

            migrationBuilder.RenameColumn(
                name: "frequency",
                schema: "gestor",
                table: "contracts",
                newName: "periodicidade");

            migrationBuilder.RenameColumn(
                name: "end_date",
                schema: "gestor",
                table: "contracts",
                newName: "data_fim");

            migrationBuilder.RenameColumn(
                name: "start_date",
                schema: "gestor",
                table: "contracts",
                newName: "data_inicio");

            migrationBuilder.RenameColumn(
                name: "amount",
                schema: "gestor",
                table: "contracts",
                newName: "valor");

            migrationBuilder.RenameColumn(
                name: "charge_type",
                schema: "gestor",
                table: "contracts",
                newName: "tipo_cobranca");

            migrationBuilder.RenameColumn(
                name: "subject",
                schema: "gestor",
                table: "contracts",
                newName: "objeto");

            migrationBuilder.RenameColumn(
                name: "title",
                schema: "gestor",
                table: "contracts",
                newName: "titulo");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                schema: "gestor",
                table: "contracts",
                newName: "cliente_id");

            migrationBuilder.RenameColumn(
                name: "number",
                schema: "gestor",
                table: "contracts",
                newName: "numero");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "contracts",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "phone",
                schema: "gestor",
                table: "company_settings",
                newName: "telefone");

            migrationBuilder.RenameColumn(
                name: "custom_domain",
                schema: "gestor",
                table: "company_settings",
                newName: "dominio_customizado");

            migrationBuilder.RenameColumn(
                name: "deposit_amount",
                schema: "gestor",
                table: "company_settings",
                newName: "valor_sinal");

            migrationBuilder.RenameColumn(
                name: "cancellation_limit_hours",
                schema: "gestor",
                table: "company_settings",
                newName: "horas_limite_cancelamento");

            migrationBuilder.RenameColumn(
                name: "auto_approve",
                schema: "gestor",
                table: "company_settings",
                newName: "aprovar_automaticamente");

            migrationBuilder.RenameColumn(
                name: "reminder_on_due_date",
                schema: "gestor",
                table: "company_settings",
                newName: "lembrete_no_dia");

            migrationBuilder.RenameColumn(
                name: "reminder7days_after",
                schema: "gestor",
                table: "company_settings",
                newName: "lembrete7d_depois");

            migrationBuilder.RenameColumn(
                name: "reminder3days_after",
                schema: "gestor",
                table: "company_settings",
                newName: "lembrete3d_depois");

            migrationBuilder.RenameColumn(
                name: "reminder3days_before",
                schema: "gestor",
                table: "company_settings",
                newName: "lembrete3d_antes");

            migrationBuilder.RenameColumn(
                name: "reminder1day_after",
                schema: "gestor",
                table: "company_settings",
                newName: "lembrete1d_depois");

            migrationBuilder.RenameColumn(
                name: "reminder1day_before",
                schema: "gestor",
                table: "company_settings",
                newName: "lembrete1d_antes");

            migrationBuilder.RenameColumn(
                name: "public_description",
                schema: "gestor",
                table: "company_settings",
                newName: "descricao_publica");

            migrationBuilder.RenameColumn(
                name: "primary_color",
                schema: "gestor",
                table: "company_settings",
                newName: "cor_primaria");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "company_settings",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                schema: "gestor",
                table: "invoice_items",
                newName: "preco_unitario");

            migrationBuilder.RenameColumn(
                name: "quantity",
                schema: "gestor",
                table: "invoice_items",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "product_name",
                schema: "gestor",
                table: "invoice_items",
                newName: "nome_produto");

            migrationBuilder.RenameColumn(
                name: "invoice_id",
                schema: "gestor",
                table: "invoice_items",
                newName: "nota_fiscal_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "invoice_items",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "invoices",
                newName: "criada_em");

            migrationBuilder.RenameColumn(
                name: "canceled_at",
                schema: "gestor",
                table: "invoices",
                newName: "cancelada_em");

            migrationBuilder.RenameColumn(
                name: "authorized_at",
                schema: "gestor",
                table: "invoices",
                newName: "autorizada_em");

            migrationBuilder.RenameColumn(
                name: "error_message",
                schema: "gestor",
                table: "invoices",
                newName: "mensagem_erro");

            migrationBuilder.RenameColumn(
                name: "cancellation_protocol",
                schema: "gestor",
                table: "invoices",
                newName: "protocolo_cancelamento");

            migrationBuilder.RenameColumn(
                name: "protocol",
                schema: "gestor",
                table: "invoices",
                newName: "protocolo");

            migrationBuilder.RenameColumn(
                name: "access_key",
                schema: "gestor",
                table: "invoices",
                newName: "chave_acesso");

            migrationBuilder.RenameColumn(
                name: "series",
                schema: "gestor",
                table: "invoices",
                newName: "serie");

            migrationBuilder.RenameColumn(
                name: "number",
                schema: "gestor",
                table: "invoices",
                newName: "numero");

            migrationBuilder.RenameColumn(
                name: "model",
                schema: "gestor",
                table: "invoices",
                newName: "modelo");

            migrationBuilder.RenameColumn(
                name: "sale_id",
                schema: "gestor",
                table: "invoices",
                newName: "venda_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "invoices",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "deposit_pix_qr_code",
                schema: "gestor",
                table: "appointments",
                newName: "sinal_pix_qr_code");

            migrationBuilder.RenameColumn(
                name: "deposit_paid",
                schema: "gestor",
                table: "appointments",
                newName: "sinal_pago");

            migrationBuilder.RenameColumn(
                name: "deposit_asaas_id",
                schema: "gestor",
                table: "appointments",
                newName: "sinal_asaas_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "appointments",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "sale_id",
                schema: "gestor",
                table: "appointments",
                newName: "venda_id");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "gestor",
                table: "appointments",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "end_at",
                schema: "gestor",
                table: "appointments",
                newName: "data_hora_fim");

            migrationBuilder.RenameColumn(
                name: "start_at",
                schema: "gestor",
                table: "appointments",
                newName: "data_hora_inicio");

            migrationBuilder.RenameColumn(
                name: "service_id",
                schema: "gestor",
                table: "appointments",
                newName: "servico_id");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                schema: "gestor",
                table: "appointments",
                newName: "cliente_id");

            migrationBuilder.RenameColumn(
                name: "customer_phone",
                schema: "gestor",
                table: "appointments",
                newName: "cliente_telefone");

            migrationBuilder.RenameColumn(
                name: "customer_name",
                schema: "gestor",
                table: "appointments",
                newName: "cliente_nome");

            migrationBuilder.RenameColumn(
                name: "professional_id",
                schema: "gestor",
                table: "appointments",
                newName: "profissional_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "appointments",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "reason",
                schema: "gestor",
                table: "schedule_blocks",
                newName: "motivo");

            migrationBuilder.RenameColumn(
                name: "end_date",
                schema: "gestor",
                table: "schedule_blocks",
                newName: "data_fim");

            migrationBuilder.RenameColumn(
                name: "start_date",
                schema: "gestor",
                table: "schedule_blocks",
                newName: "data_inicio");

            migrationBuilder.RenameColumn(
                name: "professional_id",
                schema: "gestor",
                table: "schedule_blocks",
                newName: "profissional_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "schedule_blocks",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "end_date",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "data_fim");

            migrationBuilder.RenameColumn(
                name: "start_date",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "data_inicio");

            migrationBuilder.RenameColumn(
                name: "end_time",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "hora_fim");

            migrationBuilder.RenameColumn(
                name: "start_time",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "hora_inicio");

            migrationBuilder.RenameColumn(
                name: "week_day",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "dia_semana");

            migrationBuilder.RenameColumn(
                name: "professional_id",
                schema: "gestor",
                table: "weekly_availabilities",
                newName: "profissional_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "professionals",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "is_active",
                schema: "gestor",
                table: "professionals",
                newName: "ativo");

            migrationBuilder.RenameColumn(
                name: "phone",
                schema: "gestor",
                table: "professionals",
                newName: "telefone");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "gestor",
                table: "professionals",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "professionals",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "gestor",
                table: "transaction_categories",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "gestor",
                table: "transaction_categories",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "transaction_categories",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "gestor",
                table: "categories",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "categories",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "gestor",
                table: "products",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "duration_minutes",
                schema: "gestor",
                table: "products",
                newName: "duracao_minutos");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "gestor",
                table: "products",
                newName: "atualizado_em");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "products",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "is_active",
                schema: "gestor",
                table: "products",
                newName: "ativo");

            migrationBuilder.RenameColumn(
                name: "barcode",
                schema: "gestor",
                table: "products",
                newName: "codigo_barras");

            migrationBuilder.RenameColumn(
                name: "minimum_stock",
                schema: "gestor",
                table: "products",
                newName: "estoque_minimo");

            migrationBuilder.RenameColumn(
                name: "current_stock",
                schema: "gestor",
                table: "products",
                newName: "estoque_atual");

            migrationBuilder.RenameColumn(
                name: "average_cost",
                schema: "gestor",
                table: "products",
                newName: "custo_medio");

            migrationBuilder.RenameColumn(
                name: "sale_price",
                schema: "gestor",
                table: "products",
                newName: "preco_venda");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "gestor",
                table: "products",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "gestor",
                table: "products",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "category_id",
                schema: "gestor",
                table: "products",
                newName: "categoria_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "products",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "discount",
                schema: "gestor",
                table: "sale_items",
                newName: "desconto");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                schema: "gestor",
                table: "sale_items",
                newName: "preco_unitario");

            migrationBuilder.RenameColumn(
                name: "quantity",
                schema: "gestor",
                table: "sale_items",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "product_id",
                schema: "gestor",
                table: "sale_items",
                newName: "produto_id");

            migrationBuilder.RenameColumn(
                name: "sale_id",
                schema: "gestor",
                table: "sale_items",
                newName: "venda_id");

            migrationBuilder.RenameColumn(
                name: "professional_name",
                schema: "gestor",
                table: "sales",
                newName: "profissional_nome");

            migrationBuilder.RenameColumn(
                name: "professional_id",
                schema: "gestor",
                table: "sales",
                newName: "profissional_id");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "gestor",
                table: "sales",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "installments",
                schema: "gestor",
                table: "sales",
                newName: "parcelas");

            migrationBuilder.RenameColumn(
                name: "payment_method",
                schema: "gestor",
                table: "sales",
                newName: "forma_pagamento");

            migrationBuilder.RenameColumn(
                name: "discount",
                schema: "gestor",
                table: "sales",
                newName: "desconto");

            migrationBuilder.RenameColumn(
                name: "sale_date",
                schema: "gestor",
                table: "sales",
                newName: "data_hora");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                schema: "gestor",
                table: "sales",
                newName: "cliente_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "sales",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "gestor",
                table: "customers",
                newName: "data_cadastro");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "gestor",
                table: "customers",
                newName: "observacoes");

            migrationBuilder.RenameColumn(
                name: "whats_app",
                schema: "gestor",
                table: "customers",
                newName: "whatsapp");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "gestor",
                table: "customers",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "company_id",
                schema: "gestor",
                table: "customers",
                newName: "empresa_id");

            migrationBuilder.RenameTable(
                name: "subscription_plan_items",
                schema: "gestor",
                newName: "PlanoAssinaturaItens");

            migrationBuilder.RenameTable(
                name: "contract_template_items",
                schema: "gestor",
                newName: "ContratoTemplateItens");

            migrationBuilder.RenameTable(
                name: "weekly_availabilities",
                schema: "gestor",
                newName: "disponibilidade_semanais");
        }
    }
}
