using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Estoque;

public record ProdutoResponse(
    Guid Id,
    Guid CategoryId,
    string CategoriaNome,
    string Name,
    string? Description,
    decimal SalePrice,
    decimal AverageCost,
    decimal CurrentStock,
    decimal MinimumStock,
    string? Barcode,
    bool IsActive,
    bool EstoqueBaixo,
    int? DurationMinutes,
    TipoProduto Type);

public record CreateProdutoRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal SalePrice,
    decimal AverageCost,
    decimal CurrentStock,
    decimal MinimumStock,
    string? Barcode,
    TipoProduto Type = TipoProduto.Produto,
    int? DurationMinutes = null);

public record UpdateProdutoRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal SalePrice,
    decimal MinimumStock,
    string? Barcode,
    bool IsActive,
    int? DurationMinutes);

public record EntradaEstoqueRequest(
    Guid ProductId,
    decimal Quantity,
    decimal? CustoUnitario,
    string? Notes);
