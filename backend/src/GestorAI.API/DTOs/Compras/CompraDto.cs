namespace GestorAI.API.DTOs.Compras;

public record ItemCompraRequest(
    Guid? ProdutoId,
    string Descricao,
    string DestinoCompra,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal Desconto,
    decimal FreteRateado,
    decimal Impostos,
    string? CategoriaFinanceira,
    string? CentroCusto);

public record ItemCompraResponse(
    Guid Id,
    Guid? ProdutoId,
    string Descricao,
    string DestinoCompra,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal Desconto,
    decimal FreteRateado,
    decimal Impostos,
    decimal ValorTotal,
    string? CategoriaFinanceira,
    string? CentroCusto);

public record ParcelaPreviewRequest(
    int Numero,
    DateTime DataVencimento,
    decimal Valor);

public record CreateCompraRequest(
    Guid FornecedorId,
    DateTime Data,
    string? NumeroNota,
    string TipoCompra,
    Guid? PedidoCompraId,
    string? Observacoes,
    List<ItemCompraRequest> Itens,
    string CondicaoPagamento,
    string FormaPagamento,
    int? QtdParcelas,
    List<ParcelaPreviewRequest>? ParcelasPersonalizadas);

public record UpdateCompraRequest(
    Guid FornecedorId,
    DateTime Data,
    string? NumeroNota,
    string TipoCompra,
    Guid? PedidoCompraId,
    string? Observacoes,
    List<ItemCompraRequest> Itens,
    string CondicaoPagamento,
    string FormaPagamento,
    int? QtdParcelas,
    List<ParcelaPreviewRequest>? ParcelasPersonalizadas);

public record CompraResponse(
    Guid Id,
    int Numero,
    DateTime Data,
    Guid FornecedorId,
    string FornecedorNome,
    Guid? PedidoCompraId,
    string TipoCompra,
    string? NumeroNota,
    string CondicaoPagamento,
    string FormaPagamento,
    string Status,
    decimal ValorTotal,
    string? Observacoes,
    DateTime CriadaEm,
    List<ItemCompraResponse> Itens,
    ParcelamentoResumoResponse? Parcelamento);

public record ParcelamentoResumoResponse(
    Guid Id,
    string Descricao,
    decimal ValorTotal,
    int QtdParcelas,
    string Status);

public record CompraResumoResponse(
    decimal TotalCompraMes,
    int QtdComprasMes,
    decimal TotalContasPagarGeradas);
