using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Fornecedor : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public string? CnpjCpf { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Logradouro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Cep { get; set; }
    public string? Contato { get; set; }
    public string? Observacoes { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Whatsapp { get; set; }
    public StatusFornecedor Status { get; set; } = StatusFornecedor.Ativo;
}
