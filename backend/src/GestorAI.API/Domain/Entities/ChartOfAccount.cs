using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class ChartOfAccount : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public AccountType Type { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
    public ChartOfAccount? Parent { get; set; }
    public List<ChartOfAccount> Children { get; set; } = [];
}
