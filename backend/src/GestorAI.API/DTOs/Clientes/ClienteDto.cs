namespace GestorAI.API.DTOs.Clientes;

public record ClienteResponse(
    Guid Id,
    string Name,
    string WhatsApp,
    string? Email,
    string? Notes,
    DateTime CreatedAt,
    string? PersonType = null,
    string? CnpjCpf = null);

public record CreateClienteRequest(
    string Name,
    string WhatsApp,
    string? Email,
    string? Notes,
    string? PersonType = null,
    string? CnpjCpf = null);

public record UpdateClienteRequest(
    string Name,
    string WhatsApp,
    string? Email,
    string? Notes,
    string? PersonType = null,
    string? CnpjCpf = null);
