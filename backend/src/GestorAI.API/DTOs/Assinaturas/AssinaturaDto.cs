namespace GestorAI.API.DTOs.Assinaturas;

public record AssinarRequest(string Name, string WhatsApp, string? Email);

public record AssinarResponse(
    Guid AssinaturaId,
    Guid ContractId,
    Guid ChargeId,
    string? PixQrCode,
    string? BoletoUrl,
    decimal Amount,
    DateOnly Vencimento);

public record AssinaturaListItem(
    Guid Id,
    string CustomerName,
    string PlanNome,
    string Status,
    DateOnly RenewalDate,
    int CurrentCycle);

public record AssinaturaResponse(
    Guid Id,
    string CustomerName,
    string ClienteWhatsapp,
    Guid PlanoId,
    string PlanoNome,
    decimal PlanPreco,
    string Status,
    DateOnly StartDate,
    DateOnly RenewalDate,
    int CurrentCycle,
    Guid ContractId);
