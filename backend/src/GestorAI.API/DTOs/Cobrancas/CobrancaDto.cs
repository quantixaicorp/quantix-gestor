namespace GestorAI.API.DTOs.Cobrancas;

public record CreateCobrancaRequest(
    Guid ClienteId,
    string Referencia,
    decimal Valor,
    DateOnly DataVencimento,
    string? Observacao);

public record PagarCobrancaRequest(
    DateTime DataPagamento,
    string FormaPagamento);

public record CobrancaResponse(
    Guid Id,
    string ClienteNome,
    string ClienteWhatsapp,
    Guid? ContratoId,
    string? ContratoTitulo,
    string Referencia,
    decimal Valor,
    DateOnly DataVencimento,
    DateTime? DataPagamento,
    string Status,
    string? FormaPagamento,
    string? Observacao,
    DateTime CriadoEm);

public record CobrancaListItem(
    Guid Id,
    string ClienteNome,
    Guid? ContratoId,
    string? ContratoTitulo,
    string Referencia,
    decimal Valor,
    DateOnly DataVencimento,
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
