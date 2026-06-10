namespace GestorAI.API.DTOs.Contratos;

public record ContratoItemRequest(
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario);

public record CreateContratoRequest(
    Guid ClienteId,
    string Titulo,
    string Objeto,
    string TipoCobranca,
    decimal Valor,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string Periodicidade,
    int DiaVencimento,
    string? Observacao,
    List<ContratoItemRequest> Itens);

public record UpdateContratoRequest(
    string? Observacao,
    List<ContratoItemRequest>? Itens);

public record GerarCobrancasRequest(
    DateOnly De,
    DateOnly Ate);

public record ContratoItemResponse(
    Guid Id,
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario);

public record ContratoResponse(
    Guid Id,
    int Numero,
    string ClienteNome,
    string ClienteWhatsapp,
    string Titulo,
    string Objeto,
    string TipoCobranca,
    decimal Valor,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string Periodicidade,
    int DiaVencimento,
    string Status,
    string? Observacao,
    DateTime CriadoEm,
    List<ContratoItemResponse> Itens,
    decimal Total,
    string? ClickSignStatus,
    string? ClickSignViewerUrl);

public record EnviarAssinaturaRequest(string EmailSignatario);

public record EnviarAssinaturaResponse(string DocKey, string ViewerUrl, string Status);

public record ContratoListItem(
    Guid Id,
    int Numero,
    string ClienteNome,
    string Titulo,
    string TipoCobranca,
    decimal Valor,
    string Status,
    DateOnly DataInicio,
    DateOnly? DataFim);

public record ContratoVencendoItem(
    Guid Id,
    int Numero,
    string ClienteNome,
    string Titulo,
    DateOnly DataFim,
    decimal Valor);
