namespace GestorAI.API.DTOs.Relatorios;

// Visão Geral
public record KpisGeralResponse(
    // Vendas (período)
    decimal Faturamento,
    decimal TicketMedio,
    decimal MargemEstimada,
    int TotalVendas,
    int ClientesAtendidos,
    // Financeiro (período)
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal SaldoPeriodo,
    decimal Inadimplencia,
    // Situação atual
    decimal ContasReceber,
    decimal TotalVencidoCobrancas,
    int ContratosAtivos,
    decimal MrrContratos,
    int CobrancasVencidas,
    int OrcamentosAbertos,
    int AgendamentosNoPeriodo,
    // Gráficos e tabelas
    List<TendenciaVendasResponse> TendenciaVendas,
    List<FluxoCaixaDiaResponse> FluxoPorDia,
    List<RankingProdutoResponse> TopProdutos,
    List<RankingClienteResponse> TopClientes);

// Vendas
public record TendenciaVendasResponse(DateTime Data, decimal Total, int Quantity);
public record RankingProdutoResponse(string Name, decimal Quantity, decimal Total);
public record RankingClienteResponse(string Name, int Compras, decimal Total);
public record VendasPorPagamentoResponse(string PaymentMethod, int Quantity, decimal Total);

public record RelatorioVendasResponse(
    List<TendenciaVendasResponse> Tendencia,
    List<RankingProdutoResponse> TopProdutos,
    List<RankingClienteResponse> TopClientes,
    List<VendasPorPagamentoResponse> PorFormaPagamento);

// Financeiro
public record FluxoCaixaDiaResponse(DateTime Data, decimal Receitas, decimal Despesas, decimal Saldo);
public record CategoriaDespesaResponse(string Category, decimal Total);
public record LancamentoAnaliticoResponse(
    Guid Id,
    string Type,
    string Description,
    string Category,
    decimal Amount,
    DateTime DueDate,
    DateTime? PaymentDate,
    string Status);

public record RelatorioFinanceiroResponse(
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal Saldo,
    List<FluxoCaixaDiaResponse> FluxoPorDia,
    List<CategoriaDespesaResponse> CategoriasDespesas,
    List<LancamentoAnaliticoResponse> Analitico);

// Estoque
public record GiroProdutoResponse(string Name, decimal Entradas, decimal Saidas, decimal GiroLiquido);
public record ProdutoSemMovimentacaoResponse(string Name, decimal CurrentStock, decimal ValorEmEstoque);

public record RelatorioEstoqueResponse(
    decimal ValorTotalEstoque,
    int ProdutosAtivos,
    int ProdutosEstoqueBaixo,
    List<GiroProdutoResponse> GiroProdutos,
    List<ProdutoSemMovimentacaoResponse> SemMovimentacao);

// Clientes
public record ClienteRankingResponse(string Name, string WhatsApp, int Compras, decimal TotalGasto);

public record RelatorioClientesResponse(
    int TotalClientes,
    int ClientesCompraram,
    decimal TicketMedioCliente,
    List<ClienteRankingResponse> TopClientes);

// Histórico de Clientes (lifetime)
public record HistoricoClienteItemResponse(
    Guid CustomerId,
    string Name,
    string WhatsApp,
    int QtdPedidos,
    decimal TotalGasto,
    decimal TicketMedio,
    DateTime PrimeiraCompra,
    DateTime UltimaCompra,
    int? TempoMedioEntreComprasDias,
    int DiasDesdeUltimaCompra,
    string Classificacao);

public record HistoricoClientesResponse(
    int TotalClientesComCompras,
    int Recorrentes,
    int Inativos,
    int Novos,
    int EmRisco,
    decimal LtvMedio,
    decimal TicketMedioGeral,
    int? TempoMedioEntreComprasGeralDias,
    List<HistoricoClienteItemResponse> Clientes);

public record CompraHistoricoItemResponse(
    Guid SaleId,
    DateTime SaleDate,
    int QtdItens,
    decimal Total,
    string PaymentMethod,
    string Status);

public record HistoricoClienteDetalheResponse(
    Guid CustomerId,
    string Name,
    string WhatsApp,
    string? Email,
    DateTime CreatedAt,
    int QtdPedidos,
    decimal TotalGasto,
    decimal TicketMedio,
    DateTime PrimeiraCompra,
    DateTime UltimaCompra,
    int? TempoMedioEntreComprasDias,
    string Classificacao,
    List<CompraHistoricoItemResponse> Compras);

// Curva ABC
public record CurvaAbcItemResponse(
    string Name,
    decimal Quantity,
    decimal Total,
    decimal Percentual,
    decimal PercentualAcumulado,
    string Classe);

public record CurvaAbcResponse(
    List<CurvaAbcItemResponse> Items,
    decimal TotalGeral);

// DRE
public record DreLinhaResponse(string Description, decimal Amount);

public record DreResponse(
    decimal ReceitaBrutaVendas,
    decimal OutrasReceitas,
    decimal TotalDescontos,
    decimal ReceitaLiquida,
    decimal Cmv,
    decimal LucroBruto,
    decimal MargemBruta,
    List<DreLinhaResponse> DespesasOperacionais,
    decimal TotalDespesasOperacionais,
    decimal ResultadoOperacional,
    decimal MargemOperacional);

// Appointments
public record RelatorioAgendamentosResponse(
    int TotalNoPeriodo,
    int Concluidos,
    int Cancelados,
    decimal TaxaConclusao,
    decimal TaxaOcupacao,
    List<AgendamentoStatusItemRel> PorStatus,
    List<AgendamentoProfissionalItemRel> PorProfissional);

public record AgendamentoStatusItemRel(string Status, int Count);
public record AgendamentoProfissionalItemRel(string Professional, int Total, int Concluidos, decimal TaxaConclusao);

// Contratos
public record RelatorioContratosResponse(
    int TotalAtivos,
    decimal MrrTotal,
    int VencendoEm30,
    List<ContratoDetalheRel> Contratos);

public record ContratoDetalheRel(string Title, string CustomerName, decimal Amount, string Frequency, DateOnly? EndDate, string Status);

// Cobranças
public record RelatorioCobrancasResponse(
    decimal TotalReceber,
    decimal TotalVencido,
    int VencidosCount,
    decimal TaxaInadimplencia,
    List<AgingFaixaRel> Aging,
    List<CobrancaDetalheRel> Charges);

public record AgingFaixaRel(string Faixa, int Count, decimal Total);
public record CobrancaDetalheRel(string Reference, string CustomerName, decimal Amount, DateOnly DueDate, string Status, int DiasAtraso);

// Orçamentos
public record RelatorioOrcamentosResponse(
    int TotalNoPeriodo,
    decimal TaxaConversao,
    decimal ValorPipeline,
    List<OrcamentoStatusItemRel> PorStatus,
    List<OrcamentoDetalheRel> Orcamentos);

public record OrcamentoStatusItemRel(string Status, int Count, decimal TotalAmount);
public record OrcamentoDetalheRel(int Number, string Title, string CustomerName, decimal TotalAmount, string Status, DateTime CreatedAt);

// Assinaturas
public record RelatorioAssinaturasResponse(
    int TotalAtivas,
    decimal MrrTotal,
    int CanceladasNoPeriodo,
    decimal TaxaChurn,
    List<EvolucaoAssinaturaMesRel> Evolucao,
    List<AssinaturaDetalheRel> Assinaturas);

public record EvolucaoAssinaturaMesRel(string Mes, int Ativas, int Novas, int Canceladas);
public record AssinaturaDetalheRel(string CustomerName, string Plan, decimal Amount, string Frequency, DateOnly StartDate, DateOnly RenewalDate, string Status);
