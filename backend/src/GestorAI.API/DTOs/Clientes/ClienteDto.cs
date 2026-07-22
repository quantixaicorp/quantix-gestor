namespace GestorAI.API.DTOs.Clientes;

public record ClienteResponse(
    Guid Id,
    string Name,
    string WhatsApp,
    string? Email,
    string? Notes,
    DateTime CreatedAt);

public record CreateClienteRequest(
    string Name,
    string WhatsApp,
    string? Email,
    string? Notes);

public record UpdateClienteRequest(
    string Name,
    string WhatsApp,
    string? Email,
    string? Notes);
