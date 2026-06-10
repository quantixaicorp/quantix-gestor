namespace GestorAI.API.DTOs.Financeiro;

public record CategoriaLancamentoResponse(Guid Id, string Nome, string Tipo);
public record CreateCategoriaLancamentoRequest(string Nome, string Tipo);
public record UpdateCategoriaLancamentoRequest(string Nome);
