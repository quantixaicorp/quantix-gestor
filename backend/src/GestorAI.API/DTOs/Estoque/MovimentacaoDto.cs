namespace GestorAI.API.DTOs.Estoque;

public record MovimentacaoResponse(
    Guid Id,
    Guid ProdutoId,
    string ProdutoNome,
    string Tipo,
    decimal Quantidade,
    string Origem,
    DateTime DataHora,
    string? Observacao);
