using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Contabilidade;

public record AccountingExportRequest(
    AccountingSystem System,
    List<string> Months,  // formato "yyyy-MM", ex: ["2025-01", "2025-02"]
    bool IncludePending);

public record AccountingEntry(
    DateOnly Date,
    string Description,
    string DebitCode,
    string CreditCode,
    decimal Amount);
