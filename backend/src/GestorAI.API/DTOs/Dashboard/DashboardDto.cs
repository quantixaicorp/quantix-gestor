namespace GestorAI.API.DTOs.Dashboard;

public record DashboardLayoutResponse(List<string> Widgets);
public record UpdateDashboardLayoutRequest(List<string> Widgets);

public record RelatorioLayoutResponse(List<string> Tabs);
public record UpdateRelatorioLayoutRequest(List<string> Tabs);

public record KpiResponse(
    decimal TotalVendidoHoje,
    decimal TotalVendidoMes,
    decimal LucroEstimadoMes,
    decimal ContasPagarVencidas,
    decimal ContasPagarProximas7Dias,
    decimal ContasReceberPendentes,
    int ProdutosEstoqueBaixo,
    decimal TotalReceitasMes,
    decimal TotalDespesasMes,
    // Expanded KPIs
    int QtyVendasMes,
    decimal TicketMedio,
    int TotalClientes,
    int ClientesNovosMes,
    decimal ValorEstoque,
    decimal Inadimplencia,
    decimal SaldoProjetado);

public record VendasDiaResponse(DateTime Data, decimal Total, int Quantity);
public record FluxoDiaResponse(DateTime Data, decimal Receitas, decimal Despesas);
public record TopProdutoResponse(string Name, decimal QuantidadeVendida, decimal TotalFaturado);
public record TopClienteResponse(string Name, decimal TotalGasto);
public record CategoriaDespesaDashResponse(string Category, decimal Total);

public record DashboardResponse(
    KpiResponse Kpis,
    List<VendasDiaResponse> VendasUltimos7Dias,
    List<FluxoDiaResponse> FluxoMes,
    List<TopProdutoResponse> TopProdutos,
    List<TopClienteResponse> TopClientes,
    List<CategoriaDespesaDashResponse> DespesasPorCategoria);

// Appointments estendido
public record AgendamentoStatusItem(string Status, int Count);
public record AgendamentoProfissionalItem(string Professional, int Total, int Concluidos);
public record AgendamentoDoDiaItem(string CustomerName, string Service, string StartTime, string Status);

// Contratos estendido
public record ContratoVencendoItem(string Title, string CustomerName, decimal Amount, DateOnly EndDate, int DiasRestantes);

// Cobranças estendido
public record CobrancaVencidaItem(string Reference, string CustomerName, decimal Amount, DateOnly DueDate, int DiasAtraso);
public record AgingFaixa(string Faixa, int Count, decimal Total);

// Orçamentos estendido
public record OrcamentoStatusItem(string Status, int Count, decimal Total);

// Assinaturas estendido
public record EvolucaoAssinaturaMes(string Mes, int Ativas, int Novas, int Canceladas);

// Module-specific dashboard KPIs
public record AgendamentosDashResponse(
    int Hoje,
    int ConfirmadosHoje,
    int CanceladosMes,
    decimal TaxaConclusaoMes,
    decimal TaxaOcupacao,
    List<AgendamentoStatusItem> PorStatus,
    List<AgendamentoProfissionalItem> PorProfissional,
    List<AgendamentoDoDiaItem> AgendaHoje);

public record ContratosDashResponse(
    int Ativos,
    decimal Mrr,
    int VencendoEm30,
    List<ContratoVencendoItem> ContratosVencendo);

public record CobrancasDashResponse(
    decimal TotalReceber,
    int VencidosCount,
    decimal TotalVencido,
    List<CobrancaVencidaItem> CobrancasVencidas,
    List<AgingFaixa> Aging);

public record OrcamentosDashResponse(
    int Abertos,
    decimal TaxaConversao,
    decimal ValorPipeline,
    List<OrcamentoStatusItem> PorStatus);

public record AssinaturasDashResponse(
    int Ativas,
    decimal Mrr,
    int CanceladasMes,
    int NovasMes,
    List<EvolucaoAssinaturaMes> Evolucao12Meses);

public record ModulosDashboardResponse(
    AgendamentosDashResponse Appointments,
    ContratosDashResponse Contratos,
    CobrancasDashResponse Charges,
    OrcamentosDashResponse Orcamentos,
    AssinaturasDashResponse Assinaturas);
