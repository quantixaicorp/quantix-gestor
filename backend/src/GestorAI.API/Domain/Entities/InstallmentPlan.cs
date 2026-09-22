using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class InstallmentPlan : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? PurchaseId { get; set; }
    public required string Description { get; set; }
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public StatusParcelamento Status { get; set; } = StatusParcelamento.EmAberto;
    public Purchase? Purchase { get; set; }
    public ICollection<Transaction> Installments { get; set; } = [];
}
