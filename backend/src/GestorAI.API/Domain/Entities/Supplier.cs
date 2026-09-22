using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Supplier : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? CnpjCpf { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Logradouro { get; set; }
    public string? City { get; set; }
    public string? Uf { get; set; }
    public string? Cep { get; set; }
    public string? ContactPerson { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? WhatsApp { get; set; }
    public StatusFornecedor Status { get; set; } = StatusFornecedor.Ativo;
}
