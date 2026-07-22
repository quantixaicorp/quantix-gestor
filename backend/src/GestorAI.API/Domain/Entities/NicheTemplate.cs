using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class NicheTemplate
{
    public Guid Id { get; set; }
    public required string Niche { get; set; }
    public required string PlanName { get; set; }
    public string? Description { get; set; }
    public decimal SuggestedPrice { get; set; }
    public bool BestSeller { get; set; }
    public Periodicidade Frequency { get; set; }
    public ICollection<NicheTemplateItem> Items { get; set; } = [];
}

public class NicheTemplateItem
{
    public Guid Id { get; set; }
    public Guid NicheTemplateId { get; set; }
    public required string Description { get; set; }
    public int QuantityPerCycle { get; set; }
    public TipoItemPlano Type { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public NicheTemplate? Template { get; set; }
}
