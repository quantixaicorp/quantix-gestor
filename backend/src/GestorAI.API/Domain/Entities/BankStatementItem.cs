using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;
public class BankStatementItem
{
    public Guid Id { get; set; }
    public Guid BankStatementId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    public string? BankTransactionId { get; set; }
    public BankStatementItemStatus Status { get; set; } = BankStatementItemStatus.Unmatched;
    public BankStatement? BankStatement { get; set; }
    public BankReconciliation? Reconciliation { get; set; }
}
