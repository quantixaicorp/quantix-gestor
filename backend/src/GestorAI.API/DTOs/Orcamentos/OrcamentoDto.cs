// backend/src/GestorAI.API/DTOs/Orcamentos/OrcamentoDto.cs
namespace GestorAI.API.DTOs.Orcamentos;

public record OrcamentoItemRequest(
    string Type,
    Guid? ProductId,
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public record CreateOrcamentoRequest(
    Guid? CustomerId,
    string Title,
    DateTime ExpirationDate,
    string? Notes,
    List<OrcamentoItemRequest> Items);

public record OrcamentoItemResponse(
    Guid Id,
    string Type,
    Guid? ProductId,
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public record OrcamentoResponse(
    Guid Id,
    int Number,
    string Title,
    Guid? CustomerId,
    string? CustomerName,
    string? ClienteWhatsapp,
    DateTime ExpirationDate,
    string Status,
    string? Notes,
    Guid? SaleId,
    Guid? PublicToken,
    DateTime CreatedAt,
    List<OrcamentoItemResponse> Items,
    decimal Total);

public record OrcamentoListItem(
    Guid Id,
    int Number,
    string Title,
    string? CustomerName,
    DateTime ExpirationDate,
    string Status,
    decimal Total);

public record OrcamentoPublicoResponse(
    string Title,
    string? CustomerName,
    DateTime ExpirationDate,
    string Status,
    string? Notes,
    List<OrcamentoItemPublicoResponse> Items,
    decimal Total);

public record OrcamentoItemPublicoResponse(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total);

public record GerarCobrancaOrcamentoRequest(DateOnly DueDate);
