namespace GestorAI.API.DTOs.Assinaturas;

public record PlanoItemRequest(
    string Descricao,
    Guid? ServicoId,
    int QuantidadePorCiclo,
    string Tipo,
    decimal? PercentualDesconto);

public record CreatePlanoAssinaturaRequest(
    string Nome,
    string? Descricao,
    string Nicho,
    decimal Preco,
    string Periodicidade,
    bool MaisVendido,
    List<PlanoItemRequest> Itens);

public record UpdatePlanoAssinaturaRequest(
    string Nome,
    string? Descricao,
    string Nicho,
    decimal Preco,
    string Periodicidade,
    bool MaisVendido,
    bool Ativo,
    List<PlanoItemRequest> Itens);

public record PlanoItemResponse(
    Guid Id,
    string Descricao,
    Guid? ServicoId,
    int QuantidadePorCiclo,
    string Tipo,
    decimal? PercentualDesconto);

public record PlanoAssinaturaResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    string Nicho,
    decimal Preco,
    string Periodicidade,
    bool Ativo,
    bool MaisVendido,
    int TotalAssinantes,
    List<PlanoItemResponse> Itens,
    DateTime CriadoEm);

public record PlanoAssinaturaListItem(
    Guid Id,
    string Nome,
    string Nicho,
    decimal Preco,
    string Periodicidade,
    bool Ativo,
    bool MaisVendido,
    int TotalAssinantes);

public record NichoTemplateItemResponse(
    Guid Id,
    string Descricao,
    int QuantidadePorCiclo,
    string Tipo,
    decimal? PercentualDesconto);

public record NichoTemplateResponse(
    Guid Id,
    string Nicho,
    string NomePlano,
    string? Descricao,
    decimal PrecoSugerido,
    bool MaisVendido,
    string Periodicidade,
    List<NichoTemplateItemResponse> Itens);
