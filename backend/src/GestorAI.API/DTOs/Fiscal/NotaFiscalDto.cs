namespace GestorAI.API.DTOs.Fiscal;

public record NotaFiscalItemResponse(
    Guid Id,
    string ProductName,
    string? Ncm,
    string? Cfop,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total);

public record NotaFiscalResponse(
    Guid Id,
    Guid SaleId,
    string Model,
    int? Number,
    int? Series,
    string Status,
    string? AccessKey,
    string? Protocol,
    string? XmlUrl,
    string? PdfUrl,
    string? ErrorMessage,
    DateTime? AuthorizedAt,
    DateTime? CanceledAt,
    DateTime CreatedAt,
    NotaFiscalItemResponse[] Items);

public record EmitirNotaFiscalRequest(Guid SaleId, string Type);

public record CancelarNotaFiscalRequest(string Reason);
