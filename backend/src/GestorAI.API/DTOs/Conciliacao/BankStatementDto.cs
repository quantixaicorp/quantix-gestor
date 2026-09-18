using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Conciliacao;

public record BankStatementListItem(
    Guid Id,
    Guid BankAccountId,
    string BankAccountName,
    string FileName,
    string Format,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateTime ImportedAt,
    int ItemCount,
    int AutoConciliated,
    int PendingReview,
    int Unmatched,
    int ManuallyIgnored);

public record BankStatementItemResponse(
    Guid Id,
    DateOnly Date,
    decimal Amount,
    string Description,
    string? BankTransactionId,
    string Status,
    BankReconciliationResponse? Reconciliation);

public record BankReconciliationResponse(
    Guid Id,
    Guid TransactionId,
    string TransactionDescription,
    decimal TransactionAmount,
    DateTime TransactionDueDate,
    int ConfidenceScore,
    string MatchType,
    bool CreatedByImport);

public record ManualMatchRequest(Guid ItemId, Guid TransactionId);
