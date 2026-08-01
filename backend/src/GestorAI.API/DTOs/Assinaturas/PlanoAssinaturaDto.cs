namespace GestorAI.API.DTOs.Assinaturas;

public record PlanoItemRequest(
    string Description,
    Guid? ServiceId,
    int QuantityPerCycle,
    string Type,
    decimal? DiscountPercentage);

public record CreatePlanoAssinaturaRequest(
    string Name,
    string? Description,
    string Niche,
    decimal Price,
    string Frequency,
    bool BestSeller,
    List<PlanoItemRequest> Items);

public record UpdatePlanoAssinaturaRequest(
    string Name,
    string? Description,
    string Niche,
    decimal Price,
    string Frequency,
    bool BestSeller,
    bool IsActive,
    List<PlanoItemRequest> Items);

public record PlanoItemResponse(
    Guid Id,
    string Description,
    Guid? ServiceId,
    int QuantityPerCycle,
    string Type,
    decimal? DiscountPercentage);

public record PlanoAssinaturaResponse(
    Guid Id,
    string Name,
    string? Description,
    string Niche,
    decimal Price,
    string Frequency,
    bool IsActive,
    bool BestSeller,
    int TotalAssinantes,
    List<PlanoItemResponse> Items,
    DateTime CreatedAt);

public record PlanoAssinaturaListItem(
    Guid Id,
    string Name,
    string Niche,
    decimal Price,
    string Frequency,
    bool IsActive,
    bool BestSeller,
    int TotalAssinantes);

public record NichoTemplateItemResponse(
    Guid Id,
    string Description,
    int QuantityPerCycle,
    string Type,
    decimal? DiscountPercentage);

public record NichoTemplateResponse(
    Guid Id,
    string Niche,
    string PlanName,
    string? Description,
    decimal SuggestedPrice,
    bool BestSeller,
    string Frequency,
    List<NichoTemplateItemResponse> Items);
