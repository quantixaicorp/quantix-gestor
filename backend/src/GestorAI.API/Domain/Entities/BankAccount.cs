using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;
public class BankAccount : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? Agency { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<BankStatement> Statements { get; set; } = [];
}
