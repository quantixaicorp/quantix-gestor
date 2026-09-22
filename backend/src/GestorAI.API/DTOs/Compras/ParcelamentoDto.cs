namespace GestorAI.API.DTOs.Compras;

public record ParcelaResponse(
    Guid Id,
    int InstallmentNumber,
    decimal Amount,
    DateTime DueDate,
    DateTime? PaymentDate,
    string Status,
    bool Vencido);

public record ParcelamentoDetalheResponse(
    Guid Id,
    Guid? PurchaseId,
    string Description,
    decimal TotalAmount,
    int InstallmentCount,
    string Status,
    string Category,
    List<ParcelaResponse> Installments);
