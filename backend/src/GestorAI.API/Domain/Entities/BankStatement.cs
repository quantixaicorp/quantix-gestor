using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;
public class BankStatement : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public required string FileName { get; set; }
    public BankStatementFormat Format { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public int ItemCount { get; set; }
    public BankAccount? BankAccount { get; set; }
    public ICollection<BankStatementItem> Items { get; set; } = [];
}
