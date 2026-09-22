using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class SubscriptionPlanItem
{
    public Guid Id { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public required string Description { get; set; }
    public Guid? ServiceId { get; set; }
    public int QuantityPerCycle { get; set; } = 1;
    public TipoItemPlano Type { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public SubscriptionPlan? Plan { get; set; }
}
