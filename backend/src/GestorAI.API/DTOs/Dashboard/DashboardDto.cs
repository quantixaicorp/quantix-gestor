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

public record VendasDiaResponse(DateTime Data, decimal Total, int Quantidade);
public record FluxoDiaResponse(DateTime Data, decimal Receitas, decimal Despesas);
public record TopProdutoResponse(string Nome, decimal QuantidadeVendida, decimal TotalFaturado);
public record TopClienteResponse(string Nome, decimal TotalGasto);
public record CategoriaDespesaDashResponse(string Categoria, decimal Total);

public record DashboardResponse(
    KpiResponse Kpis,
    List<VendasDiaResponse> VendasUltimos7Dias,
    List<FluxoDiaResponse> FluxoMes,
    List<TopProdutoResponse> TopProdutos,
    List<TopClienteResponse> TopClientes,
    List<CategoriaDespesaDashResponse> DespesasPorCategoria);

// Agendamentos estendido
public record AgendamentoStatusItem(string Status, int Count);
public record AgendamentoProfissionalItem(string Profissional, int Total, int Concluidos);
public record AgendamentoDoDiaItem(string ClienteNome, string Servico, string HoraInicio, string Status);

// Contratos estendido
public record ContratoVencendoItem(string Titulo, string ClienteNome, decimal Valor, DateOnly DataFim, int DiasRestantes);

// Cobranças estendido
public record CobrancaVencidaItem(string Referencia, string ClienteNome, decimal Valor, DateOnly DataVencimento, int DiasAtraso);
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
    AgendamentosDashResponse Agendamentos,
    ContratosDashResponse Contratos,
    CobrancasDashResponse Cobrancas,
    OrcamentosDashResponse Orcamentos,
    AssinaturasDashResponse Assinaturas);
