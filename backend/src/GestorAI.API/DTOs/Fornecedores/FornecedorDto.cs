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
    DateTime DataCadastro);

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
    string? Observacoes);

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
    string? Observacoes);
