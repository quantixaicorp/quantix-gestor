namespace GestorAI.API.DTOs.Assinaturas;

public record AssinarRequest(string Nome, string Whatsapp, string? Email);

public record AssinarResponse(
    Guid AssinaturaId,
    Guid ContratoId,
    Guid CobrancaId,
    string? PixQrCode,
    string? BoletoUrl,
    decimal Valor,
    DateOnly Vencimento);

public record AssinaturaListItem(
    Guid Id,
    string ClienteNome,
    string PlanNome,
    string Status,
    DateOnly DataRenovacao,
    int CicloAtual);

public record AssinaturaResponse(
    Guid Id,
    string ClienteNome,
    string ClienteWhatsapp,
    Guid PlanoId,
    string PlanoNome,
    decimal PlanPreco,
    string Status,
    DateOnly DataInicio,
    DateOnly DataRenovacao,
    int CicloAtual,
    Guid ContratoId);
