using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;
public class BankReconciliation
{
    public Guid Id { get; set; }
    public Guid BankStatementItemId { get; set; }
    public Guid TransactionId { get; set; }
    public int ConfidenceScore { get; set; }
    public BankMatchType MatchType { get; set; }
    public bool CreatedByImport { get; set; }
    public DateTime ReconciledAt { get; set; } = DateTime.UtcNow;
    public BankStatementItem? BankStatementItem { get; set; }
    public Transaction? Transaction { get; set; }
}
