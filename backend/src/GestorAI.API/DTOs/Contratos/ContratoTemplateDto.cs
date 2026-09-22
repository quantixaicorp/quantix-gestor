namespace GestorAI.API.DTOs.Contratos;

public record ContratoTemplateItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public record CreateContratoTemplateRequest(
    string Name,
    string Subject,
    string ChargeType,
    string Frequency,
    int DueDay,
    decimal? DefaultAmount,
    List<ContratoTemplateItemRequest> Items);

public record ContratoTemplateItemResponse(Guid Id, string Description, decimal Quantity, decimal UnitPrice);

public record ContratoTemplateResponse(
    Guid Id,
    string Name,
    string Subject,
    string ChargeType,
    string Frequency,
    int DueDay,
    decimal? DefaultAmount,
    DateTime CreatedAt,
    List<ContratoTemplateItemResponse> Items,
    decimal Total);

public record ContratoTemplateListItem(
    Guid Id,
    string Name,
    string ChargeType,
    string Frequency,
    decimal? DefaultAmount,
    int QtdItens);
