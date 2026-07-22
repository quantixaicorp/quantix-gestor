namespace GestorAI.API.DTOs.Compras;

public record ItemPedidoRequest(
    Guid? ProductId,
    string Description,
    decimal Quantity,
    decimal EstimatedAmount);

public record ItemPedidoResponse(
    Guid Id,
    Guid? ProductId,
    string Description,
    decimal Quantity,
    decimal EstimatedAmount);

public record CreatePedidoCompraRequest(
    Guid SupplierId,
    DateTime Date,
    string? Notes,
    List<ItemPedidoRequest> Items);

public record UpdatePedidoCompraRequest(
    Guid SupplierId,
    DateTime Date,
    string? Notes,
    List<ItemPedidoRequest> Items);

public record PedidoCompraResponse(
    Guid Id,
    int Number,
    DateTime Date,
    Guid SupplierId,
    string FornecedorNome,
    string Status,
    decimal EstimatedAmount,
    string? Notes,
    DateTime CreatedAt,
    List<ItemPedidoResponse> Items);
