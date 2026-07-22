namespace GestorAI.API.DTOs.Vendas;

public record ItemVendaRequest(Guid ProductId, decimal Quantity, decimal Discount);

public record CreateVendaRequest(
    Guid? CustomerId,
    List<ItemVendaRequest> Items,
    decimal Discount,
    string PaymentMethod,
    int? Installments,
    string? Notes,
    DateTime? SaleDate = null,
    Guid? ProfessionalId = null,
    string? ServiceOrderNotes = null);

public record ItemVendaResponse(
    Guid ProductId,
    string ProdutoNome,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Total);

public record VendaResponse(
    Guid Id,
    Guid? CustomerId,
    string? CustomerName,
    DateTime SaleDate,
    string Status,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    string PaymentMethod,
    int? Installments,
    string? Notes,
    List<ItemVendaResponse> Items,
    string? ProfessionalName = null,
    string? ServiceOrderNotes = null);

public record VendaListItem(
    Guid Id,
    Guid? CustomerId,
    string? CustomerName,
    DateTime SaleDate,
    string Status,
    decimal Total,
    string PaymentMethod,
    string? ProfessionalName = null);

public record FecharVendaRequest(string PaymentMethod, int? Installments, string? Notes);

public record UpdateVendaRequest(
    Guid? CustomerId,
    string PaymentMethod,
    DateTime SaleDate);
