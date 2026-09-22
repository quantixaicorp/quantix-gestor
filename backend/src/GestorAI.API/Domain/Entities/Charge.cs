using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Charge : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? ContractId { get; set; }
    public required string Reference { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public CobrancaStatus Status { get; set; } = CobrancaStatus.Pendente;
    public FormaPagamento? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public string? AsaasId { get; set; }
    public string? AsaasPaymentLink { get; set; }
    public string? AsaasPixQrCode { get; set; }
    public string? AsaasBoletoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Customer? Customer { get; set; }
    public Contract? Contract { get; set; }
}
