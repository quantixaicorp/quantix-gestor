using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Contabilidade;

public record CreateChartOfAccountRequest(
    string Code,
    string Name,
    AccountType Type,
    Guid? ParentId);

public record UpdateChartOfAccountRequest(
    string Code,
    string Name);

public record ChartOfAccountResponse(
    Guid Id,
    string Code,
    string Name,
    AccountType Type,
    Guid? ParentId,
    bool IsActive,
    List<ChartOfAccountResponse> Children);
