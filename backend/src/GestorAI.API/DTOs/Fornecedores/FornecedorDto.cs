namespace GestorAI.API.DTOs.Fornecedores;

public record FornecedorResponse(
    Guid Id,
    string Nome,
    string? CnpjCpf,
    string? Telefone,
    string? Email,
    string? Logradouro,
    string? Cidade,
    string? Uf,
    string? Cep,
    string? Contato,
    string? Observacoes,
    DateTime DataCadastro,
    string? RazaoSocial = null,
    string? NomeFantasia = null,
    string? InscricaoEstadual = null,
    string? Whatsapp = null,
    string Status = "Ativo");

public record CreateFornecedorRequest(
    string Nome,
    string? CnpjCpf,
    string? Telefone,
    string? Email,
    string? Logradouro,
    string? Cidade,
    string? Uf,
    string? Cep,
    string? Contato,
    string? Observacoes,
    string? RazaoSocial = null,
    string? NomeFantasia = null,
    string? InscricaoEstadual = null,
    string? Whatsapp = null);

public record UpdateFornecedorRequest(
    string Nome,
    string? CnpjCpf,
    string? Telefone,
    string? Email,
    string? Logradouro,
    string? Cidade,
    string? Uf,
    string? Cep,
    string? Contato,
    string? Observacoes,
    string? RazaoSocial = null,
    string? NomeFantasia = null,
    string? InscricaoEstadual = null,
    string? Whatsapp = null,
    string Status = "Ativo");
