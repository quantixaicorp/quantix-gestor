namespace GestorAI.API.DTOs.Estoque;

public record MovimentacaoResponse(
    Guid Id,
    Guid ProductId,
    string ProdutoNome,
    string Type,
    decimal Quantity,
    string Source,
    DateTime MovementDate,
    string? Notes);
