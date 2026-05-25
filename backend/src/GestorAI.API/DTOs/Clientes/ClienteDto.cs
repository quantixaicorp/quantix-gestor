namespace GestorAI.API.DTOs.Clientes;

public record ClienteResponse(
    Guid Id,
    string Nome,
    string Whatsapp,
    string? Email,
    string? Observacoes,
    DateTime DataCadastro);

public record CreateClienteRequest(
    string Nome,
    string Whatsapp,
    string? Email,
    string? Observacoes);

public record UpdateClienteRequest(
    string Nome,
    string Whatsapp,
    string? Email,
    string? Observacoes);
