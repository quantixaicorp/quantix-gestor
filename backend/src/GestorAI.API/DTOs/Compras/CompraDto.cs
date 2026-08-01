namespace GestorAI.API.DTOs.Compras;

public record ItemCompraRequest(
    Guid? ProductId,
    string Description,
    string Destination,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal AllocatedFreight,
    decimal Taxes,
    string? FinancialCategory,
    string? CostCenter);

public record ItemCompraResponse(
    Guid Id,
    Guid? ProductId,
    string Description,
    string Destination,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal AllocatedFreight,
    decimal Taxes,
    decimal TotalAmount,
    string? FinancialCategory,
    string? CostCenter);

public record ParcelaPreviewRequest(
    int Number,
    DateTime DueDate,
    decimal Amount);

public record CreateCompraRequest(
    Guid SupplierId,
    DateTime Date,
    string? NoteNumber,
    string PurchaseType,
    Guid? PurchaseOrderId,
    string? Notes,
    List<ItemCompraRequest> Items,
    string PaymentTerms,
    string PaymentMethod,
    int? InstallmentCount,
    List<ParcelaPreviewRequest>? ParcelasPersonalizadas);

public record UpdateCompraRequest(
    Guid SupplierId,
    DateTime Date,
    string? NoteNumber,
    string PurchaseType,
    Guid? PurchaseOrderId,
    string? Notes,
    List<ItemCompraRequest> Items,
    string PaymentTerms,
    string PaymentMethod,
    int? InstallmentCount,
    List<ParcelaPreviewRequest>? ParcelasPersonalizadas);

public record CompraResponse(
    Guid Id,
    int Number,
    DateTime Date,
    Guid SupplierId,
    string FornecedorNome,
    Guid? PurchaseOrderId,
    string PurchaseType,
    string? NoteNumber,
    string PaymentTerms,
    string PaymentMethod,
    string Status,
    decimal TotalAmount,
    string? Notes,
    DateTime CreatedAt,
    List<ItemCompraResponse> Items,
    ParcelamentoResumoResponse? InstallmentPlan);

public record ParcelamentoResumoResponse(
    Guid Id,
    string Description,
    decimal TotalAmount,
    int InstallmentCount,
    string Status);

public record CompraResumoResponse(
    decimal TotalCompraMes,
    int QtdComprasMes,
    decimal TotalContasPagarGeradas);
