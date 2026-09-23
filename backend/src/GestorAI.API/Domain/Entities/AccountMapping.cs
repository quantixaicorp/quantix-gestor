namespace GestorAI.API.Domain.Entities;

public class AccountMapping : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string CategoryName { get; set; }
    public Guid AccountId { get; set; }
    public ChartOfAccount? Account { get; set; }
}
