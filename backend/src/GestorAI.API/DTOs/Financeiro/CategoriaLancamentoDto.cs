namespace GestorAI.API.DTOs.Financeiro;

public record CategoriaLancamentoResponse(Guid Id, string Name, string Type);
public record CreateCategoriaLancamentoRequest(string Name, string Type);
public record UpdateCategoriaLancamentoRequest(string Name);
