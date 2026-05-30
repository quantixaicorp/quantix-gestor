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
