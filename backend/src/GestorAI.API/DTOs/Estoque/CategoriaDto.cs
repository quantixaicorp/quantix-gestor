namespace GestorAI.API.DTOs.Estoque;

public record CategoriaResponse(Guid Id, string Nome);
public record CreateCategoriaRequest(string Nome);
