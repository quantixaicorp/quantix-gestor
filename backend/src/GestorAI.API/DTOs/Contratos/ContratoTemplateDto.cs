namespace GestorAI.API.DTOs.Contratos;

public record ContratoTemplateItemRequest(
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario);

public record CreateContratoTemplateRequest(
    string Nome,
    string Objeto,
    string TipoCobranca,
    string Periodicidade,
    int DiaVencimento,
    decimal? ValorPadrao,
    List<ContratoTemplateItemRequest> Itens);

public record ContratoTemplateItemResponse(Guid Id, string Descricao, decimal Quantidade, decimal ValorUnitario);

public record ContratoTemplateResponse(
    Guid Id,
    string Nome,
    string Objeto,
    string TipoCobranca,
    string Periodicidade,
    int DiaVencimento,
    decimal? ValorPadrao,
    DateTime CriadoEm,
    List<ContratoTemplateItemResponse> Itens,
    decimal Total);

public record ContratoTemplateListItem(
    Guid Id,
    string Nome,
    string TipoCobranca,
    string Periodicidade,
    decimal? ValorPadrao,
    int QtdItens);
