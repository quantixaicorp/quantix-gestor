namespace GestorAI.API.DTOs.Compras;

public record ComprasDashboardResponse(
    decimal TotalMes,
    decimal TotalAno,
    decimal TicketMedio,
    int QtdComprasMes,
    int FornecedoresAtivos,
    List<ComprasMensalSerieItem> SeriesMensal,
    List<ComprasPorFornecedorItem> PorFornecedor,
    List<TopProdutoCompradoItem> TopProdutos);

public record ComprasMensalSerieItem(string Mes, decimal Total, int Quantity);

public record ComprasPorFornecedorItem(string Supplier, decimal Total);

public record TopProdutoCompradoItem(string Product, decimal QuantidadeTotal, decimal TotalAmount);
