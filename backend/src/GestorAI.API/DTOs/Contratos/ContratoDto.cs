namespace GestorAI.API.DTOs.Contratos;

public record ContratoItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public record CreateContratoRequest(
    Guid CustomerId,
    string Title,
    string Subject,
    string ChargeType,
    decimal Amount,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Frequency,
    int DueDay,
    string? Notes,
    List<ContratoItemRequest> Items);

public record UpdateContratoRequest(
    string? Notes,
    List<ContratoItemRequest>? Items);

public record GerarCobrancasRequest(
    DateOnly De,
    DateOnly Ate);

public record ContratoItemResponse(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public record ContratoResponse(
    Guid Id,
    int Number,
    string CustomerName,
    string ClienteWhatsapp,
    string Title,
    string Subject,
    string ChargeType,
    decimal Amount,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Frequency,
    int DueDay,
    string Status,
    string? Notes,
    DateTime CreatedAt,
    List<ContratoItemResponse> Items,
    decimal Total,
    string? ClickSignStatus,
    string? ClickSignViewerUrl);

public record EnviarAssinaturaRequest(string EmailSignatario);

public record EnviarAssinaturaResponse(string DocKey, string ViewerUrl, string Status);

public record ContratoListItem(
    Guid Id,
    int Number,
    string CustomerName,
    string Title,
    string ChargeType,
    decimal Amount,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate);

public record ContratoVencendoItem(
    Guid Id,
    int Number,
    string CustomerName,
    string Title,
    DateOnly EndDate,
    decimal Amount);
