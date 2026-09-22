namespace GestorAI.API.DTOs.Conciliacao;

public record CreateBankAccountRequest(
    string Name,
    string BankName,
    string? AccountNumber,
    string? Agency);

public record BankAccountResponse(
    Guid Id,
    string Name,
    string BankName,
    string? AccountNumber,
    string? Agency,
    bool IsActive);
