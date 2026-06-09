using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Migrations
{
    /// <inheritdoc />
    public partial class ResyncSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agendamentos_Clientes_ClienteId",
                table: "Agendamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Agendamentos_Produtos_ServicoId",
                table: "Agendamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Agendamentos_Profissionais_ProfissionalId",
                table: "Agendamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_AutomacaoLogs_Cobrancas_CobrancaId",
                table: "AutomacaoLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_BloqueiosAgenda_Profissionais_ProfissionalId",
                table: "BloqueiosAgenda");

            migrationBuilder.DropForeignKey(
                name: "FK_Cobrancas_Clientes_ClienteId",
                table: "Cobrancas");

            migrationBuilder.DropForeignKey(
                name: "FK_Cobrancas_Contratos_ContratoId",
                table: "Cobrancas");

            migrationBuilder.DropForeignKey(
                name: "FK_ContratoItens_Contratos_ContratoId",
                table: "ContratoItens");

            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_Clientes_ClienteId",
                table: "Contratos");

            migrationBuilder.DropForeignKey(
                name: "FK_ContratoTemplateItens_ContratoTemplates_ContratoTemplateId",
                table: "ContratoTemplateItens");

            migrationBuilder.DropForeignKey(
                name: "FK_DisponibilidadeSemanais_Profissionais_ProfissionalId",
                table: "DisponibilidadeSemanais");

            migrationBuilder.DropForeignKey(
                name: "FK_ItensVenda_Produtos_ProdutoId",
                table: "ItensVenda");

            migrationBuilder.DropForeignKey(
                name: "FK_ItensVenda_Vendas_VendaId",
                table: "ItensVenda");

            migrationBuilder.DropForeignKey(
                name: "FK_Lancamentos_Vendas_VendaId",
                table: "Lancamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimentacoesEstoque_Produtos_ProdutoId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropForeignKey(
                name: "FK_NotaFiscalItens_NotasFiscais_NotaFiscalId",
                table: "NotaFiscalItens");

            migrationBuilder.DropForeignKey(
                name: "FK_NotasFiscais_Vendas_VendaId",
                table: "NotasFiscais");

            migrationBuilder.DropForeignKey(
                name: "FK_OrcamentoItens_Orcamentos_OrcamentoId",
                table: "OrcamentoItens");

            migrationBuilder.DropForeignKey(
                name: "FK_OrcamentoItens_Produtos_ProdutoId",
                table: "OrcamentoItens");

            migrationBuilder.DropForeignKey(
                name: "FK_Orcamentos_Clientes_ClienteId",
                table: "Orcamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_Categorias_CategoriaId",
                table: "Produtos");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_Clientes_ClienteId",
                table: "Vendas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vendas",
                table: "Vendas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Profissionais",
                table: "Profissionais");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Produtos",
                table: "Produtos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orcamentos",
                table: "Orcamentos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Lancamentos",
                table: "Lancamentos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Fornecedores",
                table: "Fornecedores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContratoTemplateItens",
                table: "ContratoTemplateItens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Contratos",
                table: "Contratos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContratoItens",
                table: "ContratoItens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cobrancas",
                table: "Cobrancas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Clientes",
                table: "Clientes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categorias",
                table: "Categorias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Agendamentos",
                table: "Agendamentos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrcamentoItens",
                table: "OrcamentoItens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotasFiscais",
                table: "NotasFiscais");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotaFiscalItens",
                table: "NotaFiscalItens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MovimentacoesEstoque",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItensVenda",
                table: "ItensVenda");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DisponibilidadeSemanais",
                table: "DisponibilidadeSemanais");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContratoTemplates",
                table: "ContratoTemplates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConfiguracoesEmpresa",
                table: "ConfiguracoesEmpresa");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BloqueiosAgenda",
                table: "BloqueiosAgenda");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AutomacaoLogs",
                table: "AutomacaoLogs");

            migrationBuilder.RenameTable(
                name: "Vendas",
                newName: "vendas");

            migrationBuilder.RenameTable(
                name: "Profissionais",
                newName: "profissionais");

            migrationBuilder.RenameTable(
                name: "Produtos",
                newName: "produtos");

            migrationBuilder.RenameTable(
                name: "Orcamentos",
                newName: "orcamentos");

            migrationBuilder.RenameTable(
                name: "Lancamentos",
                newName: "lancamentos");

            migrationBuilder.RenameTable(
                name: "Fornecedores",
                newName: "fornecedores");

            migrationBuilder.RenameTable(
                name: "Contratos",
                newName: "contratos");

            migrationBuilder.RenameTable(
                name: "Cobrancas",
                newName: "cobrancas");

            migrationBuilder.RenameTable(
                name: "Clientes",
                newName: "clientes");

            migrationBuilder.RenameTable(
                name: "Categorias",
                newName: "categorias");

            migrationBuilder.RenameTable(
                name: "Agendamentos",
                newName: "agendamentos");

            migrationBuilder.RenameTable(
                name: "OrcamentoItens",
                newName: "orcamento_itens");

            migrationBuilder.RenameTable(
                name: "NotasFiscais",
                newName: "notas_fiscais");

            migrationBuilder.RenameTable(
                name: "NotaFiscalItens",
                newName: "nota_fiscal_itens");

            migrationBuilder.RenameTable(
                name: "MovimentacoesEstoque",
                newName: "movimentacoes_estoque");

            migrationBuilder.RenameTable(
                name: "ItensVenda",
                newName: "itens_venda");

            migrationBuilder.RenameTable(
                name: "DisponibilidadeSemanais",
                newName: "disponibilidade_semanais");

            migrationBuilder.RenameTable(
                name: "ContratoTemplates",
                newName: "contrato_templates");

            migrationBuilder.RenameTable(
                name: "ConfiguracoesEmpresa",
                newName: "configuracoes_empresa");

            migrationBuilder.RenameTable(
                name: "BloqueiosAgenda",
                newName: "bloqueios_agenda");

            migrationBuilder.RenameTable(
                name: "AutomacaoLogs",
                newName: "automacao_logs");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "vendas",
                newName: "total");

            migrationBuilder.RenameColumn(
                name: "Subtotal",
                table: "vendas",
                newName: "subtotal");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "vendas",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Parcelas",
                table: "vendas",
                newName: "parcelas");

            migrationBuilder.RenameColumn(
                name: "Observacao",
                table: "vendas",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "Desconto",
                table: "vendas",
                newName: "desconto");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "vendas",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "FormaPagamento",
                table: "vendas",
                newName: "forma_pagamento");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "vendas",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DataHora",
                table: "vendas",
                newName: "data_hora");

            migrationBuilder.RenameColumn(
                name: "ClienteId",
                table: "vendas",
                newName: "cliente_id");

            migrationBuilder.RenameIndex(
                name: "IX_Vendas_ClienteId",
                table: "vendas",
                newName: "ix_vendas_cliente_id");

            migrationBuilder.RenameColumn(
                name: "Telefone",
                table: "profissionais",
                newName: "telefone");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "profissionais",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Ativo",
                table: "profissionais",
                newName: "ativo");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "profissionais",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "profissionais",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "profissionais",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "Tipo",
                table: "produtos",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "produtos",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Descricao",
                table: "produtos",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "Ativo",
                table: "produtos",
                newName: "ativo");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "produtos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PrecoVenda",
                table: "produtos",
                newName: "preco_venda");

            migrationBuilder.RenameColumn(
                name: "EstoqueMinimo",
                table: "produtos",
                newName: "estoque_minimo");

            migrationBuilder.RenameColumn(
                name: "EstoqueAtual",
                table: "produtos",
                newName: "estoque_atual");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "produtos",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DuracaoMinutos",
                table: "produtos",
                newName: "duracao_minutos");

            migrationBuilder.RenameColumn(
                name: "CustoMedio",
                table: "produtos",
                newName: "custo_medio");

            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "produtos",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "CodigoBarras",
                table: "produtos",
                newName: "codigo_barras");

            migrationBuilder.RenameColumn(
                name: "CategoriaId",
                table: "produtos",
                newName: "categoria_id");

            migrationBuilder.RenameColumn(
                name: "AtualizadoEm",
                table: "produtos",
                newName: "atualizado_em");

            migrationBuilder.RenameIndex(
                name: "IX_Produtos_CategoriaId",
                table: "produtos",
                newName: "ix_produtos_categoria_id");

            migrationBuilder.RenameColumn(
                name: "Titulo",
                table: "orcamentos",
                newName: "titulo");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "orcamentos",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Observacao",
                table: "orcamentos",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "Numero",
                table: "orcamentos",
                newName: "numero");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "orcamentos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VendaId",
                table: "orcamentos",
                newName: "venda_id");

            migrationBuilder.RenameColumn(
                name: "TokenPublico",
                table: "orcamentos",
                newName: "token_publico");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "orcamentos",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DataValidade",
                table: "orcamentos",
                newName: "data_validade");

            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "orcamentos",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "ClienteId",
                table: "orcamentos",
                newName: "cliente_id");

            migrationBuilder.RenameIndex(
                name: "IX_Orcamentos_ClienteId",
                table: "orcamentos",
                newName: "ix_orcamentos_cliente_id");

            migrationBuilder.RenameColumn(
                name: "Valor",
                table: "lancamentos",
                newName: "valor");

            migrationBuilder.RenameColumn(
                name: "Tipo",
                table: "lancamentos",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "lancamentos",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Observacao",
                table: "lancamentos",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "Descricao",
                table: "lancamentos",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "Categoria",
                table: "lancamentos",
                newName: "categoria");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "lancamentos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VendaId",
                table: "lancamentos",
                newName: "venda_id");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "lancamentos",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DataVencimento",
                table: "lancamentos",
                newName: "data_vencimento");

            migrationBuilder.RenameColumn(
                name: "DataPagamento",
                table: "lancamentos",
                newName: "data_pagamento");

            migrationBuilder.RenameIndex(
                name: "IX_Lancamentos_VendaId",
                table: "lancamentos",
                newName: "ix_lancamentos_venda_id");

            migrationBuilder.RenameColumn(
                name: "Uf",
                table: "fornecedores",
                newName: "uf");

            migrationBuilder.RenameColumn(
                name: "Telefone",
                table: "fornecedores",
                newName: "telefone");

            migrationBuilder.RenameColumn(
                name: "Observacoes",
                table: "fornecedores",
                newName: "observacoes");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "fornecedores",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Logradouro",
                table: "fornecedores",
                newName: "logradouro");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "fornecedores",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Contato",
                table: "fornecedores",
                newName: "contato");

            migrationBuilder.RenameColumn(
                name: "Cidade",
                table: "fornecedores",
                newName: "cidade");

            migrationBuilder.RenameColumn(
                name: "Cep",
                table: "fornecedores",
                newName: "cep");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fornecedores",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "fornecedores",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DataCadastro",
                table: "fornecedores",
                newName: "data_cadastro");

            migrationBuilder.RenameColumn(
                name: "CnpjCpf",
                table: "fornecedores",
                newName: "cnpj_cpf");

            migrationBuilder.RenameIndex(
                name: "IX_Fornecedores_EmpresaId_CnpjCpf",
                table: "fornecedores",
                newName: "ix_fornecedores_empresa_id_cnpj_cpf");

            migrationBuilder.RenameColumn(
                name: "Quantidade",
                table: "ContratoTemplateItens",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "Descricao",
                table: "ContratoTemplateItens",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ContratoTemplateItens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ValorUnitario",
                table: "ContratoTemplateItens",
                newName: "valor_unitario");

            migrationBuilder.RenameColumn(
                name: "ContratoTemplateId",
                table: "ContratoTemplateItens",
                newName: "contrato_template_id");

            migrationBuilder.RenameIndex(
                name: "IX_ContratoTemplateItens_ContratoTemplateId",
                table: "ContratoTemplateItens",
                newName: "ix_contrato_template_itens_contrato_template_id");

            migrationBuilder.RenameColumn(
                name: "Valor",
                table: "contratos",
                newName: "valor");

            migrationBuilder.RenameColumn(
                name: "Titulo",
                table: "contratos",
                newName: "titulo");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "contratos",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Periodicidade",
                table: "contratos",
                newName: "periodicidade");

            migrationBuilder.RenameColumn(
                name: "Observacao",
                table: "contratos",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "Objeto",
                table: "contratos",
                newName: "objeto");

            migrationBuilder.RenameColumn(
                name: "Numero",
                table: "contratos",
                newName: "numero");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "contratos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TipoCobranca",
                table: "contratos",
                newName: "tipo_cobranca");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "contratos",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DiaVencimento",
                table: "contratos",
                newName: "dia_vencimento");

            migrationBuilder.RenameColumn(
                name: "DataInicio",
                table: "contratos",
                newName: "data_inicio");

            migrationBuilder.RenameColumn(
                name: "DataFim",
                table: "contratos",
                newName: "data_fim");

            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "contratos",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "ClienteId",
                table: "contratos",
                newName: "cliente_id");

            migrationBuilder.RenameIndex(
                name: "IX_Contratos_ClienteId",
                table: "contratos",
                newName: "ix_contratos_cliente_id");

            migrationBuilder.RenameColumn(
                name: "Quantidade",
                table: "ContratoItens",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "Descricao",
                table: "ContratoItens",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ContratoItens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ValorUnitario",
                table: "ContratoItens",
                newName: "valor_unitario");

            migrationBuilder.RenameColumn(
                name: "ContratoId",
                table: "ContratoItens",
                newName: "contrato_id");

            migrationBuilder.RenameIndex(
                name: "IX_ContratoItens_ContratoId",
                table: "ContratoItens",
                newName: "ix_contrato_itens_contrato_id");

            migrationBuilder.RenameColumn(
                name: "Valor",
                table: "cobrancas",
                newName: "valor");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "cobrancas",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Referencia",
                table: "cobrancas",
                newName: "referencia");

            migrationBuilder.RenameColumn(
                name: "Observacao",
                table: "cobrancas",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "cobrancas",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "FormaPagamento",
                table: "cobrancas",
                newName: "forma_pagamento");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "cobrancas",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DataVencimento",
                table: "cobrancas",
                newName: "data_vencimento");

            migrationBuilder.RenameColumn(
                name: "DataPagamento",
                table: "cobrancas",
                newName: "data_pagamento");

            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "cobrancas",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "ContratoId",
                table: "cobrancas",
                newName: "contrato_id");

            migrationBuilder.RenameColumn(
                name: "ClienteId",
                table: "cobrancas",
                newName: "cliente_id");

            migrationBuilder.RenameColumn(
                name: "AsaasPixQrCode",
                table: "cobrancas",
                newName: "asaas_pix_qr_code");

            migrationBuilder.RenameColumn(
                name: "AsaasPaymentLink",
                table: "cobrancas",
                newName: "asaas_payment_link");

            migrationBuilder.RenameColumn(
                name: "AsaasId",
                table: "cobrancas",
                newName: "asaas_id");

            migrationBuilder.RenameColumn(
                name: "AsaasBoletoUrl",
                table: "cobrancas",
                newName: "asaas_boleto_url");

            migrationBuilder.RenameIndex(
                name: "IX_Cobrancas_ContratoId",
                table: "cobrancas",
                newName: "ix_cobrancas_contrato_id");

            migrationBuilder.RenameIndex(
                name: "IX_Cobrancas_ClienteId",
                table: "cobrancas",
                newName: "ix_cobrancas_cliente_id");

            migrationBuilder.RenameColumn(
                name: "Whatsapp",
                table: "clientes",
                newName: "whatsapp");

            migrationBuilder.RenameColumn(
                name: "Observacoes",
                table: "clientes",
                newName: "observacoes");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "clientes",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "clientes",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "clientes",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "clientes",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DataCadastro",
                table: "clientes",
                newName: "data_cadastro");

            migrationBuilder.RenameIndex(
                name: "IX_Clientes_EmpresaId_Whatsapp",
                table: "clientes",
                newName: "ix_clientes_empresa_id_whatsapp");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "categorias",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "categorias",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "categorias",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "agendamentos",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Observacao",
                table: "agendamentos",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "agendamentos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VendaId",
                table: "agendamentos",
                newName: "venda_id");

            migrationBuilder.RenameColumn(
                name: "ServicoId",
                table: "agendamentos",
                newName: "servico_id");

            migrationBuilder.RenameColumn(
                name: "ProfissionalId",
                table: "agendamentos",
                newName: "profissional_id");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "agendamentos",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DataHoraInicio",
                table: "agendamentos",
                newName: "data_hora_inicio");

            migrationBuilder.RenameColumn(
                name: "DataHoraFim",
                table: "agendamentos",
                newName: "data_hora_fim");

            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "agendamentos",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "ClienteTelefone",
                table: "agendamentos",
                newName: "cliente_telefone");

            migrationBuilder.RenameColumn(
                name: "ClienteNome",
                table: "agendamentos",
                newName: "cliente_nome");

            migrationBuilder.RenameColumn(
                name: "ClienteId",
                table: "agendamentos",
                newName: "cliente_id");

            migrationBuilder.RenameIndex(
                name: "IX_Agendamentos_ServicoId",
                table: "agendamentos",
                newName: "ix_agendamentos_servico_id");

            migrationBuilder.RenameIndex(
                name: "IX_Agendamentos_ProfissionalId",
                table: "agendamentos",
                newName: "ix_agendamentos_profissional_id");

            migrationBuilder.RenameIndex(
                name: "IX_Agendamentos_ClienteId",
                table: "agendamentos",
                newName: "ix_agendamentos_cliente_id");

            migrationBuilder.RenameColumn(
                name: "Tipo",
                table: "orcamento_itens",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "Quantidade",
                table: "orcamento_itens",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "Descricao",
                table: "orcamento_itens",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "orcamento_itens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ValorUnitario",
                table: "orcamento_itens",
                newName: "valor_unitario");

            migrationBuilder.RenameColumn(
                name: "ProdutoId",
                table: "orcamento_itens",
                newName: "produto_id");

            migrationBuilder.RenameColumn(
                name: "OrcamentoId",
                table: "orcamento_itens",
                newName: "orcamento_id");

            migrationBuilder.RenameIndex(
                name: "IX_OrcamentoItens_ProdutoId",
                table: "orcamento_itens",
                newName: "ix_orcamento_itens_produto_id");

            migrationBuilder.RenameIndex(
                name: "IX_OrcamentoItens_OrcamentoId",
                table: "orcamento_itens",
                newName: "ix_orcamento_itens_orcamento_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "notas_fiscais",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Serie",
                table: "notas_fiscais",
                newName: "serie");

            migrationBuilder.RenameColumn(
                name: "Protocolo",
                table: "notas_fiscais",
                newName: "protocolo");

            migrationBuilder.RenameColumn(
                name: "Numero",
                table: "notas_fiscais",
                newName: "numero");

            migrationBuilder.RenameColumn(
                name: "Modelo",
                table: "notas_fiscais",
                newName: "modelo");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "notas_fiscais",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "XmlUrl",
                table: "notas_fiscais",
                newName: "xml_url");

            migrationBuilder.RenameColumn(
                name: "VendaId",
                table: "notas_fiscais",
                newName: "venda_id");

            migrationBuilder.RenameColumn(
                name: "ProtocoloCancelamento",
                table: "notas_fiscais",
                newName: "protocolo_cancelamento");

            migrationBuilder.RenameColumn(
                name: "PdfUrl",
                table: "notas_fiscais",
                newName: "pdf_url");

            migrationBuilder.RenameColumn(
                name: "MensagemErro",
                table: "notas_fiscais",
                newName: "mensagem_erro");

            migrationBuilder.RenameColumn(
                name: "FocusNfeRef",
                table: "notas_fiscais",
                newName: "focus_nfe_ref");

            migrationBuilder.RenameColumn(
                name: "FocusNfeId",
                table: "notas_fiscais",
                newName: "focus_nfe_id");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "notas_fiscais",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "CriadaEm",
                table: "notas_fiscais",
                newName: "criada_em");

            migrationBuilder.RenameColumn(
                name: "ChaveAcesso",
                table: "notas_fiscais",
                newName: "chave_acesso");

            migrationBuilder.RenameColumn(
                name: "CanceladaEm",
                table: "notas_fiscais",
                newName: "cancelada_em");

            migrationBuilder.RenameColumn(
                name: "AutorizadaEm",
                table: "notas_fiscais",
                newName: "autorizada_em");

            migrationBuilder.RenameIndex(
                name: "IX_NotasFiscais_VendaId",
                table: "notas_fiscais",
                newName: "ix_notas_fiscais_venda_id");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "nota_fiscal_itens",
                newName: "total");

            migrationBuilder.RenameColumn(
                name: "Quantidade",
                table: "nota_fiscal_itens",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "Ncm",
                table: "nota_fiscal_itens",
                newName: "ncm");

            migrationBuilder.RenameColumn(
                name: "Cfop",
                table: "nota_fiscal_itens",
                newName: "cfop");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "nota_fiscal_itens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PrecoUnitario",
                table: "nota_fiscal_itens",
                newName: "preco_unitario");

            migrationBuilder.RenameColumn(
                name: "NotaFiscalId",
                table: "nota_fiscal_itens",
                newName: "nota_fiscal_id");

            migrationBuilder.RenameColumn(
                name: "NomeProduto",
                table: "nota_fiscal_itens",
                newName: "nome_produto");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "nota_fiscal_itens",
                newName: "empresa_id");

            migrationBuilder.RenameIndex(
                name: "IX_NotaFiscalItens_NotaFiscalId",
                table: "nota_fiscal_itens",
                newName: "ix_nota_fiscal_itens_nota_fiscal_id");

            migrationBuilder.RenameColumn(
                name: "Tipo",
                table: "movimentacoes_estoque",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "Quantidade",
                table: "movimentacoes_estoque",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "Origem",
                table: "movimentacoes_estoque",
                newName: "origem");

            migrationBuilder.RenameColumn(
                name: "Observacao",
                table: "movimentacoes_estoque",
                newName: "observacao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "movimentacoes_estoque",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ReferenciaId",
                table: "movimentacoes_estoque",
                newName: "referencia_id");

            migrationBuilder.RenameColumn(
                name: "ProdutoId",
                table: "movimentacoes_estoque",
                newName: "produto_id");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "movimentacoes_estoque",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DataHora",
                table: "movimentacoes_estoque",
                newName: "data_hora");

            migrationBuilder.RenameIndex(
                name: "IX_MovimentacoesEstoque_ProdutoId",
                table: "movimentacoes_estoque",
                newName: "ix_movimentacoes_estoque_produto_id");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "itens_venda",
                newName: "total");

            migrationBuilder.RenameColumn(
                name: "Quantidade",
                table: "itens_venda",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "Desconto",
                table: "itens_venda",
                newName: "desconto");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "itens_venda",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VendaId",
                table: "itens_venda",
                newName: "venda_id");

            migrationBuilder.RenameColumn(
                name: "ProdutoId",
                table: "itens_venda",
                newName: "produto_id");

            migrationBuilder.RenameColumn(
                name: "PrecoUnitario",
                table: "itens_venda",
                newName: "preco_unitario");

            migrationBuilder.RenameIndex(
                name: "IX_ItensVenda_VendaId",
                table: "itens_venda",
                newName: "ix_itens_venda_venda_id");

            migrationBuilder.RenameIndex(
                name: "IX_ItensVenda_ProdutoId",
                table: "itens_venda",
                newName: "ix_itens_venda_produto_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "disponibilidade_semanais",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ProfissionalId",
                table: "disponibilidade_semanais",
                newName: "profissional_id");

            migrationBuilder.RenameColumn(
                name: "HoraInicio",
                table: "disponibilidade_semanais",
                newName: "hora_inicio");

            migrationBuilder.RenameColumn(
                name: "HoraFim",
                table: "disponibilidade_semanais",
                newName: "hora_fim");

            migrationBuilder.RenameColumn(
                name: "DiaSemana",
                table: "disponibilidade_semanais",
                newName: "dia_semana");

            migrationBuilder.RenameColumn(
                name: "DataInicio",
                table: "disponibilidade_semanais",
                newName: "data_inicio");

            migrationBuilder.RenameColumn(
                name: "DataFim",
                table: "disponibilidade_semanais",
                newName: "data_fim");

            migrationBuilder.RenameIndex(
                name: "IX_DisponibilidadeSemanais_ProfissionalId",
                table: "disponibilidade_semanais",
                newName: "ix_disponibilidade_semanais_profissional_id");

            migrationBuilder.RenameColumn(
                name: "Periodicidade",
                table: "contrato_templates",
                newName: "periodicidade");

            migrationBuilder.RenameColumn(
                name: "Objeto",
                table: "contrato_templates",
                newName: "objeto");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "contrato_templates",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "contrato_templates",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ValorPadrao",
                table: "contrato_templates",
                newName: "valor_padrao");

            migrationBuilder.RenameColumn(
                name: "TipoCobranca",
                table: "contrato_templates",
                newName: "tipo_cobranca");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "contrato_templates",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DiaVencimento",
                table: "contrato_templates",
                newName: "dia_vencimento");

            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "contrato_templates",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "Uf",
                table: "configuracoes_empresa",
                newName: "uf");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "configuracoes_empresa",
                newName: "slug");

            migrationBuilder.RenameColumn(
                name: "Numero",
                table: "configuracoes_empresa",
                newName: "numero");

            migrationBuilder.RenameColumn(
                name: "Municipio",
                table: "configuracoes_empresa",
                newName: "municipio");

            migrationBuilder.RenameColumn(
                name: "Logradouro",
                table: "configuracoes_empresa",
                newName: "logradouro");

            migrationBuilder.RenameColumn(
                name: "Complemento",
                table: "configuracoes_empresa",
                newName: "complemento");

            migrationBuilder.RenameColumn(
                name: "Cnpj",
                table: "configuracoes_empresa",
                newName: "cnpj");

            migrationBuilder.RenameColumn(
                name: "Cep",
                table: "configuracoes_empresa",
                newName: "cep");

            migrationBuilder.RenameColumn(
                name: "Bairro",
                table: "configuracoes_empresa",
                newName: "bairro");

            migrationBuilder.RenameColumn(
                name: "Ambiente",
                table: "configuracoes_empresa",
                newName: "ambiente");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "configuracoes_empresa",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SerieNfe",
                table: "configuracoes_empresa",
                newName: "serie_nfe");

            migrationBuilder.RenameColumn(
                name: "SerieNfce",
                table: "configuracoes_empresa",
                newName: "serie_nfce");

            migrationBuilder.RenameColumn(
                name: "RegimeTributario",
                table: "configuracoes_empresa",
                newName: "regime_tributario");

            migrationBuilder.RenameColumn(
                name: "RazaoSocial",
                table: "configuracoes_empresa",
                newName: "razao_social");

            migrationBuilder.RenameColumn(
                name: "NomeFantasia",
                table: "configuracoes_empresa",
                newName: "nome_fantasia");

            migrationBuilder.RenameColumn(
                name: "LogoUrl",
                table: "configuracoes_empresa",
                newName: "logo_url");

            migrationBuilder.RenameColumn(
                name: "LembreteNoDia",
                table: "configuracoes_empresa",
                newName: "lembrete_no_dia");

            migrationBuilder.RenameColumn(
                name: "Lembrete7dDepois",
                table: "configuracoes_empresa",
                newName: "lembrete7d_depois");

            migrationBuilder.RenameColumn(
                name: "Lembrete3dDepois",
                table: "configuracoes_empresa",
                newName: "lembrete3d_depois");

            migrationBuilder.RenameColumn(
                name: "Lembrete3dAntes",
                table: "configuracoes_empresa",
                newName: "lembrete3d_antes");

            migrationBuilder.RenameColumn(
                name: "Lembrete1dDepois",
                table: "configuracoes_empresa",
                newName: "lembrete1d_depois");

            migrationBuilder.RenameColumn(
                name: "Lembrete1dAntes",
                table: "configuracoes_empresa",
                newName: "lembrete1d_antes");

            migrationBuilder.RenameColumn(
                name: "InscricaoMunicipal",
                table: "configuracoes_empresa",
                newName: "inscricao_municipal");

            migrationBuilder.RenameColumn(
                name: "InscricaoEstadual",
                table: "configuracoes_empresa",
                newName: "inscricao_estadual");

            migrationBuilder.RenameColumn(
                name: "FocusNfeToken",
                table: "configuracoes_empresa",
                newName: "focus_nfe_token");

            migrationBuilder.RenameColumn(
                name: "EvolutionInstance",
                table: "configuracoes_empresa",
                newName: "evolution_instance");

            migrationBuilder.RenameColumn(
                name: "EvolutionApiUrl",
                table: "configuracoes_empresa",
                newName: "evolution_api_url");

            migrationBuilder.RenameColumn(
                name: "EvolutionApiKey",
                table: "configuracoes_empresa",
                newName: "evolution_api_key");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "configuracoes_empresa",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DescricaoPublica",
                table: "configuracoes_empresa",
                newName: "descricao_publica");

            migrationBuilder.RenameColumn(
                name: "CscToken",
                table: "configuracoes_empresa",
                newName: "csc_token");

            migrationBuilder.RenameColumn(
                name: "CscId",
                table: "configuracoes_empresa",
                newName: "csc_id");

            migrationBuilder.RenameColumn(
                name: "CorPrimaria",
                table: "configuracoes_empresa",
                newName: "cor_primaria");

            migrationBuilder.RenameColumn(
                name: "CodigoMunicipio",
                table: "configuracoes_empresa",
                newName: "codigo_municipio");

            migrationBuilder.RenameColumn(
                name: "AsaasSandbox",
                table: "configuracoes_empresa",
                newName: "asaas_sandbox");

            migrationBuilder.RenameColumn(
                name: "AsaasApiKey",
                table: "configuracoes_empresa",
                newName: "asaas_api_key");

            migrationBuilder.RenameIndex(
                name: "IX_ConfiguracoesEmpresa_Slug",
                table: "configuracoes_empresa",
                newName: "ix_configuracoes_empresa_slug");

            migrationBuilder.RenameColumn(
                name: "Motivo",
                table: "bloqueios_agenda",
                newName: "motivo");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "bloqueios_agenda",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ProfissionalId",
                table: "bloqueios_agenda",
                newName: "profissional_id");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "bloqueios_agenda",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "DataInicio",
                table: "bloqueios_agenda",
                newName: "data_inicio");

            migrationBuilder.RenameColumn(
                name: "DataFim",
                table: "bloqueios_agenda",
                newName: "data_fim");

            migrationBuilder.RenameIndex(
                name: "IX_BloqueiosAgenda_ProfissionalId",
                table: "bloqueios_agenda",
                newName: "ix_bloqueios_agenda_profissional_id");

            migrationBuilder.RenameColumn(
                name: "Sucesso",
                table: "automacao_logs",
                newName: "sucesso");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "automacao_logs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TipoEvento",
                table: "automacao_logs",
                newName: "tipo_evento");

            migrationBuilder.RenameColumn(
                name: "ErroMsg",
                table: "automacao_logs",
                newName: "erro_msg");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "automacao_logs",
                newName: "empresa_id");

            migrationBuilder.RenameColumn(
                name: "CobrancaId",
                table: "automacao_logs",
                newName: "cobranca_id");

            migrationBuilder.RenameColumn(
                name: "EnviadoEm",
                table: "automacao_logs",
                newName: "criado_em");

            migrationBuilder.RenameIndex(
                name: "IX_AutomacaoLogs_CobrancaId_TipoEvento",
                table: "automacao_logs",
                newName: "ix_automacao_logs_cobranca_id_tipo_evento");

            migrationBuilder.AddPrimaryKey(
                name: "pk_vendas",
                table: "vendas",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_profissionais",
                table: "profissionais",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_produtos",
                table: "produtos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_orcamentos",
                table: "orcamentos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_lancamentos",
                table: "lancamentos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fornecedores",
                table: "fornecedores",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_contrato_template_itens",
                table: "ContratoTemplateItens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_contratos",
                table: "contratos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_contrato_itens",
                table: "ContratoItens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_cobrancas",
                table: "cobrancas",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_clientes",
                table: "clientes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_categorias",
                table: "categorias",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_agendamentos",
                table: "agendamentos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_orcamento_itens",
                table: "orcamento_itens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_notas_fiscais",
                table: "notas_fiscais",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_nota_fiscal_itens",
                table: "nota_fiscal_itens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_movimentacoes_estoque",
                table: "movimentacoes_estoque",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_itens_venda",
                table: "itens_venda",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_disponibilidade_semanais",
                table: "disponibilidade_semanais",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_contrato_templates",
                table: "contrato_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_configuracoes_empresa",
                table: "configuracoes_empresa",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_bloqueios_agenda",
                table: "bloqueios_agenda",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_automacao_logs",
                table: "automacao_logs",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_agendamentos_clientes_cliente_id",
                table: "agendamentos",
                column: "cliente_id",
                principalTable: "clientes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_agendamentos_produtos_servico_id",
                table: "agendamentos",
                column: "servico_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_agendamentos_profissionais_profissional_id",
                table: "agendamentos",
                column: "profissional_id",
                principalTable: "profissionais",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_bloqueios_agenda_profissionais_profissional_id",
                table: "bloqueios_agenda",
                column: "profissional_id",
                principalTable: "profissionais",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_cobrancas_clientes_cliente_id",
                table: "cobrancas",
                column: "cliente_id",
                principalTable: "clientes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cobrancas_contratos_contrato_id",
                table: "cobrancas",
                column: "contrato_id",
                principalTable: "contratos",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_contrato_itens_contratos_contrato_id",
                table: "ContratoItens",
                column: "contrato_id",
                principalTable: "contratos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_contratos_clientes_cliente_id",
                table: "contratos",
                column: "cliente_id",
                principalTable: "clientes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contrato_template_itens_contrato_templates_contrato_template_",
                table: "ContratoTemplateItens",
                column: "contrato_template_id",
                principalTable: "contrato_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_disponibilidade_semanais_profissionais_profissional_id",
                table: "disponibilidade_semanais",
                column: "profissional_id",
                principalTable: "profissionais",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_itens_venda_produtos_produto_id",
                table: "itens_venda",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_itens_venda_vendas_venda_id",
                table: "itens_venda",
                column: "venda_id",
                principalTable: "vendas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lancamentos_vendas_venda_id",
                table: "lancamentos",
                column: "venda_id",
                principalTable: "vendas",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_movimentacoes_estoque_produtos_produto_id",
                table: "movimentacoes_estoque",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_nota_fiscal_itens_notas_fiscais_nota_fiscal_id",
                table: "nota_fiscal_itens",
                column: "nota_fiscal_id",
                principalTable: "notas_fiscais",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_notas_fiscais_vendas_venda_id",
                table: "notas_fiscais",
                column: "venda_id",
                principalTable: "vendas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_orcamento_itens_orcamentos_orcamento_id",
                table: "orcamento_itens",
                column: "orcamento_id",
                principalTable: "orcamentos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_orcamento_itens_produtos_produto_id",
                table: "orcamento_itens",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_orcamentos_clientes_cliente_id",
                table: "orcamentos",
                column: "cliente_id",
                principalTable: "clientes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_produtos_categorias_categoria_id",
                table: "produtos",
                column: "categoria_id",
                principalTable: "categorias",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_vendas_clientes_cliente_id",
                table: "vendas",
                column: "cliente_id",
                principalTable: "clientes",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_agendamentos_clientes_cliente_id",
                table: "agendamentos");

            migrationBuilder.DropForeignKey(
                name: "fk_agendamentos_produtos_servico_id",
                table: "agendamentos");

            migrationBuilder.DropForeignKey(
                name: "fk_agendamentos_profissionais_profissional_id",
                table: "agendamentos");

            migrationBuilder.DropForeignKey(
                name: "fk_bloqueios_agenda_profissionais_profissional_id",
                table: "bloqueios_agenda");

            migrationBuilder.DropForeignKey(
                name: "fk_cobrancas_clientes_cliente_id",
                table: "cobrancas");

            migrationBuilder.DropForeignKey(
                name: "fk_cobrancas_contratos_contrato_id",
                table: "cobrancas");

            migrationBuilder.DropForeignKey(
                name: "fk_contrato_itens_contratos_contrato_id",
                table: "ContratoItens");

            migrationBuilder.DropForeignKey(
                name: "fk_contratos_clientes_cliente_id",
                table: "contratos");

            migrationBuilder.DropForeignKey(
                name: "fk_contrato_template_itens_contrato_templates_contrato_template_",
                table: "ContratoTemplateItens");

            migrationBuilder.DropForeignKey(
                name: "fk_disponibilidade_semanais_profissionais_profissional_id",
                table: "disponibilidade_semanais");

            migrationBuilder.DropForeignKey(
                name: "fk_itens_venda_produtos_produto_id",
                table: "itens_venda");

            migrationBuilder.DropForeignKey(
                name: "fk_itens_venda_vendas_venda_id",
                table: "itens_venda");

            migrationBuilder.DropForeignKey(
                name: "fk_lancamentos_vendas_venda_id",
                table: "lancamentos");

            migrationBuilder.DropForeignKey(
                name: "fk_movimentacoes_estoque_produtos_produto_id",
                table: "movimentacoes_estoque");

            migrationBuilder.DropForeignKey(
                name: "fk_nota_fiscal_itens_notas_fiscais_nota_fiscal_id",
                table: "nota_fiscal_itens");

            migrationBuilder.DropForeignKey(
                name: "fk_notas_fiscais_vendas_venda_id",
                table: "notas_fiscais");

            migrationBuilder.DropForeignKey(
                name: "fk_orcamento_itens_orcamentos_orcamento_id",
                table: "orcamento_itens");

            migrationBuilder.DropForeignKey(
                name: "fk_orcamento_itens_produtos_produto_id",
                table: "orcamento_itens");

            migrationBuilder.DropForeignKey(
                name: "fk_orcamentos_clientes_cliente_id",
                table: "orcamentos");

            migrationBuilder.DropForeignKey(
                name: "fk_produtos_categorias_categoria_id",
                table: "produtos");

            migrationBuilder.DropForeignKey(
                name: "fk_vendas_clientes_cliente_id",
                table: "vendas");

            migrationBuilder.DropPrimaryKey(
                name: "pk_vendas",
                table: "vendas");

            migrationBuilder.DropPrimaryKey(
                name: "pk_profissionais",
                table: "profissionais");

            migrationBuilder.DropPrimaryKey(
                name: "pk_produtos",
                table: "produtos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_orcamentos",
                table: "orcamentos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_lancamentos",
                table: "lancamentos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fornecedores",
                table: "fornecedores");

            migrationBuilder.DropPrimaryKey(
                name: "pk_contrato_template_itens",
                table: "ContratoTemplateItens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_contratos",
                table: "contratos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_contrato_itens",
                table: "ContratoItens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_cobrancas",
                table: "cobrancas");

            migrationBuilder.DropPrimaryKey(
                name: "pk_clientes",
                table: "clientes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_categorias",
                table: "categorias");

            migrationBuilder.DropPrimaryKey(
                name: "pk_agendamentos",
                table: "agendamentos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_orcamento_itens",
                table: "orcamento_itens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_notas_fiscais",
                table: "notas_fiscais");

            migrationBuilder.DropPrimaryKey(
                name: "pk_nota_fiscal_itens",
                table: "nota_fiscal_itens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_movimentacoes_estoque",
                table: "movimentacoes_estoque");

            migrationBuilder.DropPrimaryKey(
                name: "pk_itens_venda",
                table: "itens_venda");

            migrationBuilder.DropPrimaryKey(
                name: "pk_disponibilidade_semanais",
                table: "disponibilidade_semanais");

            migrationBuilder.DropPrimaryKey(
                name: "pk_contrato_templates",
                table: "contrato_templates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_configuracoes_empresa",
                table: "configuracoes_empresa");

            migrationBuilder.DropPrimaryKey(
                name: "pk_bloqueios_agenda",
                table: "bloqueios_agenda");

            migrationBuilder.DropPrimaryKey(
                name: "pk_automacao_logs",
                table: "automacao_logs");

            migrationBuilder.RenameTable(
                name: "vendas",
                newName: "Vendas");

            migrationBuilder.RenameTable(
                name: "profissionais",
                newName: "Profissionais");

            migrationBuilder.RenameTable(
                name: "produtos",
                newName: "Produtos");

            migrationBuilder.RenameTable(
                name: "orcamentos",
                newName: "Orcamentos");

            migrationBuilder.RenameTable(
                name: "lancamentos",
                newName: "Lancamentos");

            migrationBuilder.RenameTable(
                name: "fornecedores",
                newName: "Fornecedores");

            migrationBuilder.RenameTable(
                name: "contratos",
                newName: "Contratos");

            migrationBuilder.RenameTable(
                name: "cobrancas",
                newName: "Cobrancas");

            migrationBuilder.RenameTable(
                name: "clientes",
                newName: "Clientes");

            migrationBuilder.RenameTable(
                name: "categorias",
                newName: "Categorias");

            migrationBuilder.RenameTable(
                name: "agendamentos",
                newName: "Agendamentos");

            migrationBuilder.RenameTable(
                name: "orcamento_itens",
                newName: "OrcamentoItens");

            migrationBuilder.RenameTable(
                name: "notas_fiscais",
                newName: "NotasFiscais");

            migrationBuilder.RenameTable(
                name: "nota_fiscal_itens",
                newName: "NotaFiscalItens");

            migrationBuilder.RenameTable(
                name: "movimentacoes_estoque",
                newName: "MovimentacoesEstoque");

            migrationBuilder.RenameTable(
                name: "itens_venda",
                newName: "ItensVenda");

            migrationBuilder.RenameTable(
                name: "disponibilidade_semanais",
                newName: "DisponibilidadeSemanais");

            migrationBuilder.RenameTable(
                name: "contrato_templates",
                newName: "ContratoTemplates");

            migrationBuilder.RenameTable(
                name: "configuracoes_empresa",
                newName: "ConfiguracoesEmpresa");

            migrationBuilder.RenameTable(
                name: "bloqueios_agenda",
                newName: "BloqueiosAgenda");

            migrationBuilder.RenameTable(
                name: "automacao_logs",
                newName: "AutomacaoLogs");

            migrationBuilder.RenameColumn(
                name: "total",
                table: "Vendas",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "subtotal",
                table: "Vendas",
                newName: "Subtotal");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Vendas",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "parcelas",
                table: "Vendas",
                newName: "Parcelas");

            migrationBuilder.RenameColumn(
                name: "observacao",
                table: "Vendas",
                newName: "Observacao");

            migrationBuilder.RenameColumn(
                name: "desconto",
                table: "Vendas",
                newName: "Desconto");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Vendas",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "forma_pagamento",
                table: "Vendas",
                newName: "FormaPagamento");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "Vendas",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "data_hora",
                table: "Vendas",
                newName: "DataHora");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                table: "Vendas",
                newName: "ClienteId");

            migrationBuilder.RenameIndex(
                name: "ix_vendas_cliente_id",
                table: "Vendas",
                newName: "IX_Vendas_ClienteId");

            migrationBuilder.RenameColumn(
                name: "telefone",
                table: "Profissionais",
                newName: "Telefone");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "Profissionais",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "ativo",
                table: "Profissionais",
                newName: "Ativo");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Profissionais",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "Profissionais",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                table: "Profissionais",
                newName: "CriadoEm");

            migrationBuilder.RenameColumn(
                name: "tipo",
                table: "Produtos",
                newName: "Tipo");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "Produtos",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "descricao",
                table: "Produtos",
                newName: "Descricao");

            migrationBuilder.RenameColumn(
                name: "ativo",
                table: "Produtos",
                newName: "Ativo");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Produtos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "preco_venda",
                table: "Produtos",
                newName: "PrecoVenda");

            migrationBuilder.RenameColumn(
                name: "estoque_minimo",
                table: "Produtos",
                newName: "EstoqueMinimo");

            migrationBuilder.RenameColumn(
                name: "estoque_atual",
                table: "Produtos",
                newName: "EstoqueAtual");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "Produtos",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "duracao_minutos",
                table: "Produtos",
                newName: "DuracaoMinutos");

            migrationBuilder.RenameColumn(
                name: "custo_medio",
                table: "Produtos",
                newName: "CustoMedio");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                table: "Produtos",
                newName: "CriadoEm");

            migrationBuilder.RenameColumn(
                name: "codigo_barras",
                table: "Produtos",
                newName: "CodigoBarras");

            migrationBuilder.RenameColumn(
                name: "categoria_id",
                table: "Produtos",
                newName: "CategoriaId");

            migrationBuilder.RenameColumn(
                name: "atualizado_em",
                table: "Produtos",
                newName: "AtualizadoEm");

            migrationBuilder.RenameIndex(
                name: "ix_produtos_categoria_id",
                table: "Produtos",
                newName: "IX_Produtos_CategoriaId");

            migrationBuilder.RenameColumn(
                name: "titulo",
                table: "Orcamentos",
                newName: "Titulo");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Orcamentos",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "observacao",
                table: "Orcamentos",
                newName: "Observacao");

            migrationBuilder.RenameColumn(
                name: "numero",
                table: "Orcamentos",
                newName: "Numero");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Orcamentos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "venda_id",
                table: "Orcamentos",
                newName: "VendaId");

            migrationBuilder.RenameColumn(
                name: "token_publico",
                table: "Orcamentos",
                newName: "TokenPublico");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "Orcamentos",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "data_validade",
                table: "Orcamentos",
                newName: "DataValidade");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                table: "Orcamentos",
                newName: "CriadoEm");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                table: "Orcamentos",
                newName: "ClienteId");

            migrationBuilder.RenameIndex(
                name: "ix_orcamentos_cliente_id",
                table: "Orcamentos",
                newName: "IX_Orcamentos_ClienteId");

            migrationBuilder.RenameColumn(
                name: "valor",
                table: "Lancamentos",
                newName: "Valor");

            migrationBuilder.RenameColumn(
                name: "tipo",
                table: "Lancamentos",
                newName: "Tipo");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Lancamentos",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "observacao",
                table: "Lancamentos",
                newName: "Observacao");

            migrationBuilder.RenameColumn(
                name: "descricao",
                table: "Lancamentos",
                newName: "Descricao");

            migrationBuilder.RenameColumn(
                name: "categoria",
                table: "Lancamentos",
                newName: "Categoria");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Lancamentos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "venda_id",
                table: "Lancamentos",
                newName: "VendaId");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "Lancamentos",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "data_vencimento",
                table: "Lancamentos",
                newName: "DataVencimento");

            migrationBuilder.RenameColumn(
                name: "data_pagamento",
                table: "Lancamentos",
                newName: "DataPagamento");

            migrationBuilder.RenameIndex(
                name: "ix_lancamentos_venda_id",
                table: "Lancamentos",
                newName: "IX_Lancamentos_VendaId");

            migrationBuilder.RenameColumn(
                name: "uf",
                table: "Fornecedores",
                newName: "Uf");

            migrationBuilder.RenameColumn(
                name: "telefone",
                table: "Fornecedores",
                newName: "Telefone");

            migrationBuilder.RenameColumn(
                name: "observacoes",
                table: "Fornecedores",
                newName: "Observacoes");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "Fornecedores",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "logradouro",
                table: "Fornecedores",
                newName: "Logradouro");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Fornecedores",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "contato",
                table: "Fornecedores",
                newName: "Contato");

            migrationBuilder.RenameColumn(
                name: "cidade",
                table: "Fornecedores",
                newName: "Cidade");

            migrationBuilder.RenameColumn(
                name: "cep",
                table: "Fornecedores",
                newName: "Cep");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Fornecedores",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "Fornecedores",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "data_cadastro",
                table: "Fornecedores",
                newName: "DataCadastro");

            migrationBuilder.RenameColumn(
                name: "cnpj_cpf",
                table: "Fornecedores",
                newName: "CnpjCpf");

            migrationBuilder.RenameIndex(
                name: "ix_fornecedores_empresa_id_cnpj_cpf",
                table: "Fornecedores",
                newName: "IX_Fornecedores_EmpresaId_CnpjCpf");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                table: "ContratoTemplateItens",
                newName: "Quantidade");

            migrationBuilder.RenameColumn(
                name: "descricao",
                table: "ContratoTemplateItens",
                newName: "Descricao");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ContratoTemplateItens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "valor_unitario",
                table: "ContratoTemplateItens",
                newName: "ValorUnitario");

            migrationBuilder.RenameColumn(
                name: "contrato_template_id",
                table: "ContratoTemplateItens",
                newName: "ContratoTemplateId");

            migrationBuilder.RenameIndex(
                name: "ix_contrato_template_itens_contrato_template_id",
                table: "ContratoTemplateItens",
                newName: "IX_ContratoTemplateItens_ContratoTemplateId");

            migrationBuilder.RenameColumn(
                name: "valor",
                table: "Contratos",
                newName: "Valor");

            migrationBuilder.RenameColumn(
                name: "titulo",
                table: "Contratos",
                newName: "Titulo");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Contratos",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "periodicidade",
                table: "Contratos",
                newName: "Periodicidade");

            migrationBuilder.RenameColumn(
                name: "observacao",
                table: "Contratos",
                newName: "Observacao");

            migrationBuilder.RenameColumn(
                name: "objeto",
                table: "Contratos",
                newName: "Objeto");

            migrationBuilder.RenameColumn(
                name: "numero",
                table: "Contratos",
                newName: "Numero");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Contratos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "tipo_cobranca",
                table: "Contratos",
                newName: "TipoCobranca");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "Contratos",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "dia_vencimento",
                table: "Contratos",
                newName: "DiaVencimento");

            migrationBuilder.RenameColumn(
                name: "data_inicio",
                table: "Contratos",
                newName: "DataInicio");

            migrationBuilder.RenameColumn(
                name: "data_fim",
                table: "Contratos",
                newName: "DataFim");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                table: "Contratos",
                newName: "CriadoEm");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                table: "Contratos",
                newName: "ClienteId");

            migrationBuilder.RenameIndex(
                name: "ix_contratos_cliente_id",
                table: "Contratos",
                newName: "IX_Contratos_ClienteId");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                table: "ContratoItens",
                newName: "Quantidade");

            migrationBuilder.RenameColumn(
                name: "descricao",
                table: "ContratoItens",
                newName: "Descricao");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ContratoItens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "valor_unitario",
                table: "ContratoItens",
                newName: "ValorUnitario");

            migrationBuilder.RenameColumn(
                name: "contrato_id",
                table: "ContratoItens",
                newName: "ContratoId");

            migrationBuilder.RenameIndex(
                name: "ix_contrato_itens_contrato_id",
                table: "ContratoItens",
                newName: "IX_ContratoItens_ContratoId");

            migrationBuilder.RenameColumn(
                name: "valor",
                table: "Cobrancas",
                newName: "Valor");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Cobrancas",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "referencia",
                table: "Cobrancas",
                newName: "Referencia");

            migrationBuilder.RenameColumn(
                name: "observacao",
                table: "Cobrancas",
                newName: "Observacao");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Cobrancas",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "forma_pagamento",
                table: "Cobrancas",
                newName: "FormaPagamento");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "Cobrancas",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "data_vencimento",
                table: "Cobrancas",
                newName: "DataVencimento");

            migrationBuilder.RenameColumn(
                name: "data_pagamento",
                table: "Cobrancas",
                newName: "DataPagamento");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                table: "Cobrancas",
                newName: "CriadoEm");

            migrationBuilder.RenameColumn(
                name: "contrato_id",
                table: "Cobrancas",
                newName: "ContratoId");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                table: "Cobrancas",
                newName: "ClienteId");

            migrationBuilder.RenameColumn(
                name: "asaas_pix_qr_code",
                table: "Cobrancas",
                newName: "AsaasPixQrCode");

            migrationBuilder.RenameColumn(
                name: "asaas_payment_link",
                table: "Cobrancas",
                newName: "AsaasPaymentLink");

            migrationBuilder.RenameColumn(
                name: "asaas_id",
                table: "Cobrancas",
                newName: "AsaasId");

            migrationBuilder.RenameColumn(
                name: "asaas_boleto_url",
                table: "Cobrancas",
                newName: "AsaasBoletoUrl");

            migrationBuilder.RenameIndex(
                name: "ix_cobrancas_contrato_id",
                table: "Cobrancas",
                newName: "IX_Cobrancas_ContratoId");

            migrationBuilder.RenameIndex(
                name: "ix_cobrancas_cliente_id",
                table: "Cobrancas",
                newName: "IX_Cobrancas_ClienteId");

            migrationBuilder.RenameColumn(
                name: "whatsapp",
                table: "Clientes",
                newName: "Whatsapp");

            migrationBuilder.RenameColumn(
                name: "observacoes",
                table: "Clientes",
                newName: "Observacoes");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "Clientes",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Clientes",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Clientes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "Clientes",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "data_cadastro",
                table: "Clientes",
                newName: "DataCadastro");

            migrationBuilder.RenameIndex(
                name: "ix_clientes_empresa_id_whatsapp",
                table: "Clientes",
                newName: "IX_Clientes_EmpresaId_Whatsapp");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "Categorias",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Categorias",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "Categorias",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Agendamentos",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "observacao",
                table: "Agendamentos",
                newName: "Observacao");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Agendamentos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "venda_id",
                table: "Agendamentos",
                newName: "VendaId");

            migrationBuilder.RenameColumn(
                name: "servico_id",
                table: "Agendamentos",
                newName: "ServicoId");

            migrationBuilder.RenameColumn(
                name: "profissional_id",
                table: "Agendamentos",
                newName: "ProfissionalId");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "Agendamentos",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "data_hora_inicio",
                table: "Agendamentos",
                newName: "DataHoraInicio");

            migrationBuilder.RenameColumn(
                name: "data_hora_fim",
                table: "Agendamentos",
                newName: "DataHoraFim");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                table: "Agendamentos",
                newName: "CriadoEm");

            migrationBuilder.RenameColumn(
                name: "cliente_telefone",
                table: "Agendamentos",
                newName: "ClienteTelefone");

            migrationBuilder.RenameColumn(
                name: "cliente_nome",
                table: "Agendamentos",
                newName: "ClienteNome");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                table: "Agendamentos",
                newName: "ClienteId");

            migrationBuilder.RenameIndex(
                name: "ix_agendamentos_servico_id",
                table: "Agendamentos",
                newName: "IX_Agendamentos_ServicoId");

            migrationBuilder.RenameIndex(
                name: "ix_agendamentos_profissional_id",
                table: "Agendamentos",
                newName: "IX_Agendamentos_ProfissionalId");

            migrationBuilder.RenameIndex(
                name: "ix_agendamentos_cliente_id",
                table: "Agendamentos",
                newName: "IX_Agendamentos_ClienteId");

            migrationBuilder.RenameColumn(
                name: "tipo",
                table: "OrcamentoItens",
                newName: "Tipo");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                table: "OrcamentoItens",
                newName: "Quantidade");

            migrationBuilder.RenameColumn(
                name: "descricao",
                table: "OrcamentoItens",
                newName: "Descricao");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "OrcamentoItens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "valor_unitario",
                table: "OrcamentoItens",
                newName: "ValorUnitario");

            migrationBuilder.RenameColumn(
                name: "produto_id",
                table: "OrcamentoItens",
                newName: "ProdutoId");

            migrationBuilder.RenameColumn(
                name: "orcamento_id",
                table: "OrcamentoItens",
                newName: "OrcamentoId");

            migrationBuilder.RenameIndex(
                name: "ix_orcamento_itens_produto_id",
                table: "OrcamentoItens",
                newName: "IX_OrcamentoItens_ProdutoId");

            migrationBuilder.RenameIndex(
                name: "ix_orcamento_itens_orcamento_id",
                table: "OrcamentoItens",
                newName: "IX_OrcamentoItens_OrcamentoId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "NotasFiscais",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "serie",
                table: "NotasFiscais",
                newName: "Serie");

            migrationBuilder.RenameColumn(
                name: "protocolo",
                table: "NotasFiscais",
                newName: "Protocolo");

            migrationBuilder.RenameColumn(
                name: "numero",
                table: "NotasFiscais",
                newName: "Numero");

            migrationBuilder.RenameColumn(
                name: "modelo",
                table: "NotasFiscais",
                newName: "Modelo");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "NotasFiscais",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "xml_url",
                table: "NotasFiscais",
                newName: "XmlUrl");

            migrationBuilder.RenameColumn(
                name: "venda_id",
                table: "NotasFiscais",
                newName: "VendaId");

            migrationBuilder.RenameColumn(
                name: "protocolo_cancelamento",
                table: "NotasFiscais",
                newName: "ProtocoloCancelamento");

            migrationBuilder.RenameColumn(
                name: "pdf_url",
                table: "NotasFiscais",
                newName: "PdfUrl");

            migrationBuilder.RenameColumn(
                name: "mensagem_erro",
                table: "NotasFiscais",
                newName: "MensagemErro");

            migrationBuilder.RenameColumn(
                name: "focus_nfe_ref",
                table: "NotasFiscais",
                newName: "FocusNfeRef");

            migrationBuilder.RenameColumn(
                name: "focus_nfe_id",
                table: "NotasFiscais",
                newName: "FocusNfeId");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "NotasFiscais",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "criada_em",
                table: "NotasFiscais",
                newName: "CriadaEm");

            migrationBuilder.RenameColumn(
                name: "chave_acesso",
                table: "NotasFiscais",
                newName: "ChaveAcesso");

            migrationBuilder.RenameColumn(
                name: "cancelada_em",
                table: "NotasFiscais",
                newName: "CanceladaEm");

            migrationBuilder.RenameColumn(
                name: "autorizada_em",
                table: "NotasFiscais",
                newName: "AutorizadaEm");

            migrationBuilder.RenameIndex(
                name: "ix_notas_fiscais_venda_id",
                table: "NotasFiscais",
                newName: "IX_NotasFiscais_VendaId");

            migrationBuilder.RenameColumn(
                name: "total",
                table: "NotaFiscalItens",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                table: "NotaFiscalItens",
                newName: "Quantidade");

            migrationBuilder.RenameColumn(
                name: "ncm",
                table: "NotaFiscalItens",
                newName: "Ncm");

            migrationBuilder.RenameColumn(
                name: "cfop",
                table: "NotaFiscalItens",
                newName: "Cfop");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "NotaFiscalItens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "preco_unitario",
                table: "NotaFiscalItens",
                newName: "PrecoUnitario");

            migrationBuilder.RenameColumn(
                name: "nota_fiscal_id",
                table: "NotaFiscalItens",
                newName: "NotaFiscalId");

            migrationBuilder.RenameColumn(
                name: "nome_produto",
                table: "NotaFiscalItens",
                newName: "NomeProduto");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "NotaFiscalItens",
                newName: "EmpresaId");

            migrationBuilder.RenameIndex(
                name: "ix_nota_fiscal_itens_nota_fiscal_id",
                table: "NotaFiscalItens",
                newName: "IX_NotaFiscalItens_NotaFiscalId");

            migrationBuilder.RenameColumn(
                name: "tipo",
                table: "MovimentacoesEstoque",
                newName: "Tipo");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                table: "MovimentacoesEstoque",
                newName: "Quantidade");

            migrationBuilder.RenameColumn(
                name: "origem",
                table: "MovimentacoesEstoque",
                newName: "Origem");

            migrationBuilder.RenameColumn(
                name: "observacao",
                table: "MovimentacoesEstoque",
                newName: "Observacao");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "MovimentacoesEstoque",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "referencia_id",
                table: "MovimentacoesEstoque",
                newName: "ReferenciaId");

            migrationBuilder.RenameColumn(
                name: "produto_id",
                table: "MovimentacoesEstoque",
                newName: "ProdutoId");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "MovimentacoesEstoque",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "data_hora",
                table: "MovimentacoesEstoque",
                newName: "DataHora");

            migrationBuilder.RenameIndex(
                name: "ix_movimentacoes_estoque_produto_id",
                table: "MovimentacoesEstoque",
                newName: "IX_MovimentacoesEstoque_ProdutoId");

            migrationBuilder.RenameColumn(
                name: "total",
                table: "ItensVenda",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                table: "ItensVenda",
                newName: "Quantidade");

            migrationBuilder.RenameColumn(
                name: "desconto",
                table: "ItensVenda",
                newName: "Desconto");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ItensVenda",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "venda_id",
                table: "ItensVenda",
                newName: "VendaId");

            migrationBuilder.RenameColumn(
                name: "produto_id",
                table: "ItensVenda",
                newName: "ProdutoId");

            migrationBuilder.RenameColumn(
                name: "preco_unitario",
                table: "ItensVenda",
                newName: "PrecoUnitario");

            migrationBuilder.RenameIndex(
                name: "ix_itens_venda_venda_id",
                table: "ItensVenda",
                newName: "IX_ItensVenda_VendaId");

            migrationBuilder.RenameIndex(
                name: "ix_itens_venda_produto_id",
                table: "ItensVenda",
                newName: "IX_ItensVenda_ProdutoId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "DisponibilidadeSemanais",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "profissional_id",
                table: "DisponibilidadeSemanais",
                newName: "ProfissionalId");

            migrationBuilder.RenameColumn(
                name: "hora_inicio",
                table: "DisponibilidadeSemanais",
                newName: "HoraInicio");

            migrationBuilder.RenameColumn(
                name: "hora_fim",
                table: "DisponibilidadeSemanais",
                newName: "HoraFim");

            migrationBuilder.RenameColumn(
                name: "dia_semana",
                table: "DisponibilidadeSemanais",
                newName: "DiaSemana");

            migrationBuilder.RenameColumn(
                name: "data_inicio",
                table: "DisponibilidadeSemanais",
                newName: "DataInicio");

            migrationBuilder.RenameColumn(
                name: "data_fim",
                table: "DisponibilidadeSemanais",
                newName: "DataFim");

            migrationBuilder.RenameIndex(
                name: "ix_disponibilidade_semanais_profissional_id",
                table: "DisponibilidadeSemanais",
                newName: "IX_DisponibilidadeSemanais_ProfissionalId");

            migrationBuilder.RenameColumn(
                name: "periodicidade",
                table: "ContratoTemplates",
                newName: "Periodicidade");

            migrationBuilder.RenameColumn(
                name: "objeto",
                table: "ContratoTemplates",
                newName: "Objeto");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "ContratoTemplates",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ContratoTemplates",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "valor_padrao",
                table: "ContratoTemplates",
                newName: "ValorPadrao");

            migrationBuilder.RenameColumn(
                name: "tipo_cobranca",
                table: "ContratoTemplates",
                newName: "TipoCobranca");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "ContratoTemplates",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "dia_vencimento",
                table: "ContratoTemplates",
                newName: "DiaVencimento");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                table: "ContratoTemplates",
                newName: "CriadoEm");

            migrationBuilder.RenameColumn(
                name: "uf",
                table: "ConfiguracoesEmpresa",
                newName: "Uf");

            migrationBuilder.RenameColumn(
                name: "slug",
                table: "ConfiguracoesEmpresa",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "numero",
                table: "ConfiguracoesEmpresa",
                newName: "Numero");

            migrationBuilder.RenameColumn(
                name: "municipio",
                table: "ConfiguracoesEmpresa",
                newName: "Municipio");

            migrationBuilder.RenameColumn(
                name: "logradouro",
                table: "ConfiguracoesEmpresa",
                newName: "Logradouro");

            migrationBuilder.RenameColumn(
                name: "complemento",
                table: "ConfiguracoesEmpresa",
                newName: "Complemento");

            migrationBuilder.RenameColumn(
                name: "cnpj",
                table: "ConfiguracoesEmpresa",
                newName: "Cnpj");

            migrationBuilder.RenameColumn(
                name: "cep",
                table: "ConfiguracoesEmpresa",
                newName: "Cep");

            migrationBuilder.RenameColumn(
                name: "bairro",
                table: "ConfiguracoesEmpresa",
                newName: "Bairro");

            migrationBuilder.RenameColumn(
                name: "ambiente",
                table: "ConfiguracoesEmpresa",
                newName: "Ambiente");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ConfiguracoesEmpresa",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "serie_nfe",
                table: "ConfiguracoesEmpresa",
                newName: "SerieNfe");

            migrationBuilder.RenameColumn(
                name: "serie_nfce",
                table: "ConfiguracoesEmpresa",
                newName: "SerieNfce");

            migrationBuilder.RenameColumn(
                name: "regime_tributario",
                table: "ConfiguracoesEmpresa",
                newName: "RegimeTributario");

            migrationBuilder.RenameColumn(
                name: "razao_social",
                table: "ConfiguracoesEmpresa",
                newName: "RazaoSocial");

            migrationBuilder.RenameColumn(
                name: "nome_fantasia",
                table: "ConfiguracoesEmpresa",
                newName: "NomeFantasia");

            migrationBuilder.RenameColumn(
                name: "logo_url",
                table: "ConfiguracoesEmpresa",
                newName: "LogoUrl");

            migrationBuilder.RenameColumn(
                name: "lembrete_no_dia",
                table: "ConfiguracoesEmpresa",
                newName: "LembreteNoDia");

            migrationBuilder.RenameColumn(
                name: "lembrete7d_depois",
                table: "ConfiguracoesEmpresa",
                newName: "Lembrete7dDepois");

            migrationBuilder.RenameColumn(
                name: "lembrete3d_depois",
                table: "ConfiguracoesEmpresa",
                newName: "Lembrete3dDepois");

            migrationBuilder.RenameColumn(
                name: "lembrete3d_antes",
                table: "ConfiguracoesEmpresa",
                newName: "Lembrete3dAntes");

            migrationBuilder.RenameColumn(
                name: "lembrete1d_depois",
                table: "ConfiguracoesEmpresa",
                newName: "Lembrete1dDepois");

            migrationBuilder.RenameColumn(
                name: "lembrete1d_antes",
                table: "ConfiguracoesEmpresa",
                newName: "Lembrete1dAntes");

            migrationBuilder.RenameColumn(
                name: "inscricao_municipal",
                table: "ConfiguracoesEmpresa",
                newName: "InscricaoMunicipal");

            migrationBuilder.RenameColumn(
                name: "inscricao_estadual",
                table: "ConfiguracoesEmpresa",
                newName: "InscricaoEstadual");

            migrationBuilder.RenameColumn(
                name: "focus_nfe_token",
                table: "ConfiguracoesEmpresa",
                newName: "FocusNfeToken");

            migrationBuilder.RenameColumn(
                name: "evolution_instance",
                table: "ConfiguracoesEmpresa",
                newName: "EvolutionInstance");

            migrationBuilder.RenameColumn(
                name: "evolution_api_url",
                table: "ConfiguracoesEmpresa",
                newName: "EvolutionApiUrl");

            migrationBuilder.RenameColumn(
                name: "evolution_api_key",
                table: "ConfiguracoesEmpresa",
                newName: "EvolutionApiKey");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "ConfiguracoesEmpresa",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "descricao_publica",
                table: "ConfiguracoesEmpresa",
                newName: "DescricaoPublica");

            migrationBuilder.RenameColumn(
                name: "csc_token",
                table: "ConfiguracoesEmpresa",
                newName: "CscToken");

            migrationBuilder.RenameColumn(
                name: "csc_id",
                table: "ConfiguracoesEmpresa",
                newName: "CscId");

            migrationBuilder.RenameColumn(
                name: "cor_primaria",
                table: "ConfiguracoesEmpresa",
                newName: "CorPrimaria");

            migrationBuilder.RenameColumn(
                name: "codigo_municipio",
                table: "ConfiguracoesEmpresa",
                newName: "CodigoMunicipio");

            migrationBuilder.RenameColumn(
                name: "asaas_sandbox",
                table: "ConfiguracoesEmpresa",
                newName: "AsaasSandbox");

            migrationBuilder.RenameColumn(
                name: "asaas_api_key",
                table: "ConfiguracoesEmpresa",
                newName: "AsaasApiKey");

            migrationBuilder.RenameIndex(
                name: "ix_configuracoes_empresa_slug",
                table: "ConfiguracoesEmpresa",
                newName: "IX_ConfiguracoesEmpresa_Slug");

            migrationBuilder.RenameColumn(
                name: "motivo",
                table: "BloqueiosAgenda",
                newName: "Motivo");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "BloqueiosAgenda",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "profissional_id",
                table: "BloqueiosAgenda",
                newName: "ProfissionalId");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "BloqueiosAgenda",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "data_inicio",
                table: "BloqueiosAgenda",
                newName: "DataInicio");

            migrationBuilder.RenameColumn(
                name: "data_fim",
                table: "BloqueiosAgenda",
                newName: "DataFim");

            migrationBuilder.RenameIndex(
                name: "ix_bloqueios_agenda_profissional_id",
                table: "BloqueiosAgenda",
                newName: "IX_BloqueiosAgenda_ProfissionalId");

            migrationBuilder.RenameColumn(
                name: "sucesso",
                table: "AutomacaoLogs",
                newName: "Sucesso");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AutomacaoLogs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "tipo_evento",
                table: "AutomacaoLogs",
                newName: "TipoEvento");

            migrationBuilder.RenameColumn(
                name: "erro_msg",
                table: "AutomacaoLogs",
                newName: "ErroMsg");

            migrationBuilder.RenameColumn(
                name: "empresa_id",
                table: "AutomacaoLogs",
                newName: "EmpresaId");

            migrationBuilder.RenameColumn(
                name: "cobranca_id",
                table: "AutomacaoLogs",
                newName: "CobrancaId");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                table: "AutomacaoLogs",
                newName: "EnviadoEm");

            migrationBuilder.RenameIndex(
                name: "ix_automacao_logs_cobranca_id_tipo_evento",
                table: "AutomacaoLogs",
                newName: "IX_AutomacaoLogs_CobrancaId_TipoEvento");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vendas",
                table: "Vendas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Profissionais",
                table: "Profissionais",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Produtos",
                table: "Produtos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orcamentos",
                table: "Orcamentos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Lancamentos",
                table: "Lancamentos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Fornecedores",
                table: "Fornecedores",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContratoTemplateItens",
                table: "ContratoTemplateItens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Contratos",
                table: "Contratos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContratoItens",
                table: "ContratoItens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cobrancas",
                table: "Cobrancas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Clientes",
                table: "Clientes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categorias",
                table: "Categorias",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Agendamentos",
                table: "Agendamentos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrcamentoItens",
                table: "OrcamentoItens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotasFiscais",
                table: "NotasFiscais",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotaFiscalItens",
                table: "NotaFiscalItens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovimentacoesEstoque",
                table: "MovimentacoesEstoque",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItensVenda",
                table: "ItensVenda",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DisponibilidadeSemanais",
                table: "DisponibilidadeSemanais",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContratoTemplates",
                table: "ContratoTemplates",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConfiguracoesEmpresa",
                table: "ConfiguracoesEmpresa",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BloqueiosAgenda",
                table: "BloqueiosAgenda",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AutomacaoLogs",
                table: "AutomacaoLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamentos_Clientes_ClienteId",
                table: "Agendamentos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamentos_Produtos_ServicoId",
                table: "Agendamentos",
                column: "ServicoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamentos_Profissionais_ProfissionalId",
                table: "Agendamentos",
                column: "ProfissionalId",
                principalTable: "Profissionais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AutomacaoLogs_Cobrancas_CobrancaId",
                table: "AutomacaoLogs",
                column: "CobrancaId",
                principalTable: "Cobrancas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BloqueiosAgenda_Profissionais_ProfissionalId",
                table: "BloqueiosAgenda",
                column: "ProfissionalId",
                principalTable: "Profissionais",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cobrancas_Clientes_ClienteId",
                table: "Cobrancas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cobrancas_Contratos_ContratoId",
                table: "Cobrancas",
                column: "ContratoId",
                principalTable: "Contratos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContratoItens_Contratos_ContratoId",
                table: "ContratoItens",
                column: "ContratoId",
                principalTable: "Contratos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_Clientes_ClienteId",
                table: "Contratos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContratoTemplateItens_ContratoTemplates_ContratoTemplateId",
                table: "ContratoTemplateItens",
                column: "ContratoTemplateId",
                principalTable: "ContratoTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DisponibilidadeSemanais_Profissionais_ProfissionalId",
                table: "DisponibilidadeSemanais",
                column: "ProfissionalId",
                principalTable: "Profissionais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItensVenda_Produtos_ProdutoId",
                table: "ItensVenda",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItensVenda_Vendas_VendaId",
                table: "ItensVenda",
                column: "VendaId",
                principalTable: "Vendas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lancamentos_Vendas_VendaId",
                table: "Lancamentos",
                column: "VendaId",
                principalTable: "Vendas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentacoesEstoque_Produtos_ProdutoId",
                table: "MovimentacoesEstoque",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotaFiscalItens_NotasFiscais_NotaFiscalId",
                table: "NotaFiscalItens",
                column: "NotaFiscalId",
                principalTable: "NotasFiscais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotasFiscais_Vendas_VendaId",
                table: "NotasFiscais",
                column: "VendaId",
                principalTable: "Vendas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrcamentoItens_Orcamentos_OrcamentoId",
                table: "OrcamentoItens",
                column: "OrcamentoId",
                principalTable: "Orcamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrcamentoItens_Produtos_ProdutoId",
                table: "OrcamentoItens",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orcamentos_Clientes_ClienteId",
                table: "Orcamentos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_Categorias_CategoriaId",
                table: "Produtos",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_Clientes_ClienteId",
                table: "Vendas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id");
        }
    }
}
