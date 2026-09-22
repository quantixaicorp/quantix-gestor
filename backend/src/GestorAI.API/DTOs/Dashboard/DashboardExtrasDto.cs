namespace GestorAI.API.DTOs.Dashboard;

// Vendas extras
public record UltimaVendaResponse(Guid Id, DateTime SaleDate, string CustomerName, decimal Total, string PaymentMethod);
public record VendaFormaPgtoResponse(string PaymentMethod, int Quantity, decimal Total);

// Financeiro extras
public record ReceitaCategoriaResponse(string Category, decimal Total);
public record FluxoMensalResponse(string Mes, decimal Receitas, decimal Despesas, decimal Saldo);
public record ContaVencidaDetalheResponse(Guid Id, string Description, string Category, decimal Amount, DateTime DueDate, int DiasAtraso);
public record ProximoVencimentoResponse(Guid Id, string Description, string Category, decimal Amount, DateTime DueDate, int DiasParaVencer);

// Estoque extras
public record EstoqueCategoriaResponse(string Category, int Quantity, decimal Amount);
public record EstoqueBaixoDetalheResponse(string Name, decimal CurrentStock, decimal MinimumStock, decimal SalePrice);

// Resposta agregada
public record DashboardExtrasResponse(
    decimal MaiorVendaDia,
    List<UltimaVendaResponse> UltimasVendas,
    List<VendaFormaPgtoResponse> VendasPorFormaPgto,
    List<ReceitaCategoriaResponse> ReceitasPorCategoria,
    List<FluxoMensalResponse> FluxoAnual,
    List<ContaVencidaDetalheResponse> ContasVencidas,
    List<ProximoVencimentoResponse> ProximosVencimentos,
    int ProdutosAtivos,
    List<EstoqueCategoriaResponse> DistribuicaoCategorias,
    List<EstoqueBaixoDetalheResponse> EstoqueBaixo,
    int ClientesInativos
);
