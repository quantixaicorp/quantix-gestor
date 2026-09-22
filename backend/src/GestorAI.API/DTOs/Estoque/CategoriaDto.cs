namespace GestorAI.API.DTOs.Estoque;

public record CategoriaResponse(Guid Id, string Name);
public record CreateCategoriaRequest(string Name);
