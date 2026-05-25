namespace GestorAI.API.DTOs.Dashboard;

public record KpiResponse(
    decimal TotalVendidoHoje,
    decimal TotalVendidoMes,
    decimal LucroEstimadoMes,
    decimal ContasPagarVencidas,
    decimal ContasPagarProximas7Dias,
    decimal ContasReceberPendentes,
    int ProdutosEstoqueBaixo);

public record VendasDiaResponse(DateTime Data, decimal Total, int Quantidade);

public record FluxoDiaResponse(DateTime Data, decimal Receitas, decimal Despesas);

public record TopProdutoResponse(string Nome, decimal QuantidadeVendida, decimal TotalFaturado);

public record DashboardResponse(
    KpiResponse Kpis,
    List<VendasDiaResponse> VendasUltimos7Dias,
    List<FluxoDiaResponse> FluxoMes,
    List<TopProdutoResponse> TopProdutos);
