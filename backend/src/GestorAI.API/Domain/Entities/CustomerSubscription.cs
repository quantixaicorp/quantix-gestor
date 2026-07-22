using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class CustomerSubscription : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public Guid ContractId { get; set; }
    public AssinaturaStatus Status { get; set; } = AssinaturaStatus.Ativa;
    public DateOnly StartDate { get; set; }
    public DateOnly RenewalDate { get; set; }
    public int CurrentCycle { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Customer? Customer { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public Contract? Contract { get; set; }
}
