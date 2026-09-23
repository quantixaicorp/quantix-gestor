using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Contabilidade;

public record UpdateAccountingSettingsRequest(
    Guid? DefaultCashAccountId,
    AccountingSystem? PreferredAccountingSystem);

public record AccountingSettingsResponse(
    Guid? DefaultCashAccountId,
    string? DefaultCashAccountCode,
    string? DefaultCashAccountName,
    AccountingSystem? PreferredAccountingSystem);
