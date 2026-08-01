using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Transaction : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public TipoLancamento Type { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public StatusLancamento Status { get; set; } = StatusLancamento.Pendente;
    public required string Category { get; set; }
    public Guid? SaleId { get; set; }
    public string? Notes { get; set; }
    public Sale? Sale { get; set; }
    public Guid? InstallmentPlanId { get; set; }
    public int? InstallmentNumber { get; set; }
    public InstallmentPlan? InstallmentPlan { get; set; }
}
