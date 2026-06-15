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

public record ComprasMensalSerieItem(string Mes, decimal Total, int Quantidade);

public record ComprasPorFornecedorItem(string Fornecedor, decimal Total);

public record TopProdutoCompradoItem(string Produto, decimal QuantidadeTotal, decimal ValorTotal);
