namespace GestorAI.API.DTOs.Fornecedores;

public record FornecedorResponse(
    Guid Id,
    string Name,
    string? CnpjCpf,
    string? Phone,
    string? Email,
    string? Logradouro,
    string? City,
    string? Uf,
    string? Cep,
    string? ContactPerson,
    string? Notes,
    DateTime CreatedAt,
    string? RazaoSocial = null,
    string? NomeFantasia = null,
    string? InscricaoEstadual = null,
    string? WhatsApp = null,
    string Status = "IsActive");

public record CreateFornecedorRequest(
    string Name,
    string? CnpjCpf,
    string? Phone,
    string? Email,
    string? Logradouro,
    string? City,
    string? Uf,
    string? Cep,
    string? ContactPerson,
    string? Notes,
    string? RazaoSocial = null,
    string? NomeFantasia = null,
    string? InscricaoEstadual = null,
    string? WhatsApp = null);

public record UpdateFornecedorRequest(
    string Name,
    string? CnpjCpf,
    string? Phone,
    string? Email,
    string? Logradouro,
    string? City,
    string? Uf,
    string? Cep,
    string? ContactPerson,
    string? Notes,
    string? RazaoSocial = null,
    string? NomeFantasia = null,
    string? InscricaoEstadual = null,
    string? WhatsApp = null,
    string Status = "IsActive");
