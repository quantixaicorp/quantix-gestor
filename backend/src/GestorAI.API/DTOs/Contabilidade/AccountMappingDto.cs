namespace GestorAI.API.DTOs.Contabilidade;

public record AccountMappingItem(string CategoryName, Guid AccountId);
public record BulkUpsertAccountMappingsRequest(List<AccountMappingItem> Mappings);

public record AccountMappingResponse(
    Guid Id,
    string CategoryName,
    Guid AccountId,
    string AccountCode,
    string AccountName);
