using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class SubscriptionPlan : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string Niche { get; set; } = "Personalizado";
    public decimal Price { get; set; }
    public Periodicidade Frequency { get; set; }
    public bool IsActive { get; set; } = true;
    public bool BestSeller { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<SubscriptionPlanItem> Items { get; set; } = [];
    public ICollection<CustomerSubscription> Subscribers { get; set; } = [];
}
