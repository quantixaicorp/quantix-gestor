namespace GestorAI.API.DTOs.Fiscal;

public record NotaFiscalItemResponse(
    Guid Id,
    string NomeProduto,
    string? Ncm,
    string? Cfop,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal Total);

public record NotaFiscalResponse(
    Guid Id,
    Guid VendaId,
    string Modelo,
    int? Numero,
    int? Serie,
    string Status,
    string? ChaveAcesso,
    string? Protocolo,
    string? XmlUrl,
    string? PdfUrl,
    string? MensagemErro,
    DateTime? AutorizadaEm,
    DateTime? CanceladaEm,
    DateTime CriadaEm,
    NotaFiscalItemResponse[] Itens);

public record EmitirNotaFiscalRequest(Guid VendaId, string Tipo);

public record CancelarNotaFiscalRequest(string Motivo);
