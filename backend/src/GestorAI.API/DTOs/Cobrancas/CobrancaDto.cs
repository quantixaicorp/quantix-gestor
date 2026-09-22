namespace GestorAI.API.DTOs.Cobrancas;

public record CreateCobrancaRequest(
    Guid CustomerId,
    string Reference,
    decimal Amount,
    DateOnly DueDate,
    string? Notes);

public record PagarCobrancaRequest(
    DateTime PaymentDate,
    string PaymentMethod);

public record CobrancaResponse(
    Guid Id,
    string CustomerName,
    string ClienteWhatsapp,
    Guid? ContractId,
    string? ContratoTitulo,
    string Reference,
    decimal Amount,
    DateOnly DueDate,
    DateTime? PaymentDate,
    string Status,
    string? PaymentMethod,
    string? Notes,
    DateTime CreatedAt);

public record CobrancaListItem(
    Guid Id,
    string CustomerName,
    Guid? ContractId,
    string? ContratoTitulo,
    string Reference,
    decimal Amount,
    DateOnly DueDate,
    string Status);

public record WhatsappUrlResponse(string Url);

public record AgingResponse(
    decimal Atual,
    decimal Ate30Dias,
    decimal De31A60Dias,
    decimal De61A90Dias,
    decimal Acima90Dias,
    decimal Total,
    int QtdAtual,
    int QtdAte30Dias,
    int QtdDe31A60Dias,
    int QtdDe61A90Dias,
    int QtdAcima90Dias);

public record EnviarAsaasRequest(string BillingType);

public record CobrancaAsaasResponse(
    string AsaasId,
    string? PaymentLink,
    string? PixQrCode,
    string? BoletoUrl);

public record CobrancaResumo(
    decimal TotalAReceber,
    decimal TotalVencido,
    decimal TotalRecebidoNoMes);
